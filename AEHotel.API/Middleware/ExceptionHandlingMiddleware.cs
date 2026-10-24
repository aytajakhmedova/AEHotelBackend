using AEHotel.Service.Exceptions;

namespace AEHotel.API.Middleware;

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
        catch (NotFoundException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, exception.Message);
        }
        catch (UnauthorizedException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, exception.Message);
        }
        catch (ForbiddenException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, exception.Message);
        }
        catch (ValidationException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status503ServiceUnavailable, exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API exception.");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message });
    }
}
