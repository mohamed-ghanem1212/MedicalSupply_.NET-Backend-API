using System.Net;
using System.Text.Json;
using MedicalSupply.Domain.Exceptions;

namespace MedicalSupply.Api.Middleware;

// Catches every exception and turns it into a consistent JSON error response,
// so controllers don't need their own try/catch blocks.
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
            var (statusCode, code) = ex switch
            {
                NotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND"),
                ValidationException => (HttpStatusCode.BadRequest, "VALIDATION_ERROR"),
                ConflictException => (HttpStatusCode.Conflict, "CONFLICT"),
                ForbiddenException => (HttpStatusCode.Forbidden, "FORBIDDEN"),
                _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR")
            };

            if (statusCode == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", context.TraceIdentifier);
            else
                _logger.LogWarning("{Code}: {Message}", code, ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var body = JsonSerializer.Serialize(new
            {
                code,
                message = statusCode == HttpStatusCode.InternalServerError ? "An unexpected error occurred." : ex.Message,
                traceId = context.TraceIdentifier
            });

            await context.Response.WriteAsync(body);
        }
    }
}
