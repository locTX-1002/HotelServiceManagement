namespace HotelServiceManagement.Application.DTOs.Auth
{
    public class AuthServiceResult<T>
    {
        public bool IsSuccess { get; init; }
        public int StatusCode { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }

        public static AuthServiceResult<T> Success(T data, string message = "Success")
        {
            return new AuthServiceResult<T>
            {
                IsSuccess = true,
                StatusCode = 200,
                Message = message,
                Data = data
            };
        }

        public static AuthServiceResult<T> Failure(string message, int statusCode = 400)
        {
            return new AuthServiceResult<T>
            {
                IsSuccess = false,
                StatusCode = statusCode,
                Message = message
            };
        }
    }
}
