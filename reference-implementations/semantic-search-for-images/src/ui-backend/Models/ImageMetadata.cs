namespace ui_backend.Models
{
    public record ImageMetadata(
        string objectId,
        string imageUrl,
        string artist,
        string title,
        string creationDate,
        float similarityScore
    ) {
        public string id { 
            get {
                return objectId;
            }
        }
    }
}