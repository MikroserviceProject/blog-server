using AuthenticationService.Core.DTOs;

namespace AuthenticationService.Core.Interfaces;

public interface IAdminService
{
    Task<ApiResponseDto<UserDto>> CreateAdminAsync(CreateAdminRequestDto request);
    Task<ApiResponseDto<List<AuthorApplicationDto>>> GetAuthorApplicationsAsync();
    Task<ApiResponseDto<bool>> ApproveAuthorApplicationAsync(Guid authorId);
    Task<ApiResponseDto<bool>> RejectAuthorApplicationAsync(Guid authorId, string? reason);
    Task<ApiResponseDto<PaginatedResultDto<UserDto>>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null);
    Task<ApiResponseDto<bool>> BanUserAsync(BanUserRequestDto request);
    Task<ApiResponseDto<bool>> UnbanUserAsync(Guid userId);
    Task<ApiResponseDto<bool>> SendAdminNotificationAsync(AdminSendNotificationDto request);
}
