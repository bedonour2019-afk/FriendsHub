using FriendsHub.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Hubs
{
    public class PresenceHub : Hub
    {
        private readonly AppDbContext _db;
        private static readonly Dictionary<string, string> _userConnections = new();

        public PresenceHub(AppDbContext db)
        {
            _db = db;
        }

        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.Identity?.Name;
            var userId = Context.User?.FindFirst("UserId")?.Value;

            if (username != null && userId != null)
            {
                _userConnections[Context.ConnectionId] = userId;

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    user.IsOnline = true;
                    user.LastSeen = DateTime.Now;
                    await _db.SaveChangesAsync();
                }

                await Clients.Others.SendAsync("UserOnline", username);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.Identity?.Name;
            var userId = Context.User?.FindFirst("UserId")?.Value;

            if (username != null && userId != null)
            {
                _userConnections.Remove(Context.ConnectionId);

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeen = DateTime.Now;
                    await _db.SaveChangesAsync();
                }

                await Clients.Others.SendAsync("UserOffline", username);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
