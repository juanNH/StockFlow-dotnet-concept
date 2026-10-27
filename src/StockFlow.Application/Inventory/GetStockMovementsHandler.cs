using StockFlow.Application.Abstractions;
using StockFlow.Application.Products;
using StockFlow.Application.Warehouses;

namespace StockFlow.Application.Inventory;

public sealed class GetStockMovementsHandler(
    IInventoryRepository inventory,
    IWarehouseRepository warehouses,
    IProductRepository products,
    ICurrentTenant currentTenant)
{
    public async Task<StockMovementSearchResult> HandleAsync(
        Guid warehouseId,
        StockMovementSearchOptions options,
        CancellationToken cancellationToken)
    {
        var organizationId = currentTenant.OrganizationId;
        var warehouse = await warehouses.GetByIdAsync(organizationId, warehouseId, cancellationToken);
        if (warehouse is null)
        {
            throw new WarehouseNotFoundException();
        }

        if (options.ProductId is { } productId
            && await products.GetByIdAsync(organizationId, productId, cancellationToken) is null)
        {
            throw new ProductNotFoundException();
        }

        var page = await inventory.GetMovementsAsync(organizationId, warehouseId, options, cancellationToken);
        var totalPages = page.TotalCount == 0
            ? 0
            : (long)Math.Ceiling((double)page.TotalCount / options.PageSize);

        return new StockMovementSearchResult(
            page.Items.Select(StockMovementResponse.From).ToArray(),
            options.Page,
            options.PageSize,
            page.TotalCount,
            totalPages);
    }
}
