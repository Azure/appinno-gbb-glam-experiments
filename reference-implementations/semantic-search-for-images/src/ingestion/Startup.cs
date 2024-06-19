using System.Text;
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
                            .AddJsonFile("appsettings.json", optional: true)
                            .AddEnvironmentVariables()
                            .Build();

        var appSettings = Configuration.GetSection("AppSettings").Get<AppSettings>();

        ValidateAppSettings(appSettings!);
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
                    BackoffType = DelayBackoffType.Linear
                
                })
                .AddTimeout(TimeSpan.FromSeconds(10));
        });
    }

    private void ValidateAppSettings(AppSettings appSettings) {
        if (appSettings == null)
            throw new Exception("AppSettings not found via appsettings.json or environment variables.");

        var settingsMissing = false;
        var sb = new StringBuilder();
        sb.AppendLine("The following app settings were not found in appsettings.json or an environment variable and are missing:");

        if (string.IsNullOrEmpty(appSettings.StorageAccountUri) ||
            string.IsNullOrEmpty(appSettings.StorageAccountImageCsvContainerName) ||
            string.IsNullOrEmpty(appSettings.StorageAccountProcessedContainerName) ||
            string.IsNullOrEmpty(appSettings.CosmosDbUri) ||
            string.IsNullOrEmpty(appSettings.CosmosDbName) ||
            string.IsNullOrEmpty(appSettings.CosmosDbImageVectorPath) ||
            string.IsNullOrEmpty(appSettings.CosmosDbImageMetadataContainerName) ||
            string.IsNullOrEmpty(appSettings.CosmosDbPartitionkey) ||
            string.IsNullOrEmpty(appSettings.AiServicesUri) ||
            string.IsNullOrEmpty(appSettings.AiServicesApiVersion) ||
            string.IsNullOrEmpty(appSettings.AiServicesModelVersion))
        {
            settingsMissing = true;
        }

        if (string.IsNullOrEmpty(appSettings.StorageAccountUri))
            sb.AppendLine("StorageAccountUri not found.");
        if (string.IsNullOrEmpty(appSettings.StorageAccountImageCsvContainerName))
            sb.AppendLine("StorageAccountImageCsvContainerName not found.");
        if (string.IsNullOrEmpty(appSettings.StorageAccountProcessedContainerName))
            sb.AppendLine("StorageAccountProcessedContainerName not found.");
        if (string.IsNullOrEmpty(appSettings.CosmosDbUri))
            sb.AppendLine("CosmosDbUri not found.");
        if (string.IsNullOrEmpty(appSettings.CosmosDbName))
            sb.AppendLine("CosmosDbName not found.");
        if (string.IsNullOrEmpty(appSettings.CosmosDbImageVectorPath))
            sb.AppendLine("CosmosDbImageVectorPath not found.");
        if (string.IsNullOrEmpty(appSettings.CosmosDbImageMetadataContainerName))
            sb.AppendLine("CosmosDbImageMetadataContainerName not found.");
        if (string.IsNullOrEmpty(appSettings.CosmosDbPartitionkey))
            sb.AppendLine("CosmosDbPartitionkey not found.");
        if (string.IsNullOrEmpty(appSettings.AiServicesUri))
            sb.AppendLine("AiServicesUri not found.");
        if (string.IsNullOrEmpty(appSettings.AiServicesApiVersion))
            sb.AppendLine("AiServicesApiVersion not found.");
        if (string.IsNullOrEmpty(appSettings.AiServicesModelVersion))
            sb.AppendLine("AiServicesModelVersion not found.");
        
        if(settingsMissing)
            throw new Exception(sb.ToString());
    }
}