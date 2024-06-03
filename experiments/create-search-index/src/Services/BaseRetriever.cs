using Indexer.Models;

namespace Indexer.Services;

public abstract class BaseRetriever(Settings settings)
{
    private readonly Settings _settings = settings;
    protected readonly int _recordLimit = settings.RecordCountToIndex;

    public abstract Task<IEnumerable<IndexDocument>> GetAllRecords();
}