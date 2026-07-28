using Microsoft.Extensions.Logging;
using SAS.Application.Contracts;
using SAS.Domain;

namespace SAS.Application.Services;

public sealed class QuoteProcessor : IQuoteProcessor
{
    private readonly ILogger<QuoteProcessor> _logger;
    private readonly IQuoteDeduplicator _deduplicator;
    private readonly IQuoteStorage _storage;

    public QuoteProcessor(
        ILogger<QuoteProcessor> logger,
        IQuoteDeduplicator deduplicator,
        IQuoteStorage storage)
    {
        _logger = logger;
        _deduplicator = deduplicator;
        _storage = storage;
    }

    public async Task ProcessAsync(
        Quote quote,
        CancellationToken cancellationToken)
    {
        if (!_deduplicator.TryAccept(quote))
        {
            _logger.LogDebug(
                "Duplicate quote ignored: {Source} {Ticker}",
                quote.Source,
                quote.Ticker);

            _storage.IncrementDuplicates();
        }

        /*
        _logger.LogInformation(
            "[{Source}] {Ticker} Price={Price}; Volume={Volume}; Timestamp={Timestamp}",
            quote.Source,
            quote.Ticker,
            quote.Price,
            quote.Volume,
            quote.Timestamp);
        */

        await _storage.EnqueueAsync(
            quote,
            cancellationToken);
    }
}
