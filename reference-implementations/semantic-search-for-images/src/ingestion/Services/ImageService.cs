using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using ingestion.Models;
using Microsoft.Extensions.Logging;

namespace ingestion.Services
{
    /// <summary>
    /// Service for handling image related operations.
    /// </summary>
    public class ImageService : IImageService
    {
        private ILogger<ImageService> _logger;
        private AppSettings _appSettings;
        private HttpClient _httpClient;
        private HttpClient _aiServicesHttpClient;
        private TokenCredential _tokenCredential;

        /// <summary>
        /// Initializes a new instance of the ImageService class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        public ImageService(ILoggerFactory loggerFactory, AppSettings appSettings, IHttpClientFactory httpClientFactory, TokenCredential tokenCredential)
        {
            _logger = loggerFactory.CreateLogger<ImageService>();
            _appSettings = appSettings;
            _httpClient = httpClientFactory.CreateClient(Constants.NAMED_HTTP_CLIENT_GENERAL);
            _aiServicesHttpClient = httpClientFactory.CreateClient(Constants.NAMED_HTTP_CLIENT_AI_SERVICES);
            _tokenCredential = tokenCredential;
        }
        
        /// <summary>
        /// Downloads an image from a given URL.
        /// </summary>
        /// <param name="imageUrl">The URL of the image to download.</param>
        /// <returns>A stream containing the downloaded image.</returns>
        public async Task<Stream> DownloadImage(string imageUrl) {
            CheckForNullImageUrl(imageUrl);

            _logger.LogInformation($"Downloading image from: {imageUrl}");

            var imageResponse = await _httpClient.GetAsync(imageUrl);
            imageResponse.EnsureSuccessStatusCode();

            _logger.LogInformation($"Downloaded image from: {imageUrl}");

            return await imageResponse.Content.ReadAsStreamAsync();
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

            return await GenerateImageEmbeddings(requestContent);
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

            return await GenerateImageEmbeddings(requestContent);
        }

        /// <summary>
        /// Generates embeddings for an image.
        /// </summary>
        /// <param name="requestContent">Http request content representing either an image stream or a json payload specifying the url of the image.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<float []> GenerateImageEmbeddings(HttpContent requestContent) {
            // Obtain a token from the token credential before making the call to the Vision endpoint so that appropriate token refresh can take place, if necessary.
            var tokenResult = await _tokenCredential.GetTokenAsync(new TokenRequestContext(["https://cognitiveservices.azure.com/"]), CancellationToken.None);

            _aiServicesHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
            
            using HttpResponseMessage response = await _aiServicesHttpClient.PostAsync(
                $"{_appSettings.AiServices.Uri}/computervision/retrieval:vectorizeImage?api-version={_appSettings.AiServices.ApiVersion}&model-version={_appSettings.AiServices.ModelVersion}",
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
    }
}