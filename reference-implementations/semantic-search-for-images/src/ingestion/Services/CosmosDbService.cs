using System.Collections.ObjectModel;
using Azure.Identity;
using ingestion.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace ingestion.Services
{
    /// <summary>
    /// Service for handling operations related to Azure Cosmos DB.
    /// </summary>
    public class CosmosDbService : IDatabaseService
    {
        private AppSettings _appSettings;
        private ILogger<CosmosDbService> _logger;
        private ContainerResponse? _containerResponse;

        /// <summary>
        /// Initializes a new instance of the CosmosDbService class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="appSettings">The application settings.</param>
        public CosmosDbService(ILoggerFactory loggerFactory, AppSettings appSettings)
        {
            _logger = loggerFactory.CreateLogger<CosmosDbService>();
            _appSettings = appSettings;            
        }

        /// <summary>
        /// Initializes the Cosmos DB client and container.  This initialization includes creating the database and container if they do not already exist.
        /// Also, this sets up a container with the necessary indexing policy and vector embedding policy.
        /// </summary>
        public async Task InitializeAsync()
        {
            // Connect to a cosmos db container using DefaultAzureCredential
            var cosmosClient = new CosmosClient(_appSettings.CosmosDb.Uri, new DefaultAzureCredential());
            var cosmosDb = await cosmosClient.CreateDatabaseIfNotExistsAsync(_appSettings.CosmosDb.Database);
            List<Embedding> embeddings = new List<Embedding>()
            {
                new Embedding()
                {
                    Path = _appSettings.CosmosDb.ImageVectorPath,
                    DataType = VectorDataType.Float32,
                    DistanceFunction = DistanceFunction.Cosine,
                    Dimensions = 1024
                }
            };
            Collection<Embedding> collection = new Collection<Embedding>(embeddings);
            ContainerProperties containerProperties = new ContainerProperties(id: _appSettings.CosmosDb.ImageMetadataContainer, partitionKeyPath: _appSettings.CosmosDb.PartitionKey)
            {   
                VectorEmbeddingPolicy = new(collection),
                IndexingPolicy = new IndexingPolicy()
                {
                    VectorIndexes = new()
                    {
                        new VectorIndexPath()
                        {
                            Path = _appSettings.CosmosDb.ImageVectorPath,
                            Type = VectorIndexType.QuantizedFlat,
                        }
                    }
                },
            };
            
            _containerResponse = await cosmosDb.Database.CreateContainerIfNotExistsAsync(containerProperties, _appSettings.CosmosDb.RUs);
        }

        /// <summary>
        /// Upserts an item in the Cosmos DB container.
        /// </summary>
        /// <param name="imageMetadata">The ImageMetadata object to upsert.</param>
        public async Task UpsertItemAsync(ImageMetadata imageMetadata) {
            _logger.LogInformation($"Upserting item with title: {imageMetadata.title} and objectId: {imageMetadata.objectId}.");

            await _containerResponse!.Container.UpsertItemAsync(imageMetadata, new PartitionKey(imageMetadata.objectId));
            
            _logger.LogInformation($"Upserted item with title: {imageMetadata.title} and objectId: {imageMetadata.objectId}.");
        }
    }

}