using FluentValidation;

namespace DeveloperStore.WebApi.Features.Sales;

public sealed class UpdateSaleRequestValidator : AbstractValidator<UpdateSaleRequest>
{
    public UpdateSaleRequestValidator()
    {
        RuleFor(request => request.SaleNumber)
            .NotEmpty()
            .WithErrorCode("sale_number_required")
            .WithMessage("saleNumber is required");

        RuleFor(request => request.SoldAt)
            .NotEmpty()
            .WithErrorCode("sold_at_required")
            .WithMessage("soldAt is required");

        RuleFor(request => request.CustomerExternalId)
            .NotEmpty()
            .WithErrorCode("customer_external_id_required")
            .WithMessage("customerExternalId is required");

        RuleFor(request => request.CustomerName)
            .NotEmpty()
            .WithErrorCode("customer_name_required")
            .WithMessage("customerName is required");

        RuleFor(request => request.BranchExternalId)
            .NotEmpty()
            .WithErrorCode("branch_external_id_required")
            .WithMessage("branchExternalId is required");

        RuleFor(request => request.BranchName)
            .NotEmpty()
            .WithErrorCode("branch_name_required")
            .WithMessage("branchName is required");

        RuleFor(request => request.Items)
            .NotEmpty()
            .WithErrorCode("items_required")
            .WithMessage("items must contain at least one item");

        RuleForEach(request => request.Items).SetValidator(new SaleItemRequestValidator());
    }
}
