using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using StockFlow.Application.Abstractions;

namespace StockFlow.Infrastructure.Caching;

internal sealed class RedisCacheStore(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<RedisCacheStore> logger) : ICacheStore
{
    private static readonly TimeSpan CacheOperationTimeout = TimeSpan.FromSeconds(1);
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<CacheReadResult> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var value = await _database.StringGetAsync(key).WaitAsync(CacheOperationTimeout, cancellationToken);
            return new CacheReadResult(true, value.HasValue ? value.ToString() : null);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis read failed for cache key {CacheKey}.", key);
            return CacheReadResult.Unavailable;
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Redis read timed out for cache key {CacheKey}.", key);
            return CacheReadResult.Unavailable;
        }
    }

    public async Task<bool> SetAsync(
        string key,
        string value,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _database.StringSetAsync(key, value, timeToLive)
                .WaitAsync(CacheOperationTimeout, cancellationToken);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis write failed for cache key {CacheKey}.", key);
            return false;
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Redis write timed out for cache key {CacheKey}.", key);
            return false;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _database.KeyDeleteAsync(key).WaitAsync(CacheOperationTimeout, cancellationToken);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis delete failed for cache key {CacheKey}.", key);
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Redis delete timed out for cache key {CacheKey}.", key);
        }
    }
}
