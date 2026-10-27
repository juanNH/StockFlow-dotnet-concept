using StockFlow.Domain.Products;

namespace StockFlow.Application.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid organizationId, Guid productId, CancellationToken cancellationToken);

    Task<ProductPage> SearchAsync(
        Guid organizationId,
        ProductSearchOptions options,
        CancellationToken cancellationToken);

    void Add(Product product);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record ProductPage(IReadOnlyList<Product> Items, long TotalCount);

public sealed record ProductSearchOptions(
    string? Search,
    int Page,
    int PageSize,
    string SortBy,
    string SortDirection,
    bool IncludeInactive);
