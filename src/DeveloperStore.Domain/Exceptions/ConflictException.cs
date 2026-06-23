using System.Net;

namespace DeveloperStore.Domain.Exceptions;

public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base("conflict", message, HttpStatusCode.Conflict)
    {
    }
}
