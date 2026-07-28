namespace SAS.Infrastructure.Adapters.Models;

public class QuoteDtoC
{
    public required string Source { get; init; }
    public required string Instrument { get; init; }
    public decimal Last { get; init; }
    public required string Qty { get; init; }
    public long Ts { get; init; }
}
