using Azure;
using Azure.Search.Documents;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SemanticSearchWithImages;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddOptions<AiVisionOptions>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection("AiVisionOptions").Bind(settings);
        });
        services.AddOptions<AiSearchOptions>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection("AiSearchOptions").Bind(settings);
        });
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient(
            Constants.AI_VISION_HTTP_CLIENT_NAME,
            configureClient: httpClient => 
            {
                var provider = services.BuildServiceProvider();
                var optionsProvider = provider.GetRequiredService<IOptions<AiVisionOptions>>();
                var visionOptions = optionsProvider.Value;
                httpClient.BaseAddress = new Uri(visionOptions.Endpoint);
                httpClient.DefaultRequestHeaders.Add(Constants.AI_VISION_KEY_REQUEST_HEADER_NAME, visionOptions.Key);
            });
        services.AddSingleton(provider => 
        {
            var optionsProvider = provider.GetRequiredService<IOptions<AiSearchOptions>>();
            var searchOptions = optionsProvider.Value;
            var searchCredential = new AzureKeyCredential(searchOptions.Key);
            var searchEndpoint = new Uri(searchOptions.Endpoint);
            return new SearchClient(searchEndpoint, searchOptions.IndexName, searchCredential);
        });
    })
    .Build();

host.Run();
