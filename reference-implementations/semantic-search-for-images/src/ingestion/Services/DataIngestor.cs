using ingestion.Models;
using ingestion.Services;
using Microsoft.Extensions.Logging;

namespace ingestion {

    /// <summary>
    /// Coordinates the ingestion of data from uploaded csv files.
    /// </summary>
    public class DataIngestor : IDataIngestor {

        private ILogger<DataIngestor> _logger;
        private IDatabaseService _databaseService;
        private IBlobService _blobService;
        private ICsvService _csvService;
        private IImageService _imageService;

        /// <summary>
        /// Initializes a new instance of the DataIngestor class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="databaseService">The database service.</param>
        /// <param name="blobService">The blob service.</param>
        /// <param name="csvService">The CSV service.</param>
        /// <param name="imageService">The image service.</param>
        public DataIngestor(ILoggerFactory loggerFactory, IDatabaseService databaseService, IBlobService blobService, ICsvService csvService, IImageService imageService)
        {
            _logger = loggerFactory.CreateLogger<DataIngestor>();
            _databaseService = databaseService;
            _blobService = blobService;
            _csvService = csvService;
            _imageService = imageService;
        }

        /// <summary>
        /// Orchestrates the ingestion of data from uploaded csv files.  Data from the csv files will be serialized and 
        /// uploaded to the database service that has been injected.
        /// </summary>
        public async Task IngestDataAsync() {
            IList<ImageMetadata> records;

            await _databaseService.InitializeAsync();

            await foreach (var blobItem in _blobService.GetImageCsvBlobsAsync())
            {
                try {
                    _logger.LogInformation($"Processing blob: {blobItem.Name}");

                    using var memoryStream = new MemoryStream();
                    await _blobService.DownloadImageCsvBlob(blobItem.Name, memoryStream);

                    records = _csvService.GetRecords(memoryStream);
                }
                catch(Exception ex) {
                    _logger.LogError(ex, $"Error processing blob: {blobItem.Name}. Skipping...");
                    continue;
                }

                await Parallel.ForEachAsync(records, async (record, token) => {
                    try {
                        record.imageVector = await _imageService.GenerateImageEmbeddings(record.imageUrl);

                        await _databaseService.UpsertItemAsync(record);
                    }
                    catch (ArgumentNullException ex) {
                        _logger.LogError(ex, $"Record with title: {record.title} and objectId: {record.objectId} had a null imageUrl. Skipping...");
                        return;
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, $"Error processing record with title: {record.title} and objectId: {record.objectId}. Skipping...");
                        return;
                    }
                });

                await _blobService.MoveBlobToProcessedContainer(blobItem);
            }
        }

    }
}