using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Validation;
using StockFlow.Application.Products;

namespace StockFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/products")]
public sealed class ProductsController(
    CreateProductHandler createProduct,
    GetProductHandler getProduct,
    SearchProductsHandler searchProducts,
    UpdateProductHandler updateProduct,
    DeactivateProductHandler deactivateProduct) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await createProduct.HandleAsync(
            new CreateProductCommand(
                request.Sku,
                request.Name,
                request.Description,
                request.UnitPrice,
                request.Currency),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { productId = product.Id }, product);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ProductSearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductSearchResult>> Search(
        [FromQuery] ProductSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await searchProducts.HandleAsync(
            new ProductSearchOptions(
                request.Search,
                request.Page,
                request.PageSize,
                request.SortBy,
                request.SortDirection,
                request.IncludeInactive),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{productId:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(
        [FromRoute] Guid productId,
        CancellationToken cancellationToken)
    {
        return Ok(await getProduct.HandleAsync(productId, cancellationToken));
    }

    [HttpPut("{productId:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Update(
        [FromRoute] Guid productId,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await updateProduct.HandleAsync(
            productId,
            new UpdateProductCommand(
                request.Sku,
                request.Name,
                request.Description,
                request.UnitPrice,
                request.Currency,
                request.IsActive),
            cancellationToken);

        return Ok(product);
    }

    [HttpDelete("{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        [FromRoute] Guid productId,
        CancellationToken cancellationToken)
    {
        await deactivateProduct.HandleAsync(productId, cancellationToken);
        return NoContent();
    }
}

public sealed class CreateProductRequest
{
    [Required, StringLength(64), RegularExpression("^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$")]
    public string Sku { get; init; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [PositiveDecimal]
    public decimal UnitPrice { get; init; }

    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public string Currency { get; init; } = string.Empty;
}

public sealed class UpdateProductRequest
{
    [Required, StringLength(64), RegularExpression("^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$")]
    public string Sku { get; init; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [PositiveDecimal]
    public decimal UnitPrice { get; init; }

    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public string Currency { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

public sealed class ProductSearchRequest
{
    [StringLength(100)]
    public string? Search { get; init; }

    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [RegularExpression("^(name|sku|unitPrice|createdAtUtc)$")]
    public string SortBy { get; init; } = "name";

    [RegularExpression("^(asc|desc)$")]
    public string SortDirection { get; init; } = "asc";

    public bool IncludeInactive { get; init; }
}
