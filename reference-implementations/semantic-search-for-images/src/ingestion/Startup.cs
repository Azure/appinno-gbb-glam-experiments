using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using ingestion;
using ingestion.Models;
using ingestion.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

public class Startup
{
    IConfiguration Configuration { get; }

    public Startup()
    {
        Configuration = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false)
                            .Build();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var appSettings = Configuration.GetSection("AppSettings").Get<AppSettings>();
        var credential = new ChainedTokenCredential(new AzureCliCredential(), new ManagedIdentityCredential());

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(appSettings!);
        services.AddSingleton<IDataIngestor, DataIngestor>();
        services.AddTransient<IDatabaseService, CosmosDbService>();
        services.AddSingleton(new BlobServiceClient(new Uri(appSettings!.StorageAccountUri), new DefaultAzureCredential()));
        services.AddTransient<IBlobService, BlobService>();
        services.AddTransient<ICsvService, CsvService>();
        services.AddTransient<IImageService, ImageService>();
        services.AddHttpClient();
        services.AddSingleton<TokenCredential>(credential);
        services.AddHttpClient(Constants.NAMED_HTTP_CLIENT_AI_SERVICES, c =>
        {
            c.BaseAddress = new Uri(appSettings.AiServicesUri);   
        });
        services.AddResiliencePipeline(Constants.NAMED_RESILIENCE_PIPELINE, builder =>
        {
            builder
                .AddRetry(new RetryStrategyOptions{
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Constant
                
                })
                .AddTimeout(TimeSpan.FromSeconds(10));
        });
    }
}