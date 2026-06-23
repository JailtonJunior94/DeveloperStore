using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Common.Validation;
using FluentValidation;

namespace DeveloperStore.Application.Sales.UpdateSale;

public sealed class UpdateSaleValidator : ApiValidator<UpdateSaleCommand>
{
    public UpdateSaleValidator()
    {
        RuleFor(command => command.Id)
            .Must(id => id.Value != Guid.Empty)
            .WithErrorCode("sale_id_required")
            .WithMessage("id is required");

        RuleFor(command => command.SaleNumber)
            .Must(saleNumber => !string.IsNullOrWhiteSpace(saleNumber.Value))
            .WithErrorCode("sale_number_required")
            .WithMessage("saleNumber is required");

        RuleFor(command => command.Customer.Id)
            .NotEmpty()
            .WithErrorCode("customer_external_id_required")
            .WithMessage("customerExternalId is required");

        RuleFor(command => command.Customer.Description)
            .NotEmpty()
            .WithErrorCode("customer_name_required")
            .WithMessage("customerName is required");

        RuleFor(command => command.Branch.Id)
            .NotEmpty()
            .WithErrorCode("branch_external_id_required")
            .WithMessage("branchExternalId is required");

        RuleFor(command => command.Branch.Description)
            .NotEmpty()
            .WithErrorCode("branch_name_required")
            .WithMessage("branchName is required");

        RuleFor(command => command.Items)
            .NotEmpty()
            .WithErrorCode("items_required")
            .WithMessage("items must contain at least one item");

        RuleFor(command => command.Items)
            .Must(items => items is null || items.GroupBy(i => i.Product.Id, StringComparer.OrdinalIgnoreCase).All(g => g.Count() == 1))
            .WithErrorCode("duplicate_product_in_sale")
            .WithMessage("each product must appear at most once per sale");

        RuleForEach(command => command.Items).SetValidator(new SaleItemInputValidator());
    }
}
