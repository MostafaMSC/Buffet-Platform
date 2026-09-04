using System.Net;
using System.Text.Json;
using ApplicationExceptions = BuffetDiscovery.Application.Common.Exceptions;

namespace BuffetDiscovery.Api.Middleware;

/// Translates Application-layer exceptions into HTTP responses so controllers stay thin
/// (just dispatch to MediatR and return the result) instead of each one hand-rolling
/// NotFound()/Conflict()/Unauthorized() checks.
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (statusCode, body) = ex switch
        {
            ApplicationExceptions.ValidationException validationEx =>
                (HttpStatusCode.BadRequest, (object)new { message = "Validation failed.", errors = validationEx.Errors }),
            ApplicationExceptions.NotFoundException notFoundEx =>
                (HttpStatusCode.NotFound, (object)new { message = notFoundEx.Message, code = notFoundEx.Code, @params = notFoundEx.Params }),
            ApplicationExceptions.ConflictException conflictEx =>
                (HttpStatusCode.Conflict, new { message = conflictEx.Message, code = conflictEx.Code, @params = conflictEx.Params }),
            ApplicationExceptions.UnauthorizedException unauthorizedEx =>
                (HttpStatusCode.Unauthorized, new { message = unauthorizedEx.Message }),
            _ => (HttpStatusCode.InternalServerError, new { message = "An unexpected error occurred." })
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
