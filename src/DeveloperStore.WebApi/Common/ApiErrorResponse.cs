using DeveloperStore.Common.Validation;

namespace DeveloperStore.WebApi.Common;

public sealed record ApiErrorResponse(
    string Type,
    string Error,
    string Detail,
    int Status,
    string TraceId,
    IReadOnlyCollection<ValidationErrorDetail>? Errors = null);
