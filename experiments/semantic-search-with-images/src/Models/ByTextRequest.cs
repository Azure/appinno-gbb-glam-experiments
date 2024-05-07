using System.Text.Json.Serialization;

namespace SemanticSearchWithImages.Models;

public record ByTextRequest(
    [property: JsonPropertyName("text")] string Text
);