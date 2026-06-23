using DeveloperStore.Common.Validation;
using FluentValidation;

namespace DeveloperStore.Application.Sales.CancelSale;

public sealed class CancelSaleValidator : ApiValidator<CancelSaleCommand>
{
    public CancelSaleValidator()
    {
        RuleFor(command => command.Id)
            .Must(id => id.Value != Guid.Empty)
            .WithErrorCode("sale_id_required")
            .WithMessage("id is required");
    }
}
