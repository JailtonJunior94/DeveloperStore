using FluentValidation;

namespace DeveloperStore.Common.Validation;

public static class Validator
{
    public static async Task<IReadOnlyCollection<ValidationErrorDetail>> ValidateAsync<T>(T instance)
    {
        Type validatorType = typeof(IValidator<>).MakeGenericType(typeof(T));

        if (Activator.CreateInstance(validatorType) is not IValidator validator)
        {
            throw new InvalidOperationException($"No validator found for: {typeof(T).Name}");
        }

        var result = await validator.ValidateAsync(new ValidationContext<T>(instance));
        return result.IsValid
            ? Array.Empty<ValidationErrorDetail>()
            : result.Errors.Select(error => (ValidationErrorDetail)error).ToArray();
    }
}
