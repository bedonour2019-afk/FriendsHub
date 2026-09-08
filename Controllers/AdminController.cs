using FriendsHub.Data;
using FriendsHub.Models;
using FriendsHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<AppUser> _hasher = new();
        private readonly NotificationService _notifications;

        public AdminController(AppDbContext db, NotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        // ---------- لوحة تحكم الأدمن وقائمة المستخدمين ----------
        public async Task<IActionResult> Index()
        {
            var users = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            ViewBag.TotalUsers = users.Count;
            ViewBag.TotalPosts = await _db.Posts.CountAsync();
            ViewBag.TotalStories = await _db.Stories.CountAsync();
            ViewBag.TotalMessages = await _db.Messages.CountAsync();

            return View(users);
        }

        // ---------- إعادة تعيين كلمة المرور لمستخدم من قبل الأدمن ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            {
                TempData["Error"] = "كلمة المرور يجب أن تكون 4 أحرف على الأقل.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.PasswordHash = _hasher.HashPassword(user, newPassword.Trim());
            await _db.SaveChangesAsync();

            TempData["Success"] = $"تم تعيين كلمة المرور الجديدة للمستخدم {user.Username} بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        // ---------- تغيير دور المستخدم (Admin / User) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int userId, string newRole)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (user.Username.ToLower() == "admin" && newRole != "Admin")
            {
                TempData["Error"] = "لا يمكن تغيير دور الأدمن الأساسي.";
                return RedirectToAction(nameof(Index));
            }

            user.Role = (newRole == "Admin") ? "Admin" : "User";
            await _db.SaveChangesAsync();

            TempData["Success"] = $"تم تعديل دور {user.Username} إلى {user.Role}.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- حذف مستخدم نهائياً ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (user.Username.ToLower() == "admin")
            {
                TempData["Error"] = "لا يمكن حذف الأدمن الرئيسي.";
                return RedirectToAction(nameof(Index));
            }

            // حذف بيانات المستخدم
            var posts = _db.Posts.Where(p => p.Username == user.Username);
            _db.Posts.RemoveRange(posts);

            var stories = _db.Stories.Where(s => s.Username == user.Username);
            _db.Stories.RemoveRange(stories);

            var messages = _db.Messages.Where(m => m.Username == user.Username);
            _db.Messages.RemoveRange(messages);

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"تم حذف المستخدم {user.Username} وجميع بياناته بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- إرسال إشعار للجميع ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BroadcastNotification(string title, string message)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "لازم تكتب عنوان ورسالة للإشعار.";
                return RedirectToAction(nameof(Index));
            }

            var adminUsername = User.Identity?.Name ?? "Admin";

            await _notifications.SendNotificationAsync(
                recipientUsername: null,
                actorUsername: adminUsername,
                type: "admin_broadcast",
                title: title,
                message: message,
                linkUrl: "/Home"
            );

            TempData["Success"] = "تم إرسال الإشعار للجميع بنجاح!";
            return RedirectToAction(nameof(Index));
        }
    }
}
