using Indexer.Models;
using Npgsql;

namespace Indexer.Services;

public class NGAOpenDataRetriever(Settings settings)
{
    private readonly string _connectionString = settings.NgaOpenDataPostgresqlConnectionString;
    private readonly int _recordLimit = settings.RecordCountToIndex;

    public async Task<IEnumerable<NGAOpenDataRecord>> GetAllNGAOpenDataRecords() 
    {
        var records = new List<NGAOpenDataRecord>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var command = @"select
    o.objectID::text,
    o.accessionNum,
    o.title,
    o.displayDate,
    o.medium,
    o.attribution,
    l.description,
    pi.iiifURL,
    pi.iiifThumbURL,
    ot_keyword.keywords,
    ot_place.places,
    ot_school.schools,
    ot_style.styles,
    ot_technique.techniques,
    ot_theme.themes
from objects o 
    left join locations l on o.locationID = l.locationID 
    left join published_images pi on o.objectID = pi.depictstmsobjectid
    left join (
        select objectID, termtype, string_agg(term::text, ', ') as keywords from objects_terms
        group by objectID, termtype) ot_keyword on o.objectID = ot_keyword.objectID and ot_keyword.termtype = 'Keyword'
    left join (
        select objectID, termtype, string_agg(term::text, ', ') as places from objects_terms
        group by objectID, termtype) ot_place on o.objectID = ot_place.objectID and ot_place.termtype = 'Place Executed'
    left join (
        select objectID, termtype, string_agg(term::text, ', ') as schools from objects_terms
        group by objectID, termtype) ot_school on o.objectID = ot_school.objectID and ot_school.termtype = 'School'
    left join (
        select objectID, termtype, string_agg(term::text, ', ') as styles from objects_terms
        group by objectID, termtype) ot_style on o.objectID = ot_style.objectID and ot_style.termtype = 'Style'
    left join (
        select objectID, termtype, string_agg(term::text, ', ') as techniques from objects_terms
        group by objectID, termtype) ot_technique on o.objectID = ot_technique.objectID and ot_technique.termtype = 'Technique'
    left join (
        select objectID, termtype, string_agg(term::text, ', ') as themes from objects_terms
        group by objectID, termtype) ot_theme on o.objectID = ot_theme.objectID and ot_theme.termtype = 'Theme'
where pi.iiifURL <> ''";
        
        if (_recordLimit > 0)
        {
            command += $" limit {_recordLimit};";
        }
        else
        {
            command += ";";
        }

        await using var cmd = new NpgsqlCommand(command, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            NGAOpenDataRecord record = new(
                GetStringValue(reader, 0),
                GetStringValue(reader, 1),
                GetStringValue(reader, 2),
                GetStringValue(reader, 3),
                GetStringValue(reader, 4),
                GetStringValue(reader, 5),
                GetStringValue(reader, 6),
                GetStringValue(reader, 7),
                GetStringValue(reader, 8),
                GetStringValue(reader, 9),
                GetStringValue(reader, 10),
                GetStringValue(reader, 11),
                GetStringValue(reader, 12),
                GetStringValue(reader, 13),
                GetStringValue(reader, 14)
            );
            records.Add(record);
        }

        Console.WriteLine($"Retrieved {records.Count} records from NGA OpenData store.");

        return records;
    }

    private static string GetStringValue(NpgsqlDataReader reader, int ordinal) 
    {
        return reader.IsDBNull(ordinal) 
            ? string.Empty
            : reader.GetString(ordinal);
    }
}