using Indexer.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Net.Http.Json;

namespace Indexer.Services;

public class MetOpenAccessPreProcessor
{
    private readonly Settings _settings;
    private readonly HttpClient _httpClient = new();
    private const string BASE_URL = "https://collectionapi.metmuseum.org/public/collection/v1";
    private readonly CosmosClient _cosmosClient;
    private readonly Database _database;
    private readonly Container _container;

    public MetOpenAccessPreProcessor(Settings settings)
    {
        _settings = settings;
        _cosmosClient = new(settings.ExternalSourceConnectionInfo);
        _database = _cosmosClient.GetDatabase(settings.ExternalSourceConnectionCosmosDatabase);
        _container = _database.GetContainer(settings.ExternalSourceConnectionCosmosContainer);
    }

    public async Task LoadAllRecordsIntoCosmosNoSql()
    {
        // Query for all objects with images

        var response = await _httpClient.GetAsync($"{BASE_URL}/search?hasImages=true&q=*");
        response.EnsureSuccessStatusCode();
        var searchResponse = await response.Content.ReadFromJsonAsync<MetOpenAccessApiSearchResponse>()
            ?? throw new Exception("Getting all objects with images failed because response format was unexpected.");

        Console.WriteLine($"{searchResponse.Total} objects found. Requesting object data for each.");

        // Don't look for details of objects we've already cached
        var q = _container.GetItemLinqQueryable<MetOpenAccessApiObjectResponse>();
        var iterator = q.Select(obj => obj.ObjectID).ToFeedIterator();
        var alreadyCached = new List<int>();
        while (iterator.HasMoreResults)
            alreadyCached.AddRange(await iterator.ReadNextAsync());
        
        var needToLookupAndCache = searchResponse.ObjectIDs.Except(alreadyCached);

        Console.WriteLine($"From {searchResponse.Total}, already stored {alreadyCached.Count}. Processing the remaining {needToLookupAndCache.Count()}");

        // Query for object data including image URL

        int storedCount = 0;
        foreach (int objectID in needToLookupAndCache)
        {
            response = await _httpClient.GetAsync($"{BASE_URL}/objects/{objectID}");
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                    response.EnsureSuccessStatusCode();
                // else, skipping
            }
            else // Success!
            {
                //Console.Write($"Object ID {objectID} found. Processing... ");
                var objectResponse = await response.Content.ReadFromJsonAsync<MetOpenAccessApiObjectResponse>()
                    ?? throw new Exception($"Getting object data failed for {objectID} because response format was unexpected.");

                // Originally requiring they be Public Domain... rerunning to allow as long as there's an image.
                //if (objectResponse.IsPublicDomain && !string.IsNullOrEmpty(objectResponse.PrimaryImageSmall))
                if (!string.IsNullOrEmpty(objectResponse.PrimaryImageSmall))
                {
                    Console.WriteLine($"Adding: {objectResponse}");
                    storedCount++;
                    _ = await _container.UpsertItemAsync<MetOpenAccessApiObjectResponse>(
                        item: objectResponse, 
                        partitionKey: new PartitionKey(objectResponse.ObjectID)
                    );
                } 
                // else, skipping
            }
        }

        Console.WriteLine($"Stored {storedCount} records from the Met Open Access APIs.");

    }
}