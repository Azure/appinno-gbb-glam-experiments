namespace SemanticSearchWithImages;

public class AiVisionOptions
{
    public string Key { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string MultimodalEmbeddingsApiVersion { get; set; } = string.Empty;
    public string MultimodalEmbeddingsModelVersion { get; set; } = string.Empty;
}

public class AiSearchOptions
{
    public string Key { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public string IndexImageVectorsFieldName { get; set; } = string.Empty;
    public int TopNCount { get; set; } = 3;
}