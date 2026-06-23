using System.Net;

namespace DeveloperStore.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string code, string title, string message, HttpStatusCode statusCode) : base(message)
    {
        Code = code;
        Title = title;
        StatusCode = statusCode;
    }

    public string Code { get; }

    public string Title { get; }

    public HttpStatusCode StatusCode { get; }
}
