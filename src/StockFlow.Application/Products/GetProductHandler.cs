using StockFlow.Application.Abstractions;

namespace StockFlow.Application.Products;

public sealed class GetProductHandler(
    IProductRepository products,
    ProductCatalogCache productCatalogCache,
    ICurrentTenant currentTenant)
{
    public async Task<ProductResponse> HandleAsync(Guid productId, CancellationToken cancellationToken)
    {
        var organizationId = currentTenant.OrganizationId;
        var cacheContext = await productCatalogCache.GetContextAsync(organizationId, cancellationToken);
        var cachedProduct = await productCatalogCache.GetProductAsync(cacheContext, productId, cancellationToken);
        if (cachedProduct is not null)
        {
            return cachedProduct;
        }

        var product = await products.GetByIdAsync(organizationId, productId, cancellationToken)
            ?? throw new ProductNotFoundException();

        var response = ProductResponse.From(product);
        await productCatalogCache.SetProductAsync(cacheContext, response, cancellationToken);
        return response;
    }
}
