using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Core.DTOs;

public class ConfirmEmailDto
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Onay token / kodu zorunludur.")]
    public string Token { get; set; } = string.Empty;
}
