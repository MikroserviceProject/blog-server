namespace AuthenticationService.Core.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? University { get; set; }
    public string? CvUrl { get; set; }
    public string? AuthorApprovalStatus { get; set; }
    public DateTime? AuthorApplicationDate { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public bool IsBanned { get; set; }
    public DateTime? BannedUntil { get; set; }
    public string? BanReason { get; set; }
    public bool IsDeactivated { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int UnreadNotificationCount { get; set; }
}
