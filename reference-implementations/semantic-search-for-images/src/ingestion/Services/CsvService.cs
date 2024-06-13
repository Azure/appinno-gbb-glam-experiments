using System.Data;
using System.Globalization;
using CsvHelper;
using ingestion.Models;
using Microsoft.Extensions.Logging;

namespace ingestion.Services
{
    /// <summary>
    /// Service for handling operations related to CSV files.
    /// </summary>
    public class CsvService : ICsvService
    {
        private ILogger<CsvService> _logger;

        /// <summary>
        /// Initializes a new instance of the CsvService class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        public CsvService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CsvService>();    
        }

        /// <summary>
        /// Gets records from a CSV file represented as a memory stream.  This method will deserialize the CSV data into a list of ImageMetadata objects.
        /// Validation against the records in the CSV will be performed to ensure that the required columns (objectId and imageUrl) are present.
        /// </summary>
        /// <param name="memoryStream">The MemoryStream containing the CSV data.</param>
        /// <returns>A list of ImageMetadata objects representing the records in the CSV file.</returns>
        public IList<ImageMetadata> GetRecords(MemoryStream memoryStream) {

            var imageMetadata = new List<ImageMetadata>();

            using (var reader = new StreamReader(memoryStream))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                using (var dr = new CsvDataReader(csv))
                {		
                    var dt = new DataTable();
                    dt.Load(dr);
                    
                    ValidateTable(dt.Columns);

                    var metadataColumns = dt.Columns.Cast<DataColumn>()
                                            .Where(x => string.Compare(x.ColumnName, Constants.DATA_TABLE_COLUMN_NAME_OBJECT_ID, StringComparison.OrdinalIgnoreCase) != 0
                                                     && string.Compare(x.ColumnName, Constants.DATA_TABLE_COLUMN_NAME_IMAGE_URL, StringComparison.OrdinalIgnoreCase) != 0 
                                                     && string.Compare(x.ColumnName, Constants.DATA_TABLE_COLUMN_NAME_ARTIST, StringComparison.OrdinalIgnoreCase) != 0
                                                     && string.Compare(x.ColumnName, Constants.DATA_TABLE_COLUMN_NAME_TITLE, StringComparison.OrdinalIgnoreCase) != 0
                                                     && string.Compare(x.ColumnName, Constants.DATA_TABLE_COLUMN_NAME_CREATION_DATE, StringComparison.OrdinalIgnoreCase) != 0)
                                            .ToList();

                    foreach(DataRow row in dt.Rows)
                    {
                        var metadata = new ImageMetadata(
                            SafeGetValue(row, Constants.DATA_TABLE_COLUMN_NAME_OBJECT_ID),
                            SafeGetValue(row, Constants.DATA_TABLE_COLUMN_NAME_IMAGE_URL),
                            SafeGetValue(row, Constants.DATA_TABLE_COLUMN_NAME_ARTIST),
                            SafeGetValue(row, Constants.DATA_TABLE_COLUMN_NAME_TITLE),
                            SafeGetValue(row, Constants.DATA_TABLE_COLUMN_NAME_CREATION_DATE),
                            new Dictionary<string, string>()
                        );

                        foreach(var metadataColumn in metadataColumns)
                        {
                            metadata.metadata.Add(metadataColumn.ColumnName, row[metadataColumn.ColumnName].ToString()!);
                        }

                        imageMetadata.Add(metadata);
                    }
                }
            }

            return imageMetadata;
        }

        /// <summary>
        /// Validates the DataTable to ensure it contains the required columns (objectId and imageUrl).
        /// </summary>
        /// <param name="columns">The DataColumnCollection from the DataTable to validate.</param>
        /// <exception cref="Exception">Thrown when the required columns are not present in the DataTable.</exception>
        private void ValidateTable(DataColumnCollection columns) {
            if (!columns.Contains(Constants.DATA_TABLE_COLUMN_NAME_OBJECT_ID) || !columns.Contains(Constants.DATA_TABLE_COLUMN_NAME_IMAGE_URL))
            {
                throw new Exception("The CSV file must contain columns named objectId and imageUrl.");
            }
        }

        /// <summary>
        /// Safely retrieves the value of a specified column from a DataRow.
        /// </summary>
        /// <param name="row">The DataRow from which to retrieve the value.</param>
        /// <param name="columnName">The name of the column whose value is to be retrieved.</param>
        /// <returns>The value of the specified column as a string, or an empty string if the column does not exist.</returns>        
        private string SafeGetValue(DataRow row, string columnName) {
            if (row.Table.Columns.Contains(columnName))
            {
                return row[columnName].ToString()!;
            }

            return string.Empty;

        }
    }
}