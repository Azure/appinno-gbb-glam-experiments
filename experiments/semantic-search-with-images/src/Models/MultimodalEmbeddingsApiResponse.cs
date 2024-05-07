using System.Text.Json.Serialization;

namespace SemanticSearchWithImages.Models;

public record MultimodalEmbeddingsApiResponse(
    [property: JsonPropertyName("modelVersion")] string ModelVersion,
    [property: JsonPropertyName("vector")] float[] Vector
);