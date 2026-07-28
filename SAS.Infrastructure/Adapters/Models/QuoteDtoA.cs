namespace SAS.Infrastructure.Adapters.Models;

public class QuoteDtoA
{
    public required string Source { get; init; }
    public required string Ticker { get; init; }
    public decimal Price { get; init; }
    public decimal Volume { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
