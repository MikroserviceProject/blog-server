using System.Security.Claims;
using AuthenticationService.Core.DTOs;
using AuthenticationService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>
    /// Yeni bir Yönetici (Admin) hesabı oluşturur. Güvenlik anahtarı (AdminSecretKey) gerektirir.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/create-admin")]
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

        var result = await _adminService.CreateAdminAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// [Admin] Tüm yazar başvurularını listeler.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/author-applications")]
    [ProducesResponseType(typeof(ApiResponseDto<List<AuthorApplicationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuthorApplications()
    {
        var result = await _adminService.GetAuthorApplicationsAsync();
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
        var result = await _adminService.ApproveAuthorApplicationAsync(id);
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
        var result = await _adminService.RejectAuthorApplicationAsync(id, request?.Reason);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [Admin] Sistemdeki tüm kayıtlı kullanıcıları listeler (sayfalama ve arama destekli).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/users")]
    [ProducesResponseType(typeof(ApiResponseDto<PaginatedResultDto<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _adminService.GetAllUsersAsync(pageNumber, pageSize, searchTerm);
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Kullanıcıyı süreli veya süresiz askıya alır (banlar) ve mail gönderir.

    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/ban-user")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BanUser([FromBody] BanUserRequestDto request)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.GetEffectiveReason()))
        {
            return BadRequest(ApiResponseDto<bool>.Fail("Lütfen geçerli bir kullanıcı ve en az 3 karakterli ban gerekçesi belirtiniz."));
        }

        var result = await _adminService.BanUserAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [Admin] Kullanıcının banını kaldırır.

    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/unban-user/{id}")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnbanUser(Guid id)
    {
        var result = await _adminService.UnbanUserAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [Admin] Kullanıcıya özel sistem içi bildirim / uyarı iletir.

    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/notify-user")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> NotifyUser([FromBody] AdminSendNotificationDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Fail("Başlık ve mesaj içeriği zorunludur."));
        }

        var result = await _adminService.SendAdminNotificationAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }



    /// <summary>
    /// Kullanıcı hesap silme talebinde bulunur, e-posta adresine onay linki gönderilir.
    /// </summary>

}
