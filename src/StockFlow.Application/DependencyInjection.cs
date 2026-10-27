using Microsoft.Extensions.DependencyInjection;
using StockFlow.Application.CurrencyRates;

namespace StockFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetCurrencyRateHandler>();
        return services;
    }
}
