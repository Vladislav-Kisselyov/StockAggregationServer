using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using SAS.Application.Configuration;
using SAS.Application.Contracts;
using SAS.Infrastructure.Contracts;

namespace SAS.Infrastructure.WebSockets;

public sealed class WebSocketExchangeClient : IWebSocketExchangeClient
{
    private readonly IEnumerable<IExchangeAdapter> _adapters;
    private readonly IQuoteProcessor _processor;
    private readonly ILogger<WebSocketExchangeClient> _logger;

    public WebSocketExchangeClient(
        IEnumerable<IExchangeAdapter> adapters,
        IQuoteProcessor processor,
        ILogger<WebSocketExchangeClient> logger)
    {
        _adapters = adapters;
        _processor = processor;
        _logger = logger;
    }

    public async Task RunAsync(
        ExchangeConnectionSettings settings,
        CancellationToken cancellationToken)
    {
        var adapter = _adapters.Single(x =>
            x.ExchangeType == settings.ExchangeType);

        var backoff = TimeSpan.FromSeconds(1);

        while (!cancellationToken.IsCancellationRequested)
        {
            using var socket = new ClientWebSocket();

            try
            {
                _logger.LogInformation(
                    "Connecting to {Exchange} ({Url})",
                    settings.Name,
                    settings.Url);

                await socket.ConnectAsync(
                    new Uri(settings.Url),
                    cancellationToken);

                _logger.LogInformation(
                    "Connected to {Exchange}",
                    settings.Name);

                backoff = TimeSpan.FromSeconds(1);

                await ReceiveLoop(
                    socket,
                    adapter,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Connection to {Exchange} lost",
                    settings.Name);
            }

            _logger.LogInformation(
                "Reconnect in {Delay}s",
                backoff.TotalSeconds);

            await Task.Delay(backoff, cancellationToken);

            backoff = TimeSpan.FromSeconds(
                Math.Min(backoff.TotalSeconds * 2, 30));
        }
    }

    private async Task ReceiveLoop(
        ClientWebSocket socket,
        IExchangeAdapter adapter,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        while (socket.State == WebSocketState.Open &&
               !cancellationToken.IsCancellationRequested)
        {
            using var ms = new MemoryStream();

            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(
                    buffer,
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                    return;

                ms.Write(buffer, 0, result.Count);

            } while (!result.EndOfMessage);

            var json = Encoding.UTF8.GetString(ms.ToArray());

            var quote = adapter.Parse(json);

            await _processor.ProcessAsync(
                quote,
                cancellationToken);
        }
    }
}
