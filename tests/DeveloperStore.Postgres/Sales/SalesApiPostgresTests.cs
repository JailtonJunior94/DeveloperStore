using System.Net;
using System.Net.Http.Json;
using DeveloperStore.Application.Sales.Common;
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
