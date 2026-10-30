using StockFlow.Application.Abstractions;

namespace StockFlow.Application.Products;

public sealed class DeactivateProductHandler(
    IProductRepository products,
    ProductCatalogCache productCatalogCache,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider)
{
    public async Task HandleAsync(Guid productId, CancellationToken cancellationToken)
    {
        var organizationId = currentTenant.OrganizationId;
        var product = await products.GetByIdAsync(organizationId, productId, cancellationToken)
            ?? throw new ProductNotFoundException();

        product.Deactivate(timeProvider.GetUtcNow());
        await products.SaveChangesAsync(cancellationToken);
        await productCatalogCache.InvalidateOrganizationAsync(organizationId);
    }
}
