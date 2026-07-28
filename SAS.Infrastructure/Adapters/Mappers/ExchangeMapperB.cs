using System.Globalization;
using Riok.Mapperly.Abstractions;
using SAS.Domain;
using SAS.Infrastructure.Adapters.Models;

namespace SAS.Infrastructure.Adapters.Mappers;

public partial class ExchangeMapper
{
    [MapProperty(nameof(QuoteDtoB.Symbol), nameof(Quote.Ticker))]
    [MapProperty(nameof(QuoteDtoB.P), nameof(Quote.Price))]
    [MapProperty(nameof(QuoteDtoB.V), nameof(Quote.Volume))]
    [MapProperty(nameof(QuoteDtoB.Time), nameof(Quote.Timestamp))]
    public static partial Quote MapFromB(QuoteDtoB quote);

    private static decimal MapPrice(string price)
        => decimal.Parse(price, CultureInfo.InvariantCulture);

    private static DateTimeOffset MapTime(long timestamp)
        => DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
}
