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
        services.AddSingleton<TokenCredential>(credential);
        services.AddHttpClient(Constants.NAMED_HTTP_CLIENT_GENERAL).AddStandardResilienceHandler();
        services.AddHttpClient(Constants.NAMED_HTTP_CLIENT_AI_SERVICES, c =>
        {
            c.BaseAddress = new Uri(appSettings.AiServices.Uri);   
        })
        .AddStandardResilienceHandler();
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
            string.IsNullOrEmpty(appSettings.AiServices.Uri) ||
            string.IsNullOrEmpty(appSettings.AiServices.ApiVersion) ||
            string.IsNullOrEmpty(appSettings.AiServices.ModelVersion))
        {
            settingsMissing = true;
        }

        if(string.IsNullOrEmpty(appSettings.DatabaseTargeted))
        {
            settingsMissing = true;
            sb.AppendLine("DatabaseTargeted not found. Allowed values are 'CosmosDb' or 'AiSearch'.");
        }
        else if(!appSettings.DatabaseTargeted.Equals(Constants.DATABASE_TARGETED_COSMOSDB) && !appSettings.DatabaseTargeted.Equals(Constants.DATABASE_TARGETED_AI_SEARCH))
        {
            settingsMissing = true;
            sb.AppendLine($"DatabaseTargeted value is '{appSettings.DatabaseTargeted}' which is invalid. Allowed values are 'CosmosDb' or 'AiSearch'.");
        }            

        if (appSettings.DatabaseTargeted is not null && appSettings.DatabaseTargeted.Equals(Constants.DATABASE_TARGETED_COSMOSDB)) {
            var cosmosSettingsMissing = false;

            if(appSettings.CosmosDb is null) {
                cosmosSettingsMissing = true;
            }
            else if (string.IsNullOrEmpty(appSettings.CosmosDb.Uri) ||
                string.IsNullOrEmpty(appSettings.CosmosDb.Database) ||
                string.IsNullOrEmpty(appSettings.CosmosDb.ImageVectorPath) ||
                string.IsNullOrEmpty(appSettings.CosmosDb.ImageMetadataContainer) ||
                string.IsNullOrEmpty(appSettings.CosmosDb.PartitionKey) ||
                appSettings.CosmosDb.RUs <= 0) {

                cosmosSettingsMissing = true;
            }

            if(cosmosSettingsMissing) {
                settingsMissing = true;
                sb.AppendLine("CosmosDb is the targeted database, however, some settings were not found. Please ensure that the following settings are present in appsettings.json or environment variables:");
                sb.AppendLine("- CosmosDb.Uri");
                sb.AppendLine("- CosmosDb.Database");
                sb.AppendLine("- CosmosDb.ImageVectorPath");
                sb.AppendLine("- CosmosDb.ImageMetadataContainer");
                sb.AppendLine("- CosmosDb.PartitionKey");
                sb.AppendLine("- CosmosDb.RUs");
            }
        }

        if (appSettings.DatabaseTargeted is not null && appSettings.DatabaseTargeted.Equals(Constants.DATABASE_TARGETED_AI_SEARCH)) {
            var aiSearchSettingsMissing = false;

            if(appSettings.AiSearch is null) {
                aiSearchSettingsMissing = true;
            }
            else if (string.IsNullOrEmpty(appSettings.AiSearch.Uri) ||
                string.IsNullOrEmpty(appSettings.AiSearch.Index) ||
                string.IsNullOrEmpty(appSettings.AiSearch.VectorSearchProfile) ||
                string.IsNullOrEmpty(appSettings.AiSearch.VectorSearchHnswConfig) ||
                appSettings.AiSearch.VectorSearchDimensions <= 0) {
                
                aiSearchSettingsMissing = true;
            }

            if(aiSearchSettingsMissing) {
                settingsMissing = true;
                sb.AppendLine("AiSearch is the targeted database, however, some settings were not found. Please ensure that the following settings are present in appsettings.json or environment variables:");
                sb.AppendLine("- AiSearch.Uri");
                sb.AppendLine("- AiSearch.Index");
                sb.AppendLine("- AiSearch.VectorSearchProfile");
                sb.AppendLine("- AiSearch.VectorSearchHnswConfig");
                sb.AppendLine("- AiSearch.VectorSearchDimensions");
            }
        }

        if (string.IsNullOrEmpty(appSettings.StorageAccount.Uri))
            sb.AppendLine("StorageAccount.Uri not found.");
        if (string.IsNullOrEmpty(appSettings.StorageAccount.ImageCsvContainer))
            sb.AppendLine("StorageAccount.ImageCsvContainer not found.");
        if (string.IsNullOrEmpty(appSettings.StorageAccount.ProcessedContainer))
            sb.AppendLine("StorageAccount.ProcessedContainer not found.");
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