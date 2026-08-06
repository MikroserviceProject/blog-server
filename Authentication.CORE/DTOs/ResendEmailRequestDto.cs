using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Core.DTOs;

public class ResendEmailRequestDto
{
    [Required(ErrorMessage = "E-posta adresi boş bırakılamaz.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;
}
