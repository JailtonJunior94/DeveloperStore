using DeveloperStore.Common.Validation;
using FluentValidation;

namespace DeveloperStore.Application.Sales.GetSale;

public sealed class GetSaleValidator : ApiValidator<GetSaleQuery>
{
    public GetSaleValidator()
    {
        RuleFor(query => query.Id)
            .Must(id => id.Value != Guid.Empty)
            .WithErrorCode("sale_id_required")
            .WithMessage("id is required");
    }
}
