using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Indexer.Models;

namespace Indexer.Services;

public class IndexManager
{
    private readonly SearchIndexClient _searchIndexClient;
    private readonly SearchClient _searchClient;

    public IndexManager(Settings settings)
    {
        var credential = new AzureKeyCredential(settings.AzureAiSearchKey);
        var endpoint = new Uri(settings.AzureAiSearchEndpoint);
        _searchIndexClient = new(endpoint, credential);
        _searchClient = new(endpoint, settings.AzureAiSearchIndexName, credential);
    }

    public async Task Create(bool dropIfIndexExists)
    {
        // Check if the index exists and drop if configured
        var indexExists = _searchIndexClient.GetIndexNames().Any(name => name == _searchClient.IndexName);
        if (dropIfIndexExists && indexExists)
            await _searchIndexClient.DeleteIndexAsync(_searchClient.IndexName);

        // Create the index
        var searchFields = new FieldBuilder().Build(typeof(IndexDocument));
        var indexDefinition = new SearchIndex(_searchClient.IndexName)
        {
            VectorSearch = new()
            {
                Profiles = 
                {
                    new VectorSearchProfile(Constants.IMAGE_VECTOR_SEARCH_PROFILE_NAME, Constants.VECTOR_SEARCH_HNSW_CONFIG)
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(Constants.VECTOR_SEARCH_HNSW_CONFIG)
                }
            },
            Fields = searchFields
        };

        await _searchIndexClient.CreateIndexAsync(indexDefinition);
    }

    public async Task<Response<IndexDocumentsResult>> BulkInsert(IEnumerable<IndexDocument> indexDocuments)
    {
        // Azure AI Search upload documents has a limit of 1000 documents per request
        if (indexDocuments.Count() > 1000)
            throw new ArgumentException("Bulk insert limit exceeded. Maximum 1000 documents per request.");

        return await _searchClient.UploadDocumentsAsync(indexDocuments);
    }
}