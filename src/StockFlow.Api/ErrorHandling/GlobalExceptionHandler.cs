using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.CurrencyRates;
using StockFlow.Application.Identity;

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
