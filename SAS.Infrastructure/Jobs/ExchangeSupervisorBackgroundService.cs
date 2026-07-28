using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SAS.Application.Contracts;
using SAS.Infrastructure.Contracts;
using SAS.Infrastructure.Configuration;

namespace SAS.Infrastructure.Jobs;

public sealed class ExchangeSupervisorBackgroundService : BackgroundService
{
    private readonly IWebSocketExchangeClient _client;
    private readonly IQuoteStorage _quoteStorage;
    private readonly AggregatorSettings _settings;

    public ExchangeSupervisorBackgroundService(
        IWebSocketExchangeClient client,
        IQuoteStorage quoteStorage,
        IOptions<AggregatorSettings> settings)
    {
        _client = client;
        _quoteStorage = quoteStorage;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var tasks = _settings.Exchanges
                .Select(exchange => _client.RunAsync(exchange, stoppingToken));

            await Task.WhenAll(tasks);
        }
        finally
        {
            // Гарантия того, что "хранилище" завершит прием котировок и сохранит накопленные
            // даже если сервисы забинжены не в том порядке (т.е. никто не следил за порядком
            // вызова stoppingToken хостом)
            _quoteStorage.Complete();
        }
    }
}
