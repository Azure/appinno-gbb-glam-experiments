namespace ingestion.Models
{
    public record ImageMetadata(
        string objectId,
        string imageUrl,
        string artist,
        string title,
        string creationDate,
        IDictionary<string, string> metadata) 
        {
            public string id { 
                get {
                    return objectId;
                }
            }
            public float[]? imageVector { get; set; }
        }
}