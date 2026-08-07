using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Core.DTOs;

public class BanUserRequestDto
{
    [Required(ErrorMessage = "Kullanıcı ID gereklidir.")]
    public Guid UserId { get; set; }

    public string? BanReason { get; set; }
    public string? Reason { get; set; }

    public string GetEffectiveReason()
    {
        if (!string.IsNullOrWhiteSpace(BanReason)) return BanReason.Trim();
        if (!string.IsNullOrWhiteSpace(Reason)) return Reason.Trim();
        return string.Empty;
    }

    /// <summary>
    /// Eğer null ise ve BannedUntil null ise süresiz bandır.
    /// </summary>
    public int? DurationMinutes { get; set; }
    public DateTime? BannedUntil { get; set; }
}

public class UnbanUserRequestDto
{
    [Required]
    public Guid UserId { get; set; }
}

public class ConfirmAccountDeletionDto
{
    public string? Email { get; set; }

    [Required(ErrorMessage = "Hesap silme onay anahtarı (token) gereklidir.")]
    public string Token { get; set; } = string.Empty;
}

public class UserNotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminSendNotificationDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = "Warning";
}
