using AuthenticationService.API.Extensions;
using AuthenticationService.API.Middlewares;
using Scalar.AspNetCore;

// --- BUILDER PATTERN (İnşa Edici Tasarım Deseni) ---
// WebApplication.CreateBuilder metodu, uygulamamızın başlangıç ayarlarını yapmak için kullanılır.
// Builder pattern, karmaşık nesnelerin adım adım oluşturulmasını sağlar.
// Burada 'builder' nesnesi; servislerin (Dependency Injection), konfigürasyonların (appsettings.json vb.)
// ve loglama gibi alt yapısal özelliklerin yapılandırıldığı yerdir.
var builder = WebApplication.CreateBuilder(args);

// --- DEPENDENCY INJECTION (Bağımlılık Enjeksiyonu - DI) ---
// DI, bir sınıfın ihtiyaç duyduğu bağımlılıkları (başka sınıfları/servisleri) kendi içinde oluşturmak yerine,
// dışarıdan almasını sağlayan bir tasarım prensibidir. Bu sayede kod daha test edilebilir, esnek ve modüler olur.
// 'builder.Services' koleksiyonu, uygulamamızda kullanılacak servislerin (Dependency Injection container'ına) kaydedildiği yerdir.

// 1. Uygulama Servislerini Kaydetme (Controllers, DB, DI, AutoMapper vb.)
builder.Services.AddApplicationServices(builder.Configuration);

// 2. JWT Kimlik Doğrulama ve Yetkilendirme Servislerini Kaydetme
builder.Services.AddJwtAuthentication(builder.Configuration);

// 3. OpenAPI, Swagger & Scalar UI için gerekli servisler (API dokümantasyonu)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- CORS (Cross-Origin Resource Sharing - Çapraz Kaynak Kaynak Paylaşımı) ---
// Web tarayıcıları, güvenlik nedeniyle bir web sayfasının farklı bir domain'deki (origin) API'ye 
// istek atmasını varsayılan olarak engeller. CORS, hangi domain'lerin bu API'ye erişebileceğini
// sunucu tarafında belirlememizi sağlar.
// 4. CORS politikasını servislere ekleme
builder.Services.AddCorsPolicy();

// Uygulamayı (app nesnesini) builder üzerinden inşa ediyoruz. Artık servis ekleme aşaması bitti, 
// gelen isteklerin nasıl işleneceğini (Request Pipeline) belirleme aşamasına geçiyoruz.
var app = builder.Build();

// 5. Veritabanını Başlatma (Tabloları oluşturma ve varsayılan Admin kullanıcısını ekleme)
app.InitializeDatabase();

// --- REQUEST PIPELINE (İstek İşleme Hattı / Middlewares) ---
// Gelen her HTTP isteği, burada sırasıyla eklenen 'middleware' (ara yazılım) katmanlarından geçer.
// Sıralama ÇOK ÖNEMLİDİR. Örneğin, yetkilendirme (Authorization) işleminden önce kimlik doğrulama (Authentication) yapılmalıdır.

// CORS middleware'ini aktif etme (İsteklerin belirlenen origin'lerden gelip gelmediğini kontrol eder)
app.UseCors();

// Statik dosyaların (HTML, CSS, resimler vb.) sunulmasını sağlar.
app.UseStaticFiles();

// 6. Gelen HTTP isteklerini loglayan özel middleware'imiz.
app.UseMiddleware<RequestLoggingMiddleware>();

// 7. Geliştirme (Development) ortamına özel ayarlar
if (app.Environment.IsDevelopment())
{
    // Swagger JSON çıktısını üreten middleware
    app.UseSwagger();
    // Swagger UI arayüzünü sunan middleware
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Authentication API v1");
        options.RoutePrefix = "swagger";
    });
    
    // Alternatif bir API dokümantasyon arayüzü olan Scalar UI ayarları
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Authentication Service API")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// 8. Kimlik Doğrulama ve Yetkilendirme Middleware'leri
// UseAuthentication: "Bu kullanıcı kim?" sorusunu cevaplar (Token geçerli mi?).
app.UseAuthentication();
// UseAuthorization: "Bu kullanıcının bu işlemi yapmaya yetkisi var mı?" sorusunu cevaplar.
app.UseAuthorization();

// Banlı kullanıcıları kontrol eden özel middleware (Yetkilendirmeden sonra çalışması mantıklıdır)
app.UseMiddleware<BanCheckMiddleware>();

// 9. Gelen istekleri ilgili Controller'lara yönlendiren middleware (Endpoint eşleştirme)
app.MapControllers();

// Uygulamayı başlatır ve gelen istekleri dinlemeye başlar.
app.Run();
