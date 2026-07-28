namespace SAS.Application.Configuration;

public sealed class StorageSettings
{
    public int BatchSize { get; init; } = 100;

    public TimeSpan FlushInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    public int MaxRetryCount { get; init; } = 5;
}
