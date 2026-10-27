using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Warehouses;

namespace StockFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/warehouses")]
public sealed class WarehousesController(
    CreateWarehouseHandler createWarehouse,
    GetWarehouseHandler getWarehouse,
    ListWarehousesHandler listWarehouses) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(WarehouseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WarehouseResponse>> Create(
        [FromBody] CreateWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        var warehouse = await createWarehouse.HandleAsync(
            new CreateWarehouseCommand(request.Code, request.Name),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { warehouseId = warehouse.Id }, warehouse);
    }

    [HttpGet("{warehouseId:guid}")]
    [ProducesResponseType(typeof(WarehouseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WarehouseResponse>> GetById(
        [FromRoute] Guid warehouseId,
        CancellationToken cancellationToken)
    {
        return Ok(await getWarehouse.HandleAsync(warehouseId, cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WarehouseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WarehouseResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await listWarehouses.HandleAsync(cancellationToken));
    }
}

public sealed class CreateWarehouseRequest
{
    [Required, StringLength(32), RegularExpression("^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$")]
    public string Code { get; init; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;
}
