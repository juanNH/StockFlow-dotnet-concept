using StockFlow.Domain.Products;

namespace StockFlow.Application.Products;

public sealed record ProductResponse(
    Guid Id,
    Guid OrganizationId,
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    string Currency,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static ProductResponse From(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.OrganizationId,
            product.Sku,
            product.Name,
            product.Description,
            product.UnitPrice,
            product.Currency,
            product.IsActive,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);
    }
}

public sealed record ProductSearchResult(
    IReadOnlyList<ProductResponse> Items,
    int Page,
    int PageSize,
    long TotalCount,
    int TotalPages);
