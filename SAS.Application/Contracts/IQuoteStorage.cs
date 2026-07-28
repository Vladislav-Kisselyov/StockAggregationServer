using SAS.Domain;

namespace SAS.Application.Contracts;

public interface IQuoteStorage
{
    public long TotalSaved { get; }
    public long TotalDropped { get; }
    public long TotalDuplicates { get; }

    public int LastBatchSize { get; }
    public TimeSpan LastBatchPause { get; }

    public void IncrementDuplicates();
    public void Complete();
    public ValueTask EnqueueAsync(
        Quote quote,
        CancellationToken cancellationToken = default);
}
