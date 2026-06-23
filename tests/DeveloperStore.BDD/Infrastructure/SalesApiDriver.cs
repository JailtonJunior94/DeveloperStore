using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeveloperStore.Application.Sales.Common;
using DeveloperStore.WebApi.Common;

namespace DeveloperStore.BDD.Infrastructure;

public sealed class SalesApiDriver
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public HttpResponseMessage? LastResponse { get; private set; }
    public SaleDto? LastSale { get; private set; }
    public ApiErrorResponse? LastError { get; private set; }

    public SalesApiDriver(HttpClient client)
    {
        _client = client;
    }

    public async Task<SaleDto> CreateSaleAsync(object request)
    {
        LastResponse = await _client.PostAsJsonAsync("/api/sales", request);
        LastResponse.EnsureSuccessStatusCode();
        var response = await LastResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
        LastSale = response!.Data;
        return LastSale;
    }

    public async Task SendCreateSaleAsync(object request)
    {
        LastResponse = await _client.PostAsJsonAsync("/api/sales", request);
        if (!LastResponse.IsSuccessStatusCode)
            LastError = await LastResponse.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        else
        {
            var response = await LastResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
            LastSale = response?.Data;
        }
    }

    public async Task SendGetSaleAsync(string id)
    {
        LastResponse = await _client.GetAsync($"/api/sales/{id}");
        if (!LastResponse.IsSuccessStatusCode)
            LastError = await LastResponse.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        else
        {
            var response = await LastResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
            LastSale = response?.Data;
        }
    }

    public async Task<ApiPagedResponse<SaleSummaryDto>?> SendListSalesAsync(string queryString = "")
    {
        LastResponse = await _client.GetAsync($"/api/sales{queryString}");
        if (!LastResponse.IsSuccessStatusCode)
        {
            LastError = await LastResponse.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
            return null;
        }
        return await LastResponse.Content.ReadFromJsonAsync<ApiPagedResponse<SaleSummaryDto>>(JsonOptions);
    }

    public async Task SendUpdateSaleAsync(Guid id, object request)
    {
        LastResponse = await _client.PutAsJsonAsync($"/api/sales/{id}", request);
        if (!LastResponse.IsSuccessStatusCode)
            LastError = await LastResponse.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        else
        {
            var response = await LastResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
            LastSale = response?.Data;
        }
    }

    public async Task SendUpdateSaleAsync(string rawId, object request)
    {
        LastResponse = await _client.PutAsJsonAsync($"/api/sales/{rawId}", request);
        if (!LastResponse.IsSuccessStatusCode)
            LastError = await LastResponse.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        else
        {
            var response = await LastResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
            LastSale = response?.Data;
        }
    }

    public async Task SendCancelSaleAsync(string rawId)
    {
        LastResponse = await _client.DeleteAsync($"/api/sales/{rawId}");
        if (!LastResponse.IsSuccessStatusCode)
            LastError = await LastResponse.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        else
        {
            var response = await LastResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
            LastSale = response?.Data;
        }
    }

    public async Task SendCancelItemAsync(string rawSaleId, string rawItemId)
    {
        LastResponse = await _client.PostAsync($"/api/sales/{rawSaleId}/items/{rawItemId}/cancel", null);
        if (!LastResponse.IsSuccessStatusCode)
            LastError = await LastResponse.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        else
        {
            var response = await LastResponse.Content.ReadFromJsonAsync<ApiResponse<SaleDto>>(JsonOptions);
            LastSale = response?.Data;
        }
    }
}
