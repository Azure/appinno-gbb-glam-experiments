namespace ui_backend.Models
{
    // Convert ImageMetadata to a class

    public class ImageMetadata {
        public required string objectId { get; set; }
        public required string imageUrl { get; set; }
        public required string artist { get; set; }
        public required string title { get; set; }
        public required string creationDate { get; set; }
        public double? similarityScore { get; set; }

        public string id { 
            get {
                return objectId;
            }
        }
    }
}