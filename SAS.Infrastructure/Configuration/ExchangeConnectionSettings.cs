namespace SAS.Application.Configuration;

public sealed class ExchangeConnectionSettings
{
    public required string Name { get; init; }

    public required string Url { get; init; }

    public required string ExchangeType { get; init; }
}
