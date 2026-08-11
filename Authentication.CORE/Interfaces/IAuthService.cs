using AuthenticationService.Core.DTOs;

namespace AuthenticationService.Core.Interfaces;

public interface IAuthService
{
    Task<ApiResponseDto<UserDto>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResponseDto<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResponseDto<bool>> ConfirmEmailAsync(ConfirmEmailDto request);
    Task<ApiResponseDto<bool>> ResendConfirmationEmailAsync(string email);
    Task<ApiResponseDto<bool>> LogoutAsync(Guid userId);
    Task<ApiResponseDto<bool>> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<ApiResponseDto<bool>> ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<ApiResponseDto<bool>> RequestAccountDeletionAsync(Guid userId);
    Task<ApiResponseDto<bool>> ConfirmAccountDeletionAsync(ConfirmAccountDeletionDto request);
    Task<ApiResponseDto<bool>> DeactivateAccountAsync(Guid userId);
    Task<ApiResponseDto<bool>> SendSupportRequestAsync(Guid userId, SupportRequestDto request);
}
