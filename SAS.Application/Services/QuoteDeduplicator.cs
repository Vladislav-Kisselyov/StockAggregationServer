using System.Collections.Concurrent;
using SAS.Application.Contracts;
using SAS.Domain;

namespace SAS.Application.Services;

public sealed class QuoteDeduplicator : IQuoteDeduplicator
{
    private readonly ConcurrentDictionary<DedupKey, byte> _cache = new();
    private readonly ConcurrentQueue<ExpirationItem> _expirationQueue = new();
    private readonly TimeSpan _window = TimeSpan.FromSeconds(30);

    public bool TryAccept(Quote quote)
    {
        var key = new DedupKey(
            quote.Source,
            quote.Ticker,
            quote.Price,
            quote.Volume,
            quote.Timestamp);

        if (!_cache.TryAdd(key, 0))
        {
            return false;
        }

        _expirationQueue.Enqueue(new ExpirationItem(
            key,
            DateTimeOffset.UtcNow + _window));

        return true;
    }

    public void CleanupExpired()
    {
        var utcNow = DateTimeOffset.UtcNow;

        while (_expirationQueue.TryPeek(out var item))
        {
            if (item.ExpirationTime > utcNow)
                break;

            _expirationQueue.TryDequeue(out _);
            _cache.TryRemove(item.Key, out _);
        }
    }

    private readonly record struct DedupKey(
        string Source,
        string Ticker,
        decimal Price,
        long Volume,
        DateTimeOffset Timestamp);

    private readonly record struct ExpirationItem(
        DedupKey Key,
        DateTimeOffset ExpirationTime);
}
