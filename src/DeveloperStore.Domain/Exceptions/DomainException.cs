using System.Net;

namespace DeveloperStore.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string code, string message, HttpStatusCode statusCode) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }

    public HttpStatusCode StatusCode { get; }
}
