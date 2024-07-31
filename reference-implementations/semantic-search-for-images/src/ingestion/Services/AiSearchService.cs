using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using ingestion.Models;
using Microsoft.Extensions.Logging;

namespace ingestion.Services
{
    public class AiSearchService : IDatabaseService
    {
        private AppSettings _appSettings;
        private ILogger<AiSearchService> _logger;
        private SearchClient _searchClient;
        private TokenCredential _tokenCredential;

        public AiSearchService(ILoggerFactory loggerFactory, AppSettings appSettings, TokenCredential tokenCredential)
        {
            _logger = loggerFactory.CreateLogger<AiSearchService>();
            _appSettings = appSettings;
            _tokenCredential = tokenCredential;
            _searchClient = new(new Uri(_appSettings.AiSearch.Uri), _appSettings.AiSearch.Index, _tokenCredential);
        }

        public async Task InitializeAsync()
        {   
            var indexClient = new SearchIndexClient(new Uri(_appSettings.AiSearch.Uri), _tokenCredential);
            
            SearchIndex index = new SearchIndex(_appSettings.AiSearch.Index)
            {
                Fields = {
                    new SimpleField("objectId", SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true },
                    new SimpleField("imageUrl", SearchFieldDataType.String),
                    new SimpleField("artist", SearchFieldDataType.String) { IsFacetable = true, IsSortable = true },
                    new SearchableField("title") { IsSortable = true },
                    new SimpleField("creationDate", SearchFieldDataType.String) { IsSortable = true },
                    new SimpleField("metadata", SearchFieldDataType.String),
                    new VectorSearchField("imageVector", _appSettings.AiSearch.VectorSearchDimensions, _appSettings.AiSearch.VectorSearchProfile)
                },
                VectorSearch = new()
                {
                    Profiles =
                    {
                        new VectorSearchProfile(_appSettings.AiSearch.VectorSearchProfile, _appSettings.AiSearch.VectorSearchHnswConfig)
                    },
                    Algorithms = 
                    {
                        new HnswAlgorithmConfiguration(_appSettings.AiSearch.VectorSearchHnswConfig)
                    }
                }
            };

            await indexClient.CreateOrUpdateIndexAsync(index);
        }

        public async Task UpsertItemAsync(ImageMetadata imageMetadata)
        {
            _logger.LogInformation($"Upserting item with title: {imageMetadata.title} and objectId: {imageMetadata.objectId},");

            var imageMetadataAiSearchModel = new ImageMetadataAiSearchModel(
                imageMetadata.objectId,
                imageMetadata.imageUrl,
                imageMetadata.artist,
                imageMetadata.title,
                imageMetadata.creationDate,
                imageMetadata.imageVector!,
                JsonSerializer.Serialize(imageMetadata.metadata));

            await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.MergeOrUpload(new List<ImageMetadataAiSearchModel> { imageMetadataAiSearchModel }));

            _logger.LogInformation($"Upserted item with title: {imageMetadata.title} and objectId: {imageMetadata.objectId},");
        }

        private record ImageMetadataAiSearchModel(
            string objectId,
            string imageUrl,
            string artist,
            string title,
            string creationDate,
            float[] imageVector,
            string metadata);
    }
}