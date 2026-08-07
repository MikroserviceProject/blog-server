namespace AuthenticationService.Core.Entities;

public class UserNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Bildirim Türü: "Warning", "PostDeleted", "Account", "Info"
    /// </summary>
    public string Type { get; set; } = "Info";
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
