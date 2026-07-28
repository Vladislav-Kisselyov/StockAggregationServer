using System.Text.Json;
using SAS.Domain;
using SAS.Infrastructure.Adapters.Mappers;
using SAS.Infrastructure.Adapters.Models;
using SAS.Infrastructure.Contracts;

namespace SAS.Infrastructure.Adapters;

public class ExchangeAdapterTypeA : IExchangeAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string ExchangeType => nameof(QuoteDtoA);

    public Quote Parse(string json)
    {
        var dto = JsonSerializer.Deserialize<QuoteDtoA>(json, JsonOptions);

        if (dto == null)
            throw new NullReferenceException($"Deserialized {nameof(QuoteDtoA)} object is null.");

        return ExchangeMapper.MapFromA(dto);
    }
}
