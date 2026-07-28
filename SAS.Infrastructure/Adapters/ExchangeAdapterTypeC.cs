using System.Text.Json;
using SAS.Domain;
using SAS.Infrastructure.Adapters.Mappers;
using SAS.Infrastructure.Adapters.Models;
using SAS.Infrastructure.Contracts;

namespace SAS.Infrastructure.Adapters;

public class ExchangeAdapterTypeC : IExchangeAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string ExchangeType => nameof(QuoteDtoC);

    public Quote Parse(string json)
    {
        var dto = JsonSerializer.Deserialize<QuoteDtoC>(json, JsonOptions);

        if (dto == null)
            throw new NullReferenceException($"Deserialized {nameof(QuoteDtoC)} object is null.");

        return ExchangeMapper.MapFromC(dto);
    }
}
