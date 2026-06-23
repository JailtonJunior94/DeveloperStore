using DeveloperStore.Application.Sales.CancelSale;
using DeveloperStore.Application.Sales.CancelSaleItem;
using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Application.Sales.CreateSale;
using DeveloperStore.Application.Sales.GetSale;
using DeveloperStore.Application.Sales.ListSales;
using DeveloperStore.Application.Sales.UpdateSale;
using DeveloperStore.Domain.Enums;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.ValueObjects;
using DeveloperStore.WebApi.Common;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeveloperStore.WebApi.Features.Sales;

[ApiController]
[Route("api/sales")]
public sealed class SalesController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IMediator _mediator;
    private readonly IValidator<CreateSaleRequest> _createSaleValidator;
    private readonly IValidator<UpdateSaleRequest> _updateSaleValidator;

    public SalesController(
        IMediator mediator,
        IValidator<CreateSaleRequest> createSaleValidator,
        IValidator<UpdateSaleRequest> updateSaleValidator)
    {
        _mediator = mediator;
        _createSaleValidator = createSaleValidator;
        _updateSaleValidator = updateSaleValidator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SaleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, _createSaleValidator, cancellationToken);
        var sale = await _mediator.Send(MapCreateCommand(request), cancellationToken);

        return JsonContent(HttpStatusCode.Created, new ApiResponse<SaleDto>(sale), $"/api/sales/{sale.Id}");
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SaleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSale([FromRoute] string id, CancellationToken cancellationToken)
    {
        var sale = await _mediator.Send(new GetSaleQuery(ParseSaleIdOrThrow(id)), cancellationToken);
        return JsonContent(HttpStatusCode.OK, new ApiResponse<SaleDto>(sale));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResponse<SaleSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ListSales(
        [FromQuery] string? saleNumber,
        [FromQuery] string? customerName,
        [FromQuery] string? branchName,
        [FromQuery] string? customer,
        [FromQuery] string? branch,
        [FromQuery] SaleStatus? status,
        [FromQuery(Name = "_minSoldAt")] DateTimeOffset? minSoldAt,
        [FromQuery(Name = "_maxSoldAt")] DateTimeOffset? maxSoldAt,
        [FromQuery(Name = "_order")] string? order,
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        CancellationToken cancellationToken = default)
    {
        var resolvedCustomerName = ResolveTextFilter(customerName, customer);
        var resolvedBranchName = ResolveTextFilter(branchName, branch);

        var response = await _mediator.Send(
            new ListSalesQuery(saleNumber, resolvedCustomerName, resolvedBranchName, status, minSoldAt, maxSoldAt, order, page, size),
            cancellationToken);

        return JsonContent(HttpStatusCode.OK, new ApiPagedResponse<SaleSummaryDto>(
            response.Items,
            response.PageNumber,
            response.PageSize,
            response.TotalPages,
            response.TotalCount));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SaleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSale([FromRoute] string id, [FromBody] UpdateSaleRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, _updateSaleValidator, cancellationToken);
        var sale = await _mediator.Send(MapUpdateCommand(id, request), cancellationToken);

        return JsonContent(HttpStatusCode.OK, new ApiResponse<SaleDto>(sale));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SaleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSale([FromRoute] string id, CancellationToken cancellationToken)
    {
        var sale = await _mediator.Send(new CancelSaleCommand(ParseSaleIdOrThrow(id)), cancellationToken);
        return JsonContent(HttpStatusCode.OK, new ApiResponse<SaleDto>(sale));
    }

    [HttpPost("{saleId}/items/{itemId}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<SaleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelItem([FromRoute] string saleId, [FromRoute] string itemId, CancellationToken cancellationToken)
    {
        var sale = await _mediator.Send(
            new CancelSaleItemCommand(ParseSaleIdOrThrow(saleId), ParseItemIdOrThrow(itemId)),
            cancellationToken);
        return JsonContent(HttpStatusCode.OK, new ApiResponse<SaleDto>(sale));
    }

    private static SaleItemInput MapItem(SaleItemRequest item)
    {
        return new SaleItemInput(
            ProductReference.Create(item.ProductExternalId, item.ProductName),
            ItemQuantity.Create(item.Quantity),
            Money.Create(item.UnitPrice, "item unit price", false));
    }

    private static CreateSaleCommand MapCreateCommand(CreateSaleRequest request)
    {
        return new CreateSaleCommand(
            SaleNumber.Create(request.SaleNumber),
            SoldAt.Create(request.SoldAt),
            CustomerReference.Create(request.CustomerExternalId, request.CustomerName),
            BranchReference.Create(request.BranchExternalId, request.BranchName),
            request.Items.Select(MapItem).ToArray());
    }

    private static UpdateSaleCommand MapUpdateCommand(string rawId, UpdateSaleRequest request)
    {
        return new UpdateSaleCommand(
            ParseSaleIdOrThrow(rawId),
            SaleNumber.Create(request.SaleNumber),
            SoldAt.Create(request.SoldAt),
            CustomerReference.Create(request.CustomerExternalId, request.CustomerName),
            BranchReference.Create(request.BranchExternalId, request.BranchName),
            request.Items.Select(MapItem).ToArray());
    }

    private static SaleId ParseSaleIdOrThrow(string rawId)
    {
        return Guid.TryParse(rawId, out var saleId)
            ? SaleId.Create(saleId)
            : throw BuildRouteValidationException("id", "sale_id_invalid", "id must be a valid sale identifier");
    }

    private static SaleItemId ParseItemIdOrThrow(string rawId)
    {
        return Guid.TryParse(rawId, out var itemId)
            ? SaleItemId.Create(itemId)
            : throw BuildRouteValidationException("itemId", "item_id_invalid", "itemId must be a valid sale item identifier");
    }

    private ContentResult JsonContent<T>(HttpStatusCode statusCode, T payload, string? location = null)
    {
        if (!string.IsNullOrWhiteSpace(location))
        {
            Response.Headers.Location = location;
        }

        return new ContentResult
        {
            StatusCode = (int)statusCode,
            ContentType = "application/json",
            Content = JsonSerializer.Serialize(payload, JsonOptions)
        };
    }

    private static async Task ValidateAsync<TRequest>(
        TRequest request,
        IValidator<TRequest> validator,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }

    private static ValidationException BuildRouteValidationException(string field, string code, string message)
    {
        return new ValidationException([new ValidationFailure(field, message)
        {
            ErrorCode = code
        }]);
    }

    private static string? ResolveTextFilter(string? canonicalValue, string? legacyAliasValue)
    {
        return string.IsNullOrWhiteSpace(canonicalValue)
            ? legacyAliasValue
            : canonicalValue;
    }
}
