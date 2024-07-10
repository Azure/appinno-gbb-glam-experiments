namespace ingestion.Models
{
    public class AppSettings
    {
        public required string DatabaseTargeted{ get; set;}
        public required StorageAccount StorageAccount { get; set; }
        public required CosmosDb CosmosDb { get; set; }
        public required AiSearch AiSearch { get; set; }
        public required AiServices AiServices { get; set; }
    }

    public class StorageAccount {
        public required string Uri { get; set; }
        public required string ImageCsvContainer { get; set; }
        public required string ProcessedContainer { get; set; }
    }

    public class CosmosDb {
        public required string Uri { get; set; }
        public required string Database { get; set; }
        public required string ImageMetadataContainer { get; set; }
        public required string ImageVectorPath { get; set; }
        public required string PartitionKey { get; set; }
        public required int RUs { get; set; }
    }

    public class AiSearch {
        public required string Uri { get; set; }
        public required string Index { get; set; }
        public required string VectorSearchProfile { get; set; }
        public required string VectorSearchHnswConfig { get; set; }
        public required int VectorSearchDimensions { get; set; }
    }

    public class AiServices {
        public required string Uri { get; set; }
        public required string ApiVersion { get; set; }
        public required string ModelVersion { get; set; }
    }
}