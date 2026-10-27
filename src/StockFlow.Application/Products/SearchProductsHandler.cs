using StockFlow.Application.Abstractions;

namespace StockFlow.Application.Products;

public sealed class SearchProductsHandler(IProductRepository products, ICurrentTenant currentTenant)
{
    public async Task<ProductSearchResult> HandleAsync(
        ProductSearchOptions options,
        CancellationToken cancellationToken)
    {
        var page = await products.SearchAsync(currentTenant.OrganizationId, options, cancellationToken);
        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling((double)page.TotalCount / options.PageSize);

        return new ProductSearchResult(
            page.Items.Select(ProductResponse.From).ToArray(),
            options.Page,
            options.PageSize,
            page.TotalCount,
            totalPages);
    }
}
