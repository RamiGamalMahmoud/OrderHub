namespace OrderHub.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }

    protected Result(bool isSuccess, string errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }
    
    public static Result Success() => new Result(true, null);

    public static Result Failure(string errorMessage = "") =>
        new Result(false, errorMessage);
}

public class Result<TValue> : Result
{
    public TValue Value { get; }

    private Result(bool isSuccess, TValue value, string errorMessage) : base(isSuccess, errorMessage)
    {
        Value = value;
    }
    
    public static implicit operator Result<TValue>(TValue value) => Success(value);
    
    public static Result<TValue> Success(TValue value) => new Result<TValue>(true, value, null);
    
    public static new Result<TValue> Failure(string errorMessage) =>
        new Result<TValue>(false, default, errorMessage);
}
