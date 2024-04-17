using System.Text.Json.Serialization;

namespace SemanticSearchWithImages.Models;

public record ByImageRequest(
    [property: JsonPropertyName("url")] string Url
);