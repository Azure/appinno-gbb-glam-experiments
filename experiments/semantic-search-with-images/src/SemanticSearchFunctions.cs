using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticSearchWithImages.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text;

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
    public async Task<HttpResponseData> RunSearchByImageUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [FromBody] ByImageRequest byImageRequest)
    {
        _logger.LogInformation($"Triggered SearchByImageUrl for URL {byImageRequest.Url}");

        float[] queryEmbeddings = await GenerateEmbeddingsFromImage(byImageRequest.Url);
        return await Search(req, queryEmbeddings);
    }

    [Function("SearchByImageStream")]
    public async Task<HttpResponseData> RunSearchByImageStream(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Triggered SearchByImageStream");

        float[] queryEmbeddings = await GenerateEmbeddingsFromImage(req.Body);
        return await Search(req, queryEmbeddings);
    }

    [Function("SearchByText")]
    public async Task<HttpResponseData> RunSearchByText(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [FromBody] ByTextRequest byTextRequest)
    {
        _logger.LogInformation("Triggered SearchByText");

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

    private async Task<float[]> GenerateEmbeddingsFromImage(Stream imageStream)
    {
        var requestContent = new StreamContent(imageStream);
        requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
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

    private async Task<HttpResponseData> Search(HttpRequestData req, float[] queryEmbeddings)
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
            Size = 3, // Number to return (ranked by search score)
        };
        SearchResults<SearchDocument> searchResults = await _searchClient.SearchAsync<SearchDocument>(searchText: null, searchOptions);

        var imageResults = new List<ImageSearchResult>();
        await foreach (SearchResult<SearchDocument> result in searchResults.GetResultsAsync())
        {
            imageResults.Add(new(
                ObjectId: $"{result.Document["ObjectID"]}",
                Title: $"{result.Document["Title"]}",
                Attribution: $"{result.Document["Attribution"]}",
                DisplayDate: $"{result.Document["DisplayDate"]}",
                LocationDescription: $"{result.Document["LocationDescription"]}",
                IiifThumbUrl: $"{result.Document["IiifThumbUrl"]}",
                SearchScore: result.Score));
        }
        SimilarImagesResult similarImagesResult = new(SimilarImages: [.. imageResults]);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(similarImagesResult);
        return response;
    }
}
