namespace StockFlow.Application.Products;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    string Currency);

public sealed record UpdateProductCommand(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    string Currency,
    bool IsActive);
