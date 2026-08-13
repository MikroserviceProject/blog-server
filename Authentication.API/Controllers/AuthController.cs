using System.Security.Claims;
using AuthenticationService.Core.Data;
using AuthenticationService.Core.DTOs;
using AuthenticationService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.Controllers;

// [ApiController]: Bu nitelik (attribute), sınıfın bir API Controller olduğunu belirtir. 
// Otomatik model doğrulama (ModelState validation) ve hata yanıtları gibi özellikleri etkinleştirir.
[ApiController]
// [Route]: API'nin erişileceği URL şemasını belirler. "[controller]" ifadesi, 
// çalışma zamanında sınıfın adı olan "Auth" (Controller kelimesi atılır) ile değiştirilir. 
// Yani bu API uç noktalarına (endpoints) "api/auth" adresinden erişilir.
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserProfileService _profileService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly AppDbContext _dbContext;

    // Constructor (Yapıcı Metot) ve Dependency Injection (Bağımlılık Enjeksiyonu):
    // ASP.NET Core, bu Controller'ı oluştururken ihtiyaç duyduğu servisleri (IAuthService, vb.) 
    // yerleşik IoC (Inversion of Control) konteynerinden otomatik olarak sağlar (enjekte eder).
    // Bu sayede sınıflar arası bağımlılıklar gevşetilmiş (loosely coupled) olur ve test edilebilirlik artar.
    public AuthController(IAuthService authService, IUserProfileService profileService, IConfiguration configuration, ILogger<AuthController> logger, AppDbContext dbContext)
    {
        _authService = authService;
        _profileService = profileService;
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Kullanıcıdan gelen istek veya şikayet bildirimlerini işler.
    /// </summary>
    // [HttpPost]: Bu metodun yalnızca HTTP POST isteklerine yanıt vereceğini belirtir. 
    // Genellikle sunucuya yeni veri göndermek veya bir işlem başlatmak için kullanılır.
    [HttpPost("support-request")]
    // [Authorize]: Bu endpoint'e sadece kimliği doğrulanmış (geçerli bir JWT token'ı olan) kullanıcıların erişebileceğini belirtir.
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    // DTO (Data Transfer Object): İstemci (Client) ile sunucu arasında taşınacak veriyi tanımlayan basit sınıflardır.
    // Güvenlik ve performans için veritabanı modelleri (Entity) yerine DTO'lar kullanılır.
    // [FromBody]: Gelen HTTP isteğinin gövdesindeki (body) JSON verisinin SupportRequestDto nesnesine bağlanacağını (bind) ifade eder.
    public async Task<IActionResult> SupportRequest([FromBody] SupportRequestDto request)
    {
        // Model doğrulaması (Validation): İstemciden gelen verilerin DTO üzerindeki kurallara uyup uymadığını kontrol eder.
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Fail("Geçersiz veya eksik bilgi gönderildi."));
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Geçersiz oturum."));
        }

        // Akış (Flow): Kontrol katmanı (Controller), iş mantığını (Business Logic) kendi içinde yazmaz.
        // İlgili servise (Service Layer) parametreleri gönderir ve dönen sonuca göre HTTP yanıtı oluşturur.
        var result = await _authService.SendSupportRequestAsync(userId, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Yeni kullanıcı / okur kaydı oluşturur.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponseDto<UserDto>.Fail("Geçersiz veri girişi.", errors));
        }

        var result = await _authService.RegisterAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Swagger / API üzerinden doğrudan Yazar (Author) hesabı oluşturur.
    /// </summary>
    [HttpPost("register-author")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> RegisterAuthor(
        [FromForm] string username,
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string university,
         IFormFile? cvFile)
    {
        string? cvUrl = null;

        if (cvFile != null && cvFile.Length > 0)
        {
            if (cvFile.Length > 10 * 1024 * 1024)
            {
                return BadRequest(ApiResponseDto<UserDto>.Fail("CV dosya boyutu en fazla 10 MB olabilir."));
            }

            var extension = Path.GetExtension(cvFile.FileName).ToLowerInvariant();
            if (extension != ".pdf")
            {
                return BadRequest(ApiResponseDto<UserDto>.Fail("Lütfen sadece PDF formatında CV yükleyiniz."));
            }

            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "cvs");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"cv_{Guid.NewGuid():N}_{Path.GetFileName(cvFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await cvFile.CopyToAsync(stream);
            }

            cvUrl = $"/uploads/cvs/{uniqueFileName}";
        }

        var requestDto = new RegisterRequestDto
        {
            Username = username,
            Email = email,
            Password = password,
            Role = "Author",
            University = university,
            CvUrl = cvUrl
        };

        var result = await _authService.RegisterAsync(requestDto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Kullanıcı girişi yapar ve geçerli bir JWT Token döner.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponseDto<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponseDto<LoginResponseDto>.Fail("Geçersiz veri girişi.", errors));
        }

        var result = await _authService.LoginAsync(request);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// E-posta onay linki tıklandığında (GET isteğiyle) hesabı doğrular ve frontend sayfasına yönlendirir.
    /// </summary>
    // [HttpGet]: Bu metodun HTTP GET isteklerine yanıt vereceğini belirtir. Veri okumak veya link yönlendirmeleri için kullanılır.
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmailGet([FromQuery] string? email, [FromQuery] string? token)
    {
        var clientAppUrl = _configuration["ClientAppUrl"] ?? "http://localhost:4200";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return Redirect($"{clientAppUrl}/confirm-email?error={System.Net.WebUtility.UrlEncode("Geçersiz doğrulama bağlantısı.")}");
        }

        var result = await _authService.ConfirmEmailAsync(new ConfirmEmailDto { Email = email, Token = token });
        if (result.Success)
        {
            return Redirect($"{clientAppUrl}/confirm-email?verified=true&email={System.Net.WebUtility.UrlEncode(email)}");
        }

        return Redirect($"{clientAppUrl}/confirm-email?error={System.Net.WebUtility.UrlEncode(result.Message)}&email={System.Net.WebUtility.UrlEncode(email)}");
    }

    /// <summary>
    /// E-postaya gelen aktivasyon kodu veya linki ile hesabı doğrular (POST isteği).
    /// </summary>
    [HttpPost("confirm-email")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Fail("Geçersiz e-posta veya kod."));
        }

        var result = await _authService.ConfirmEmailAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// E-posta onay kodunu tekrar gönderir.
    /// </summary>
    [HttpPost("resend-confirmation")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendEmailRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request?.Email))
        {
            return BadRequest(ApiResponseDto<bool>.Fail("E-posta adresi belirtilmelidir."));
        }

        var result = await _authService.ResendConfirmationEmailAsync(request.Email);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Şifremi unuttum talebi oluşturur ve e-postaya sıfırlama linki gönderir.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Fail("Geçerli bir e-posta adresi giriniz."));
        }

        var result = await _authService.ForgotPasswordAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// E-postadaki bağlantı ile yeni şifre belirler.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponseDto<bool>.Fail("Geçersiz veri girişi.", errors));
        }

        var result = await _authService.ResetPasswordAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    // NOT: ChangePassword, UpdateProfile, UploadAvatar ve GetCurrentUser ("me") endpoint'leri
    // artık UserProfileController.cs içinde (aynı "api/auth" route'unda, IUserProfileService ile).
    // Burada tekrar tanımlanırsa route çakışması (AmbiguousMatchException) oluşur.

    /// <summary>
    /// Bir kullanıcının herkese açık, hassas olmayan profil bilgilerini (kullanıcı adı, profil fotoğrafı)
    /// getirir. Giriş yapmış olmaya gerek yok — blog yazarının bilgilerini göstermek gibi amaçlar için.
    /// </summary>
    [HttpGet("users/{id:guid}/public-profile")]
    [ProducesResponseType(typeof(ApiResponseDto<PublicUserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<PublicUserProfileDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicUserProfile(Guid id)
    {
        var user = await _dbContext.Users
            .Where(u => u.Id == id)
            .Select(u => new PublicUserProfileDto
            {
                Id = u.Id,
                Username = u.Username,
                ProfilePictureUrl = u.ProfilePictureUrl
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound(ApiResponseDto<PublicUserProfileDto>.Fail("Kullanıcı bulunamadı."));
        }

        return Ok(ApiResponseDto<PublicUserProfileDto>.Ok(user));
    }

    /// <summary>
    /// Kullanıcının oturumunu sonlandırır ve tüm cihazlardaki token'ları geçersiz kılar.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            await _authService.LogoutAsync(userId);
        }

        return Ok(ApiResponseDto<bool>.Ok(true, "Oturum başarıyla sonlandırıldı."));
    }

    /// <summary>
    /// Aktif oturumun hala geçerli olup olmadığını kontrol eder.
    /// </summary>
    [Authorize]
    [HttpGet("validate-session")]
    public async Task<IActionResult> ValidateSession()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Geçersiz oturum."));
        }

        var sessionTokenClaim = User.FindFirst("session_token")?.Value;
        var result = await _profileService.GetCurrentUserAsync(userId, sessionTokenClaim);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(ApiResponseDto<bool>.Ok(true, "Oturum aktif."));
    }


    /// <summary>
    /// Giriş yapmış kullanıcının kendi isteğiyle hesabını dondurmasını (askıya almasını) sağlar.
    /// Tekrar giriş yaptığında hesap otomatik olarak aktifleşir.
    /// </summary>
    [Authorize]
    [HttpPost("deactivate-account")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateAccount()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Geçersiz oturum."));
        }

        var result = await _authService.DeactivateAccountAsync(userId);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının hesap silme talebini başlatır, e-postaya onay bağlantısı gönderir.
    /// </summary>
    [Authorize]
    [HttpPost("request-account-deletion")]
    [HttpPost("request-delete-account")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestDeleteAccount()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Geçersiz oturum."));
        }

        var result = await _authService.RequestAccountDeletionAsync(userId);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// E-postadan gelen link tıklandığında (GET) frontend silme sayfasına yönlendirir.
    /// </summary>
    [HttpGet("confirm-account-deletion")]
    [HttpGet("confirm-delete-account")]
    public IActionResult ConfirmDeleteAccountGet([FromQuery] string? email, [FromQuery] string? token)
    {
        var clientAppUrl = _configuration["ClientAppUrl"] ?? "http://localhost:4200";

        if (string.IsNullOrWhiteSpace(token))
        {
            return Redirect($"{clientAppUrl}/confirm-delete-account?error={System.Net.WebUtility.UrlEncode("Geçersiz hesap silme bağlantısı.")}");
        }

        var emailParam = !string.IsNullOrWhiteSpace(email) ? $"&email={System.Net.WebUtility.UrlEncode(email)}" : "";
        return Redirect($"{clientAppUrl}/confirm-delete-account?token={System.Net.WebUtility.UrlEncode(token)}{emailParam}");
    }

    /// <summary>
    /// Token ve e-posta ile hesabı kalıcı olarak siler (POST).
    /// </summary>
    [HttpPost("confirm-account-deletion")]
    [HttpPost("confirm-delete-account")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmDeleteAccount([FromBody] ConfirmAccountDeletionDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Fail("Geçersiz e-posta veya token."));
        }

        var result = await _authService.ConfirmAccountDeletionAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
