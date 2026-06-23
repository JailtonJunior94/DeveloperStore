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
        // Arrange
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

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/sales", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdSale = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        createdSale.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"/api/sales/{createdSale!.Data.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        var sale = await getResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        sale!.Data.SaleNumber.Should().Be("SALE-301");
        sale.Data.TotalAmount.Should().Be(36);
    }

    [Fact]
    public async Task Post_ShouldReturnSemanticValidationErrors()
    {
        // Arrange
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

        // Act
        var response = await _client.PostAsJsonAsync("/api/sales", createRequest);

        // Assert
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
        // Act
        var response = await _client.GetAsync("/api/sales/not-a-guid");

        // Assert
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
        // Act
        var response = await _client.GetAsync("/api/sales?_page=0&_size=500");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        error.Should().NotBeNull();
        error!.Type.Should().Be("validation_failed");
        error.Errors.Should().Contain(detail => detail.Code == "page_number_invalid" && detail.Field == "PageNumber");
        error.Errors.Should().Contain(detail => detail.Code == "page_size_invalid" && detail.Field == "PageSize");
    }

    [Fact]
    public async Task List_ShouldAcceptCustomerNameAndBranchNameFilters()
    {
        // Arrange
        const string matchingSaleNumber = "SALE-LIST-401";
        const string otherSaleNumber = "SALE-LIST-402";

        await CreateSaleAsync(matchingSaleNumber, "Customer Filter Name", "Branch Filter Name");
        await CreateSaleAsync(otherSaleNumber, "Other Customer", "Other Branch");

        // Act
        var response = await _client.GetAsync("/api/sales?customerName=Customer Filter Name&branchName=Branch Filter Name");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<SaleSummaryDto>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Data.Should().ContainSingle(sale =>
            sale.SaleNumber == matchingSaleNumber &&
            sale.CustomerName == "Customer Filter Name" &&
            sale.BranchName == "Branch Filter Name");
    }

    [Fact]
    public async Task List_ShouldKeepSupportingLegacyCustomerAndBranchAliases()
    {
        // Arrange
        const string matchingSaleNumber = "SALE-LIST-403";
        const string otherSaleNumber = "SALE-LIST-404";

        await CreateSaleAsync(matchingSaleNumber, "Legacy Alias Customer", "Legacy Alias Branch");
        await CreateSaleAsync(otherSaleNumber, "Different Legacy Customer", "Different Legacy Branch");

        // Act
        var response = await _client.GetAsync("/api/sales?customer=Legacy Alias Customer&branch=Legacy Alias Branch");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<SaleSummaryDto>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Data.Should().ContainSingle(sale =>
            sale.SaleNumber == matchingSaleNumber &&
            sale.CustomerName == "Legacy Alias Customer" &&
            sale.BranchName == "Legacy Alias Branch");
    }

    [Fact]
    public async Task Post_ShouldReturn409_WhenSaleNumberAlreadyExists()
    {
        await CreateSaleAsync("SALE-DUP-001", "Customer A", "Branch A");

        var duplicate = new
        {
            saleNumber = "SALE-DUP-001",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-2",
            customerName = "Customer B",
            branchExternalId = "b-2",
            branchName = "Branch B",
            items = new[] { new { productExternalId = "p-1", productName = "P1", quantity = 1, unitPrice = 5m } }
        };

        var response = await _client.PostAsJsonAsync("/api/sales", duplicate);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        error!.Type.Should().Be("sale_number_conflict");
    }

    [Fact]
    public async Task Post_ShouldReturn422_WhenItemsListIsEmpty()
    {
        var request = new
        {
            saleNumber = "SALE-EMPTY-001",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-1",
            customerName = "Customer A",
            branchExternalId = "b-1",
            branchName = "Branch A",
            items = Array.Empty<object>()
        };

        var response = await _client.PostAsJsonAsync("/api/sales", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        error!.Errors.Should().Contain(e => e.Code == "items_required");
    }

    [Fact]
    public async Task Post_ShouldReturn422_WhenDuplicateProductInItems()
    {
        var request = new
        {
            saleNumber = "SALE-DUPROD-001",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-1",
            customerName = "Customer A",
            branchExternalId = "b-1",
            branchName = "Branch A",
            items = new[]
            {
                new { productExternalId = "product-same", productName = "Product A", quantity = 2, unitPrice = 10m },
                new { productExternalId = "product-same", productName = "Product A Again", quantity = 3, unitPrice = 10m }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/sales", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        error!.Errors.Should().Contain(e => e.Code == "duplicate_product_in_sale");
    }

    [Fact]
    public async Task Get_ShouldReturn404_WhenSaleDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/sales/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        error!.Type.Should().Be("resource_not_found");
    }

    [Fact]
    public async Task Put_ShouldUpdateSale()
    {
        var created = await CreateSaleAsync("SALE-PUT-001", "Original Customer", "Original Branch");

        var updateRequest = new
        {
            saleNumber = "SALE-PUT-001-UPDATED",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-updated",
            customerName = "Updated Customer",
            branchExternalId = "b-updated",
            branchName = "Updated Branch",
            items = new[] { new { productExternalId = "p-1", productName = "P1", quantity = 10, unitPrice = 10m } }
        };

        var response = await _client.PutAsJsonAsync($"/api/sales/{created.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        updated!.Data.SaleNumber.Should().Be("SALE-PUT-001-UPDATED");
        updated.Data.TotalAmount.Should().Be(80m);
        updated.Data.CustomerName.Should().Be("Updated Customer");
    }

    [Fact]
    public async Task Put_ShouldReturn409_WhenUpdatingCancelledSale()
    {
        var created = await CreateSaleAsync("SALE-PUT-CAN-001", "Customer", "Branch");
        await _client.DeleteAsync($"/api/sales/{created.Id}");

        var updateRequest = new
        {
            saleNumber = "SALE-PUT-CAN-001-UPDATED",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-1",
            customerName = "Customer",
            branchExternalId = "b-1",
            branchName = "Branch",
            items = new[] { new { productExternalId = "p-1", productName = "P1", quantity = 1, unitPrice = 10m } }
        };

        var response = await _client.PutAsJsonAsync($"/api/sales/{created.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        error!.Type.Should().Be("sale_state_conflict");
    }

    [Fact]
    public async Task Put_ShouldReturn404_WhenSaleDoesNotExist()
    {
        var updateRequest = new
        {
            saleNumber = "SALE-GHOST",
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = "c-1",
            customerName = "Customer",
            branchExternalId = "b-1",
            branchName = "Branch",
            items = new[] { new { productExternalId = "p-1", productName = "P1", quantity = 1, unitPrice = 10m } }
        };

        var response = await _client.PutAsJsonAsync($"/api/sales/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldCancelSale()
    {
        var created = await CreateSaleAsync("SALE-DEL-001", "Customer", "Branch");

        var response = await _client.DeleteAsync($"/api/sales/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        result!.Data.Status.Should().Be(SaleStatus.Cancelled);
        result.Data.TotalAmount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_ShouldBeIdempotent_WhenSaleAlreadyCancelled()
    {
        var created = await CreateSaleAsync("SALE-DEL-IDEM-001", "Customer", "Branch");
        await _client.DeleteAsync($"/api/sales/{created.Id}");

        var response = await _client.DeleteAsync($"/api/sales/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        result!.Data.Status.Should().Be(SaleStatus.Cancelled);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenSaleDoesNotExist()
    {
        var response = await _client.DeleteAsync($"/api/sales/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelItem_ShouldCancelItem_AndKeepSaleActive()
    {
        var created = await CreateSaleAsync("SALE-CI-FUNC-001", "Customer", "Branch", twoItems: true);
        var itemToCancel = created.Items.First();

        var response = await _client.PostAsync($"/api/sales/{created.Id}/items/{itemToCancel.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        result!.Data.Items.Single(i => i.Id == itemToCancel.Id).IsCancelled.Should().BeTrue();
        result.Data.Status.Should().Be(SaleStatus.NotCancelled);
    }

    [Fact]
    public async Task CancelItem_ShouldCancelSale_WhenLastActiveItemCancelled()
    {
        var created = await CreateSaleAsync("SALE-CI-LAST-001", "Customer", "Branch");
        var item = created.Items.Single();

        var response = await _client.PostAsync($"/api/sales/{created.Id}/items/{item.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        result!.Data.Status.Should().Be(SaleStatus.Cancelled);
        result.Data.TotalAmount.Should().Be(0);
    }

    [Fact]
    public async Task CancelItem_ShouldBeIdempotent_WhenItemAlreadyCancelled()
    {
        var created = await CreateSaleAsync("SALE-CI-IDEM-001", "Customer", "Branch", twoItems: true);
        var item = created.Items.First();

        await _client.PostAsync($"/api/sales/{created.Id}/items/{item.Id}/cancel", null);
        var response = await _client.PostAsync($"/api/sales/{created.Id}/items/{item.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        result!.Data.Items.Single(i => i.Id == item.Id).IsCancelled.Should().BeTrue();
    }

    [Fact]
    public async Task CancelItem_ShouldReturn409_WhenSaleIsCancelled()
    {
        var created = await CreateSaleAsync("SALE-CI-CAN-001", "Customer", "Branch", twoItems: true);
        var item = created.Items.First();
        await _client.DeleteAsync($"/api/sales/{created.Id}");

        var response = await _client.PostAsync($"/api/sales/{created.Id}/items/{item.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        error!.Type.Should().Be("sale_state_conflict");
    }

    [Fact]
    public async Task CancelItem_ShouldReturn404_WhenSaleDoesNotExist()
    {
        var response = await _client.PostAsync($"/api/sales/{Guid.NewGuid()}/items/{Guid.NewGuid()}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelItem_ShouldReturn404_WhenItemDoesNotExist()
    {
        var created = await CreateSaleAsync("SALE-CI-NOITEM-001", "Customer", "Branch");

        var response = await _client.PostAsync($"/api/sales/{created.Id}/items/{Guid.NewGuid()}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_ShouldReturn422_WhenOrderIsInvalid()
    {
        var response = await _client.GetAsync("/api/sales?_order=invalidField");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        error!.Errors.Should().Contain(e => e.Code == "order_invalid");
    }

    [Fact]
    public async Task List_ShouldReturn422_WhenDateRangeIsInverted()
    {
        var future = DateTimeOffset.UtcNow.AddDays(10);
        var past = DateTimeOffset.UtcNow.AddDays(-10);
        var minEncoded = Uri.EscapeDataString(future.ToString("O"));
        var maxEncoded = Uri.EscapeDataString(past.ToString("O"));

        var response = await _client.GetAsync($"/api/sales?_minSoldAt={minEncoded}&_maxSoldAt={maxEncoded}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        error!.Errors.Should().Contain(e => e.Code == "sold_at_range_invalid");
    }

    [Fact]
    public async Task List_ShouldFilterBySoldAtDateRange()
    {
        var past = DateTimeOffset.UtcNow.AddDays(-2);
        var future = DateTimeOffset.UtcNow.AddDays(2);
        var minEncoded = Uri.EscapeDataString(past.ToString("O"));
        var maxEncoded = Uri.EscapeDataString(future.ToString("O"));

        await CreateSaleAsync("SALE-DATE-001", "Customer A", "Branch A");

        var response = await _client.GetAsync($"/api/sales?saleNumber=SALE-DATE-001&_minSoldAt={minEncoded}&_maxSoldAt={maxEncoded}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<SaleSummaryDto>>(JsonOptions);
        payload!.Data.Should().ContainSingle(s => s.SaleNumber == "SALE-DATE-001");
    }

    [Fact]
    public async Task List_ShouldOrderBySaleNumber()
    {
        await CreateSaleAsync("SALE-ORD-ZZZ", "Customer", "Branch");
        await CreateSaleAsync("SALE-ORD-AAA", "Customer", "Branch");
        await CreateSaleAsync("SALE-ORD-MMM", "Customer", "Branch");

        var response = await _client.GetAsync("/api/sales?saleNumber=SALE-ORD-*&_order=saleNumber asc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<SaleSummaryDto>>(JsonOptions);
        var numbers = payload!.Data.Select(s => s.SaleNumber).ToList();
        numbers.Should().BeInAscendingOrder();
    }

    private Task<SaleDto> CreateSaleAsync(string saleNumber, string customerName, string branchName) =>
        CreateSaleAsync(saleNumber, customerName, branchName, twoItems: false);

    private async Task<SaleDto> CreateSaleAsync(string saleNumber, string customerName, string branchName, bool twoItems)
    {
        var items = twoItems
            ? new object[]
            {
                new { productExternalId = $"{saleNumber}-p1", productName = "Product 1", quantity = 4, unitPrice = 10m },
                new { productExternalId = $"{saleNumber}-p2", productName = "Product 2", quantity = 2, unitPrice = 5m }
            }
            : new object[]
            {
                new { productExternalId = $"{saleNumber}-product", productName = "Product 1", quantity = 4, unitPrice = 10m }
            };

        var createRequest = new
        {
            saleNumber,
            soldAt = DateTimeOffset.UtcNow,
            customerExternalId = $"{saleNumber}-customer",
            customerName,
            branchExternalId = $"{saleNumber}-branch",
            branchName,
            items
        };

        var response = await _client.PostAsJsonAsync("/api/sales", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        return created!.Data;
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
