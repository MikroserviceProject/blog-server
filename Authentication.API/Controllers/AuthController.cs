using System.Security.Claims;
using AuthenticationService.Core.DTOs;
using AuthenticationService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _authService = authService;
        _configuration = configuration;
        _logger = logger;
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
    /// Swagger / API üzerinden doğrudan Yönetici (Admin) hesabı oluşturur.
    /// </summary>
    [HttpPost("create-admin")]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponseDto<UserDto>.Fail("Geçersiz veri girişi.", errors));
        }

        var result = await _authService.CreateAdminAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Yazar başvurusu için PDF dosya yüklemeli çok parçalı (multipart/form-data) kayıt endpoint'i.
    /// </summary>
    [HttpPost("register-author")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterAuthor(
        [FromForm] string username,
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string university,
        [FromForm] IFormFile? cvFile)
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

    /// <summary>
    /// Giriş yapmış kullanıcının profilinden mevcut şifresini değiştirmesini sağlar.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponseDto<bool>.Fail("Geçersiz veri girişi.", errors));
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Yetkisiz erişim."));
        }

        var result = await _authService.ChangePasswordAsync(userId, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Giriş yapmış kullanıcının profil bilgilerini günceller.
    /// </summary>
    [Authorize]
    [HttpPut("update-profile")]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponseDto<UserDto>.Fail("Geçersiz veri girişi.", errors));
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<UserDto>.Fail("Yetkisiz erişim."));
        }

        var result = await _authService.UpdateProfileAsync(userId, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının profil resmini yükler.
    /// </summary>
    [Authorize]
    [HttpPost("upload-avatar")]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile? file, [FromQuery] string? avatarUrl = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<UserDto>.Fail("Yetkisiz erişim."));
        }

        string? finalAvatarUrl = avatarUrl;

        if (file != null && file.Length > 0)
        {
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(ApiResponseDto<UserDto>.Fail("Profil resmi boyutu en fazla 5 MB olabilir."));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponseDto<UserDto>.Fail("Desteklenen resim formatları: JPG, PNG, WEBP, GIF."));
            }

            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{userId}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            finalAvatarUrl = $"/uploads/avatars/{uniqueFileName}";
        }
        else if (string.IsNullOrWhiteSpace(finalAvatarUrl))
        {
            return BadRequest(ApiResponseDto<UserDto>.Fail("Lütfen geçerli bir profil resmi dosyası veya bağlantısı sağlayınız."));
        }

        var result = await _authService.UpdateAvatarAsync(userId, finalAvatarUrl);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// JWT Token ile giriş yapmış olan kullanıcının profil bilgilerini ve aktif oturum durumunu getirir.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<UserDto>.Fail("Yetkisiz erişim. Geçerli bir token bulunamadı."));
        }

        var sessionTokenClaim = User.FindFirst("session_token")?.Value;
        var result = await _authService.GetCurrentUserAsync(userId, sessionTokenClaim);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
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
        var result = await _authService.GetCurrentUserAsync(userId, sessionTokenClaim);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(ApiResponseDto<bool>.Ok(true, "Oturum aktif."));
    }

    #region Admin Yazar Başvuru Yönetim Endpoint'leri

    /// <summary>
    /// [Admin] Tüm yazar başvurularını listeler.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/author-applications")]
    [ProducesResponseType(typeof(ApiResponseDto<List<AuthorApplicationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuthorApplications()
    {
        var result = await _authService.GetAuthorApplicationsAsync();
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Yazar başvurusunu onaylar ve aktivasyon maili tetikler.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/approve-author/{id}")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveAuthor(Guid id)
    {
        var result = await _authService.ApproveAuthorApplicationAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [Admin] Yazar başvurusunu gerekçesiyle birlikte reddeder.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/reject-author/{id}")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectAuthor(Guid id, [FromBody] RejectAuthorRequestDto? request)
    {
        var result = await _authService.RejectAuthorApplicationAsync(id, request?.Reason);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    #endregion

    /// <summary>
    /// Geliştirme ekranında veritabanı tablosundaki tüm kayıtları listeler.
    /// </summary>
    [HttpGet("database-users")]
    public async Task<IActionResult> GetDatabaseUsers([FromServices] AuthenticationService.Core.Data.AppDbContext context)
    {
        var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            context.Users.Select(u => new 
            {
                u.Id,
                u.Username,
                u.Email,
                u.Role,
                u.University,
                u.CvUrl,
                u.AuthorApprovalStatus,
                u.AuthorApplicationDate,
                u.CreatedAt,
                u.IsEmailConfirmed
            })
        );

        return Ok(users);
    }
}
