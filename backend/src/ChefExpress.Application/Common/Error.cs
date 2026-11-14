namespace ChefExpress.Application.Common;

public enum ErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict
}

/// <param name="Code">Stable machine-readable code (e.g. "Profile.NotFound") for the front-end.</param>
/// <param name="Message">Human-readable message (pt-BR, shown to the end user).</param>
/// <param name="Details">Optional per-field validation messages.</param>
public sealed record Error(
    string Code,
    string Message,
    ErrorType Type,
    IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static Error Validation(IReadOnlyDictionary<string, string[]> details) =>
        new("Validation", "Um ou mais campos são inválidos.", ErrorType.Validation, details);
}
