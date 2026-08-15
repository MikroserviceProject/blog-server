namespace Social.CORE.DTOs;

public class ApiResponseDto<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static ApiResponseDto<T> Ok(T data, string message = "")
    {
        return new ApiResponseDto<T> { Data = data, Success = true, Message = message };
    }

    public static ApiResponseDto<T> Fail(string message)
    {
        return new ApiResponseDto<T> { Success = false, Message = message };
    }
}
