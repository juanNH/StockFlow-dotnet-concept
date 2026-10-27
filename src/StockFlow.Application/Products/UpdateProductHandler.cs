using StockFlow.Application.Abstractions;

namespace StockFlow.Application.Products;

public sealed class UpdateProductHandler(
    IProductRepository products,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider)
{
    public async Task<ProductResponse> HandleAsync(
        Guid productId,
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(currentTenant.OrganizationId, productId, cancellationToken)
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
        return ProductResponse.From(product);
    }
}
