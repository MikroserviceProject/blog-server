using System.Net;
using System.Net.Mail;
using AuthenticationService.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthenticationService.Core.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailConfirmationAsync(string toEmail, string username, string confirmationToken)
    {
        var subject = "Lumina - E-Posta Adresinizi Doğrulayın";
        var clientAppUrl = _configuration["ClientAppUrl"] ?? "http://localhost:4200";
        var confirmUrl = $"{clientAppUrl}/confirm-email?email={System.Net.WebUtility.UrlEncode(toEmail)}&token={System.Net.WebUtility.UrlEncode(confirmationToken)}";

        var body = GetMailTemplate(
            title: "Hesap Doğrulama",
            greeting: $"Merhaba {System.Net.WebUtility.HtmlEncode(username)},",
            message: "Lumina hesabınızı başarıyla oluşturdunuz. Hesabınızı güvenle aktifleştirmek için aşağıdaki butona tıklayarak e-posta adresinizi tek tıkla doğrulayabilirsiniz:",
            buttonText: "✉️ E-Posta Adresimi Doğrula",
            buttonUrl: confirmUrl,
            note: "Bu bağlantı 24 saat boyunca geçerlidir."
        );

        return await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }

    public async Task<bool> SendAuthorApplicationReceivedAsync(string toEmail, string username)
    {
        var subject = "Lumina - Yazar Başvurunuz Alındı";

        var body = GetMailTemplate(
            title: "Yazar Başvurusu Alındı",
            greeting: $"Merhaba {System.Net.WebUtility.HtmlEncode(username)},",
            message: "Lumina platformuna yapmış olduğunuz <strong>Yazar Başvurusu</strong> ve CV dosyanız başarıyla sistem yöneticisine iletilmiştir.<br><br>Başvurunuz editör ekibimiz ve yöneticimiz tarafından incelendikten sonra tarafınıza onay e-postası ve hesap aktivasyon bağlantısı iletilecektir. Bu süreçte hesabınız bekleme durumundadır.",
            buttonText: null,
            buttonUrl: null,
            note: "Değerlendirme süreci genellikle 24 saat içerisinde tamamlanmaktadır."
        );

        return await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }

    public async Task<bool> SendAuthorApprovedAsync(string toEmail, string username, string confirmationToken)
    {
        var subject = "🎉 Tebrikler! Lumina Yazar Başvurunuz Onaylandı";
        var clientAppUrl = _configuration["ClientAppUrl"] ?? "http://localhost:4200";
        var confirmUrl = $"{clientAppUrl}/confirm-email?email={System.Net.WebUtility.UrlEncode(toEmail)}&token={System.Net.WebUtility.UrlEncode(confirmationToken)}";

        var body = GetMailTemplate(
            title: "Yazar Başvurunuz Onaylandı! 🎉",
            greeting: $"Tebrikler {System.Net.WebUtility.HtmlEncode(username)}!",
            message: "Lumina platformuna yaptığınız <strong>Yazar Başvurusu</strong> yöneticimiz tarafından incelenmiş ve onaylanmıştır.<br><br>Hesabınızı aktifleştirmek ve içeriklerinizi yayınlamaya başlamak için lütfen aşağıdaki butona tıklayarak e-posta adresinizi onaylayınız:",
            buttonText: "✨ Hesabımı Aktifleştir ve Başla",
            buttonUrl: confirmUrl,
            note: "Aktivasyonun ardından belirlediğiniz şifre ile doğrudan giriş yapabilirsiniz."
        );

        return await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }

    public async Task<bool> SendAuthorRejectedAsync(string toEmail, string username, string? reason)
    {
        var subject = "Lumina - Yazar Başvurunuz Hakkında Bilgilendirme";
        var reasonText = !string.IsNullOrWhiteSpace(reason) 
            ? $"<br><br><strong>Açıklama:</strong> {System.Net.WebUtility.HtmlEncode(reason)}" 
            : "";

        var body = GetMailTemplate(
            title: "Başvuru Durumu Bilgilendirmesi",
            greeting: $"Merhaba {System.Net.WebUtility.HtmlEncode(username)},",
            message: $"Lumina platformuna yapmış olduğunuz yazar başvurusu incelenmiş olup, mevcut içerik planlamamız ve kriterlerimiz doğrultusunda şu aşamada onaylanamamıştır.{reasonText}<br><br>Dilerseniz standart <strong>Okur</strong> hesabı açarak platformumuzdaki yayınları takip edebilirsiniz.",
            buttonText: null,
            buttonUrl: null,
            note: "Gelecekteki başvurularınız için profilinizi ve portfolyonuzu güncelleyebilirsiniz."
        );

        return await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }

    public async Task<bool> SendPasswordResetAsync(string toEmail, string username, string resetToken)
    {
        var subject = "Lumina - Şifre Sıfırlama Talebi";
        var clientAppUrl = _configuration["ClientAppUrl"] ?? "http://localhost:4200";
        var resetUrl = $"{clientAppUrl}/reset-password?email={System.Net.WebUtility.UrlEncode(toEmail)}&token={System.Net.WebUtility.UrlEncode(resetToken)}";

        var body = GetMailTemplate(
            title: "Şifre Sıfırlama Talebi",
            greeting: $"Merhaba {System.Net.WebUtility.HtmlEncode(username)},",
            message: "Hesabınız için şifre sıfırlama talebinde bulunuldu. Yeni şifrenizi belirlemek için aşağıdaki butona tıklayınız.<br><br>Eğer bu talebi siz yapmadıysanız bu e-postayı güvenle görmezden gelebilirsiniz.",
            buttonText: "🔒 Şifremi Sıfırla",
            buttonUrl: resetUrl,
            note: "Bu sıfırlama bağlantısı güvenlik nedeniyle 24 saat süreyle geçerlidir."
        );

        return await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }

    public async Task<bool> SendUserBannedEmailAsync(string toEmail, string username, string reason, DateTime? bannedUntil)
    {
        var subject = "⚠️ Lumina - Hesabınız Askıya Alındı";
        var clientAppUrl = _configuration["ClientAppUrl"] ?? "http://localhost:4200";
        var profileUrl = $"{clientAppUrl}/profile";

        var durationInfo = bannedUntil.HasValue
            ? $"<strong>Askı Süresi:</strong> {bannedUntil.Value:dd.MM.yyyy HH:mm} tarihine kadar sürelidir."
            : "<strong>Askı Süresi:</strong> Süresiz (Kalıcı) olarak engellenmiştir.";

        var body = GetMailTemplate(
            title: "Hesabınız Askıya Alındı ⛔",
            greeting: $"Sayın {System.Net.WebUtility.HtmlEncode(username)},",
            message: $"Lumina platformundaki hesabınız yönetici kararıyla askıya alınmıştır.<br><br>" +
                    $"<strong>Askıya Alınma Gerekçesi:</strong><br><blockquote style='background:#f1f5f9;padding:12px 16px;border-left:4px solid #ef4444;margin:12px 0;border-radius:4px;'>{System.Net.WebUtility.HtmlEncode(reason)}</blockquote><br>" +
                    $"{durationInfo}<br><br>" +
                    $"Askı süresi boyunca platform içeriklerine erişiminiz kısıtlanmıştır. Dilerseniz hesabınıza giriş yaparak hesabınızı kalıcı olarak silme talebinde bulunabilirsiniz.",
            buttonText: "🔒 Hesabıma Git",
            buttonUrl: profileUrl,
            note: "Karara itiraz etmek veya detaylı bilgi almak için destek ekibiyle iletişime geçebilirsiniz."
        );

        return await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }

    public async Task<bool> SendPostDeletedEmailAsync(string toEmail, string username, string postTitle, string reason)
    {
        var subject = "⚠️ Lumina - Yazınız Yayından Kaldırıldı";

        var body = GetMailTemplate(
            title: "Yazınız Yayından Kaldırıldı 🗑️",
            greeting: $"Sayın {System.Net.WebUtility.HtmlEncode(username)},",
            message: $"Lumina platformunda yayınlamış olduğunuz <strong>\"{System.Net.WebUtility.HtmlEncode(postTitle)}\"</strong> başlıklı yazınız topluluk ve yayın ilkeleri doğrultusunda yönetici tarafından yayından kaldırılmıştır.<br><br>" +
                    $"<strong>Kaldırılma Gerekçesi:</strong><br><blockquote style='background:#f1f5f9;padding:12px 16px;border-left:4px solid #f59e0b;margin:12px 0;border-radius:4px;'>{System.Net.WebUtility.HtmlEncode(reason)}</blockquote><br>" +
                    $"Platform kurallarına uygun içerikler paylaşarak yayın hayatınıza devam edebilirsiniz.",
            buttonText: null,
            buttonUrl: null,
            note: "İçerik kurallarımız hakkında daha fazla bilgi almak için platform yönergelerimizi inceleyebilirsiniz."
        );

        return await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }

    public async Task<bool> SendAccountDeletionConfirmationAsync(string toEmail, string username, string token)
    {
        var subject = "⚠️ Lumina - Hesap Silme Onayı";
        var clientAppUrl = _configuration["ClientAppUrl"] ?? "http://localhost:4200";
        var confirmDeleteUrl = $"{clientAppUrl}/confirm-delete-account?email={System.Net.WebUtility.UrlEncode(toEmail)}&token={System.Net.WebUtility.UrlEncode(token)}";

        var body = GetMailTemplate(
            title: "Hesap Silme Onayı ⚠️",
            greeting: $"Sayın {System.Net.WebUtility.HtmlEncode(username)},",
            message: "Lumina hesabınızı kalıcı olarak silme talebinde bulundunuz.<br><br>" +
                    "<strong style='color:#ef4444;'>DİKKAT:</strong> Hesabınızı sildiğinizde profil bilgileriniz, oturumunuz ve platformdaki tüm yetkileriniz geri döndürülemez şekilde silinecektir.<br><br>" +
                    "Hesabınızı kalıcı olarak silmek istiyorsanız aşağıdaki butona tıklayarak işlemi onaylayınız:",
            buttonText: "🗑️ Hesabımı Kalıcı Olarak Sil",
            buttonUrl: confirmDeleteUrl,
            note: "Bu hesap silme bağlantısı güvenlik nedeniyle 1 saat süreyle geçerlidir. Bu talebi siz yapmadıysanız şifrenizi hemen değiştiriniz."
        );

        return await SendEmailAsync(toEmail, subject, body, isHtml: true);
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        var host = _configuration["SmtpSettings:Host"];
        var portStr = _configuration["SmtpSettings:Port"];
        var username = _configuration["SmtpSettings:Username"];
        var password = _configuration["SmtpSettings:Password"];
        var fromEmail = _configuration["SmtpSettings:FromEmail"] ?? username ?? "noreply@mikroservis.local";
        var enableSsl = bool.TryParse(_configuration["SmtpSettings:EnableSsl"], out var ssl) && ssl;

        // Eğer SMTP bilgileri girilmemişse (Development ortamında) token'ı konsola logla ve başarılı dön
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _logger.LogInformation("📧 [DEVELOPMENT MAIL MOCK] Alıcı: {To} | Konu: {Subject}\nİçerik: {Body}", toEmail, subject, body);
            return true;
        }

        try
        {
            int port = int.TryParse(portStr, out var p) ? p : 587;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username.Trim(), password.Replace(" ", "").Trim()),
                EnableSsl = enableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "Lumina Kimlik Sistemi"),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("✅ E-posta başarıyla gönderildi: {ToEmail}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ E-posta gönderilirken hata oluştu: {ToEmail}", toEmail);
            return false;
        }
    }

    private static string GetMailTemplate(string title, string greeting, string message, string? buttonText, string? buttonUrl, string? note)
    {
        var buttonHtml = !string.IsNullOrEmpty(buttonText) && !string.IsNullOrEmpty(buttonUrl)
            ? $$"""
              <div style="text-align: center; margin: 32px 0;">
                  <a href="{{buttonUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e3a8a 0%, #0f172a 100%); color: #ffffff !important; padding: 16px 36px; border-radius: 10px; text-decoration: none; font-weight: 700; font-size: 16px; box-shadow: 0 6px 18px rgba(30, 58, 138, 0.35); letter-spacing: 0.3px;" target="_blank">
                      {{buttonText}}
                  </a>
              </div>
              """
            : "";

        var noteHtml = !string.IsNullOrEmpty(note)
            ? $$"""
              <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; padding: 14px; margin-top: 24px; font-size: 12px; color: #64748b; line-height: 1.5;">
                  ℹ️ {{note}}
              </div>
              """
            : "";

        return $$"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <style>
                    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #070d1e; margin: 0; padding: 32px 12px; color: #1e293b; }
                    .mail-wrapper { max-width: 540px; margin: 0 auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.3); }
                    .mail-header { background: linear-gradient(135deg, #070d1e 0%, #1e3a8a 100%); padding: 36px 24px; text-align: center; color: #ffffff; }
                    .logo { font-size: 26px; font-weight: 800; color: #fbbf24; letter-spacing: 0.5px; margin-bottom: 6px; }
                    .header-title { font-size: 20px; font-weight: 700; color: #ffffff; margin: 0; }
                    .mail-body { padding: 36px 30px; }
                    .greeting { font-size: 16px; font-weight: 600; color: #0f172a; margin-top: 0; margin-bottom: 16px; }
                    .desc-text { font-size: 14px; color: #475569; line-height: 1.6; margin-bottom: 24px; }
                    .footer { background: #f8fafc; border-top: 1px solid #edf2f7; padding: 20px; text-align: center; font-size: 12px; color: #94a3b8; line-height: 1.5; }
                </style>
            </head>
            <body>
                <div class="mail-wrapper">
                    <div class="mail-header">
                        <div class="logo">✨ Lumina</div>
                        <h1 class="header-title">{{title}}</h1>
                    </div>

                    <div class="mail-body">
                        <p class="greeting">{{greeting}}</p>
                        <div class="desc-text">{{message}}</div>
                        {{buttonHtml}}
                        {{noteHtml}}
                    </div>

                    <div class="footer">
                        <p style="margin: 0 0 6px 0;">Bu e-posta güvenliğiniz için otomatik olarak gönderilmiştir.</p>
                        <p style="margin: 0;">© 2026 Lumina Kimlik & Yetkilendirme Platformu</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }
}
