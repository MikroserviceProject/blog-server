using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Core.DTOs;

public class SupportRequestDto
{
    [Required(ErrorMessage = "Lütfen destek türünü seçiniz (İstek veya Şikayet).")]
    public string Type { get; set; } = null!;

    [Required(ErrorMessage = "Mesaj alanı boş bırakılamaz.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Mesajınız 10 ile 1000 karakter arasında olmalıdır.")]
    public string Message { get; set; } = null!;
}
