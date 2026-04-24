namespace PixPro.Services.Auth.Application.Common.Results;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = null;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    private Result(bool isSuccess, Error? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}
