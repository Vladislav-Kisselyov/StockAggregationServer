using SAS.Domain;

namespace SAS.Infrastructure.Contracts;

public interface IExchangeAdapter
{
    string ExchangeType { get; }

    Quote Parse(string json);
}
