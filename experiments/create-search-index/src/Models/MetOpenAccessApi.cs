using System.Text.Json.Serialization;

namespace Indexer.Models;

public record MetOpenAccessApiSearchResponse(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("objectIDs")] int[] ObjectIDs
);

public record MetOpenAccessApiObjectResponse(
    [property: JsonPropertyName("objectID")] int ObjectID,
    [property: JsonPropertyName("isHighlight")] bool IsHighlight,
    [property: JsonPropertyName("accessionNumber")] string AccessionNumber,
    [property: JsonPropertyName("accessionYear")] string AccessionYear,
    [property: JsonPropertyName("isPublicDomain")] bool IsPublicDomain,
    [property: JsonPropertyName("primaryImage")] string PrimaryImage,
    [property: JsonPropertyName("primaryImageSmall")] string PrimaryImageSmall,
    [property: JsonPropertyName("additionalImages")] string[] AdditionalImages,
    [property: JsonPropertyName("constituents")] Constituent[] Constituents,
    [property: JsonPropertyName("department")] string Department,
    [property: JsonPropertyName("objectName")] string ObjectName,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("culture")] string Culture,
    [property: JsonPropertyName("period")] string Period,
    [property: JsonPropertyName("dynasty")] string Dynasty,
    [property: JsonPropertyName("reign")] string Reign,
    [property: JsonPropertyName("portfolio")] string Portfolio,
    [property: JsonPropertyName("artistRole")] string ArtistRole,
    [property: JsonPropertyName("artistPrefix")] string ArtistPrefix,
    [property: JsonPropertyName("artistDisplayName")] string ArtistDisplayName,
    [property: JsonPropertyName("artistDisplayBio")] string ArtistDisplayBio,
    [property: JsonPropertyName("artistSuffix")] string ArtistSuffix,
    [property: JsonPropertyName("artistAlphaSort")] string ArtistAlphaSort,
    [property: JsonPropertyName("artistNationality")] string ArtistNationality,
    [property: JsonPropertyName("artistBeginDate")] string ArtistBeginDate,
    [property: JsonPropertyName("artistEndDate")] string ArtistEndDate,
    [property: JsonPropertyName("artistGender")] string ArtistGender,
    [property: JsonPropertyName("artistWikidata_URL")] string ArtistWikidata_URL,
    [property: JsonPropertyName("artistULAN_URL")] string ArtistULAN_URL,
    [property: JsonPropertyName("objectDate")] string ObjectDate,
    [property: JsonPropertyName("objectBeginDate")] int ObjectBeingDate,
    [property: JsonPropertyName("objectEndDate")] int ObjectEndDate,
    [property: JsonPropertyName("medium")] string Medium,
    [property: JsonPropertyName("dimensions")] string Dimensions,
    [property: JsonPropertyName("dimensionsParsed")] ElementDimension[] DimensionsParsed,
    [property: JsonPropertyName("measurements")] ElementMeasurements[] Measurements,
    [property: JsonPropertyName("creditLine")] string CreditLine,
    [property: JsonPropertyName("geographyType")] string GeographyType,
    [property: JsonPropertyName("city")] string City,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("county")] string County,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("region")] string Region,
    [property: JsonPropertyName("subregion")] string SubRegion,
    [property: JsonPropertyName("locale")] string Locale,
    [property: JsonPropertyName("locus")] string Locus,
    [property: JsonPropertyName("excavation")] string Excavation,
    [property: JsonPropertyName("river")] string River,
    [property: JsonPropertyName("classification")] string Classification,
    [property: JsonPropertyName("rightsAndReproduction")] string RightsAndReproduction,
    [property: JsonPropertyName("linkResource")] string LinkResource,
    [property: JsonPropertyName("metadataDate")] DateTime MetadataDate,
    [property: JsonPropertyName("repository")] string Repository,
    [property: JsonPropertyName("objectURL")] string ObjectURL,
    [property: JsonPropertyName("tags")] Tag[] Tags,
    [property: JsonPropertyName("objectWikidata_URL")] string ObjectWikidata_URL,
    [property: JsonPropertyName("isTimelineWork")] bool IsTimelineWork,
    [property: JsonPropertyName("GalleryNumber")] string GalleryNumber)
{
#pragma warning disable IDE1006 // Naming Styles
    public string id { get; init; } = ObjectID.ToString();
#pragma warning restore IDE1006 // Naming Styles
}

public record Constituent(
    [property: JsonPropertyName("constituentID")] int ConstituentID,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("constituentULAN_URL")] string ConstituentULAN_URL,
    [property: JsonPropertyName("constituentWikidata_URL")] string ConstituentWikidata_URL,
    [property: JsonPropertyName("gender")] string Gender
);

public record ElementDimension(
    [property: JsonPropertyName("element")] string Element,
    [property: JsonPropertyName("dimensionType")] string DimensionType,
    [property: JsonPropertyName("dimension")] float Dimension
);

public record ElementMeasurements(
    [property: JsonPropertyName("elementName")] string ElementName,
    [property: JsonPropertyName("elementDescription")] string ElementDescription,
    [property: JsonPropertyName("elementMeasurements")] Measurements ElementMeasurement
);

public record Measurements(
    [property: JsonPropertyName("Height")] float Height,
    [property: JsonPropertyName("Length")] float Length,
    [property: JsonPropertyName("Width")] float Width
);

public record Tag(
    [property: JsonPropertyName("term")] string Term,
    [property: JsonPropertyName("AAT_URL")] string AAT_URL,
    [property: JsonPropertyName("Wikidata_URL")] string Wikidata_URL
);