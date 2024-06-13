using ingestion.Models;

namespace ingestion.Services {

    public interface IDatabaseService
    {
        Task InitializeAsync();
        Task UpsertItemAsync(ImageMetadata imageMetadata);
    }

}