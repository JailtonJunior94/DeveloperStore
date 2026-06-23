namespace DeveloperStore.Domain.Exceptions;

public sealed class DuplicateSaleNumberException : DomainException
{
    public DuplicateSaleNumberException(string message)
        : base("sale_number_conflict", "Sale number conflict", message)
    {
    }
}
