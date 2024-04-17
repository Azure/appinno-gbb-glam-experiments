namespace Indexer;

internal static class Constants
{
    internal const int VECTORIZED_IMAGE_DIMENSIONS = 1024;
    internal const int VECTORIZED_TEXT_DIMENSIONS = 1536;
    internal const string IMAGE_VECTOR_SEARCH_PROFILE_NAME = "image-vector-search-profile";
    internal const string TEXT_VECTOR_SEARCH_PROFILE_NAME = "text-vector-search-profile";
    internal const string VECTOR_SEARCH_HNSW_CONFIG = "vector-search-hnsw-config";
    internal const string IMAGE_VECTORIZOR_HTTP_CLIENT = "imagevectorizerclient";
}