namespace ui_backend.Models
{
    public class AppSettings
    {
        public required string DatabaseTargeted{ get; set;}
        public required CosmosDb CosmosDb { get; set; }
        public required AiSearch AiSearch { get; set; }
        public required AiServices AiServices { get; set; }
    }

    public class CosmosDb {
        public required string Uri { get; set; }
        public required string Database { get; set; }
        public required string ImageMetadataContainer { get; set; }
        public required int NumItemsToReturn { get; set; }
    }

    public class AiSearch {
        public required string Uri { get; set; }
        public required string Index { get; set; }
        public required string VectorField { get; set; }
        public required int NumItemsToReturn { get; set; }
    }

    public class AiServices {
        public required string Uri { get; set; }
        public required string ApiVersion { get; set; }
        public required string ModelVersion { get; set; }
    }
}