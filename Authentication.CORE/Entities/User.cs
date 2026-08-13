namespace AuthenticationService.Core.Entities;

/// <summary>
/// Entity sınıfları, veritabanındaki tabloların C# nesnesi (Object) karşılıklarıdır (OR-Mapping - Object Relational Mapping).
/// Bu sınıf veritabanındaki "Users" tablosu ile eşleşir.
/// Sınıf içindeki her bir 'property' (özellik), tabloda bir sütuna (column) karşılık gelir.
/// '?' (Nullable) işareti, bu alanın veritabanında 'NULL' olabileceğini, yani boş bırakılabileceğini belirtir.
/// </summary>
public class User
{
    /// <summary>
    /// Kullanıcının benzersiz kimliği (Primary Key - Birincil Anahtar).
    /// Yeni bir kullanıcı oluşturulduğunda otomatik olarak rastgele bir GUID atanır.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Sisteme giriş yapmak veya kullanıcıyı tanımlamak için kullanılan benzersiz kullanıcı adı.
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Kullanıcının iletişim ve doğrulama için kullanılan e-posta adresi.
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Güvenlik gereği kullanıcının şifresi düz metin olarak değil, 
    /// şifrelenmiş (hash'lenmiş) bir metin olarak veritabanında saklanır.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Kullanıcı Rolü: "User", "Author" veya "Admin"
    /// Author: Blog ve köşe yazısı oluşturma yetkisine sahiptir.
    /// Admin: Tüm yetkilere sahip süper kullanıcıdır.
    /// Varsayılan olarak sisteme kayıt olan herkes "User" rolünü alır.
    /// </summary>
    public string Role { get; set; } = "User";
    
    /// <summary>
    /// E-posta doğrulama durumu (Mail Onayı gereksinimi).
    /// Kayıt aşamasında false olur, kullanıcı e-postasına giden linke tıkladığında true'ya çekilir.
    /// </summary>
    public bool IsEmailConfirmed { get; set; } = false;
    
    /// <summary>
    /// E-posta doğrulama kodu / token'ı.
    /// Her zaman doğrulama süreci aktif olmayabilir, bu nedenle null olabilir (?).
    /// </summary>
    public string? EmailConfirmationToken { get; set; }
    
    /// <summary>
    /// E-posta doğrulama token'ının son geçerlilik tarihi.
    /// Token yoksa bu değere de ihtiyaç olmadığı için null olabilir (?).
    /// </summary>
    public DateTime? EmailConfirmationTokenExpiresAt { get; set; }
    
    /// <summary>
    /// Tekil oturum takibi için son üretilen oturum belirteci.
    /// "1 kullanıcı aynı anda 2 farklı yerde login olamasın" opsiyonel gereksinimi için kullanılır.
    /// Kullanıcı henüz login olmadıysa null olabilir (?).
    /// </summary>
    public string? CurrentSessionToken { get; set; }
    
    /// <summary>
    /// Kullanıcının profil fotoğrafının dosya yolu veya URL adresi.
    /// Kullanıcı fotoğraf yüklememiş olabileceği için null olabilir (?).
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// Yazar başvurusu için mezun olunan üniversite.
    /// Her kullanıcı yazar başvurusu yapmayacağı için boş (null) olabilir (?).
    /// </summary>
    public string? University { get; set; }

    /// <summary>
    /// Yazar başvurusu için yüklenen CV PDF dosyasının URL'si / dosya yolu.
    /// Sadece yazar başvurusu yapanlar yüklediği için null olabilir (?).
    /// </summary>
    public string? CvUrl { get; set; }

    /// <summary>
    /// Yazar başvurusu onay durumu: "Pending", "Approved", "Rejected" veya null.
    /// Yazar başvurusu yapmamış normal kullanıcılar için değeri null (?) olur.
    /// </summary>
    public string? AuthorApprovalStatus { get; set; }

    /// <summary>
    /// Yazar başvuru tarihi. Başvuru yoksa null'dır (?).
    /// </summary>
    public DateTime? AuthorApplicationDate { get; set; }

    /// <summary>
    /// Yazar başvurusu reddedilirse admin tarafından belirtilen gerekçe.
    /// Reddedilme durumu yoksa null (?) olarak kalır.
    /// </summary>
    public string? AuthorRejectionReason { get; set; }

    /// <summary>
    /// Şifremi unuttum / sıfırlama token'ı.
    /// Şifre sıfırlama talebinde bulunmayan kullanıcılar için null (?) olur.
    /// </summary>
    public string? PasswordResetToken { get; set; }

    /// <summary>
    /// Şifre sıfırlama token'ının geçerlilik süresi.
    /// Token oluşturulmamışsa bu tarih de null (?) olacaktır.
    /// </summary>
    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    /// <summary>
    /// Kullanıcı ban / askıya alınma durumu.
    /// Normalde false'dur, kural ihlali durumunda true yapılır.
    /// </summary>
    public bool IsBanned { get; set; } = false;

    /// <summary>
    /// Ban bitiş tarihi. Eğer null (?) ise süresiz (kalıcı) ban anlamına gelir 
    /// (veya kullanıcı banlı değildir).
    /// </summary>
    public DateTime? BannedUntil { get; set; }

    /// <summary>
    /// Admin tarafından yazılan banlanma / askıya alınma gerekçesi.
    /// Ban durumu yoksa null (?) olabilir.
    /// </summary>
    public string? BanReason { get; set; }

    /// <summary>
    /// Hesap silme onay token'ı. Kullanıcı hesap silmek istediğinde oluşturulur.
    /// </summary>
    public string? AccountDeletionToken { get; set; }

    /// <summary>
    /// Hesap silme onay token'ının son geçerlilik tarihi.
    /// </summary>
    public DateTime? AccountDeletionTokenExpiresAt { get; set; }
    
    /// <summary>
    /// Hesap dondurma (Deactivation) durumu. True ise kullanıcı hesabını dondurmuş demektir.
    /// Giriş yaptığında tekrar false olarak güncellenir (aktive olur).
    /// </summary>
    public bool IsDeactivated { get; set; } = false;

    /// <summary>
    /// Kayıt oluşturulma tarihi. Veritabanına kullanıcının ne zaman eklendiğini tutar.
    /// Otomatik olarak o anki zaman (UTC) atanır.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Kullanıcının sisteme son giriş yaptığı tarih.
    /// Hiç giriş yapmamış (yeni kayıt olmuş) biri için null (?) olabilir.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
