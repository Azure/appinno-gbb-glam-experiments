using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ui_backend.Models;

namespace ui_backend.Services
{
    /// <summary>
    /// Represents a service that performs searches against Azure Ai Search.
    /// </summary>
    public class AiSearchService : IDatabaseService
    {
        private AppSettings _appSettings;
        private SearchClient _searchClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AiSearchService"/> class.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        public AiSearchService(AppSettings appSettings, TokenCredential tokenCredential)
        {
            _appSettings = appSettings;
            _searchClient = new SearchClient(new Uri(_appSettings.AiSearch.Uri), _appSettings.AiSearch.Index, tokenCredential);
        }

        /// <summary>
        /// Searches for image metadata based on the given embeddings.
        /// </summary>
        /// <param name="embeddings">The embeddings to search for.</param>
        /// <returns>A list of image metadata matching the search criteria.</returns>
        public async Task<IList<ImageMetadata>> Search(float[] embeddings)
        {
            List<ImageMetadata> results = [];

            SearchResults<ImageMetadata> response = await _searchClient.SearchAsync<ImageMetadata>(
                new SearchOptions
                {
                    VectorSearch = new ()
                    {
                        Queries = { new VectorizedQuery(embeddings) { KNearestNeighborsCount = _appSettings.AiSearch.NumItemsToReturn, Fields = { _appSettings.AiSearch.VectorField }}}
                    }
                }
            );

            await foreach (var result in response.GetResultsAsync())
            {
                result.Document.similarityScore = result.Score;
                results.Add(result.Document);
            }

            return results;
        }

        public async Task<bool> IsReady()
        {
            try
            {
                await _searchClient.GetDocumentCountAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}