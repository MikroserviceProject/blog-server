using System.Diagnostics;

namespace AuthenticationService.API.Middlewares;

/// <summary>
/// Middleware (Ara Yazılım), ASP.NET Core uygulamalarında HTTP istekleri (request) ve yanıtları (response)
/// üzerinde işlem yapmak için kullanılan bir tasarım desenidir. Gelen her istek, bir pipeline (boru hattı) 
/// üzerinden geçer. Middleware'ler bu pipeline'da arka arkaya sıralanır.
/// 
/// Bu özel Middleware'in (RequestLoggingMiddleware) amacı: Sisteme gelen tüm isteklerin ne zaman başladığını, 
/// hangi adrese yapıldığını kaydetmek ve istek tamamlandığında veya hata aldığında ne kadar sürdüğünü ölçmektir.
/// </summary>
public class RequestLoggingMiddleware
{
    // _next temsilcisi (delegate), pipeline'daki bir sonraki middleware'i işaret eder.
    // İstek (request) bizim middleware'imize geldikten sonra, işini yapıp bir sonrakine devretmesini bu sağlar.
    // Eğer await _next(context) demezsek, istek pipeline'da tıkanır ve ileri gidemez (Kısa devre - Short-circuiting).
    private readonly RequestDelegate _next;
    
    // Uygulama akışında olan bitenleri konsola veya log dosyalarına yazmak için ILogger kullanıyoruz.
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    // Dependency Injection (Bağımlılık Enjeksiyonu) aracılığıyla pipeline'daki bir sonraki adımı (next) ve Logger'ı alıyoruz.
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // Middleware'ler her bir HTTP isteğinde bu "InvokeAsync" veya "Invoke" metodunu çalıştırırlar.
    // HttpContext nesnesi, o anki isteğe (Request) ve verilecek yanıta (Response) dair her türlü bilgiyi barındırır.
    public async Task InvokeAsync(HttpContext context)
    {
        // İsteğin ne kadar sürdüğünü ölçmek için bir kronometre başlatıyoruz.
        var stopwatch = Stopwatch.StartNew();
        
        var request = context.Request;
        var requestPath = request.Path;
        var requestMethod = request.Method;

        // İstek içeriye girer girmez (bir sonraki adıma geçmeden önce) ilk logumuzu atıyoruz.
        _logger.LogInformation("➡️ [HTTP Request] {Method} {Path}{QueryString}", 
            requestMethod, 
            requestPath, 
            request.QueryString.HasValue ? request.QueryString.Value : string.Empty);

        try
        {
            // İsteği pipeline'daki bir sonraki middleware'e devrediyoruz.
            // Örneğin bizden sonra Authentication, Authorization veya Controller tetiklenebilir.
            // Bu metot ancak geri kalan tüm işlemler bitip Response dönmeye başladığında await'ten çıkar.
            await _next(context);

            // Geri dönüş (Response) aşaması:
            // İşlemler bitti, kronometreyi durduruyoruz.
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;

            // İsteğin başarıyla sonuçlandığını ve ne kadar sürdüğünü logluyoruz.
            _logger.LogInformation("⬅️ [HTTP Response] {Method} {Path} -> Durum: {StatusCode} | Süre: {ElapsedMs} ms", 
                requestMethod, 
                requestPath, 
                statusCode, 
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            // Eğer pipeline'ın geri kalanında işlenmeyen bir hata (Exception) fırlatılırsa, buraya düşeriz.
            stopwatch.Stop();
            
            // Hata detayını ve isteğin patlamadan önce ne kadar sürdüğünü Error seviyesinde logluyoruz.
            _logger.LogError(ex, "💥 [HTTP Error] {Method} {Path} işlenirken beklenmeyen hata oluştu! Süre: {ElapsedMs} ms", 
                requestMethod, 
                requestPath, 
                stopwatch.ElapsedMilliseconds);
            
            // Hatayı yutmayıp fırlatmaya devam ediyoruz ki, varsayılan exception handler (varsa) işleyebilsin 
            // ve istemciye (client) 500 Internal Server Error dönsün.
            throw;
        }
    }
}
