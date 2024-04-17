using System.Net;
using System.Net.Http.Json;
using System.Text;
using Indexer.Models;
using Polly;

namespace Indexer.Services;

public class ImageVectorizer
{
    private readonly HttpClient _httpClient = new();
    private readonly Settings _settings;

    public ImageVectorizer(Settings settings)
    {
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", settings.AzureAiVisionKey);
        _settings = settings;
    }

    public async Task<IEnumerable<float>> VectorizeImage(string imageUrl)
    {
        var requestContent = new StringContent(
            $"{{ 'url': '{imageUrl}' }}",
            Encoding.UTF8,
            "application/json"
        );

        var executeWithRetryAfter = await Policy
            .HandleResult<HttpResponseMessage>(res => res.StatusCode == HttpStatusCode.TooManyRequests
                && res.Headers.RetryAfter != null)
            .WaitAndRetryAsync(
                retryCount: 3, 
                sleepDurationProvider: (_, result, _) => result.Result.Headers.RetryAfter!.Delta!.Value,
                onRetryAsync: async (_, _, _, _) => await Task.CompletedTask)
            .ExecuteAndCaptureAsync(async () => 
            {
                return await _httpClient.PostAsync(
                    $"{_settings.AzureAiVisionEndpoint}computervision/retrieval:vectorizeImage?api-version={_settings.AzureAiVisionEmbeddingsApiVersion}&model-version={_settings.AzureAiVisionEmbeddingsModelVersion}",
                    requestContent);
            });

        using HttpResponseMessage response = executeWithRetryAfter.Result;

        response.EnsureSuccessStatusCode();        

        var vectorizeImageResponse = await response.Content.ReadFromJsonAsync<VectorizeImageResponse>() 
            ?? throw new Exception("Response could not be serialized as expected.");

        return vectorizeImageResponse.Vector;
    }
}