using Indexer.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Indexer.Services;

public class MetOpenAccessRetriever : BaseRetriever
{
    private readonly CosmosClient _cosmosClient;
    private readonly Database _database;
    private readonly Container _container;

    public MetOpenAccessRetriever(Settings settings) : base(settings)
    {
        _cosmosClient = new(settings.ExternalSourceConnectionInfo);
        _database = _cosmosClient.GetDatabase(settings.ExternalSourceConnectionCosmosDatabase);
        _container = _database.GetContainer(settings.ExternalSourceConnectionCosmosContainer);
    }
    
    public override async Task<IEnumerable<IndexDocument>> GetAllRecords()
    {
        var indexRecords = new List<IndexDocument>();

        var q = _container.GetItemLinqQueryable<MetOpenAccessApiObjectResponse>();
        var queryable = q.Select(obj =>
            new IndexDocument(){
                ObjectID = obj.ObjectID.ToString(),
                AccessionNum = obj.AccessionNumber,
                Title = obj.Title,
                Attribution = obj.ArtistAlphaSort,
                DisplayDate = obj.ObjectDate,
                Dimensions = obj.Dimensions,
                LocationDescription = obj.GalleryNumber,
                Medium = obj.Medium,
                ImageUrl = obj.PrimaryImageSmall
            }
        );
        if (base._recordLimit > 0)
            queryable = queryable.Take(_recordLimit);
        var iterator = queryable.ToFeedIterator();

        while (iterator.HasMoreResults)
            indexRecords.AddRange(await iterator.ReadNextAsync());

        Console.WriteLine($"Retrieved {indexRecords.Count} records from the Met Open Access APIs store.");

        return indexRecords;
    }
}