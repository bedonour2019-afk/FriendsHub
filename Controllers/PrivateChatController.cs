using FriendsHub.Data;
using FriendsHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Controllers
{
    [Authorize]
    public class PrivateChatController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public PrivateChatController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = User.Identity?.Name ?? "";
            var chats = await _db.PrivateChats
                .Where(c => c.User1 == currentUser || c.User2 == currentUser)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();

            var chatViewModels = new List<PrivateChatListItemViewModel>();

            foreach (var chat in chats)
            {
                var otherUser = chat.User1 == currentUser ? chat.User2 : chat.User1;
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == otherUser);

                var lastMessage = await _db.PrivateChatMessages
                    .Where(m => m.PrivateChatId == chat.Id)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                chatViewModels.Add(new PrivateChatListItemViewModel
                {
                    ChatId = chat.Id,
                    OtherUser = otherUser,
                    ProfilePicture = user?.ProfilePicture ?? "/uploads/profiles/default.png",
                    LastMessage = lastMessage?.Content ?? "",
                    LastMessageTime = lastMessage?.SentAt.ToString("HH:mm") ?? "",
                    UnreadCount = await _db.PrivateChatMessages
                        .Where(m => m.PrivateChatId == chat.Id && m.SenderUsername != currentUser && !m.IsRead)
                        .CountAsync()
                });
            }

            return View(chatViewModels);
        }

        [HttpGet]
        public async Task<IActionResult> Chat(int id)
        {
            var currentUser = User.Identity?.Name ?? "";
            var chat = await _db.PrivateChats.FindAsync(id);

            if (chat == null || (chat.User1 != currentUser && chat.User2 != currentUser))
            {
                return RedirectToAction("Index");
            }

            var otherUser = chat.User1 == currentUser ? chat.User2 : chat.User1;
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == otherUser);

            var messages = await _db.PrivateChatMessages
                .Where(m => m.PrivateChatId == id)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // تحديث الرسائل غير المقروءة
            var unreadMessages = messages.Where(m => m.SenderUsername != currentUser && !m.IsRead).ToList();
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            await _db.SaveChangesAsync();

            ViewBag.ChatId = id;
            ViewBag.OtherUser = otherUser;
            ViewBag.ProfilePicture = user?.ProfilePicture ?? "/uploads/profiles/default.png";
            ViewBag.CurrentUser = currentUser;

            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> StartChat(string username)
        {
            var currentUser = User.Identity?.Name ?? "";
            if (username == currentUser) return RedirectToAction("Index");

            var existingChat = await _db.PrivateChats
                .FirstOrDefaultAsync(c =>
                    (c.User1 == currentUser && c.User2 == username) ||
                    (c.User1 == username && c.User2 == currentUser));

            if (existingChat != null)
            {
                return RedirectToAction("Chat", new { id = existingChat.Id });
            }

            var newChat = new PrivateChat
            {
                User1 = currentUser,
                User2 = username,
                CreatedAt = DateTime.Now
            };

            _db.PrivateChats.Add(newChat);
            await _db.SaveChangesAsync();

            return RedirectToAction("Chat", new { id = newChat.Id });
        }

        [HttpPost]
        public async Task<IActionResult> UploadAttachment(IFormFile? file, string type)
        {
            if (file == null || file.Length == 0) return BadRequest();

            var imageExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var audioExts = new[] { ".webm", ".ogg", ".mp3", ".wav", ".m4a" };

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (type == "audio")
            {
                if (string.IsNullOrEmpty(ext) || !audioExts.Contains(ext))
                    ext = ".webm";
            }
            else
            {
                type = "image";
                if (!imageExts.Contains(ext)) return BadRequest("نوع صورة غير مسموح");
            }

            var fileName = $"{Guid.NewGuid()}{ext}";
            var folder = Path.Combine(_env.WebRootPath, "uploads", "privatechat");
            Directory.CreateDirectory(folder);
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Json(new { url = $"/uploads/privatechat/{fileName}" });
        }
    }
}
