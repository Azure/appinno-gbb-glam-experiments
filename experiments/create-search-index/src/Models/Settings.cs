namespace Indexer.Models;

public class Settings
{
    public string AzureAiVisionKey { get; set; }
    public string AzureAiVisionEndpoint { get; set; }
    public string AzureAiVisionEmbeddingsModelVersion { get; set; }
    public string AzureAiVisionEmbeddingsApiVersion { get; set; }
    public string AzureAiSearchKey { get; set; }
    public string AzureAiSearchEndpoint { get; set; }
    public string AzureAiSearchIndexName { get; set; }
    public bool DropAndRecreateIndexIfItExists { get; set; }
    public ExternalSourceType ExternalSourceType { get; set; }
    public string ExternalSourceConnectionInfo { get; set; }
    public int RecordCountToIndex { get; set; }
    public string ExternalSourceConnectionCosmosDatabase { get; }
    public string ExternalSourceConnectionCosmosContainer { get; }

    public Settings()
    {
        this.AzureAiVisionKey = string.Empty;
        this.AzureAiVisionEndpoint = string.Empty;
        this.AzureAiVisionEmbeddingsModelVersion = "2024-02-01";
        this.AzureAiVisionEmbeddingsApiVersion = "2023-04-15";
        this.AzureAiSearchKey = string.Empty;
        this.AzureAiSearchEndpoint = string.Empty;
        this.AzureAiSearchIndexName = string.Empty;
        this.DropAndRecreateIndexIfItExists = true;
        this.ExternalSourceType = ExternalSourceType.MET;
        this.ExternalSourceConnectionInfo = string.Empty;
        this.RecordCountToIndex = -1;
        this.ExternalSourceConnectionCosmosDatabase = "met-openaccess-api";
        this.ExternalSourceConnectionCosmosContainer = "objects";
    }

}

public enum ExternalSourceType
{
    MET,
    NGA
}
