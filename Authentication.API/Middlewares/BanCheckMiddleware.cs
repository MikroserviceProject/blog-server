using AuthenticationService.Core.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using AuthenticationService.Core.DTOs;

namespace AuthenticationService.API.Middlewares
{
    /// <summary>
    /// Middleware'ler, HTTP istek (request) ve yanıt (response) yaşam döngüsünde araya giren yazılım bileşenleridir.
    /// Gelen isteği işleyebilir, değiştirebilir, reddedebilir veya bir sonraki middleware'e aktarabilirler.
    /// Bu BanCheckMiddleware, istek yapan kullanıcının yasaklı (banlı) olup olmadığını kontrol etmekle görevlidir.
    /// </summary>
    public class BanCheckMiddleware
    {
        // _next delegate'i (temsilcisi), işlem sırasındaki bir sonraki middleware'i veya en sonundaysa uç noktayı (endpoint) temsil eder.
        private readonly RequestDelegate _next;

        public BanCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Her HTTP isteği geldiğinde ASP.NET Core otomatik olarak InvokeAsync (veya Invoke) metodunu çağırır.
        /// Burada Middleware, isteği keser (intercept) ve araya girerek istediğimiz mantığı uygular.
        /// DI (Dependency Injection) kullanımı: Middleware'ler varsayılan olarak Singleton ömrüne sahiptir (uygulama boyunca bir kez oluşturulur).
        /// Ancak, 'AppDbContext' gibi veritabanı bağlamları 'Scoped' (istek başına oluşturulan) ömre sahiptir.
        /// Singleton bir sınıfın kurucusuna (constructor) Scoped bir bağımlılık verilemez (hata fırlatır).
        /// Bu yüzden Scoped servisler, InvokeAsync metodunun parametresi olarak (Method Injection yöntemiyle) alınır.
        /// Böylece her istek için DI konteynerinden doğru (o isteğe özel) DbContext örneği çözümlenmiş olur.
        /// </summary>
        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            // Kullanıcı kimlik doğrulamasından geçmiş mi (giriş yapmış mı) kontrol ediliyor.
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Kullanıcının kimliğinden (claim'lerinden) ID'si alınıyor.
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    // Scoped olarak enjekte ettiğimiz dbContext üzerinden kullanıcının yasaklı olup olmadığını veritabanından çekiyoruz.
                    var userStatus = await dbContext.Users
                        .Where(u => u.Id == userId)
                        .Select(u => new { u.IsBanned, u.BannedUntil })
                        .FirstOrDefaultAsync();

                    // Kullanıcı yasaklıysa ve yasağı ya süresizse (null) ya da henüz bitmemişse...
                    if (userStatus != null && userStatus.IsBanned)
                    {
                        if (!userStatus.BannedUntil.HasValue || userStatus.BannedUntil.Value > DateTime.UtcNow)
                        {
                            // İsteği burada kesip (short-circuiting), bir sonraki middleware'e veya API'ye geçmesini engelliyoruz.
                            // 403 Forbidden: Sunucu isteği anladı ancak yetkilendirme yetersizliği veya bu örnekteki gibi hesap kısıtlaması nedeniyle reddediyor.
                            // Erken yanıt döndürerek (early return) işlem tasarrufu sağlıyoruz.
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";
                            
                            var response = ApiResponseDto<object>.Fail("Hesabınız yasaklıdır. Sisteme erişiminiz engellenmiştir.");
                            
                            // Yanıtı manuel olarak JSON formatına çevirip HTTP response gövdesine (body) yazıyoruz.
                            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                            
                            // return diyerek isteğin devam etmesini (_next'e geçmesini) engelliyoruz. 
                            // Bu sayede pipeline sonlandırılmış oluyor.
                            return;
                        }
                    }
                }
            }

            // Eğer kullanıcı giriş yapmamışsa, kullanıcı yasaklı değilse veya yasağının süresi dolmuşsa
            // istek başarılı sayılır ve _next(context) çağrılarak işlem sıradaki middleware'e devredilir.
            await _next(context);
        }
    }
}
