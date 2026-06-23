using System.Net;

namespace DeveloperStore.Domain.Exceptions;

public sealed class SaleStateConflictException : DomainException
{
    public SaleStateConflictException(string message)
        : base("sale_state_conflict", "Sale state conflict", message, HttpStatusCode.Conflict)
    {
    }
}
