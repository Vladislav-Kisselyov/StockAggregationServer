using Riok.Mapperly.Abstractions;
using SAS.Domain;
using SAS.Infrastructure.Adapters.Models;

namespace SAS.Infrastructure.Adapters.Mappers;

[Mapper]
public partial class ExchangeMapper
{
    public static partial Quote MapFromA(QuoteDtoA quote);
}
