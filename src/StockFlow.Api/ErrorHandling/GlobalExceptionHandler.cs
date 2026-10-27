using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace StockFlow.Api.ErrorHandling;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var isUpstreamFailure = exception is HttpRequestException;
        var statusCode = isUpstreamFailure
            ? StatusCodes.Status502BadGateway
            : StatusCodes.Status500InternalServerError;
        var title = isUpstreamFailure ? "Upstream service error" : "An unexpected error occurred";

        logger.LogError(exception, "Request failed with status code {StatusCode}.", statusCode);

        httpContext.Response.StatusCode = statusCode;
        await Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: "See the server logs for diagnostic details.").ExecuteAsync(httpContext);

        return true;
    }
}
