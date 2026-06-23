using DeveloperStore.Common.Validation;
using FluentValidation;

namespace DeveloperStore.Application.Sales.CancelSaleItem;

public sealed class CancelSaleItemValidator : ApiValidator<CancelSaleItemCommand>
{
    public CancelSaleItemValidator()
    {
        RuleFor(command => command.SaleId)
            .Must(id => id.Value != Guid.Empty)
            .WithErrorCode("sale_id_required")
            .WithMessage("saleId is required");

        RuleFor(command => command.ItemId)
            .Must(id => id.Value != Guid.Empty)
            .WithErrorCode("item_id_required")
            .WithMessage("itemId is required");
    }
}
