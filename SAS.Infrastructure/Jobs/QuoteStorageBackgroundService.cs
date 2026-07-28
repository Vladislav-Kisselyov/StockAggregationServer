using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SAS.Application.Configuration;
using SAS.Application.Contracts;
using SAS.Domain;
using SAS.Infrastructure.Persistence;

namespace SAS.Infrastructure.Jobs;

public sealed class QuoteStorageBackgroundService
    : BackgroundService, IQuoteStorage
{
    private readonly Channel<Quote> _channel;
    private readonly StorageSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ILogger<QuoteStorageBackgroundService> _logger;

    private long _totalSaved;
    private long _totalDropped;
    private long _totalDuplicates;

    public long TotalSaved => Interlocked.Read(ref _totalSaved);
    public long TotalDropped => Interlocked.Read(ref _totalDropped);
    public long TotalDuplicates => Interlocked.Read(ref _totalDuplicates);

    public int LastBatchSize { get; private set;}
    public TimeSpan LastBatchPause { get; private set;}

    private DateTime _lastBatchSaveTryAt = DateTime.UtcNow;

    public QuoteStorageBackgroundService(
        IOptions<StorageSettings> settings,
        ILogger<QuoteStorageBackgroundService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _settings = settings.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;

        _channel = Channel.CreateBounded<Quote>(
            new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
    }

    public void IncrementDuplicates()
    {
        Interlocked.Increment(ref _totalDuplicates);
    }

    public void Complete()
    {
        _channel.Writer.Complete();
    }

    public ValueTask EnqueueAsync(
        Quote quote,
        CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(
            quote,
            cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _channel.Reader;

        // Специально игнорируем stoppingToken. Channel завершается
        // (через Complete()). Использование stoppingToken тут не гарантирует
        // что хост отменит его раньше чем закроется WS соединение,
        // т.к. если фактор человеческой ошибки, что BackgroundService забинжены
        // в неверном порядке.
        while (await reader.WaitToReadAsync(CancellationToken.None))
        {
            var batch = new List<Quote>(_settings.BatchSize);

            DrainChannel(reader, batch);

            if (batch.Count == 0)
            {
                continue;
            }

            if (batch.Count < _settings.BatchSize && !reader.Completion.IsCompleted)
            {
                using var timeoutCts = new CancellationTokenSource(_settings.FlushInterval);

                try
                {
                    while (batch.Count < _settings.BatchSize &&
                           await reader.WaitToReadAsync(timeoutCts.Token))
                    {
                        DrainChannel(reader, batch);
                    }
                }
                catch (OperationCanceledException)
                {
                    // отмена по FlushInterval таймауту
                }
            }

            // CancellationToken.None тут намеренно для Graceful Shutdown
            // При завершении приложения накопленные котировки должны быть слиты в базу.
            await SaveBatchWithRetryAsync(batch, CancellationToken.None);
        }

        _logger.LogInformation(
            "Quote channel completed and drained. Storage service shutting down cleanly. " +
            "Saved={Saved} Dropped={Dropped} Duplicates={Duplicates}",
            TotalSaved,
            TotalDropped,
            TotalDuplicates);
    }

    private void DrainChannel(
        ChannelReader<Quote> reader,
        List<Quote> batch)
    {
        while (batch.Count < _settings.BatchSize &&
               reader.TryRead(out var quote))
        {
            batch.Add(quote);
        }
    }

    private async Task SaveBatchWithRetryAsync(
        List<Quote> batch,
        CancellationToken cancellationToken)
    {
        var retry = 0;

        LastBatchSize = batch.Count;
        LastBatchPause = DateTime.UtcNow - _lastBatchSaveTryAt;

        _lastBatchSaveTryAt = DateTime.UtcNow;

        while (true)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await db.Quotes.AddRangeAsync(batch, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);

                Interlocked.Add(ref _totalSaved, batch.Count);

                _logger.LogInformation(
                    "Saved batch of {Count} quotes",
                    batch.Count);

                return;
            }
            catch (Exception ex)
            {
                retry++;

                _logger.LogError(
                    ex,
                    "Database write failed ({Retry}/{MaxRetry})",
                    retry,
                    _settings.MaxRetryCount);

                if (retry >= _settings.MaxRetryCount)
                {
                    Interlocked.Add(ref _totalDropped, batch.Count);

                    _logger.LogCritical(
                        "Dropping batch of {Count} quotes",
                        batch.Count);

                    return;
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(Math.Pow(2, retry)),
                    cancellationToken);
            }
        }
    }
}
