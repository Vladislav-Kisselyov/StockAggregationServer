using System.Text.Json;
using SAS.Domain;
using SAS.Infrastructure.Adapters.Mappers;
using SAS.Infrastructure.Adapters.Models;
using SAS.Infrastructure.Contracts;

namespace SAS.Infrastructure.Adapters;

public class ExchangeAdapterTypeB : IExchangeAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string ExchangeType => nameof(QuoteDtoB);

    public Quote Parse(string json)
    {
        var dto = JsonSerializer.Deserialize<QuoteDtoB>(json, JsonOptions);

        if (dto == null)
            throw new NullReferenceException($"Deserialized {nameof(QuoteDtoB)} object is null.");

        return ExchangeMapper.MapFromB(dto);
    }
}
