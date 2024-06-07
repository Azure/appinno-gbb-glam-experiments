using Azure.Search.Documents.Indexes;

namespace Indexer.Models;

public class IndexDocument 
{
    [SimpleField(IsKey = true, IsFacetable = false, IsFilterable = true, IsSortable = true)]
    public string ObjectID { get; set; }

    [SearchableField(IsFacetable = false, IsSortable = true)] public string AccessionNum { get; set; }
    [SearchableField(IsFilterable = true)] public string Title { get; set; }
    [SearchableField(IsFilterable = true)] public string DisplayDate { get; set; }
    [SearchableField(IsFilterable = true)] public string Medium { get; set; }
    [SearchableField(IsFilterable = true)] public string Dimensions { get; set; }
    [SearchableField(IsFilterable = true)] public string Attribution { get; set; }
    [SearchableField(IsFilterable = true)] public string LocationDescription { get; set; }
    [SearchableField] public string ImageUrl { get; set; }
    
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
        this.Dimensions = string.Empty;
        this.Attribution = string.Empty;
        this.LocationDescription = string.Empty;
        this.ImageUrl = string.Empty;

        this.VectorizedImage = [];
    }

}