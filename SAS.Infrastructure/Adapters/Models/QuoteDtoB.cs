namespace SAS.Infrastructure.Adapters.Models;

public class QuoteDtoB
{
    public required string Source { get; init; }
    public required string Symbol { get; init; }
    public required string P { get; init; }
    public decimal V { get; init; }
    public long Time { get; init; }
}
