//using System;

//namespace OrderHub.Application.Common;

//public class Result
//{
//    public bool IsSuccess { get; }
//    public string ErrorMessage { get; }
//    public Exception Exception { get; }

//    protected Result(bool isSuccess, string errorMessage, Exception exception)
//    {
//        IsSuccess = isSuccess;
//        ErrorMessage = errorMessage;
//        Exception = exception;
//    }
    
//    public static Result Success() => new Result(true, null, null);

//    public static Result Failure(string errorMessage = "", Exception exception = null) =>
//        new Result(false, errorMessage, exception);
//}

//public class Result<TValue> : Result
//{
//    public TValue Value { get; }

//    private Result(bool isSuccess, TValue value, string errorMessage, Exception exception) : base(isSuccess, errorMessage, exception)
//    {
//        Value = value;
//    }
    
//    public static implicit operator Result<TValue>(TValue value) => Success(value);
    
//    public static Result<TValue> Success(TValue value) => new Result<TValue>(true, value, null, null);
    
//    public static new Result<TValue> Failure(string errorMessage, Exception exception = null) =>
//        new Result<TValue>(false, default, errorMessage, exception);
//}
