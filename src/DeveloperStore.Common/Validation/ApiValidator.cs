using FluentValidation;

namespace DeveloperStore.Common.Validation;

public abstract class ApiValidator<T> : AbstractValidator<T>
{
    protected ApiValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Continue;
    }
}
