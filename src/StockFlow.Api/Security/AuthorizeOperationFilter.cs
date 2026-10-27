using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StockFlow.Api.Security;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodAttributes = context.MethodInfo.GetCustomAttributes(inherit: true);
        var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true)
            ?? Array.Empty<object>();
        var hasAuthorize = methodAttributes.Concat(controllerAttributes).OfType<AuthorizeAttribute>().Any();
        var hasAllowAnonymous = methodAttributes.Concat(controllerAttributes).OfType<AllowAnonymousAttribute>().Any();

        if (!hasAuthorize || hasAllowAnonymous)
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            }
        ];
    }
}
