using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Core.DTOs;

public class LoginRequestDto
{
    [Required(ErrorMessage = "E-posta veya kullanıcı adı zorunludur.")]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    public string Password { get; set; } = string.Empty;
}
