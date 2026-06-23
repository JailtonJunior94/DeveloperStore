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
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace DeveloperStore.Functional.Sales;

public class SalesApiTests : IClassFixture<SalesApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public SalesApiTests(SalesApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostThenGet_ShouldReturnCreatedSale()
    {
        var createRequest = new
        {
            saleNumber = "SALE-301",
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
        sale!.Data.SaleNumber.Should().Be("SALE-301");
        sale.Data.TotalAmount.Should().Be(36);
    }

    [Fact]
    public async Task Post_ShouldReturnSemanticValidationErrors()
    {
        var createRequest = new
        {
            saleNumber = "",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "",
            customerName = "",
            branchExternalId = "",
            branchName = "",
            items = new[]
            {
                new { productExternalId = "", productName = "", quantity = 0, unitPrice = 0m }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/sales", createRequest);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        error.Should().NotBeNull();
        error!.Type.Should().Be("validation_failed");
        error.Status.Should().Be(422);
        error.Errors.Should().NotBeNullOrEmpty();
        error.Errors!.Should().Contain(detail => detail.Code == "sale_number_required" && detail.Field == "SaleNumber");
    }

    [Fact]
    public async Task Get_WithMalformedGuid_ShouldReturnSemanticValidationPayload()
    {
        var response = await _client.GetAsync("/api/sales/not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        error.Should().NotBeNull();
        error!.Type.Should().Be("validation_failed");
        error.Status.Should().Be(422);
        error.TraceId.Should().NotBeNullOrWhiteSpace();
        error.Errors.Should().Contain(detail => detail.Code == "sale_id_invalid" && detail.Field == "id");
    }

    [Fact]
    public async Task List_WithInvalidPagination_ShouldReturnSemanticValidationErrors()
    {
        var response = await _client.GetAsync("/api/sales?_page=0&_size=500");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        error.Should().NotBeNull();
        error!.Type.Should().Be("validation_failed");
        error.Errors.Should().Contain(detail => detail.Code == "page_number_invalid" && detail.Field == "PageNumber");
        error.Errors.Should().Contain(detail => detail.Code == "page_size_invalid" && detail.Field == "PageSize");
    }
}

public sealed class SalesApiFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<DefaultContext>));
            services.RemoveAll<DefaultContext>();
            services.AddDbContext<DefaultContext>(options => options.UseInMemoryDatabase("DeveloperStoreFunctionalTests", _databaseRoot));
        });
    }
}
