using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.CurrencyRates;
using StockFlow.Application.Identity;
using StockFlow.Application.Inventory;
using StockFlow.Application.Products;
using StockFlow.Application.Warehouses;
using StockFlow.Domain.Products;
using StockFlow.Domain.Inventory;
using StockFlow.Domain.Warehouses;

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
                "The adjustment would result in a negative stock balance."),
            StockQuantityOutOfRangeException => (
                StatusCodes.Status409Conflict,
                "Stock quantity out of range",
                "The resulting stock quantity is outside the supported range."),
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
