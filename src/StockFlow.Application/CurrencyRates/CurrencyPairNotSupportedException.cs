namespace StockFlow.Application.CurrencyRates;

public sealed class CurrencyPairNotSupportedException(
    string baseCurrency,
    string quoteCurrency,
    Exception? innerException = null)
    : Exception($"The currency pair {baseCurrency}/{quoteCurrency} is not supported.", innerException)
{
}
