using FluentValidation.Results;

namespace DeveloperStore.Common.Validation;

public sealed class ValidationErrorDetail
{
    public string Code { get; init; } = string.Empty;
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public static explicit operator ValidationErrorDetail(ValidationFailure validationFailure)
    {
        return new ValidationErrorDetail
        {
            Code = string.IsNullOrWhiteSpace(validationFailure.ErrorCode) ? "validation_error" : validationFailure.ErrorCode,
            Field = validationFailure.PropertyName,
            Message = validationFailure.ErrorMessage
        };
    }
}
