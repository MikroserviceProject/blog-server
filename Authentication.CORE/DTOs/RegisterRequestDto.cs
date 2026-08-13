using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Core.DTOs;

// --- DTO (Data Transfer Object) NEDİR VE NEDEN KULLANILIR? ---
// DTO, katmanlar arası veri taşımak için kullanılan basit veri sınıflarıdır.
// Veritabanı tablolarımızı temsil eden Entity sınıflarımızı (örneğin 'User' tablosu) 
// doğrudan dış dünyaya açmak (API üzerinden) güvenlik ve tasarım açısından doğru değildir.
// Entity'ler veritabanı ilişkileri ve gizli veriler (örneğin parola özetleri) içerebilir.
// DTO'lar, sadece istemciden almak istediğimiz veya istemciye göndermek istediğimiz
// spesifik alanları tanımlar. Bu sayede hem güvenlik artar, hem de gereksiz veri
// taşınmasının önüne geçilir.
public class RegisterRequestDto
{
    // --- DATA ANNOTATIONS (VERİ AÇIKLAMALARI) NEDİR? ---
    // [Required], [EmailAddress], [StringLength] gibi niteliklere "Data Annotations" denir.
    // Gelen verinin, belirlediğimiz kurallara uyup uymadığını (Validation) otomatik olarak 
    // kontrol etmemizi sağlarlar. İstemci geçersiz bir veri gönderdiğinde, kod mantığına (Action)
    // hiç girmeden direkt 400 Bad Request döner ve hata mesajımızı iletirler.
    
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3 ile 50 karakter arasında olmalıdır.")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Kullanıcı adı sadece harf, rakam ve alt çizgi (_) içerebilir.")]
    public string Username { get; set; } = string.Empty;

    // [EmailAddress] anotasyonu, string ifadenin bir e-posta formatında (ör. test@test.com) olup olmadığını doğrular.
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        ErrorMessage = "Şifre en az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Kayıt için Geçerli Roller: "User" (Okur), "Author" (Yazar), "Admin" (Yönetici)
    /// </summary>
    [RegularExpression(@"^(User|Author|Admin)$", ErrorMessage = "Geçerli roller: 'User', 'Author' veya 'Admin'.")]
    public string Role { get; set; } = "User";

    /// <summary>
    /// Yazar başvurusu durumunda mezun olunan üniversite
    /// </summary>
    [StringLength(100, ErrorMessage = "Üniversite adı en fazla 100 karakter olabilir.")]
    public string? University { get; set; }

    /// <summary>
    /// Yazar başvurusu durumunda yüklenen CV dosya bağlantısı / yolu
    /// </summary>
    public string? CvUrl { get; set; }
}
