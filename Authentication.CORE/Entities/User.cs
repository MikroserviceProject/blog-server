namespace AuthenticationService.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Username { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Kullanıcı Rolü: "User", "Author" veya "Admin"
    /// Author: Blog ve köşe yazısı oluşturma yetkisine sahiptir.
    /// Admin: Tüm yetkilere sahip süper kullanıcıdır.
    /// </summary>
    public string Role { get; set; } = "User";
    
    /// <summary>
    /// E-posta doğrulama durumu (Mail Onayı gereksinimi)
    /// </summary>
    public bool IsEmailConfirmed { get; set; } = false;
    
    /// <summary>
    /// E-posta doğrulama kodu / token'ı
    /// </summary>
    public string? EmailConfirmationToken { get; set; }
    
    public DateTime? EmailConfirmationTokenExpiresAt { get; set; }
    
    /// <summary>
    /// Tekil oturum takibi için son üretilen oturum belirteci.
    /// "1 kullanıcı aynı anda 2 farklı yerde login olamasın" opsiyonel gereksinimi için kullanılır.
    /// </summary>
    public string? CurrentSessionToken { get; set; }
    
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// Yazar başvurusu için mezun olunan üniversite
    /// </summary>
    public string? University { get; set; }

    /// <summary>
    /// Yazar başvurusu için yüklenen CV PDF dosyasının URL'si / dosya yolu
    /// </summary>
    public string? CvUrl { get; set; }

    /// <summary>
    /// Yazar başvurusu onay durumu: "Pending", "Approved", "Rejected" veya null
    /// </summary>
    public string? AuthorApprovalStatus { get; set; }

    /// <summary>
    /// Yazar başvuru tarihi
    /// </summary>
    public DateTime? AuthorApplicationDate { get; set; }

    /// <summary>
    /// Yazar başvurusu reddedilirse gerekçesi
    /// </summary>
    public string? AuthorRejectionReason { get; set; }

    /// <summary>
    /// Şifremi unuttum / sıfırlama token'ı
    /// </summary>
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    /// <summary>
    /// Kullanıcı ban / askıya alınma durumu
    /// </summary>
    public bool IsBanned { get; set; } = false;

    /// <summary>
    /// Ban bitiş tarihi. Eğer null ise süresiz ban.
    /// </summary>
    public DateTime? BannedUntil { get; set; }

    /// <summary>
    /// Admin tarafından yazılan banlanma / askıya alınma gerekçesi
    /// </summary>
    public string? BanReason { get; set; }

    /// <summary>
    /// Hesap silme onay token'ı ve son geçerlilik tarihi
    /// </summary>
    public string? AccountDeletionToken { get; set; }
    public DateTime? AccountDeletionTokenExpiresAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
}
