using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Validation;
using StockFlow.Application.Inventory;

namespace StockFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/warehouses/{warehouseId:guid}/inventory")]
public sealed class InventoryController(
    AdjustStockHandler adjustStock,
    GetInventoryBalancesHandler getInventoryBalances,
    GetStockMovementsHandler getStockMovements) : ControllerBase
{
    [HttpPost("{productId:guid}/adjustments")]
    [ProducesResponseType(typeof(StockMovementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockMovementResponse>> Adjust(
        [FromRoute] Guid warehouseId,
        [FromRoute] Guid productId,
        [FromBody] StockAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        var movement = await adjustStock.HandleAsync(
            new AdjustStockCommand(warehouseId, productId, request.QuantityDelta, request.Reason),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetMovements),
            new { warehouseId, productId, page = 1, pageSize = 20 },
            movement);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryBalanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<InventoryBalanceResponse>>> GetBalances(
        [FromRoute] Guid warehouseId,
        CancellationToken cancellationToken)
    {
        return Ok(await getInventoryBalances.HandleAsync(warehouseId, cancellationToken));
    }

    [HttpGet("movements")]
    [ProducesResponseType(typeof(StockMovementSearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockMovementSearchResult>> GetMovements(
        [FromRoute] Guid warehouseId,
        [FromQuery] StockMovementSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await getStockMovements.HandleAsync(
            warehouseId,
            new StockMovementSearchOptions(request.ProductId, request.Page, request.PageSize),
            cancellationToken);
        return Ok(result);
    }
}

public sealed class StockAdjustmentRequest
{
    [Required, NotZero]
    public long QuantityDelta { get; init; }

    [Required, StringLength(250, MinimumLength = 1)]
    public string Reason { get; init; } = string.Empty;
}

public sealed class StockMovementSearchRequest
{
    public Guid? ProductId { get; init; }

    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
