namespace SportFlow.Shared.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    protected Result(bool isSuccess, string? errorCode = null, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new(true);
    public static Result Failure(string errorCode, string? errorMessage = null) => new(false, errorCode, errorMessage);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string errorCode, string? errorMessage = null) => Result<T>.Failure(errorCode, errorMessage);
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed Result.");

    private Result(bool isSuccess, T? value = default, string? errorCode = null, string? errorMessage = null)
        : base(isSuccess, errorCode, errorMessage)
    {
        _value = value;
    }

    public static Result<T> Success(T value) => new(true, value);
    public new static Result<T> Failure(string errorCode, string? errorMessage = null) => new(false, default, errorCode, errorMessage);
}
