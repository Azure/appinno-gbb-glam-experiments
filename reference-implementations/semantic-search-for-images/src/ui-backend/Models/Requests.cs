using System.Text.Json.Serialization;

namespace ui_backend.Models
{
    public record ByImageUrlRequest(
        [property: JsonPropertyName("url")] string Url
    );

    public record ByTextRequest(
        [property: JsonPropertyName("text")] string Text
    );
}