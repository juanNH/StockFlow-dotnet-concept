using System.Net;
using System.Net.Http.Json;
using StockFlow.Application.Abstractions;
using StockFlow.Application.CurrencyRates;

namespace StockFlow.Infrastructure.Integrations;

internal sealed class FrankfurterExchangeRateProvider(HttpClient httpClient) : IExchangeRateProvider
{
    public async Task<CurrencyRateResponse> GetRateAsync(
        string baseCurrency,
        string quoteCurrency,
        CancellationToken cancellationToken)
    {
        var path = $"v2/rate/{Uri.EscapeDataString(baseCurrency)}/{Uri.EscapeDataString(quoteCurrency)}";
        FrankfurterRateResponse? response;
        try
        {
            response = await httpClient.GetFromJsonAsync<FrankfurterRateResponse>(path, cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            throw new CurrencyPairNotSupportedException(baseCurrency, quoteCurrency, exception);
        }

        if (response is null)
        {
            throw new HttpRequestException("The currency provider returned an empty response.");
        }

        return new CurrencyRateResponse(response.Base, response.Quote, response.Rate, response.Date);
    }

    private sealed record FrankfurterRateResponse(DateOnly Date, string Base, string Quote, decimal Rate);
}
