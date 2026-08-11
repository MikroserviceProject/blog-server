namespace BlogSite.CORE.Dtos
{
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        public static ApiResponseDto<T> Ok(T data, string? message = null) =>
            new() { Success = true, Data = data, Message = message };

        public static ApiResponseDto<T> Fail(string message) =>
            new() { Success = false, Message = message };
    }

    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}
