using FluentValidation;

namespace DeveloperStore.Application.Sales.Common;

public sealed class SaleItemInputValidator : DeveloperStore.Common.Validation.ApiValidator<SaleItemInput>
{
    public SaleItemInputValidator()
    {
        RuleFor(item => item.Product.Id)
            .NotEmpty()
            .WithErrorCode("product_external_id_required")
            .WithMessage("productExternalId is required");

        RuleFor(item => item.Product.Description)
            .NotEmpty()
            .WithErrorCode("product_name_required")
            .WithMessage("productName is required");

        RuleFor(item => item.Quantity.Value)
            .GreaterThan(0)
            .WithErrorCode("quantity_must_be_positive")
            .WithMessage("quantity must be greater than zero")
            .LessThanOrEqualTo(20)
            .WithErrorCode("quantity_limit_exceeded")
            .WithMessage("quantity must be less than or equal to 20");

        RuleFor(item => item.UnitPrice.Value)
            .GreaterThan(0m)
            .WithErrorCode("unit_price_must_be_positive")
            .WithMessage("unitPrice must be greater than zero");
    }
}
