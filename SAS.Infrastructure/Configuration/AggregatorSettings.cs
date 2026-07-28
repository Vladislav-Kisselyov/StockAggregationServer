using SAS.Application.Configuration;

namespace SAS.Infrastructure.Configuration;

public sealed class AggregatorSettings
{
    public List<ExchangeConnectionSettings> Exchanges { get; init; } = [];
}
