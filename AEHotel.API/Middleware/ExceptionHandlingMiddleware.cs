using AEHotel.Service.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
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
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(exception, "Database concurrency conflict");
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "Məlumat artıq dəyişdirilib. Zəhmət olmasa yenidən cəhd edin.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Database update error");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Məlumat bazası xətası baş verdi.");
        }
        catch (InvalidOperationException exception)
        {
            // InvalidOperationException can be various things - log details but only show generic message
            _logger.LogError(exception, "Invalid operation");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Əməliyyat icra edilə bilmədi.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API exception");
            
            // In development, show detailed error; in production, show generic message
            var message = _environment.IsDevelopment()
                ? exception.Message
                : "Gözlənilməz xəta baş verdi. Zəhmət olmasa bir az sonra yenidən cəhd edin.";

            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, message);
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
