namespace ingestion.Models
{
    public class AppSettings
    {
        public required string StorageAccountUri { get; set; }
        public required string StorageAccountImageCsvContainerName { get; set; }
        public required string StorageAccountProcessedContainerName { get; set; }
        public required string CosmosDbUri { get; set; }
        public required string CosmosDbName { get; set; }
        public required string CosmosDbImageVectorPath { get; set; }
        public required string CosmosDbImageMetadataContainerName { get; set; }
        public required string CosmosDbPartitionkey { get; set; }
        public required string AiServicesUri { get; set; }
        public required string AiServicesKey { get; set; }
        public required string AiServicesApiVersion { get; set; }
        public required string AiServicesModelVersion { get; set; }
    }

}