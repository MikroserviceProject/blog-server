using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Core.DTOs;

/// <summary>
/// Swagger / API üzerinden yeni bir Yönetici (Admin) hesabı oluşturma isteği DTO'su.
/// </summary>
public class CreateAdminRequestDto
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-50 karakter arasında olmalıdır.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Güvenlik anahtarı (Varsayılan: LuminaAdmin2026!*)
    /// </summary>
    public string? AdminSecretKey { get; set; } = "LuminaAdmin2026!*";
}
