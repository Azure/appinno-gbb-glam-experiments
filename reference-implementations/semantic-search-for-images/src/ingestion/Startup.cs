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
        var credential = new ChainedTokenCredential(new AzureCliCredential(), new ManagedIdentityCredential(clientId: Configuration["AZURE_CLIENT_ID"]));

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(appSettings!);
        services.AddSingleton<IDataIngestor, DataIngestor>();

        if(appSettings!.DatabaseTargeted == Constants.DATABASE_TARGETED_COSMOSDB)
            services.AddTransient<IDatabaseService, CosmosDbService>();
        else if(appSettings!.DatabaseTargeted == Constants.DATABASE_TARGETED_AI_SEARCH)
            services.AddTransient<IDatabaseService, AiSearchService>();

        services.AddSingleton(new BlobServiceClient(new Uri(appSettings!.StorageAccount.Uri), new DefaultAzureCredential()));
        services.AddTransient<IBlobService, BlobService>();
        services.AddTransient<ICsvService, CsvService>();
        services.AddTransient<IImageService, ImageService>();
        services.AddHttpClient();
        services.AddSingleton<TokenCredential>(credential);
        services.AddHttpClient(Constants.NAMED_HTTP_CLIENT_AI_SERVICES, c =>
        {
            c.BaseAddress = new Uri(appSettings.AiServices.Uri);   
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

        if (string.IsNullOrEmpty(appSettings.StorageAccount.Uri) ||
            string.IsNullOrEmpty(appSettings.StorageAccount.ImageCsvContainer) ||
            string.IsNullOrEmpty(appSettings.StorageAccount.ProcessedContainer) ||
            string.IsNullOrEmpty(appSettings.DatabaseTargeted) ||
            string.IsNullOrEmpty(appSettings.CosmosDb.Uri) ||
            string.IsNullOrEmpty(appSettings.CosmosDb.Database) ||
            string.IsNullOrEmpty(appSettings.CosmosDb.ImageVectorPath) ||
            string.IsNullOrEmpty(appSettings.CosmosDb.ImageMetadataContainer) ||
            string.IsNullOrEmpty(appSettings.CosmosDb.PartitionKey) ||
            string.IsNullOrEmpty(appSettings.AiServices.Uri) ||
            string.IsNullOrEmpty(appSettings.AiServices.ApiVersion) ||
            string.IsNullOrEmpty(appSettings.AiServices.ModelVersion))
        {
            settingsMissing = true;
        }
        
        if(!appSettings.DatabaseTargeted.Equals(Constants.DATABASE_TARGETED_COSMOSDB) && !appSettings.DatabaseTargeted.Equals(Constants.DATABASE_TARGETED_AI_SEARCH))
        {
            settingsMissing = true;
            sb.AppendLine($"DatabaseTargeted value is '{appSettings.DatabaseTargeted}' which is invalid. Allowed values are 'CosmosDb' or 'AiSearch'.");
        }            

        if (string.IsNullOrEmpty(appSettings.StorageAccount.Uri))
            sb.AppendLine("StorageAccount.Uri not found.");
        if (string.IsNullOrEmpty(appSettings.StorageAccount.ImageCsvContainer))
            sb.AppendLine("StorageAccount.ImageCsvContainer not found.");
        if (string.IsNullOrEmpty(appSettings.StorageAccount.ProcessedContainer))
            sb.AppendLine("StorageAccount.ProcessedContainer not found.");
        if (string.IsNullOrEmpty(appSettings.DatabaseTargeted))
            sb.AppendLine("DatabaseTargeted not found. Allowed values are 'CosmosDb' or 'AiSearch'.");
        if (string.IsNullOrEmpty(appSettings.CosmosDb.Uri))
            sb.AppendLine("CosmosDb.Uri not found.");
        if (string.IsNullOrEmpty(appSettings.CosmosDb.Database))
            sb.AppendLine("CosmosDb.Database not found.");
        if (string.IsNullOrEmpty(appSettings.CosmosDb.ImageVectorPath))
            sb.AppendLine("CosmosDb.ImageVectorPath not found.");
        if (string.IsNullOrEmpty(appSettings.CosmosDb.ImageMetadataContainer))
            sb.AppendLine("CosmosDb.ImageMetadataContainer not found.");
        if (string.IsNullOrEmpty(appSettings.CosmosDb.PartitionKey))
            sb.AppendLine("CosmosDb.Partitionkey not found.");
        if (string.IsNullOrEmpty(appSettings.AiServices.Uri))
            sb.AppendLine("AiServices.Uri not found.");
        if (string.IsNullOrEmpty(appSettings.AiServices.ApiVersion))
            sb.AppendLine("AiServices.ApiVersion not found.");
        if (string.IsNullOrEmpty(appSettings.AiServices.ModelVersion))
            sb.AppendLine("AiServices.ModelVersion not found.");
        
        if(settingsMissing)
            throw new Exception(sb.ToString());
    }
}