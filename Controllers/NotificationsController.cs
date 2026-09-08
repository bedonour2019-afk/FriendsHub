using FriendsHub.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _db;

        public NotificationsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var username = User.Identity?.Name ?? "";

            var notifications = await _db.Notifications
                .Where(n => n.RecipientUsername == username || string.IsNullOrEmpty(n.RecipientUsername))
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .Select(n => new
                {
                    n.Id,
                    n.ActorUsername,
                    n.Type,
                    n.Title,
                    n.Message,
                    n.LinkUrl,
                    n.IsRead,
                    Time = n.CreatedAt.ToString("yyyy/MM/dd HH:mm")
                })
                .ToListAsync();

            var unreadCount = await _db.Notifications
                .CountAsync(n => (n.RecipientUsername == username || string.IsNullOrEmpty(n.RecipientUsername)) && !n.IsRead);

            return Json(new { unreadCount, notifications });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var username = User.Identity?.Name ?? "";

            var unread = await _db.Notifications
                .Where(n => (n.RecipientUsername == username || string.IsNullOrEmpty(n.RecipientUsername)) && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notif = await _db.Notifications.FindAsync(id);
            if (notif != null)
            {
                notif.IsRead = true;
                await _db.SaveChangesAsync();
            }
            return Json(new { success = true });
        }
    }
}
