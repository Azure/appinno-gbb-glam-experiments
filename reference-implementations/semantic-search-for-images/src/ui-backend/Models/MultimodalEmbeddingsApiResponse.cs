using System.Text.Json.Serialization;

namespace ui_backend.Models
{
    public record MultimodalEmbeddingsApiResponse(
    [property: JsonPropertyName("modelVersion")] string ModelVersion,
    [property: JsonPropertyName("vector")] float[] Vector);
}