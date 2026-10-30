using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Orders;

namespace StockFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/orders")]
public sealed class OrdersController(
    CreateOrderHandler createOrder,
    GetOrderHandler getOrder,
    ExpireDueReservationsHandler expireDueReservations) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromHeader(Name = "Idempotency-Key"), Required, StringLength(128, MinimumLength = 1),
         RegularExpression("^[A-Za-z0-9._~-]{1,128}$")]
        string idempotencyKey,
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createOrder.HandleAsync(
            new CreateOrderCommand(
                request.WarehouseId,
                request.Lines.Select(line => new OrderLineRequest(line.ProductId, line.Quantity)).ToArray(),
                idempotencyKey),
            cancellationToken);

        Response.Headers["Idempotency-Replayed"] = result.WasReplay ? "true" : "false";
        return CreatedAtAction(nameof(GetById), new { orderId = result.Order.Id }, result.Order);
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken)
    {
        return Ok(await getOrder.HandleAsync(orderId, cancellationToken));
    }

    [HttpPost("expire-due")]
    [ProducesResponseType(typeof(ReservationExpirationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationExpirationResponse>> ExpireDue(
        CancellationToken cancellationToken)
    {
        return Ok(await expireDueReservations.HandleAsync(cancellationToken));
    }
}

public sealed class CreateOrderRequest
{
    [Required]
    public Guid WarehouseId { get; init; }

    [Required, MinLength(1), MaxLength(50)]
    public IReadOnlyList<CreateOrderLineRequest> Lines { get; init; } = [];
}

public sealed class CreateOrderLineRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Range(1, 1_000_000)]
    public long Quantity { get; init; }
}
