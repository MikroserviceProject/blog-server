using System.Security.Claims;
using AuthenticationService.Core.DTOs;
using AuthenticationService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Controllers;

// [ApiController]: Bu sınıfın bir API Controller olduğunu belirtir ve ModelState doğrulaması gibi işlemlerin otomatik yapılmasını sağlar.
[ApiController]
// [Route]: İstemcilerin (client) bu controller'daki endpointlere hangi URL üzerinden (örn: /api/auth) erişeceğini belirler.
[Route("api/auth")]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _profileService;
    private readonly ILogger<UserProfileController> _logger;

    // Constructor (Yapıcı Metot) - Dependency Injection (Bağımlılık Enjeksiyonu):
    // ASP.NET Core IoC (Inversion of Control) container'ı, gerekli servisleri (IUserProfileService ve ILogger) 
    // buraya otomatik olarak enjekte eder (Dependency Injection).
    // Böylece controller nesne üretim süreçlerinden bağımsızlaşarak Gevşek Bağımlı (Loosely Coupled) hale gelir.
    public UserProfileController(IUserProfileService profileService, ILogger<UserProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcı hesabını dondurur.
    /// </summary>
    // [Authorize]: Sadece yetkilendirilmiş (geçerli bir JWT token sunan) kullanıcıların erişimine izin verir. Token yoksa veya geçersizse 401 Unauthorized döner.
    [Authorize]
    // [HttpPost]: Bu metoda "POST /api/auth/deactivate" şeklinde istek atılabileceğini belirtir.
    [HttpPost("deactivate")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateAccount()
    {
        // İstekte bulunan kullanıcının Token'ı içindeki NameIdentifier (genelde Kullanıcı ID) bilgisini okuruz.
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Yetkisiz erişim."));
        }

        // İş mantığı (Business Logic) servis katmanına devredilir, Controller sadece trafiği yönlendirir.
        var result = await _profileService.DeactivateAccountAsync(userId);
        if (!result.Success)
        {
            // İşlem başarısız olursa HTTP 400 döner.
            return BadRequest(result);
        }

        // İşlem başarılı olursa HTTP 200 döner.
        return Ok(result);
    }

    /// <summary>
    /// Kullanıcı şifresini değiştirir.
    /// </summary>
    // [Authorize]: Yetkisiz erişimleri engeller.
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    // [FromBody]: Gelen HTTP isteğinin (Request) gövdesinden (Body) JSON verisini alıp 'request' nesnesine dönüştürür (Deserialization).
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        // ModelState: Gelen modelin kurallara (örn. Data Annotations) uygun olup olmadığını denetler. 
        // [ApiController] kullanımı sayesinde aslında bu kontrol otomatiktir, ancak burada manuel hata döndürülmek istenmiştir.
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponseDto<bool>.Fail("Geçersiz veri girişi.", errors));
        }

        // Token'dan aktif kullanıcı bilgisini çekiyoruz.
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Yetkisiz erişim."));
        }

        var result = await _profileService.ChangePasswordAsync(userId, request);
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

        var result = await _profileService.UpdateProfileAsync(userId, request);
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
    public async Task<IActionResult> UploadAvatar( IFormFile? file, [FromQuery] string? avatarUrl = null)
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

        var result = await _profileService.UpdateAvatarAsync(userId, finalAvatarUrl);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının kapak fotoğrafını yükler.
    /// </summary>
    [Authorize]
    [HttpPost("upload-cover")]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadCover(IFormFile? file, [FromQuery] string? coverUrl = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<UserDto>.Fail("Yetkisiz erişim."));
        }

        string? finalCoverUrl = coverUrl;

        if (file != null && file.Length > 0)
        {
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(ApiResponseDto<UserDto>.Fail("Kapak resmi boyutu en fazla 10 MB olabilir."));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponseDto<UserDto>.Fail("Desteklenen resim formatları: JPG, PNG, WEBP, GIF."));
            }

            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "covers");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{userId}_cover_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            finalCoverUrl = $"/uploads/covers/{uniqueFileName}";
        }
        else if (string.IsNullOrWhiteSpace(finalCoverUrl))
        {
            return BadRequest(ApiResponseDto<UserDto>.Fail("Lütfen geçerli bir kapak resmi dosyası veya bağlantısı sağlayınız."));
        }

        var result = await _profileService.UpdateCoverAsync(userId, finalCoverUrl);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Okur statüsündeki kullanıcının Yazar olmak için başvuru yapmasını sağlar.
    /// </summary>
    [Authorize]
    [HttpPost("apply-author")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyAuthor([FromForm] string university, IFormFile cvFile)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Yetkisiz erişim."));
        }

        if (string.IsNullOrWhiteSpace(university))
        {
            return BadRequest(ApiResponseDto<bool>.Fail("Üniversite/Bölüm bilgisi zorunludur."));
        }

        if (cvFile == null || cvFile.Length == 0)
        {
            return BadRequest(ApiResponseDto<bool>.Fail("CV dosyası zorunludur."));
        }

        if (cvFile.Length > 10 * 1024 * 1024)
        {
            return BadRequest(ApiResponseDto<bool>.Fail("CV dosya boyutu en fazla 10 MB olabilir."));
        }

        var extension = Path.GetExtension(cvFile.FileName).ToLowerInvariant();
        if (extension != ".pdf")
        {
            return BadRequest(ApiResponseDto<bool>.Fail("Lütfen sadece PDF formatında CV yükleyiniz."));
        }

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", "cvs");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"cv_{userId}_{Guid.NewGuid():N}.pdf";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await cvFile.CopyToAsync(stream);
        }

        var cvUrl = $"/uploads/cvs/{uniqueFileName}";

        var result = await _profileService.ApplyForAuthorAsync(userId, university, cvUrl);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// JWT Token ile giriş yapmış olan kullanıcının profil bilgilerini ve aktif oturum durumunu getirir.

    /// </summary>
    // [Authorize]: Kullanıcının kimliğinin doğrulanmış olması şartını koşar.
    [Authorize]
    // [HttpGet]: Veri okuma/getirme işlemleri için kullanılan HTTP Get metodudur. "GET /api/auth/me" yolunda çalışır.
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

        // Kullanıcının oluşturulan oturum token bilgisini claims üzerinden okuruz (Gelişmiş Güvenlik/Oturum Yönetimi).
        var sessionTokenClaim = User.FindFirst("session_token")?.Value;
        
        // Servis katmanından kullanıcının verilerini ve aktif oturum bilgilerini talep ederiz.
        var result = await _profileService.GetCurrentUserAsync(userId, sessionTokenClaim);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>

    /// Kullanıcının sistem içi bildirimlerini listeler.
    /// </summary>
    [Authorize]
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(ApiResponseDto<PaginatedResultDto<UserNotificationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool unreadOnly = false)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<PaginatedResultDto<UserNotificationDto>>.Fail("Geçersiz oturum."));
        }

        var result = await _profileService.GetUserNotificationsAsync(userId, page, pageSize, unreadOnly);
        return Ok(result);
    }

    /// <summary>

    /// Bildirimi okundu olarak işaretler.
    /// </summary>
    [Authorize]
    [HttpPost("notifications/{id}/read")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkNotificationAsRead(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Geçersiz oturum."));
        }

        var result = await _profileService.MarkNotificationAsReadAsync(userId, id);
        return Ok(result);
    }

    /// <summary>
    /// Herkese açık (Public) profil bilgilerini getirir.
    /// </summary>
    [HttpGet("public/profile/{username}")]
    [ProducesResponseType(typeof(ApiResponseDto<PublicUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicProfile(string username)
    {
        var result = await _profileService.GetPublicProfileByUsernameAsync(username);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Kullanıcı arama (Search). Aktif tüm kullanıcıları kullanıcı adı veya e-posta ile arar.
    /// </summary>
    [HttpGet("public/search-users")]
    [ProducesResponseType(typeof(ApiResponseDto<List<PublicUserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchPublicUsers([FromQuery] string query)
    {
        var result = await _profileService.SearchPublicUsersAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// ID listesine göre herkese açık profil bilgilerini getirir.
    /// </summary>
    [HttpPost("public/bulk-profiles")]
    [ProducesResponseType(typeof(ApiResponseDto<List<PublicUserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBulkPublicProfiles([FromBody] List<Guid> userIds)
    {
        var result = await _profileService.GetPublicProfilesByIdsAsync(userIds);
        return Ok(result);
    }
}
