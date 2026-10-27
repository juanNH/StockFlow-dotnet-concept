using StockFlow.Domain.Warehouses;

namespace StockFlow.Application.Warehouses;

public sealed record WarehouseResponse(Guid Id, string Code, string Name, DateTimeOffset CreatedAtUtc)
{
    public static WarehouseResponse From(Warehouse warehouse)
    {
        return new WarehouseResponse(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.CreatedAtUtc);
    }
}
