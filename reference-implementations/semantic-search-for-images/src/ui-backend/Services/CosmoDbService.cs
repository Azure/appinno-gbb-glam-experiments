using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using ui_backend.Models;

namespace ui_backend.Services
{
    /// <summary>
    /// Represents a service for interacting with Cosmos DB.
    /// </summary>
    public class CosmoDbService : IDatabaseService
    {
        private AppSettings _appSettings;
        private Container _cosmosContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmoDbService"/> class.
        /// </summary>
        /// <param name="appsSettings">The application settings.</param>
        public CosmoDbService(AppSettings appsSettings, TokenCredential tokenCredential)
        {
            _appSettings = appsSettings;
            var cosmosClient = new CosmosClient(_appSettings.CosmosDb.Uri, tokenCredential);
            _cosmosContainer = cosmosClient.GetContainer(_appSettings.CosmosDb.Database, _appSettings.CosmosDb.ImageMetadataContainer);
        }

        /// <summary>
        /// Searches for image metadata based on the given embeddings.
        /// </summary>
        /// <param name="embeddings">The embeddings to search for.</param>
        /// <returns>A list of image metadata.</returns>
        public async Task<IList<ImageMetadata>> Search(float[] embeddings)
        {
            var query = new QueryDefinition(@"SELECT c.objectId, c.title, c.imageUrl, c.artist, c.creationDate, VectorDistance(c.imageVector, @embedding) AS similarityScore 
                                              FROM c 
                                              ORDER BY VectorDistance(c.imageVector, @embedding)
                                              OFFSET 0 LIMIT @limit")
                            .WithParameter("@embedding", embeddings)
                            .WithParameter("@limit", _appSettings.CosmosDb.NumItemsToReturn);
            
            using FeedIterator<ImageMetadata> resultSet = _cosmosContainer.GetItemQueryIterator<ImageMetadata>(query);
            
            List<ImageMetadata> results = [];

            while (resultSet.HasMoreResults)
            {
                FeedResponse<ImageMetadata> response = await resultSet.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task<bool> IsReady()
        {
            try 
            {
                await _cosmosContainer.ReadContainerAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                return false;
            }
            return true;
        }
    }
}