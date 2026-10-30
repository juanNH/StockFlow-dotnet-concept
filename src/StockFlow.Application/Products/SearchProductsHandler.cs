using StockFlow.Application.Abstractions;

namespace StockFlow.Application.Products;

public sealed class SearchProductsHandler(
    IProductRepository products,
    ProductCatalogCache productCatalogCache,
    ICurrentTenant currentTenant)
{
    public async Task<ProductSearchResult> HandleAsync(
        ProductSearchOptions options,
        CancellationToken cancellationToken)
    {
        var organizationId = currentTenant.OrganizationId;
        var cacheContext = await productCatalogCache.GetContextAsync(organizationId, cancellationToken);
        var cachedResult = await productCatalogCache.GetSearchAsync(cacheContext, options, cancellationToken);
        if (cachedResult is not null)
        {
            return cachedResult;
        }

        var page = await products.SearchAsync(organizationId, options, cancellationToken);
        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling((double)page.TotalCount / options.PageSize);

        var response = new ProductSearchResult(
            page.Items.Select(ProductResponse.From).ToArray(),
            options.Page,
            options.PageSize,
            page.TotalCount,
            totalPages);
        await productCatalogCache.SetSearchAsync(cacheContext, options, response, cancellationToken);
        return response;
    }
}
