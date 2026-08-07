using AuthenticationService.Core.DTOs;

namespace AuthenticationService.Core.Interfaces;

public interface IAuthService
{
    Task<ApiResponseDto<UserDto>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResponseDto<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResponseDto<bool>> ConfirmEmailAsync(ConfirmEmailDto request);
    Task<ApiResponseDto<bool>> ResendConfirmationEmailAsync(string email);
    Task<ApiResponseDto<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request);
    Task<ApiResponseDto<UserDto>> UpdateAvatarAsync(Guid userId, string avatarUrl);
    Task<ApiResponseDto<UserDto>> GetCurrentUserAsync(Guid userId, string? sessionToken = null);
    Task<ApiResponseDto<bool>> LogoutAsync(Guid userId);

    // Şifre Yönetimi
    Task<ApiResponseDto<bool>> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<ApiResponseDto<bool>> ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<ApiResponseDto<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request);

    // Admin & Moderasyon Yönetimi
    Task<ApiResponseDto<UserDto>> CreateAdminAsync(CreateAdminRequestDto request);
    Task<ApiResponseDto<List<AuthorApplicationDto>>> GetAuthorApplicationsAsync();
    Task<ApiResponseDto<bool>> ApproveAuthorApplicationAsync(Guid authorId);
    Task<ApiResponseDto<bool>> RejectAuthorApplicationAsync(Guid authorId, string? reason);
    Task<ApiResponseDto<List<UserDto>>> GetAllUsersAsync();
    Task<ApiResponseDto<bool>> BanUserAsync(BanUserRequestDto request);
    Task<ApiResponseDto<bool>> UnbanUserAsync(Guid userId);
    Task<ApiResponseDto<bool>> SendAdminNotificationAsync(AdminSendNotificationDto request);

    // Hesap Silme (Mail Onaylı)
    Task<ApiResponseDto<bool>> RequestAccountDeletionAsync(Guid userId);
    Task<ApiResponseDto<bool>> ConfirmAccountDeletionAsync(ConfirmAccountDeletionDto request);

    // Bildirimler
    Task<ApiResponseDto<List<UserNotificationDto>>> GetUserNotificationsAsync(Guid userId);
    Task<ApiResponseDto<bool>> MarkNotificationAsReadAsync(Guid userId, Guid notificationId);
}
