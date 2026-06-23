using FluentValidation.Results;

namespace DeveloperStore.Common.Validation;

public sealed class ValidationResultDetail
{
    public ValidationResultDetail()
    {
    }

    public ValidationResultDetail(ValidationResult validationResult)
    {
        IsValid = validationResult.IsValid;
        Errors = validationResult.Errors.Select(error => (ValidationErrorDetail)error).ToArray();
    }

    public bool IsValid { get; set; }

    public IReadOnlyCollection<ValidationErrorDetail> Errors { get; set; } = Array.Empty<ValidationErrorDetail>();
}
