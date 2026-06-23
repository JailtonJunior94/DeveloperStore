namespace DeveloperStore.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message)
        : base("resource_not_found", "Resource not found", message)
    {
    }
}
