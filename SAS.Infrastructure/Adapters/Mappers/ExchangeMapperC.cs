using System.Globalization;
using Riok.Mapperly.Abstractions;
using SAS.Domain;
using SAS.Infrastructure.Adapters.Models;

namespace SAS.Infrastructure.Adapters.Mappers;

public partial class ExchangeMapper
{
    [MapProperty(nameof(QuoteDtoC.Instrument), nameof(Quote.Ticker))]
    [MapProperty(nameof(QuoteDtoC.Last), nameof(Quote.Price))]
    [MapProperty(nameof(QuoteDtoC.Qty), nameof(Quote.Volume))]
    [MapProperty(nameof(QuoteDtoC.Ts ), nameof(Quote.Timestamp))]
    public static partial Quote MapFromC(QuoteDtoC quote);

    private static long MapToVolume(string volume)
        => long.Parse(volume, CultureInfo.InvariantCulture);
}
