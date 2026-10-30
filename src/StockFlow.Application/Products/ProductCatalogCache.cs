using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockFlow.Application.Abstractions;

namespace StockFlow.Application.Products;

public readonly record struct ProductCatalogCacheContext(
    Guid OrganizationId,
    string Generation,
    bool IsCacheAvailable,
    bool IsNewGeneration);

public sealed class ProductCatalogCache(
    ICacheStore cacheStore,
    ILogger<ProductCatalogCache> logger)
{
    private static readonly TimeSpan ProductTimeToLive = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SearchTimeToLive = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan GenerationTimeToLive = TimeSpan.FromDays(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string CacheRoot = "stockflow:catalog:v1";

    public async Task<ProductCatalogCacheContext> GetContextAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var versionKey = GetGenerationKey(organizationId);
        var generationRead = await cacheStore.GetAsync(versionKey, cancellationToken);
        if (!generationRead.IsAvailable)
        {
            return new ProductCatalogCacheContext(
                organizationId,
                Guid.NewGuid().ToString("N"),
                IsCacheAvailable: false,
                IsNewGeneration: true);
        }

        var generation = generationRead.Value;
        if (Guid.TryParseExact(generation, "N", out _))
        {
            return new ProductCatalogCacheContext(organizationId, generation, IsCacheAvailable: true, IsNewGeneration: false);
        }

        generation = Guid.NewGuid().ToString("N");
        var generationStored = await cacheStore.SetAsync(versionKey, generation, GenerationTimeToLive, cancellationToken);
        return new ProductCatalogCacheContext(organizationId, generation, generationStored, IsNewGeneration: true);
    }

    public async Task<ProductResponse?> GetProductAsync(
        ProductCatalogCacheContext context,
        Guid productId,
        CancellationToken cancellationToken)
    {
        if (!context.IsCacheAvailable || context.IsNewGeneration)
        {
            LogCacheResult(cacheHit: false, "product detail", context.OrganizationId);
            return null;
        }

        var cacheKey = GetProductKey(context, productId);
        var response = await ReadAsync<ProductResponse>(cacheKey, cancellationToken);
        LogCacheResult(response is not null, "product detail", context.OrganizationId);
        return response;
    }

    public Task SetProductAsync(
        ProductCatalogCacheContext context,
        ProductResponse response,
        CancellationToken cancellationToken)
    {
        if (!context.IsCacheAvailable)
        {
            return Task.CompletedTask;
        }

        return WriteAsync(GetProductKey(context, response.Id), response, ProductTimeToLive, cancellationToken);
    }

    public async Task<ProductSearchResult?> GetSearchAsync(
        ProductCatalogCacheContext context,
        ProductSearchOptions options,
        CancellationToken cancellationToken)
    {
        if (!context.IsCacheAvailable || context.IsNewGeneration)
        {
            LogCacheResult(cacheHit: false, "product search", context.OrganizationId);
            return null;
        }

        var cacheKey = GetSearchKey(context, options);
        var response = await ReadAsync<ProductSearchResult>(cacheKey, cancellationToken);
        LogCacheResult(response is not null, "product search", context.OrganizationId);
        return response;
    }

    public Task SetSearchAsync(
        ProductCatalogCacheContext context,
        ProductSearchOptions options,
        ProductSearchResult response,
        CancellationToken cancellationToken)
    {
        if (!context.IsCacheAvailable)
        {
            return Task.CompletedTask;
        }

        return WriteAsync(GetSearchKey(context, options), response, SearchTimeToLive, cancellationToken);
    }

    public async Task InvalidateOrganizationAsync(Guid organizationId)
    {
        var generation = Guid.NewGuid().ToString("N");
        var generationStored = await cacheStore.SetAsync(
            GetGenerationKey(organizationId),
            generation,
            GenerationTimeToLive,
            CancellationToken.None);
        logger.LogInformation(
            "Product catalog cache invalidation attempted for organization {OrganizationId}; generation stored: {GenerationStored}.",
            organizationId,
            generationStored);
    }

    private async Task<T?> ReadAsync<T>(string cacheKey, CancellationToken cancellationToken)
        where T : class
    {
        var cacheRead = await cacheStore.GetAsync(cacheKey, cancellationToken);
        if (!cacheRead.IsAvailable)
        {
            return null;
        }

        var payload = cacheRead.Value;
        if (payload is null)
        {
            return null;
        }

        try
        {
            var response = JsonSerializer.Deserialize<T>(payload, JsonOptions);
            if (response is not null)
            {
                return response;
            }
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Invalid product catalog cache payload for key {CacheKey}.", cacheKey);
        }

        await cacheStore.RemoveAsync(cacheKey, cancellationToken);
        return null;
    }

    private Task WriteAsync<T>(
        string cacheKey,
        T response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        return cacheStore.SetAsync(
            cacheKey,
            JsonSerializer.Serialize(response, JsonOptions),
            timeToLive,
            cancellationToken);
    }

    private void LogCacheResult(bool cacheHit, string cacheKind, Guid organizationId)
    {
        if (cacheHit)
        {
            logger.LogInformation(
                "Product catalog cache hit for {CacheKind} in organization {OrganizationId}.",
                cacheKind,
                organizationId);
        }
        else
        {
            logger.LogInformation(
                "Product catalog cache miss for {CacheKind} in organization {OrganizationId}.",
                cacheKind,
                organizationId);
        }
    }

    private static string GetGenerationKey(Guid organizationId)
    {
        return $"{CacheRoot}:{organizationId:N}:generation";
    }

    private static string GetProductKey(ProductCatalogCacheContext context, Guid productId)
    {
        return $"{CacheRoot}:{context.OrganizationId:N}:g:{context.Generation}:product:{productId:N}";
    }

    private static string GetSearchKey(ProductCatalogCacheContext context, ProductSearchOptions options)
    {
        var canonicalOptions = JsonSerializer.Serialize(new
        {
            Search = string.IsNullOrWhiteSpace(options.Search) ? null : options.Search.Trim(),
            options.Page,
            options.PageSize,
            SortBy = options.SortBy.ToLowerInvariant(),
            SortDirection = options.SortDirection.ToLowerInvariant(),
            options.IncludeInactive
        });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalOptions)));
        return $"{CacheRoot}:{context.OrganizationId:N}:g:{context.Generation}:search:{hash}";
    }
}
