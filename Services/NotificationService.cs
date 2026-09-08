using FriendsHub.Data;
using FriendsHub.Hubs;
using FriendsHub.Models;
using Microsoft.AspNetCore.SignalR;

namespace FriendsHub.Services
{
    public class NotificationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(IServiceScopeFactory scopeFactory, IHubContext<NotificationHub> hub)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
        }

        public async Task SendNotificationAsync(string? recipientUsername, string actorUsername, string type, string title, string message, string? linkUrl = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var notif = new Notification
            {
                RecipientUsername = recipientUsername,
                ActorUsername = actorUsername,
                Type = type,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            db.Notifications.Add(notif);
            await db.SaveChangesAsync();

            var payload = new
            {
                id = notif.Id,
                recipient = recipientUsername,
                actor = actorUsername,
                type = type,
                title = title,
                message = message,
                link = linkUrl,
                time = notif.CreatedAt.ToString("HH:mm")
            };

            if (!string.IsNullOrEmpty(recipientUsername))
            {
                await _hub.Clients.Group($"user_{recipientUsername}").SendAsync("ReceiveNotification", payload);
            }
            else
            {
                await _hub.Clients.Group("global_notifications").SendAsync("ReceiveNotification", payload);
            }
        }
    }
}
