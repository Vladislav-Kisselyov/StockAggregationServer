namespace SAS.Domain;

public sealed class Quote
{
    public Guid Id { get; set; }
    public required string Source { get; set; }
    public required string Ticker { get; set; }
    public required decimal Price { get; set; }
    public required long Volume { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}
