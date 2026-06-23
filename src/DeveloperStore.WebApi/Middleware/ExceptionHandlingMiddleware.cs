using DeveloperStore.Common.Validation;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.WebApi.Common;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace DeveloperStore.WebApi.Middleware;

public sealed class ExceptionHandlingMiddleware
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
        catch (ValidationException exception)
        {
            await WriteValidationErrorAsync(context, exception);
        }
        catch (DomainException exception)
        {
            await WriteDomainErrorAsync(context, exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled request failure");
            await WriteErrorAsync(
                context,
                HttpStatusCode.InternalServerError,
                "internal_server_error",
                "Internal server error",
                "An unexpected error occurred while processing the request.");
        }
    }

    private static Task WriteValidationErrorAsync(HttpContext context, ValidationException exception)
    {
        var details = exception.Errors.Select(error => (ValidationErrorDetail)error).ToArray();
        return WriteErrorAsync(
            context,
            HttpStatusCode.UnprocessableEntity,
            "validation_failed",
            "Request validation failed",
            "One or more business input rules were violated.",
            details);
    }

    private static Task WriteDomainErrorAsync(HttpContext context, DomainException exception)
    {
        return WriteErrorAsync(
            context,
            exception.StatusCode,
            exception.Code,
            exception.Code.Replace('_', ' '),
            exception.Message);
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string type,
        string title,
        string detail,
        IReadOnlyCollection<ValidationErrorDetail>? details = null)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(ApiErrorFactory.Create(context, statusCode, type, title, detail, details));
        await context.Response.WriteAsync(payload);
    }
}
