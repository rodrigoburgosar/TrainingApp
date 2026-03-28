using System.Text.Json;
using SportFlow.Shared.Exceptions;

namespace SportFlow.API.Middleware;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException nfe => (StatusCodes.Status404NotFound, "Not Found", nfe.Message),
            ForbiddenException fe => (StatusCodes.Status403Forbidden, "Forbidden", fe.Message),
            SportFlow.Shared.Exceptions.ValidationException ve => (
                StatusCodes.Status422UnprocessableEntity,
                "Validation Error",
                string.Join("; ", ve.Errors.Select(e => $"{e.Field}: {e.Message}"))),
            SportFlowException sfe => (StatusCodes.Status400BadRequest, "Bad Request", sfe.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error",
                  "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        var problem = new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title,
            status = statusCode,
            detail,
            instance = context.Request.Path.ToString()
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}
