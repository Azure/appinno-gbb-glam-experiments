using System.Text.Json.Serialization;

namespace SemanticSearchWithImages.Models;
public record SimilarImagesResult(
    [property: JsonPropertyName("similarImages")] ImageSearchResult[] SimilarImages
);

public record ImageSearchResult(
    [property: JsonPropertyName("objectId")] string ObjectId,
    [property: JsonPropertyName("accessionNum")] string AccessionNum,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("attribution")] string Attribution,
    [property: JsonPropertyName("displayDate")] string DisplayDate,
    [property: JsonPropertyName("locationDescription")] string LocationDescription,
    [property: JsonPropertyName("medium")] string Medium,
    [property: JsonPropertyName("dimensions")] string Dimensions,
    [property: JsonPropertyName("imageUrl")] string ImageUrl,
    [property: JsonPropertyName("searchScore")] double? SearchScore
);