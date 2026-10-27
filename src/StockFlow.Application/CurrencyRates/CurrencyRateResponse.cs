namespace StockFlow.Application.CurrencyRates;

public sealed record CurrencyRateResponse(
    string BaseCurrency,
    string QuoteCurrency,
    decimal Rate,
    DateOnly RateDate);
