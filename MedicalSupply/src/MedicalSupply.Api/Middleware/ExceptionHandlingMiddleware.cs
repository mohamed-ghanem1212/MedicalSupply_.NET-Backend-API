using System.Net;
using System.Text.Json;
using MedicalSupply.Application.Exceptions;
using MedicalSupply.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MedicalSupply.Api.Middleware;

/// <summary>
/// Single place where every exception in the pipeline is translated into the
/// spec's error envelope: { code, message, details, traceId }. Nothing upstream
/// of this middleware needs its own try/catch for HTTP status mapping — domain
/// and application exceptions carry their own Code, and everything unexpected
/// falls through to a generic 500 without leaking internal details (spec Section 8).
/// </summary>
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

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, code, message, details) = exception switch
        {
            NotFoundAppException e => (HttpStatusCode.NotFound, e.Code, e.Message, e.Details),
            ForbiddenAppException e => (HttpStatusCode.Forbidden, e.Code, e.Message, e.Details),
            ValidationAppException e => (HttpStatusCode.BadRequest, e.Code, e.Message, e.Details),
            ConflictAppException e => (HttpStatusCode.Conflict, e.Code, e.Message, e.Details),

            InsufficientStockException e => (HttpStatusCode.Conflict, e.Code, e.Message, e.Details),
            BudgetExceededException e => (HttpStatusCode.Conflict, e.Code, e.Message, e.Details),
            ConcurrencyConflictException e => (HttpStatusCode.Conflict, e.Code, e.Message, e.Details),
            DuplicateApprovalException e => (HttpStatusCode.Conflict, e.Code, e.Message, e.Details),
            WrongApprovalTypeException e => (HttpStatusCode.Conflict, e.Code, e.Message, e.Details),
            InvalidRequestStateException e => (HttpStatusCode.Conflict, e.Code, e.Message, e.Details),
            AlreadyFulfilledException e => (HttpStatusCode.Conflict, e.Code, e.Message, e.Details),
            DuplicateItemInRequestException e => (HttpStatusCode.BadRequest, e.Code, e.Message, e.Details),
            InvalidQuantityException e => (HttpStatusCode.BadRequest, e.Code, e.Message, e.Details),
            InactiveEntityException e => (HttpStatusCode.BadRequest, e.Code, e.Message, e.Details),

            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT",
                "The record was modified by another operation. Please retry.", new Dictionary<string, object?>()),

            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "UNAUTHORIZED",
                "Authentication is required.", new Dictionary<string, object?>()),

            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR",
                "An unexpected error occurred.", new Dictionary<string, object?>())
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception. TraceId={TraceId}", traceId);
        else
            _logger.LogWarning("{Code}: {Message}. TraceId={TraceId}", code, message, traceId);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = new
        {
            code,
            message,
            details,
            traceId
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
