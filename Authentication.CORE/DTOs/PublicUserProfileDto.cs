namespace AuthenticationService.Core.DTOs;

/// <summary>
/// Herkese açık, hassas olmayan kullanıcı bilgileri (blog yazarını göstermek gibi amaçlar için).
/// Email, rol, ban durumu gibi hassas alanlar burada YOK.
/// </summary>
public class PublicUserProfileDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}
