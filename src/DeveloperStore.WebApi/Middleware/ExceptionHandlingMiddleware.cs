using DeveloperStore.Common.Validation;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.WebApi.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        catch (DbUpdateException exception) when (TryMapToDomainException(exception) is { } domainException)
        {
            await WriteDomainErrorAsync(context, domainException);
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
            exception.Title,
            exception.Message);
    }

    private static DomainException? TryMapToDomainException(DbUpdateException exception)
    {
        if (TryGetPostgresException(exception) is not { } postgresException)
        {
            return null;
        }

        if (postgresException.SqlState == PostgresErrorCodes.UniqueViolation && IsSaleNumberConstraint(postgresException))
        {
            return new DuplicateSaleNumberException("sale number already exists");
        }

        if (postgresException.SqlState == PostgresErrorCodes.CheckViolation)
        {
            return new BusinessRuleValidationException(BuildCheckViolationMessage(postgresException));
        }

        return null;
    }

    private static string BuildCheckViolationMessage(PostgresException exception) =>
        exception.ConstraintName switch
        {
            "ck_sale_items_quantity_positive" => "quantity must be greater than zero",
            "ck_sale_items_unit_price_non_neg" => "unitPrice must be non-negative",
            "ck_sale_items_discount_range" => "discountPercentage must be between 0 and 1",
            _ => "a database constraint was violated"
        };

    private static PostgresException? TryGetPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        return null;
    }

    private static bool IsSaleNumberConstraint(PostgresException exception)
    {
        return string.Equals(exception.ConstraintName, "uq_sales_sale_number", StringComparison.Ordinal) ||
               (string.Equals(exception.TableName, "sales", StringComparison.Ordinal) &&
                string.Equals(exception.ColumnName, "sale_number", StringComparison.Ordinal));
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
