using System.Text.Json.Serialization;

namespace ingestion.Models
{
    public record MultimodalEmbeddingsApiResponse(
    [property: JsonPropertyName("modelVersion")] string ModelVersion,
    [property: JsonPropertyName("vector")] float[] Vector);
}