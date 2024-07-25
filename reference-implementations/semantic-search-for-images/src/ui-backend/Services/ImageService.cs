using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Polly;
using ui_backend.Models;

namespace ui_backend.Services
{
    /// <summary>
    /// Service for handling image related operations.
    /// </summary>
    public class ImageService : IImageService
    {
        private ILogger<ImageService> _logger;
        private AppSettings _appSettings;
        private HttpClient _aiServicesHttpClient;
        private TokenCredential _tokenCredential;
        private const string EMBEDDING_TYPE_IMAGE = "vectorizeImage";
        private const string EMBEDDING_TYPE_TEXT = "vectorizeText";

        /// <summary>
        /// Initializes a new instance of the ImageService class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="resiliencePipeline">The Polly.Net resilience pipeline.</param>
        public ImageService(ILogger<ImageService> logger, AppSettings appSettings, IHttpClientFactory httpClientFactory, TokenCredential tokenCredential)
        {
            _logger = logger;
            _appSettings = appSettings;
            _aiServicesHttpClient = httpClientFactory.CreateClient(Constants.NAMED_HTTP_CLIENT);
            _tokenCredential = tokenCredential;
        }

        /// <summary>
        /// Generates embeddings for an image.
        /// </summary>
        /// <param name="imageStream">A stream containing the image data.</param>
        /// <returns>An array of floats representing the image embeddings.</returns>
        public async Task<float[]> GenerateImageEmbeddings(Stream imageStream) {
            var requestContent = new StreamContent(imageStream);
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            requestContent.Headers.ContentLength = imageStream.Length;

            return await GenerateImageEmbeddings(requestContent, EMBEDDING_TYPE_IMAGE);
        }

        /// <summary>
        /// Generates embeddings for an image.
        /// </summary>
        /// <param name="imageUrl">Url of the image.</param>
        /// <returns>An array of floats representing the image embeddings.</returns>
        public async Task<float[]> GenerateImageEmbeddings(string imageUrl) {
            CheckForNullImageUrl(imageUrl);
            
            var jsonPayload = JsonSerializer.Serialize(new { url = imageUrl });
            var requestContent = new StringContent(jsonPayload, new MediaTypeHeaderValue("application/json"));

            return await GenerateImageEmbeddings(requestContent, EMBEDDING_TYPE_IMAGE);
        }

        /// <summary>
        /// Generates embeddings for an image.
        /// </summary>
        /// <param name="requestContent">Http request content representing either an image stream or a json payload specifying the url of the image.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<float []> GenerateImageEmbeddings(HttpContent requestContent, string embeddingType) {
            // Obtain a token from the token credential before making the call to the Vision endpoint so that appropriate token refresh can take place, if necessary.
            var tokenResult = await _tokenCredential.GetTokenAsync(new TokenRequestContext(["https://cognitiveservices.azure.com/"]), CancellationToken.None);

            _aiServicesHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
            
            using HttpResponseMessage response = await _aiServicesHttpClient.PostAsync(
                $"{_appSettings.AiServices.Uri}/computervision/retrieval:{embeddingType}?api-version={_appSettings.AiServices.ApiVersion}&model-version={_appSettings.AiServices.ModelVersion}",
                requestContent);

            if(!response.IsSuccessStatusCode) {
                if(requestContent is StreamContent streamContent)
                    throw new Exception($"Error generating embeddings for stream content. Status code: {response.StatusCode}");
                else if(requestContent is StringContent stringContent)
                    throw new Exception($"Error generating embeddings for {await stringContent.ReadAsStringAsync()}. Status code: {response.StatusCode}; Reason: {response.ReasonPhrase}");
                else
                    throw new Exception($"Error generating embeddings. Status code: {response.StatusCode}");
            }

            var embeddingsResponse = await response.Content.ReadFromJsonAsync<MultimodalEmbeddingsApiResponse>()
                ?? throw new Exception("Response not received or could not be serialized as expected.");

            return embeddingsResponse.Vector;
        }

        private void CheckForNullImageUrl(string imageUrl) {
            if(string.IsNullOrEmpty(imageUrl)) throw new ArgumentNullException(nameof(imageUrl));
        }

        private void CheckForNullText(string text) {
            if(string.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));
        }

        public async Task<float[]> GenerateTextEmbeddings(string text)
        {
            CheckForNullText(text);

            var jsonPayload = JsonSerializer.Serialize(new { text = text });
            var requestContent = new StringContent(jsonPayload, new MediaTypeHeaderValue("application/json"));

            return await GenerateImageEmbeddings(requestContent, EMBEDDING_TYPE_TEXT);
        }
    }
}