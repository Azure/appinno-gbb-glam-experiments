using Azure.Search.Documents.Indexes;

namespace Indexer.Models;

public class IndexDocument 
{
    [SimpleField(IsKey = true, IsFacetable = false, IsFilterable = true, IsSortable = true)]
    public string ObjectID { get; set; }
    [SimpleField(IsFacetable = false, IsFilterable = true, IsSortable = true)]
    public string AccessionNum { get; set; }

    [SearchableField(IsFilterable = true)]
    public string Title { get; set; }
    [SearchableField]
    public string DisplayDate { get; set; }
    [SearchableField]
    public string Medium { get; set; }
    [SearchableField]
    public string Attribution { get; set; }
    [SearchableField(IsFilterable = true)]
    public string LocationDescription { get; set; }
    public string IiifUrl { get; set; }
    public string IiifThumbUrl { get; set; }
    [SearchableField]
    public string Keywords { get; set; }
    [SearchableField]
    public string Places { get; set; }
    [SearchableField]
    public string Schools { get; set; }
    [SearchableField]
    public string Styles { get; set; }
    [SearchableField]
    public string Techniques { get; set; }
    [SearchableField]
    public string Themes { get; set; }
    
    [VectorSearchField(
        VectorSearchDimensions = Constants.VECTORIZED_IMAGE_DIMENSIONS, 
        VectorSearchProfileName = Constants.IMAGE_VECTOR_SEARCH_PROFILE_NAME)]
    public IEnumerable<float> VectorizedImage { get; set; }

    public IndexDocument()
    {
        this.ObjectID = string.Empty;
        this.AccessionNum = string.Empty;
        this.Title = string.Empty;
        this.DisplayDate = string.Empty;
        this.Medium = string.Empty;
        this.Attribution = string.Empty;
        this.LocationDescription = string.Empty;
        this.IiifUrl = string.Empty;
        this.IiifThumbUrl = string.Empty;
        this.Keywords = string.Empty;
        this.Places = string.Empty;
        this.Schools = string.Empty;
        this.Styles = string.Empty;
        this.Techniques = string.Empty;
        this.Themes = string.Empty;

        this.VectorizedImage = [];
    }

    public IndexDocument(NGAOpenDataRecord openDataRecord)
    {
        this.ObjectID = openDataRecord.ObjectID;
        this.AccessionNum = openDataRecord.AccessionNum;
        this.Title = openDataRecord.Title;
        this.DisplayDate = openDataRecord.DisplayDate;
        this.Medium = openDataRecord.Medium;
        this.Attribution = openDataRecord.Attribution;
        this.LocationDescription = openDataRecord.LocationDescription;
        this.IiifUrl = openDataRecord.IiifUrl;
        this.IiifThumbUrl = openDataRecord.IiifThumbUrl;
        this.Keywords = openDataRecord.Keywords;
        this.Places = openDataRecord.Places;
        this.Schools = openDataRecord.Schools;
        this.Styles = openDataRecord.Styles;
        this.Techniques = openDataRecord.Techniques;
        this.Themes = openDataRecord.Themes;

        this.VectorizedImage = [];
    }
}