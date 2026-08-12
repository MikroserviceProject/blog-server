namespace AuthenticationService.Core.DTOs;

public class PaginatedResultDto<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public Dictionary<string, object> ExtraData { get; set; } = new Dictionary<string, object>();
}
