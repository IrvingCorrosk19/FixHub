namespace FixHub.Application.Common.Models;

/// <summary>
/// Resultado tipado que evita excepciones para control de flujo en casos de uso.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(string error, string? errorCode = null)
    {
        IsSuccess = false;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error, string? errorCode = null) => new(error, errorCode);
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    private Result(bool success, string? error, string? errorCode)
    {
        IsSuccess = success;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string? errorCode = null) => new(false, error, errorCode);
}
