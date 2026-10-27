using StockFlow.Application.Abstractions;

namespace StockFlow.Application.Products;

public sealed class DeactivateProductHandler(
    IProductRepository products,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider)
{
    public async Task HandleAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(currentTenant.OrganizationId, productId, cancellationToken)
            ?? throw new ProductNotFoundException();

        product.Deactivate(timeProvider.GetUtcNow());
        await products.SaveChangesAsync(cancellationToken);
    }
}
