using ingestion.Models;

namespace ingestion.Services
{
    public interface ICsvService
    {
        IList<ImageMetadata> GetRecords(MemoryStream memoryStream);
    }
}