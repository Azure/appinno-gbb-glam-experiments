using System.Text.Json.Serialization;

namespace Indexer.Models;

public record VectorizeImageResponse(
    [property: JsonPropertyName("modelVersion")] string ModelVersion,
    [property: JsonPropertyName("vector")] float[] Vector
);