using System.Collections.Concurrent;
using SAS.Application.Services;
using SAS.Domain;

namespace SAS.Application.Tests.Services;

[TestFixture]
public class QuoteDeduplicatorTests
{
    private static Quote MakeQuote(
        string source,
        string ticker,
        decimal price,
        long volume,
        DateTimeOffset timestamp)
    {
        return new Quote
        {
            Source = source,
            Ticker = ticker,
            Price = price,
            Volume = volume,
            Timestamp = timestamp
        };
    }

    [Test]
    public async Task TryAccept_WithManyThreadsSubmittingTheSameQuote_AcceptsExactlyOnce()
    {
        var deduplicator = new QuoteDeduplicator();
        var timestamp = DateTimeOffset.UtcNow;
        var quote = MakeQuote("Binance", "BTCUSDT", 65000.50m, 12, timestamp);

        const int threadCount = 64;
        var acceptedCount = 0;

        using var barrier = new Barrier(threadCount);

        var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
        {
            barrier.SignalAndWait();

            if (deduplicator.TryAccept(quote))
            {
                Interlocked.Increment(ref acceptedCount);
            }
        }));

        await Task.WhenAll(tasks);

        Assert.That(acceptedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task TryAccept_WithManyThreadsSubmittingDistinctQuotes_AcceptsAllOfThem()
    {
        var deduplicator = new QuoteDeduplicator();
        var timestamp = DateTimeOffset.UtcNow;

        const int threadCount = 32;
        const int quotesPerThread = 200;

        var acceptedCount = 0;

        using var barrier = new Barrier(threadCount);

        var tasks = Enumerable.Range(0, threadCount).Select(threadIndex => Task.Run(() =>
        {
            barrier.SignalAndWait();

            for (var i = 0; i < quotesPerThread; i++)
            {
                var quote = MakeQuote(
                    source: $"Exchange{threadIndex}",
                    ticker: "ETHUSDT",
                    price: 1000m + i,
                    volume: 1,
                    timestamp: timestamp);

                if (deduplicator.TryAccept(quote))
                {
                    Interlocked.Increment(ref acceptedCount);
                }
            }
        }));

        await Task.WhenAll(tasks);

        Assert.That(acceptedCount, Is.EqualTo(threadCount * quotesPerThread));
    }

    [Test]
    public async Task TryAccept_WithMixOfDuplicatesAndUniquesAcrossThreads_AcceptsOnlyUniqueCount()
    {
        var deduplicator = new QuoteDeduplicator();
        var timestamp = DateTimeOffset.UtcNow;

        const int uniqueQuoteCount = 100;
        const int duplicateSubmissionsPerQuote = 20;

        var uniqueQuotes = Enumerable.Range(0, uniqueQuoteCount)
            .Select(i => MakeQuote("Kraken", $"TICK{i}", 100m + i, 5, timestamp))
            .ToList();

        var acceptedPerQuote = new ConcurrentDictionary<int, int>();

        var tasks = new List<Task>();

        for (var q = 0; q < uniqueQuotes.Count; q++)
        {
            var quote = uniqueQuotes[q];
            var quoteIndex = q;

            for (var t = 0; t < duplicateSubmissionsPerQuote; t++)
            {
                tasks.Add(Task.Run(() =>
                {
                    if (deduplicator.TryAccept(quote))
                    {
                        acceptedPerQuote.AddOrUpdate(quoteIndex, 1, (_, count) => count + 1);
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);

        Assert.That(acceptedPerQuote.Count, Is.EqualTo(uniqueQuoteCount));
        Assert.That(acceptedPerQuote.Values, Has.All.EqualTo(1));
    }

    [Test]
    public async Task TryAccept_ConcurrentWithCleanupExpired_DoesNotThrowOrCorruptState()
    {
        var deduplicator = new QuoteDeduplicator();
        using var cts = new CancellationTokenSource();

        var cleanupTask = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                deduplicator.CleanupExpired();
                await Task.Yield();
            }
        });

        var writerTasks = Enumerable.Range(0, 16).Select(threadIndex => Task.Run(() =>
        {
            for (var i = 0; i < 500; i++)
            {
                var quote = MakeQuote(
                    source: $"Exchange{threadIndex}",
                    ticker: "SOLUSDT",
                    price: i,
                    volume: 1,
                    timestamp: DateTimeOffset.UtcNow);

                deduplicator.TryAccept(quote);
            }
        }));

        await Task.WhenAll(writerTasks);
        cts.Cancel();

        Assert.DoesNotThrowAsync(async () => await cleanupTask);
    }
}
