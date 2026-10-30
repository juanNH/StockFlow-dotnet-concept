using StockFlow.Application.Abstractions;
using StockFlow.Domain.Products;

namespace StockFlow.Application.Products;

public sealed class CreateProductHandler(
    IProductRepository products,
    ProductCatalogCache productCatalogCache,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider)
{
    public async Task<ProductResponse> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var organizationId = currentTenant.OrganizationId;
        var product = Product.Create(
            organizationId,
            command.Sku,
            command.Name,
            command.Description,
            command.UnitPrice,
            command.Currency,
            timeProvider.GetUtcNow());

        products.Add(product);
        await products.SaveChangesAsync(cancellationToken);
        await productCatalogCache.InvalidateOrganizationAsync(organizationId);
        return ProductResponse.From(product);
    }
}
