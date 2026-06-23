using System.Net;

namespace DeveloperStore.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base("resource_not_found", message, HttpStatusCode.NotFound)
    {
    }
}
