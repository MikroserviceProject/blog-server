namespace AuthenticationService.Core.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailConfirmationAsync(string toEmail, string username, string confirmationToken);
    Task<bool> SendAuthorApplicationReceivedAsync(string toEmail, string username);
    Task<bool> SendAuthorApprovedAsync(string toEmail, string username, string confirmationToken);
    Task<bool> SendAuthorRejectedAsync(string toEmail, string username, string? reason);
    Task<bool> SendPasswordResetAsync(string toEmail, string username, string resetToken);
    Task<bool> SendUserBannedEmailAsync(string toEmail, string username, string reason, DateTime? bannedUntil);
    Task<bool> SendPostDeletedEmailAsync(string toEmail, string username, string postTitle, string reason);
    Task<bool> SendAccountDeletionConfirmationAsync(string toEmail, string username, string token);
    Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
}
