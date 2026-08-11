using AuthenticationService.API.Hubs;
using AuthenticationService.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AuthenticationService.API.Services
{
    public class RealTimeNotificationService : IRealTimeNotificationService
    {
        private readonly IHubContext<AppHub> _hubContext;

        public RealTimeNotificationService(IHubContext<AppHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendUserBannedAsync(string userId, string message)
        {
            await _hubContext.Clients.User(userId).SendAsync("UserBanned", message);
        }

        public async Task SendNewNotificationAsync(string userId)
        {
            await _hubContext.Clients.User(userId).SendAsync("NewNotification");
        }
    }
}
