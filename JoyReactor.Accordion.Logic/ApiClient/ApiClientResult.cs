namespace JoyReactor.Accordion.Logic.ApiClient;

public record ApiClientResult<T>
    where T : INodeResponseObject
{
    public bool IsSuccess { get; set; }
    public Exception? Exception { get; set; }
    public string? ErrorMessage { get; set; }
    public T? Value { get; set; }

    public static ApiClientResult<T> Success(T value)
    {
        return new ApiClientResult<T>
        {
            IsSuccess = true,
            Value = value,
        };
    }

    public static ApiClientResult<T> Fail(string errorMessage)
    {
        return new ApiClientResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
    }

    public static ApiClientResult<T> Fail(Exception exception)
    {
        return new ApiClientResult<T>
        {
            IsSuccess = false,
            ErrorMessage = exception.Message,
            Exception = exception,
        };
    }
}