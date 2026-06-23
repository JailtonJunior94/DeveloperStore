using DeveloperStore.Common.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;

namespace DeveloperStore.WebApi.Common;

public static class ApiErrorFactory
{
    public static ApiErrorResponse Create(
        HttpContext context,
        HttpStatusCode statusCode,
        string type,
        string title,
        string detail,
        IReadOnlyCollection<ValidationErrorDetail>? errors = null)
    {
        return new ApiErrorResponse(
            Type: type,
            Error: title,
            Detail: detail,
            Status: (int)statusCode,
            TraceId: context.TraceIdentifier,
            Errors: errors);
    }

    public static ApiErrorResponse FromModelState(HttpContext context, ModelStateDictionary modelState)
    {
        var errors = modelState
            .SelectMany(entry => entry.Value?.Errors.Select(error => new ValidationErrorDetail
            {
                Code = "invalid_request",
                Field = entry.Key,
                Message = string.IsNullOrWhiteSpace(error.ErrorMessage) ? "invalid request value" : error.ErrorMessage
            }) ?? [])
            .ToArray();

        return Create(
            context,
            HttpStatusCode.UnprocessableEntity,
            "validation_failed",
            "Request validation failed",
            "One or more request values are invalid.",
            errors);
    }
}
