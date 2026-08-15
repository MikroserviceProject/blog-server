using AuthenticationService.Core.DTOs;

namespace AuthenticationService.Core.Interfaces;

public interface IUserProfileService
{
    Task<ApiResponseDto<UserDto>> GetCurrentUserAsync(Guid userId, string? sessionToken = null);
    Task<ApiResponseDto<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request);
    Task<ApiResponseDto<UserDto>> UpdateAvatarAsync(Guid userId, string profilePictureUrl);
    Task<ApiResponseDto<UserDto>> UpdateCoverAsync(Guid userId, string coverPictureUrl);
    Task<ApiResponseDto<bool>> DeactivateAccountAsync(Guid userId);
    Task<ApiResponseDto<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request);
    
    // Bildirimler
    Task<ApiResponseDto<PaginatedResultDto<UserNotificationDto>>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 10, bool unreadOnly = false);
    Task<ApiResponseDto<bool>> MarkNotificationAsReadAsync(Guid userId, Guid notificationId);

    Task<ApiResponseDto<bool>> ApplyForAuthorAsync(Guid userId, string university, string cvUrl);

    // Public Profile & Search
    Task<ApiResponseDto<PublicUserDto>> GetPublicProfileByUsernameAsync(string username);
    Task<ApiResponseDto<List<PublicUserDto>>> SearchPublicUsersAsync(string query);
    Task<ApiResponseDto<List<PublicUserDto>>> GetPublicProfilesByIdsAsync(List<Guid> userIds);
}
