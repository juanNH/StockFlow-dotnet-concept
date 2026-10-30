using StockFlow.Application.Abstractions;

namespace StockFlow.Application.Products;

public sealed class UpdateProductHandler(
    IProductRepository products,
    ProductCatalogCache productCatalogCache,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider)
{
    public async Task<ProductResponse> HandleAsync(
        Guid productId,
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        var organizationId = currentTenant.OrganizationId;
        var product = await products.GetByIdAsync(organizationId, productId, cancellationToken)
            ?? throw new ProductNotFoundException();

        product.Update(
            command.Sku,
            command.Name,
            command.Description,
            command.UnitPrice,
            command.Currency,
            command.IsActive,
            timeProvider.GetUtcNow());

        await products.SaveChangesAsync(cancellationToken);
        await productCatalogCache.InvalidateOrganizationAsync(organizationId);
        return ProductResponse.From(product);
    }
}
