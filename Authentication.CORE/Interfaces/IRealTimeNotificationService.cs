namespace AuthenticationService.Core.Interfaces
{
    public interface IRealTimeNotificationService
    {
        Task SendUserBannedAsync(string userId, string message);
        Task SendNewNotificationAsync(string userId);
    }
}
