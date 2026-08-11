using System.Security.Claims;
using AuthenticationService.Core.DTOs;
using AuthenticationService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _profileService;
    private readonly ILogger<UserProfileController> _logger;

    public UserProfileController(IUserProfileService profileService, ILogger<UserProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcı hesabını dondurur.
    /// </summary>
    [Authorize]
    [HttpPost("deactivate")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateAccount()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<bool>.Fail("Yetkisiz erişim."));
        }

        var result = await _profileService.DeactivateAccountAsync(userId);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Kullanıcı şifresini değiştirir.
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
    [ProducesResponseType(typeof(ApiResponseDto<List<UserNotificationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponseDto<List<UserNotificationDto>>.Fail("Geçersiz oturum."));
        }

        var result = await _profileService.GetUserNotificationsAsync(userId);
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


}
