using System.Diagnostics.CodeAnalysis;

namespace DocStore.Core.Common;

public enum ErrorKind
{
    NotFound,
    Invalid,
    Forbidden,
}

public sealed record Error(ErrorKind Kind, string Message)
{
    public static Error NotFound(string message) => new(ErrorKind.NotFound, message);
    public static Error Invalid(string message) => new(ErrorKind.Invalid, message);
    public static Error Forbidden(string message) => new(ErrorKind.Forbidden, message);
}

public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static implicit operator Result(Error error) => new(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(null) => _value = value;

    private Result(Error error) : base(error) { }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"Result failed: {Error.Message}");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}
