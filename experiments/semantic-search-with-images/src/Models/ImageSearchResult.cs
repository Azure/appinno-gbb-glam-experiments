using System.Text.Json.Serialization;

namespace SemanticSearchWithImages.Models;
public record SimilarImagesResult(
    [property: JsonPropertyName("similarImages")] ImageSearchResult[] SimilarImages
);

public record ImageSearchResult(
    [property: JsonPropertyName("objectId")] string ObjectId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("attribution")] string Attribution,
    [property: JsonPropertyName("displayDate")] string DisplayDate,
    [property: JsonPropertyName("locationDescription")] string LocationDescription,
    [property: JsonPropertyName("iiifThumbUrl")] string IiifThumbUrl,
    [property: JsonPropertyName("searchScore")] double? SearchScore
);