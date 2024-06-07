using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Indexer.Models;
using Indexer.Services;

namespace Indexer;

class Program
{
    private static IConfiguration? _config;
    private static Settings? _settings;

    static async Task Main(string[] args)
    {
        // ///////////////////
        // Setup services
        // ///////////////////

        // Configure the run
        _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        _settings = _config.GetRequiredSection("Settings").Get<Settings>()!;

        // Validate settings
        if (_settings is null)
            throw new ArgumentException("Settings required. Check your appsettings.json exists.");
        if (string.IsNullOrEmpty(_settings.AzureAiVisionKey) || string.IsNullOrEmpty(_settings.AzureAiVisionEndpoint))
            throw new ArgumentException("Azure AI Vision settings missing. Check your appsettings.json for 'AzureAiVisionKey' and 'AzureAiVisionEndpoint' settings.");
        if (_settings.ExternalSourceType.Equals(ExternalSourceType.NGA) && string.IsNullOrEmpty(_settings.ExternalSourceConnectionInfo))
            throw new ArgumentException("Assuming NGA source and database connection string not provided. Check your appsettings.json for 'ExternalSourceConnectionInfo' setting.");
        if (string.IsNullOrEmpty(_settings.AzureAiSearchKey) || string.IsNullOrEmpty(_settings.AzureAiSearchEndpoint))
            throw new ArgumentException("Azure AI Search settings missing. Check your appsettings.json for 'AzureAiSearchKey' and 'AzureAiSearchEndpoint' settings.");
        if (string.IsNullOrEmpty(_settings.AzureAiSearchIndexName))
            _settings.AzureAiSearchIndexName = "gallerydata-v";

        // Setup services
        IndexManager indexManager = new(_settings);
        BaseRetriever retriever = _settings.ExternalSourceType switch
        {
            ExternalSourceType.MET => new MetOpenAccessRetriever(_settings),
            ExternalSourceType.NGA => new NGAOpenDataRetriever(_settings),
            _ => throw new ArgumentException("External Source Type not recognized."),
        };
        ImageVectorizer vectorizor = new(_settings);

        // ///////////////////
        // (Optionally) Cache responses from the Met API
        // ///////////////////

        // The Met Open Access API requires a separate call per object ID. This
        // can be time consuming, and there may be extra data of interest that
        // wouldn't be in the search index but to which you may want to refer.
        // Uncommenting this next block of code will capture all responses from
        // the API into a Cosmos DB container. This also makes retrieving the
        // records much faster on subsequent runs if necessary for indexing.
        /*
        Console.WriteLine("PREPROCESSING... SAVING MET DATA TO COSMOS DB");
        MetOpenAccessPreProcessor processor = new(_settings);
        await processor.LoadAllRecordsIntoCosmosNoSql();
        return;
        */

        Console.WriteLine("Application configured. Creating index...");

        // ///////////////////
        // Create the index
        // ///////////////////

        // Ensure Index is ready
        await indexManager.Create(_settings.DropAndRecreateIndexIfItExists);

        Console.WriteLine($"Index created. Getting records from {_settings.ExternalSourceType}...");

        // ///////////////////
        // Populate the index
        // ///////////////////

        // NOTE: This process takes time! It takes roughly three hours for the
        // ~116,119 records that were in NGA's Open Data program data set when this
        // was last run. It will pull all records into memory, generate vectors for
        // all objects in memory, and bulk upload in batches after all vectors have
        // been generated for the entire data set. There is a minimal impact on memory.

        // Get data from external source
        var records = await retriever.GetAllRecords();

        Console.WriteLine("Records retrieved. Enriching with embeddings...");

        ConcurrentBag<IndexDocument> indexDocuments = [];

        // Create IndexDocuments for all records with url that returned vectors
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = -1 };
        await Parallel.ForEachAsync(records, parallelOptions, async (record, cancellationToken) =>
        {
            try
            {
                record.VectorizedImage = await vectorizor.VectorizeImage(record.ImageUrl);

                indexDocuments.Add(record);
            }
            catch (Exception e)
            {
                // 429s (Throttling) are handled in the ImageVectorizer with a built-in retry-after
                // policy; however, we occassionally see 400s (Bad Request) when object IDs no longer
                // resolve to an IIIF URL. It's a small subset, so we just print the error and skip.
                // If critical, those objects would need to be managed so that they could be
                // corrected and re-processed. Just skipping, we usually skip ~100.
                //Console.WriteLine($"ERROR: {e.Message}\n{e.StackTrace}");
                Console.WriteLine($"Unexpected issue getting vector data for object ({e.Message}). \n\tSkipping: [ ID = {record.ObjectID}, Title = {record.Title}, Url = {record.ImageUrl} ]");
            }
        });

        Console.WriteLine($"Index documents created and enriched for {indexDocuments.Count} records. Bulk inserting in batches of 1000...");

        // Bulk upload (in chunks of 1000 since there's an AI Search limit of 1000 per request)
        var chunks = indexDocuments.Chunk(1000);
        foreach (var chunk in chunks)
        {
            try 
            {
                await indexManager.BulkInsert(chunk);
            } 
            catch (Exception e)
            {
                Console.WriteLine($"Batch could not be saved to the index. Continuing. Error: {e.Message}");
            }
        }

        Console.WriteLine("All data processed and index updated. Goodbye.");
    }

}
