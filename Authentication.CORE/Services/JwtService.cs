using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthenticationService.Core.Entities;
using AuthenticationService.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticationService.Core.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user, out int expiresInMinutes)
    {
        // 1. ADIM: Konfigürasyondan gerekli ayarları okuma
        // Uygulamamızın ayarlarından JWT imzalama şifresini (secret key), yayınlayıcı (issuer) 
        // ve hedef kitleyi (audience) alıyoruz. Eğer yapılandırılmamışsa varsayılan değerler kullanıyoruz.
        var secretKey = _configuration["JwtSettings:SecretKey"] 
            ?? "VarsayilanCokGizliVeUzunBirJwtSecretKey1234567890!@#$%^&*";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "AuthenticationService";
        var audience = _configuration["JwtSettings:Audience"] ?? "MikroservisApp";
        
        // JWT'nin geçerlilik süresini dakikalar cinsinden okuyoruz.
        if (!int.TryParse(_configuration["JwtSettings:ExpirationInMinutes"], out expiresInMinutes))
        {
            expiresInMinutes = 60; // Varsayılan 60 dakika
        }

        // 2. ADIM: Güvenlik anahtarı ve imzalama kimlik bilgileri oluşturma
        // Secret Key'imizi byte dizisine çevirip SymmetricSecurityKey (Simetrik Güvenlik Anahtarı) oluşturuyoruz.
        // Simetrik şifrelemede hem token'ı şifrelerken (imzalarken) hem de çözerken (doğrularken) aynı anahtar kullanılır.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        
        // HmacSha256 algoritması kullanarak bu güvenlik anahtarıyla imzalama bilgilerimizi hazırlıyoruz.
        // Bu kimlik bilgileri (credentials), token'ın bizim tarafımızdan oluşturulduğunu ve değiştirilmediğini kanıtlar.
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 3. ADIM: Kullanıcı bilgilerini (Claims - Hak Talepleri) hazırlama
        // Token'ın içine gömülecek ve karşı tarafın (istemcinin ve diğer servislerin) okuyabileceği veriler.
        var claims = new List<Claim>
        {
            // Kullanıcının benzersiz kimliği (genelde veritabanındaki ID)
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            // Kullanıcının adı veya kullanıcı adı
            new(ClaimTypes.Name, user.Username),
            // E-posta adresi
            new(ClaimTypes.Email, user.Email),
            // Kullanıcının sahip olduğu rol (Admin, User vb.) - Yetkilendirme (Authorization) için kritiktir
            new(ClaimTypes.Role, user.Role),
            // JWT'nin benzersiz kimliği (JTI - JWT ID). Replay ataklarını engellemek vb. için kullanılır.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Eğer kullanıcının mevcut bir oturum token'ı varsa (Örn: Cihaz kontrolü veya Concurrent Login kontrolü için)
        // Bunu da claim olarak ekliyoruz.
        if (!string.IsNullOrEmpty(user.CurrentSessionToken))
        {
            claims.Add(new Claim("session_token", user.CurrentSessionToken));
        }

        // 4. ADIM: Token özelliklerini (Descriptor) tanımlama
        // Token'ın içeriğini (Subject), ne zaman sona ereceğini (Expires),
        // Kim tarafından verildiğini (Issuer) ve kime yönelik olduğunu (Audience) belirliyoruz.
        // Ayrıca oluşturduğumuz imzalama bilgilerini (SigningCredentials) de ekliyoruz.
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        // 5. ADIM: Token'ı oluşturma ve string'e çevirme
        // JwtSecurityTokenHandler sınıfı, tanımladığımız özelliklere (descriptor) göre gerçek JWT'yi yaratır.
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        // Oluşturulan token'ı string (metin) formatında döndürüyoruz ki istemci bunu kullanabilsin.
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        // Token doğrulama işlemi için ayarları okuyoruz.
        var secretKey = _configuration["JwtSettings:SecretKey"] 
            ?? "VarsayilanCokGizliVeUzunBirJwtSecretKey1234567890!@#$%^&*";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "AuthenticationService";
        var audience = _configuration["JwtSettings:Audience"] ?? "MikroservisApp";

        var tokenHandler = new JwtSecurityTokenHandler();
        
        // Gelen token'ı doğrularken hangi kurallara dikkat edeceğimizi (TokenValidationParameters) belirtiyoruz.
        var validationParameters = new TokenValidationParameters
        {
            // 1. İmzalayan Anahtarı Doğrula: Token'ın bizdeki gizli anahtarla imzalanıp imzalanmadığına bakar
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            
            // 2. Yayınlayıcıyı Doğrula: Token'ı kimin oluşturduğuna (Issuer) bakar
            ValidateIssuer = true,
            ValidIssuer = issuer,
            
            // 3. Hedef Kitleyi Doğrula: Token'ın bu uygulama için mi gönderildiğine (Audience) bakar
            ValidateAudience = true,
            ValidAudience = audience,
            
            // 4. Yaşam Süresini Doğrula: Token'ın süresinin dolup dolmadığını (Expiration) kontrol eder
            ValidateLifetime = true,
            
            // Sunucular arasındaki saat farklarını tolere etmek için kullanılır, tam zamanında dolması için sıfır yapıyoruz
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            // Token'ı doğrular ve içindeki kullanıcı bilgilerini (ClaimsPrincipal) çıkarıp döndürür
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            // Eğer token geçersizse, süresi dolmuşsa veya değiştirilmişse Exception fırlatır, null döneriz
            return null;
        }
    }
}
