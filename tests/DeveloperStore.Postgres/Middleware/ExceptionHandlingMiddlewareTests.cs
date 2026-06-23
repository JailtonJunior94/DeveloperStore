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
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var postgresException = new PostgresException(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation,
            detail: "Key (sale_number)=(SALE-100) already exists.",
            hint: string.Empty,
            position: 0,
            internalPosition: 0,
            internalQuery: string.Empty,
            where: string.Empty,
            schemaName: "public",
            tableName: "sales",
            columnName: string.Empty,
            dataTypeName: string.Empty,
            constraintName: "uq_sales_sale_number",
            file: "nbtinsert.c",
            line: "666",
            routine: "_bt_check_unique");

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new DbUpdateException("duplicate sale number", postgresException),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        context.Response.Body.Position = 0;

        var payload = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(context.Response.Body);
        payload.Should().NotBeNull();
        payload!.Type.Should().Be("sale_number_conflict");
        payload.Error.Should().Be("Sale number conflict");
        payload.Detail.Should().Be("sale number already exists");
    }
}
