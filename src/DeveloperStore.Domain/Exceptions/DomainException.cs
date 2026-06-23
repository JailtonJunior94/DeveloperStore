namespace DeveloperStore.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string code, string title, string message) : base(message)
    {
        Code = code;
        Title = title;
    }

    public string Code { get; }

    public string Title { get; }
}
