using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Core.DTOs;

public class UpdateProfileRequestDto
{
    [Required(ErrorMessage = "Kullanıcı adı boş bırakılamaz.")]
    [MinLength(3, ErrorMessage = "Kullanıcı adı en az 3 karakter olmalıdır.")]
    [MaxLength(50, ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi boş bırakılamaz.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }
}
