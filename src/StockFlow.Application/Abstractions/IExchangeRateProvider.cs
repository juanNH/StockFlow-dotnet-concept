using StockFlow.Application.CurrencyRates;

namespace StockFlow.Application.Abstractions;

public interface IExchangeRateProvider
{
    Task<CurrencyRateResponse> GetRateAsync(
        string baseCurrency,
        string quoteCurrency,
        CancellationToken cancellationToken);
}
