using AuthenticationService.Core.DTOs;

namespace AuthenticationService.Core.Interfaces;

/// <summary>
/// Interface'ler (Arayüzler), C# programlama dilinde sınıfların uyması gereken "sözleşmeler" (contracts) olarak düşünülebilir.
/// Bir interface, hangi metotların (veya özelliklerin) olacağını tanımlar, ancak bu metotların *nasıl* çalışacağını (gövdesini/implementasyonunu) içermez.
/// 
/// Neden Kullanırız?
/// 1. Soyutlama (Abstraction): Uygulamanın diğer katmanları (örneğin Controller'lar), "IAuthService" sözleşmesine güvenir. Arka planda hangi sınıfın bunu gerçekleştirdiğiyle (örneğin AuthService) ilgilenmezler.
/// 2. Bağımlılıkların Enjekte Edilmesi (Dependency Injection - DI): Controller'larımıza doğrudan somut bir sınıf (AuthService) vermek yerine "IAuthService" interface'ini veririz. Program.cs (veya Startup.cs) içerisinde bu interface talep edildiğinde hangi somut sınıfın üretileceğini ayarlarız (örneğin AddScoped<IAuthService, AuthService>). Bu sayede test edilebilirlik (örneğin bir MockAuthService kullanarak) artar ve sınıflar arası sıkı bağlar (tight coupling) engellenir.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Metot Sözleşmesi (Method Contract): Bu imza, sisteme yeni bir kullanıcı kaydetme işleminin alacağı parametreyi ve geriye döneceği asenkron (Task) tipi belirtir. 
    /// IAuthService'i uygulayan (implement eden) herhangi bir sınıf, bu metoda tam olarak aynı isim ve parametrelerle sahip olmak ZORUNDADIR.
    /// </summary>
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
