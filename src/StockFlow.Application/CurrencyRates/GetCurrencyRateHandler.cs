using System.Text.Json;
using StockFlow.Application.Abstractions;

namespace StockFlow.Application.CurrencyRates;

public sealed class GetCurrencyRateHandler(
    IExchangeRateProvider exchangeRateProvider,
    ICacheStore cacheStore)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CurrencyRateResponse> HandleAsync(
        string baseCurrency,
        string quoteCurrency,
        CancellationToken cancellationToken)
    {
        var normalizedBase = baseCurrency.Trim().ToUpperInvariant();
        var normalizedQuote = quoteCurrency.Trim().ToUpperInvariant();
        var cacheKey = $"currency-rate:v1:{normalizedBase}:{normalizedQuote}";
        var cacheRead = await cacheStore.GetAsync(cacheKey, cancellationToken);

        if (cacheRead.Value is not null)
        {
            try
            {
                var cachedRate = JsonSerializer.Deserialize<CurrencyRateResponse>(cacheRead.Value, JsonOptions);
                if (cachedRate is not null)
                {
                    return cachedRate;
                }
            }
            catch (JsonException)
            {
                await cacheStore.RemoveAsync(cacheKey, cancellationToken);
            }
        }

        var rate = await exchangeRateProvider.GetRateAsync(
            normalizedBase,
            normalizedQuote,
            cancellationToken);

        await cacheStore.SetAsync(
            cacheKey,
            JsonSerializer.Serialize(rate, JsonOptions),
            CacheDuration,
            cancellationToken);

        return rate;
    }
}
