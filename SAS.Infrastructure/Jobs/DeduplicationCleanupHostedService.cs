using Microsoft.Extensions.Hosting;
using SAS.Application.Contracts;

namespace SAS.Infrastructure.Jobs;

public sealed class DeduplicationCleanupBackgroundService : BackgroundService
{
    private readonly IQuoteDeduplicator _deduplicator;

    public DeduplicationCleanupBackgroundService(
        IQuoteDeduplicator deduplicator)
    {
        _deduplicator = deduplicator;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _deduplicator.CleanupExpired();
        }
    }
}
