using System.Net;

namespace DeveloperStore.Domain.Exceptions;

public sealed class BusinessRuleValidationException : DomainException
{
    public BusinessRuleValidationException(string message)
        : base("business_rule_violation", "Business rule violation", message, HttpStatusCode.UnprocessableEntity)
    {
    }
}
