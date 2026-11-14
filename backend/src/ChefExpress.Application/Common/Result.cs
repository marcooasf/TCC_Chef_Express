using System.Diagnostics.CodeAnalysis;

namespace ChefExpress.Application.Common;

/// <summary>
/// Outcome of a use case: either a value or an <see cref="Error"/>.
/// Expected failures (bad input, duplicate e-mail, wrong password) are returned, not thrown.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value) { _value = value; IsSuccess = true; }
    private Result(Error error) { Error = error; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public Error? Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read the value of a failed result.");

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);
}
