using EventRegistration.Api.Exceptions;

namespace EventRegistration.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, message, errors) = ex switch
        {
            NotFoundException => (404, ex.Message, Array.Empty<string>()),
            ValidationException v => (400, v.Message, v.Errors.ToArray()),
            DuplicateResourceException or BusinessException => (409, ex.Message, Array.Empty<string>()),
            _ => (500, "An unexpected error occurred.", Array.Empty<string>())
        };

        if (status == 500)
            _logger.LogError(ex, "Unhandled exception");

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            timestamp = DateTime.UtcNow,
            message,
            errors
        });
    }
}