using Microsoft.Extensions.DependencyInjection;
using StockFlow.Application.CurrencyRates;
using StockFlow.Application.Products;

namespace StockFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetCurrencyRateHandler>();
        services.AddScoped<CreateProductHandler>();
        services.AddScoped<GetProductHandler>();
        services.AddScoped<SearchProductsHandler>();
        services.AddScoped<UpdateProductHandler>();
        services.AddScoped<DeactivateProductHandler>();
        return services;
    }
}
