namespace StockFlow.Application.Abstractions;

public interface ICacheStore
{
    Task<CacheReadResult> GetAsync(string key, CancellationToken cancellationToken);

    Task<bool> SetAsync(string key, string value, TimeSpan timeToLive, CancellationToken cancellationToken);

    Task RemoveAsync(string key, CancellationToken cancellationToken);
}

public readonly record struct CacheReadResult(bool IsAvailable, string? Value)
{
    public static CacheReadResult Unavailable => new(false, null);
}
