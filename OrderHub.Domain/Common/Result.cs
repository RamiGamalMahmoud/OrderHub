namespace OrderHub.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }
    public string ErrorCode { get; }

    protected Result(bool isSuccess, string errorMessage, string errorCode = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }
    
    public static Result Success() => new Result(true, null);

    public static Result Failure(string errorMessage = "") =>
        new Result(false, errorMessage);
    public static Result Failure(string errorMessage, string errorCode) => new(false, errorMessage, errorCode);
}

public class Result<TValue> : Result
{
    public TValue Value { get; }

    private Result(bool isSuccess, TValue value, string errorMessage, string errorCode = null) : base(isSuccess, errorMessage, errorCode)
    {
        Value = value;
    }
    
    public static implicit operator Result<TValue>(TValue value) => Success(value);
    
    public static Result<TValue> Success(TValue value) => new Result<TValue>(true, value, null);
    
    public static new Result<TValue> Failure(string errorMessage) =>
        new Result<TValue>(false, default, errorMessage);
    public static new Result<TValue> Failure(string errorMessage, string errorCode) => new(false, default, errorMessage, errorCode);
}
