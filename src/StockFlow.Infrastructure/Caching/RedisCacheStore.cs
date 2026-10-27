using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using StockFlow.Application.Abstractions;

namespace StockFlow.Infrastructure.Caching;

internal sealed class RedisCacheStore(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<RedisCacheStore> logger) : ICacheStore
{
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var value = await _database.StringGetAsync(key).WaitAsync(cancellationToken);
            return value.HasValue ? value.ToString() : null;
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis read failed for cache key {CacheKey}.", key);
            return null;
        }
    }

    public async Task SetAsync(
        string key,
        string value,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        try
        {
            await _database.StringSetAsync(key, value, timeToLive).WaitAsync(cancellationToken);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis write failed for cache key {CacheKey}.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _database.KeyDeleteAsync(key).WaitAsync(cancellationToken);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis delete failed for cache key {CacheKey}.", key);
        }
    }
}
