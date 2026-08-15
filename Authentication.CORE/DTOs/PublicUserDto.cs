namespace AuthenticationService.Core.DTOs;

public class PublicUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? CoverPictureUrl { get; set; }
    public string? University { get; set; }
    public DateTime CreatedAt { get; set; }
}
