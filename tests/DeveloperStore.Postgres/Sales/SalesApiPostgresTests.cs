using System.Net;
using System.Net.Http.Json;
using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.Enums;
using DeveloperStore.ORM;
using DeveloperStore.WebApi;
using DeveloperStore.WebApi.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace DeveloperStore.Postgres.Sales;

public class SalesApiPostgresTests : IClassFixture<PostgresSalesApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public SalesApiPostgresTests(PostgresSalesApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostThenGet_ShouldPersistAndReadFromPostgreSql()
    {
        await PostgresTestDatabase.ResetAsync();

        var createRequest = new
        {
            saleNumber = "SALE-PG-301",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "customer-1",
            customerName = "Jane Doe",
            branchExternalId = "branch-1",
            branchName = "Central",
            items = new[]
            {
                new { productExternalId = "product-1", productName = "Product 1", quantity = 4, unitPrice = 10m }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/sales", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdSale = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        createdSale.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"/api/sales/{createdSale!.Data.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sale = await getResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        sale.Should().NotBeNull();
        sale!.Data.SaleNumber.Should().Be("SALE-PG-301");
        sale.Data.TotalAmount.Should().Be(36m);
    }

    [Fact]
    public async Task List_ShouldFilterByCustomerNameAndBranchName_UsingPostgreSql()
    {
        await PostgresTestDatabase.ResetAsync();

        await CreateSaleAsync("SALE-PG-401", "Alice Smith", "Downtown");
        await CreateSaleAsync("SALE-PG-402", "Bob Stone", "Airport");

        var response = await _client.GetAsync("/api/sales?customerName=Alice Smith&branchName=Downtown");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<SaleSummaryDto>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Data.Should().ContainSingle(sale =>
            sale.SaleNumber == "SALE-PG-401" &&
            sale.CustomerName == "Alice Smith" &&
            sale.BranchName == "Downtown");
    }

    [Fact]
    public async Task List_ShouldSupportWildcardCustomerFilter_UsingPostgreSql()
    {
        await PostgresTestDatabase.ResetAsync();

        await CreateSaleAsync("SALE-PG-411", "Alice Smith", "Downtown");
        await CreateSaleAsync("SALE-PG-412", "Bob Stone", "Airport");

        var response = await _client.GetAsync("/api/sales?customerName=Alice*");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<SaleSummaryDto>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Data.Should().ContainSingle(sale => sale.SaleNumber == "SALE-PG-411");
    }

    [Fact]
    public async Task List_ShouldFilterBySaleNumber_UsingPostgreSql()
    {
        await PostgresTestDatabase.ResetAsync();

        await CreateSaleAsync("SALE-PG-701", "Alice Smith", "Downtown");
        await CreateSaleAsync("SALE-PG-702", "Bob Stone", "Airport");
        await CreateSaleAsync("SALE-OTHER-001", "Charlie Day", "North");

        var response = await _client.GetAsync("/api/sales?saleNumber=SALE-PG-701");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<SaleSummaryDto>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Data.Should().ContainSingle(sale => sale.SaleNumber == "SALE-PG-701");
    }

    [Fact]
    public async Task List_ShouldFilterBySaleNumberWildcard_UsingPostgreSql()
    {
        await PostgresTestDatabase.ResetAsync();

        await CreateSaleAsync("SALE-PG-WILD-001", "Alice Smith", "Downtown");
        await CreateSaleAsync("SALE-PG-WILD-002", "Bob Stone", "Airport");
        await CreateSaleAsync("SALE-OTHER-002", "Charlie Day", "North");

        var response = await _client.GetAsync("/api/sales?saleNumber=SALE-PG-WILD-*");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<SaleSummaryDto>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Data.Should().HaveCount(2);
        payload.Data.Should().OnlyContain(sale => sale.SaleNumber.StartsWith("SALE-PG-WILD-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Delete_ShouldCancelSale_UsingPostgreSql()
    {
        await PostgresTestDatabase.ResetAsync();

        var created = await CreateSaleAsync("SALE-PG-501", "Jane Doe", "Central");

        var deleteResponse = await _client.DeleteAsync($"/api/sales/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/sales/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sale = await getResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        sale!.Data.Status.Should().Be(SaleStatus.Cancelled);
    }

    [Fact]
    public async Task Put_ShouldUpdateSale_UsingPostgreSql()
    {
        await PostgresTestDatabase.ResetAsync();

        var created = await CreateSaleAsync("SALE-PG-601", "Jane Doe", "Central");

        var updateRequest = new
        {
            saleNumber = "SALE-PG-601-UPDATED",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "customer-updated",
            customerName = "Jane Updated",
            branchExternalId = "branch-updated",
            branchName = "North",
            items = new[]
            {
                new { productExternalId = "product-1", productName = "Product 1", quantity = 10, unitPrice = 10m }
            }
        };

        var putResponse = await _client.PutAsJsonAsync($"/api/sales/{created.Id}", updateRequest);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await putResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        updated!.Data.SaleNumber.Should().Be("SALE-PG-601-UPDATED");
        updated.Data.TotalAmount.Should().Be(80m);
        updated.Data.CustomerName.Should().Be("Jane Updated");
    }

    private async Task<SaleDto> CreateSaleAsync(string saleNumber, string customerName, string branchName)
    {
        var createRequest = new
        {
            saleNumber,
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = $"{saleNumber}-customer",
            customerName,
            branchExternalId = $"{saleNumber}-branch",
            branchName,
            items = new[]
            {
                new { productExternalId = $"{saleNumber}-product", productName = "Product 1", quantity = 4, unitPrice = 10m }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/sales", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        return created!.Data;
    }

}

public sealed class PostgresSalesApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<DefaultContext>));
            services.RemoveAll<DefaultContext>();
            services.AddDbContext<DefaultContext>(options =>
                options.UseNpgsql(
                    PostgresTestDatabase.BuildWebApiConnectionString(),
                    databaseOptions => databaseOptions.MigrationsAssembly("DeveloperStore.ORM")));
        });
    }
}
