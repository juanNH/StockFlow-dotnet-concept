using StockFlow.Application.Abstractions;
using StockFlow.Domain.Products;

namespace StockFlow.Application.Products;

public sealed class CreateProductHandler(
    IProductRepository products,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider)
{
    public async Task<ProductResponse> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = Product.Create(
            currentTenant.OrganizationId,
            command.Sku,
            command.Name,
            command.Description,
            command.UnitPrice,
            command.Currency,
            timeProvider.GetUtcNow());

        products.Add(product);
        await products.SaveChangesAsync(cancellationToken);
        return ProductResponse.From(product);
    }
}
