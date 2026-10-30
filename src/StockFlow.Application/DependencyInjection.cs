using Microsoft.Extensions.DependencyInjection;
using StockFlow.Application.CurrencyRates;
using StockFlow.Application.Inventory;
using StockFlow.Application.Products;
using StockFlow.Application.Warehouses;
using StockFlow.Application.Orders;

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
        services.AddScoped<ProductCatalogCache>();
        services.AddScoped<CreateWarehouseHandler>();
        services.AddScoped<GetWarehouseHandler>();
        services.AddScoped<ListWarehousesHandler>();
        services.AddScoped<AdjustStockHandler>();
        services.AddScoped<GetInventoryBalancesHandler>();
        services.AddScoped<GetStockMovementsHandler>();
        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<GetOrderHandler>();
        services.AddScoped<ExpireDueReservationsHandler>();
        return services;
    }
}
