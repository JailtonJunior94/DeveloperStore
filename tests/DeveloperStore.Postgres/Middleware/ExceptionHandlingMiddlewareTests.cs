using System.Net;
using System.Text.Json;
using DeveloperStore.WebApi.Common;
using DeveloperStore.WebApi.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace DeveloperStore.Postgres.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldTranslateSaleNumberUniqueViolationToConflict()
    {
        var context = BuildHttpContext();
        var postgresException = BuildPostgresException(
            PostgresErrorCodes.UniqueViolation,
            constraintName: "uq_sales_sale_number",
            tableName: "sales");

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new DbUpdateException("duplicate sale number", postgresException),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        var payload = await ReadBodyAsync<ApiErrorResponse>(context);
        payload!.Type.Should().Be("sale_number_conflict");
        payload.Detail.Should().Be("sale number already exists");
    }

    [Theory]
    [InlineData("ck_sale_items_quantity_positive", "quantity must be greater than zero")]
    [InlineData("ck_sale_items_unit_price_non_neg", "unitPrice must be non-negative")]
    [InlineData("ck_sale_items_discount_range", "discountPercentage must be between 0 and 1")]
    public async Task InvokeAsync_ShouldTranslateCheckViolationToUnprocessableEntity(string constraintName, string expectedDetail)
    {
        var context = BuildHttpContext();
        var postgresException = BuildPostgresException(
            PostgresErrorCodes.CheckViolation,
            constraintName: constraintName,
            tableName: "sale_items");

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new DbUpdateException("check constraint violated", postgresException),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
        var payload = await ReadBodyAsync<ApiErrorResponse>(context);
        payload!.Type.Should().Be("business_rule_violation");
        payload.Detail.Should().Be(expectedDetail);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn500_ForUnhandledException()
    {
        var context = BuildHttpContext();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("unexpected internal error"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        var payload = await ReadBodyAsync<ApiErrorResponse>(context);
        payload!.Type.Should().Be("internal_server_error");
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn500_ForDbUpdateExceptionWithoutPostgresInnerException()
    {
        var context = BuildHttpContext();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new DbUpdateException("concurrency failure", new Exception("generic db error")),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        var payload = await ReadBodyAsync<ApiErrorResponse>(context);
        payload!.Type.Should().Be("internal_server_error");
    }

    private static DefaultHttpContext BuildHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<T?> ReadBodyAsync<T>(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<T>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static PostgresException BuildPostgresException(string sqlState, string constraintName, string tableName) =>
        new(
            messageText: "constraint violation",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: sqlState,
            detail: string.Empty,
            hint: string.Empty,
            position: 0,
            internalPosition: 0,
            internalQuery: string.Empty,
            where: string.Empty,
            schemaName: "public",
            tableName: tableName,
            columnName: string.Empty,
            dataTypeName: string.Empty,
            constraintName: constraintName,
            file: "execMain.c",
            line: "1",
            routine: "ExecConstraints");
}
