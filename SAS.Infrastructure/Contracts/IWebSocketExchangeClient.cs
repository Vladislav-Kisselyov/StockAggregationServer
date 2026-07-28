using SAS.Application.Configuration;

namespace SAS.Infrastructure.Contracts;

public interface IWebSocketExchangeClient
{
    Task RunAsync(
        ExchangeConnectionSettings settings,
        CancellationToken cancellationToken);
}
