using StockFlow.Application.Abstractions;

namespace StockFlow.Application.Products;

public sealed class GetProductHandler(IProductRepository products, ICurrentTenant currentTenant)
{
    public async Task<ProductResponse> HandleAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(currentTenant.OrganizationId, productId, cancellationToken)
            ?? throw new ProductNotFoundException();

        return ProductResponse.From(product);
    }
}
