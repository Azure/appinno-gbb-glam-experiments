namespace ui_backend.Models
{
    public class AppSettings
    {
        public required string CosmosDbUri { get; set; }
        public required string CosmosDbName { get; set; }
        public required string CosmosDbImageMetadataContainerName { get; set; }
        public required int CosmosDbNumItemsToReturn { get; set; }
        public required string AiServicesUri { get; set; }
        public required string AiServicesKey { get; set; }
        public required string AiServicesApiVersion { get; set; }
        public required string AiServicesModelVersion { get; set; }
    }

}