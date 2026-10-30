using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.CurrencyRates;
using StockFlow.Application.Identity;
using StockFlow.Application.Inventory;
using StockFlow.Application.Products;
using StockFlow.Application.Warehouses;
using StockFlow.Application.Orders;
using StockFlow.Domain.Products;
using StockFlow.Domain.Inventory;
using StockFlow.Domain.Warehouses;
using StockFlow.Domain.Orders;

namespace StockFlow.Api.ErrorHandling;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var (statusCode, title, detail) = exception switch
        {
            IdentityConflictException => (
                StatusCodes.Status409Conflict,
                "Identity conflict",
                "The email address or organization slug is already registered."),
            IdentityValidationException => (
                StatusCodes.Status400BadRequest,
                "Registration validation failed",
                "The registration details do not meet the account policy."),
            ProductRuleViolationException => (
                StatusCodes.Status400BadRequest,
                "Invalid product",
                exception.Message),
            ProductNotFoundException => (
                StatusCodes.Status404NotFound,
                "Product not found",
                "The product was not found in the current organization."),
            ProductSkuConflictException => (
                StatusCodes.Status409Conflict,
                "Product SKU conflict",
                "A product with this SKU already exists in the current organization."),
            WarehouseRuleViolationException => (
                StatusCodes.Status400BadRequest,
                "Invalid warehouse",
                exception.Message),
            StockAdjustmentRuleViolationException => (
                StatusCodes.Status400BadRequest,
                "Invalid stock adjustment",
                exception.Message),
            WarehouseNotFoundException => (
                StatusCodes.Status404NotFound,
                "Warehouse not found",
                "The warehouse was not found in the current organization."),
            WarehouseCodeConflictException => (
                StatusCodes.Status409Conflict,
                "Warehouse code conflict",
                "A warehouse with this code already exists in the current organization."),
            InsufficientStockException => (
                StatusCodes.Status409Conflict,
                "Insufficient stock",
                "The operation would make available stock negative."),
            StockQuantityOutOfRangeException => (
                StatusCodes.Status409Conflict,
                "Stock quantity out of range",
                "The resulting stock quantity is outside the supported range."),
            OrderRuleViolationException => (
                StatusCodes.Status400BadRequest,
                "Invalid order",
                exception.Message),
            OrderCurrencyMismatchException => (
                StatusCodes.Status400BadRequest,
                "Order currency mismatch",
                "All products in an order must use the same currency."),
            OrderReservationNotDueException => (
                StatusCodes.Status409Conflict,
                "Reservation not expired",
                "The order reservation has not reached its expiration time."),
            OrderInvalidStateException => (
                StatusCodes.Status409Conflict,
                "Invalid order state",
                "The order cannot make the requested state transition."),
            OrderNotFoundException => (
                StatusCodes.Status404NotFound,
                "Order not found",
                "The order was not found in the current organization."),
            OrderIdempotencyConflictException => (
                StatusCodes.Status409Conflict,
                "Idempotency key conflict",
                "This Idempotency-Key was already used with a different order request."),
            OrderInsufficientStockException => (
                StatusCodes.Status409Conflict,
                "Insufficient stock",
                "There is not enough available stock to reserve every order line."),
            OrderProductInactiveException => (
                StatusCodes.Status409Conflict,
                "Product inactive",
                "An inactive product cannot be added to an order."),
            OrderMembershipRequiredException => (
                StatusCodes.Status403Forbidden,
                "Organization membership required",
                "The authenticated user is not a member of the current organization."),
            OrderStockQuantityOutOfRangeException => (
                StatusCodes.Status409Conflict,
                "Reserved stock quantity out of range",
                "The resulting reserved stock quantity is outside the supported range."),
            CurrencyPairNotSupportedException => (
                StatusCodes.Status400BadRequest,
                "Unsupported currency pair",
                "The requested currency pair is not supported."),
            OperationCanceledException => (
                StatusCodes.Status504GatewayTimeout,
                "Upstream service timeout",
                "The currency provider did not respond in time."),
            HttpRequestException => (
                StatusCodes.Status502BadGateway,
                "Upstream service error",
                "The currency provider could not complete the request."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "See the server logs for diagnostic details.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Request failed with status code {StatusCode}.", statusCode);
        }
        else
        {
            logger.LogInformation("Request failed with status code {StatusCode}: {Title}.", statusCode, title);
        }

        httpContext.Response.StatusCode = statusCode;
        await Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail).ExecuteAsync(httpContext);

        return true;
    }
}
