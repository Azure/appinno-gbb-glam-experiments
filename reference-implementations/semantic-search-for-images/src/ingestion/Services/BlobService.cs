using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ingestion.Models;
using Microsoft.Extensions.Logging;

namespace ingestion.Services
{
    /// <summary>
    /// Service for handling operations related to Azure Blob Storage.
    /// </summary>
    public class BlobService : IBlobService
    {
        private ILogger<BlobService> _logger;
        private BlobServiceClient _blobServiceClient; 
        private BlobContainerClient _imageCSVContainerClient;
        private BlobContainerClient _processedContainerClient;

        /// <summary>
        /// Initializes a new instance of the BlobService class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="blobServiceClient">The BlobServiceClient instance.</param>
        public BlobService(ILoggerFactory loggerFactory, AppSettings appSettings, BlobServiceClient blobServiceClient)
        {
            _logger = loggerFactory.CreateLogger<BlobService>();
            _blobServiceClient = blobServiceClient;

            _imageCSVContainerClient = _blobServiceClient.GetBlobContainerClient(appSettings.StorageAccountImageCsvContainerName);
            _processedContainerClient = _blobServiceClient.GetBlobContainerClient(appSettings.StorageAccountProcessedContainerName);
        }

        /// <summary>
        /// Gets all blobs in the image CSV container.
        /// </summary>
        /// <returns>An AsyncPageable of BlobItem representing the blobs in the container.</returns>
        public AsyncPageable<BlobItem> GetImageCsvBlobsAsync()
        {
            return _imageCSVContainerClient.GetBlobsAsync();
        }

        /// <summary>
        /// Downloads a specific blob from the image CSV container.
        /// </summary>
        /// <param name="blobName">The name of the blob to download.</param>
        /// <param name="memoryStream">The MemoryStream where the blob will be downloaded to.</param>
        public async Task DownloadImageCsvBlob(string blobName, MemoryStream memoryStream)
        {
            _logger.LogInformation($"Downloading blob: {blobName}");

            var blobClient = _imageCSVContainerClient.GetBlobClient(blobName);
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0;

            _logger.LogInformation($"Downloaded blob: {blobName}");
        }

        /// <summary>
        /// Moves a blob from the image CSV container to the processed container.
        /// </summary>
        /// <param name="blob">The blob to move.</param>
        public async Task MoveBlobToProcessedContainer(BlobItem blob)
        {
            _logger.LogInformation($"Moving blob: {blob.Name} to processed container named {_processedContainerClient.Name}.");

            var sourceBlob = _imageCSVContainerClient.GetBlobClient(blob.Name);
            var destinationBlob = _processedContainerClient.GetBlobClient(blob.Name);
            await destinationBlob.StartCopyFromUriAsync(sourceBlob.Uri);
            await sourceBlob.DeleteIfExistsAsync();

            _logger.LogInformation($"Moved blob: {blob.Name} to processed container named {_processedContainerClient.Name}.");
        }
    }
}