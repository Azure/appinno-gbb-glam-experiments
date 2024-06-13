using Azure;
using Azure.Storage.Blobs.Models;

namespace ingestion.Services
{
    public interface IBlobService
    {
        AsyncPageable<BlobItem> GetImageCsvBlobsAsync();
        Task DownloadImageCsvBlob(string blobName, MemoryStream memoryStream);
        Task MoveBlobToProcessedContainer(BlobItem blob);
    }
}