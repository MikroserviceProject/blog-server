using System.Security.Claims;
using AuthenticationService.Core.DTOs;
using AuthenticationService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Controllers;

// [ApiController] niteliği (attribute), bu sınıfın bir API Controller olduğunu belirtir.
// Gelen isteklerde (request) otomatik model doğrulama (validation) gibi işlemleri sağlar.
[ApiController]
// [Route("api/auth")] niteliği, bu controller içindeki uç noktaların (endpoint)
// ana URL yolunu belirler. Örneğin: "https://domain.com/api/auth/..."
[Route("api/auth")]
public class AdminController : ControllerBase
{
    // Dependency Injection (Bağımlılık Enjeksiyonu - DI) için alanlar.
    // readonly olarak tanımlanmaları, sadece yapıcı metod (constructor) içerisinde atanabilmelerini sağlar,
    // böylece uygulamanın başka bir yerinde yanlışlıkla değiştirilmelerinin önüne geçilir.
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    // Controller'ın yapıcı metodu (Constructor).
    // .NET Core, Inversion of Control (IoC) konteyneri sayesinde buraya gerekli servisleri otomatik olarak enjekte eder.
    // Biz burada arayüzlere (interface) bağımlı oluyoruz, bu sayede sistem esnekliği (loose coupling) artıyor.
    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>
    /// Yeni bir Yönetici (Admin) hesabı oluşturur. Güvenlik anahtarı (AdminSecretKey) gerektirir.
    /// </summary>
    // [Authorize(Roles = "Admin")] niteliği, sadece JWT token içerisinde "Admin" rolüne sahip 
    // kullanıcıların bu uç noktaya erişebileceğini belirtir. Güvenliği sağlar.
    [Authorize(Roles = "Admin")]
    // [HttpPost] niteliği, bu metodun HTTP POST isteklerine cevap vereceğini ve 
    // "api/auth/admin/create-admin" yolunda çalışacağını belirtir.
    [HttpPost("admin/create-admin")]
    // ProducesResponseType nitelikleri, Swagger gibi dokümantasyon araçlarına,
    // metodun hangi HTTP durum kodlarını ve veri türlerini döndürebileceğini gösterir.
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequestDto request)
    {
        // ModelState.IsValid kontrolü: Gelen request nesnesinin kurallara uyup uymadığını denetler.
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            // Kötü İstek (BadRequest 400) durumunda hata mesajlarını API standartlarına göre döndürüyoruz.
            return BadRequest(ApiResponseDto<UserDto>.Fail("Geçersiz veri girişi.", errors));
        }

        // _adminService üzerinden iş mantığını (Business Logic) çağırıyoruz.
        var result = await _adminService.CreateAdminAsync(request);
        if (!result.Success)
        {
            // İşlem başarısızsa BadRequest (HTTP 400) döner.
            return BadRequest(result);
        }

        // İşlem başarılıysa Ok (HTTP 200) döner.
        return Ok(result);
    }

    /// [Admin] Tüm yazar başvurularını listeler.
    /// </summary>
    // Sadece "Admin" rolündekilerin erişimine açık olan, HTTP GET isteklerini karşılayan metod.
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/author-applications")]
    [ProducesResponseType(typeof(ApiResponseDto<List<AuthorApplicationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuthorApplications()
    {
        // AdminService'den yazar başvurularını getirir ve HTTP 200 (Ok) yanıtı ile döner.
        var result = await _adminService.GetAuthorApplicationsAsync();
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Yazar başvurusunu onaylar ve aktivasyon maili tetikler.
    /// </summary>
    // Yine sadece "Admin" yetkisi ve rota üzerinden parametre (id) alan bir POST metodu.
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/approve-author/{id}")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveAuthor(Guid id)
    {
        // Belirtilen başvuru ID'si üzerinden onaylama işlemi servis katmanına gönderilir.
        var result = await _adminService.ApproveAuthorApplicationAsync(id);
        if (!result.Success)
        {
            // Başarısızlık durumunda 400 dönüyoruz.
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
    // Parametre olarak hem rotadan 'id' değerini alıyoruz, hem de Body kısmından 'request' objesini alıyoruz.
    // '?' işareti request'in null (boş) olabileceğini belirtir.
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
    // Sayfalama işlemleri için FromQuery kullanılarak URL üzerinden (örn: ?pageNumber=1&pageSize=10)
    // opsiyonel parametreler alıyoruz.
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        // İstek parametrelerini servis katmanına iletiyor ve sonucu doğrudan geri döndürüyoruz.
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
        // Gelen veride UserId boş mu veya ban sebebi verilmemiş mi diye basit bir kontrol yapılıyor.
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
        // Body'den gelen bildirim objesi için model doğrulama yapılıyor.
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
