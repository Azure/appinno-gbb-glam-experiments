using System.Text.Json.Serialization;

namespace Indexer.Models;

public record NGAOpenDataRecord(
    [property: JsonPropertyName("objectID")] string ObjectID,
    [property: JsonPropertyName("accessionNum")] string AccessionNum,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("displayDate")] string DisplayDate,
    [property: JsonPropertyName("medium")] string Medium,
    [property: JsonPropertyName("attribution")] string Attribution,
    [property: JsonPropertyName("description")] string LocationDescription,
    [property: JsonPropertyName("iiifUrl")] string IiifUrl,
    [property: JsonPropertyName("iiifThumbUrl")] string IiifThumbUrl,
    [property: JsonPropertyName("keywords")] string Keywords,
    [property: JsonPropertyName("places")] string Places,
    [property: JsonPropertyName("schools")] string Schools,
    [property: JsonPropertyName("styles")] string Styles,
    [property: JsonPropertyName("techiniques")] string Techniques,
    [property: JsonPropertyName("themes")] string Themes
);
