using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticSearchWithImages.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SemanticSearchWithImages;

public class SemanticSearchFunctions(
    ILoggerFactory loggerFactory, 
    IOptions<AiVisionOptions> visionOptions,
    IOptions<AiSearchOptions> searchOptions,
    IHttpClientFactory httpClientFactory, 
    SearchClient searchClient)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<SemanticSearchFunctions>();
    private readonly AiVisionOptions _visionOptions = visionOptions.Value;
    private readonly AiSearchOptions _searchOptions = searchOptions.Value;
    private readonly HttpClient _aiVisionClient = httpClientFactory.CreateClient(Constants.AI_VISION_HTTP_CLIENT_NAME);
    private readonly SearchClient _searchClient = searchClient;

    [Function("SearchByImageUrl")]
    public async Task<IActionResult> RunSearchByImageUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        var byImageRequest = await JsonSerializer.DeserializeAsync<ByImageRequest>(req.Body);
        if (byImageRequest is null)
            return new BadRequestObjectResult("Body could not be deserialized. Expected a JSON body with a single 'url' property.");

        _logger.LogInformation($"Triggered SearchByImageUrl for URL {byImageRequest.Url}");

        float[] queryEmbeddings = await GenerateEmbeddingsFromImage(byImageRequest.Url);
        return await Search(req, queryEmbeddings);
    }

    [Function("SearchByImageStream")]
    public async Task<IActionResult> RunSearchByImageStream(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("Triggered SearchByImageStream");

        if (req.ContentType == null)
            return new BadRequestObjectResult("Request Content-Type must be set. If streaming the image as the body, use 'application/octet-stream'. If providing the image as form data, use 'multipart/form-data'");
        if (req.ContentLength == 0)
            return new BadRequestObjectResult("Request has no content.");

        float[] queryEmbeddings;

        if (req.ContentType.Contains("application/octet-stream", StringComparison.InvariantCultureIgnoreCase)
            || req.ContentType.Contains("image/png", StringComparison.InvariantCultureIgnoreCase)
            || req.ContentType.Contains("image/jpeg", StringComparison.InvariantCultureIgnoreCase)
            || req.ContentType.Contains("image/gif", StringComparison.InvariantCultureIgnoreCase))
        {
            var sanitizedContentType = req.ContentType.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
            _logger.LogInformation($"Content-Type: {sanitizedContentType}. Passing request body directly to generate embeddings.");
            // Assume the body is an image stream and directly pass along...
            queryEmbeddings = await GenerateEmbeddingsFromImage(req.Body, req.ContentLength);
        }
        else if (req.ContentType.Contains("multipart/form-data", StringComparison.InvariantCultureIgnoreCase))
        {
            if (req.Form.Files.Count == 0)
                return new BadRequestObjectResult("Request form data does not include a file.");
            if (req.Form.Files.Count > 1)
                return new BadRequestObjectResult("Request form data includes more than one file; only one may be accepted.");

            _logger.LogInformation("Content-Type: multipart/form-data. Passing request form file read stream to generate embeddings.");

            var file = req.Form.Files[0];
            queryEmbeddings = await GenerateEmbeddingsFromImage(file.OpenReadStream(), null);
        }
        else
        {
            return new BadRequestObjectResult("Request could not be handled. Expects either image as form data (multipart/form-data) or direct body stream (appliation/octet-stream).");
        }

        return await Search(req, queryEmbeddings);
    }

    [Function("SearchByText")]
    public async Task<IActionResult> RunSearchByText(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("Triggered SearchByText");

        var byTextRequest = await JsonSerializer.DeserializeAsync<ByTextRequest>(req.Body);
        if (byTextRequest is null)
            return new BadRequestObjectResult("Body could not be deserialized. Expected a JSON body with a single 'text' property.");

        float[] queryEmbeddings = await GenerateEmbeddingsFromText(byTextRequest.Text);
        return await Search(req, queryEmbeddings);
    }

    private async Task<float[]> GenerateEmbeddingsFromImage(string imageUrl)
    {
        var requestContent = new StringContent(
            $"{{ 'url': '{imageUrl}' }}",
            Encoding.UTF8,
            "application/json"
        );
        return await CallMultimodalEmbeddingsApi(requestContent, "vectorizeImage");
    }

    private async Task<float[]> GenerateEmbeddingsFromImage(Stream imageStream, long? contentLength)
    {
        var requestContent = new StreamContent(imageStream);
        requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        if (contentLength != null && requestContent.Headers.ContentLength == null)
            requestContent.Headers.ContentLength = contentLength;
        
        return await CallMultimodalEmbeddingsApi(requestContent, "vectorizeImage");
    }

    private async Task<float[]> GenerateEmbeddingsFromText(string text)
    {
        var requestContent = new StringContent(
            $"{{ 'text': '{text}' }}",
            Encoding.UTF8,
            "application/json"
        );
        return await CallMultimodalEmbeddingsApi(requestContent, "vectorizeText");
    }

    private async Task<float[]> CallMultimodalEmbeddingsApi(HttpContent requestContent, string operation)
    {
        using HttpResponseMessage response = await _aiVisionClient.PostAsync(
            $"computervision/retrieval:{operation}?api-version={_visionOptions.MultimodalEmbeddingsApiVersion}&model-version={_visionOptions.MultimodalEmbeddingsModelVersion}",
            requestContent);

        response.EnsureSuccessStatusCode();

        var vectorizeImageResponse = await response.Content.ReadFromJsonAsync<MultimodalEmbeddingsApiResponse>()
            ?? throw new Exception("Response not received or could not be serialized as expected.");
        
        return vectorizeImageResponse.Vector;
    }

    private async Task<IActionResult> Search(HttpRequest req, float[] queryEmbeddings)
    {
        SearchOptions searchOptions = new()
        {
            VectorSearch = new()
            {
                Queries =
                {
                    new VectorizedQuery(queryEmbeddings) 
                    { 
                        KNearestNeighborsCount = 3, 
                        Fields = 
                        { 
                            _searchOptions.IndexImageVectorsFieldName
                        } 
                    }
                }
            },
            Size = _searchOptions.TopNCount, // Number to return (ranked by search score)
        };
        SearchResults<SearchDocument> searchResults = await _searchClient.SearchAsync<SearchDocument>(searchText: null, searchOptions);

        var imageResults = new List<ImageSearchResult>();
        await foreach (SearchResult<SearchDocument> result in searchResults.GetResultsAsync())
        {
            imageResults.Add(new(
                ObjectId: $"{result.Document["ObjectID"]}",
                AccessionNum: $"{result.Document["AccessionNum"]}",
                Title: $"{result.Document["Title"]}",
                Attribution: $"{result.Document["Attribution"]}",
                DisplayDate: $"{result.Document["DisplayDate"]}",
                LocationDescription: $"{result.Document["LocationDescription"]}",
                Medium: $"{result.Document["Medium"]}",
                Dimensions: $"{result.Document["Dimensions"]}",
                ImageUrl: $"{result.Document["ImageUrl"]}",
                SearchScore: result.Score));
        }
        SimilarImagesResult similarImagesResult = new(SimilarImages: [.. imageResults]);

        return new OkObjectResult(similarImagesResult);
    }
}
