using FriendsHub.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ChatController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var history = await _db.Messages
                .OrderByDescending(m => m.Id)
                .Take(150)
                .OrderBy(m => m.Id)
                .ToListAsync();

            return View(history);
        }

        // بيرفع صورة أو تسجيل صوتي من الشات ويرجع الرابط بتاعه
        // الرفع ده بيتنادى بـ fetch من الجافاسكريبت، والرسالة نفسها بتتبعت بعد كده عن طريق SignalR
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
                    ext = ".webm"; // تسجيلات المتصفح غالبًا بتيجي من غير امتداد واضح
            }
            else
            {
                type = "image";
                if (!imageExts.Contains(ext)) return BadRequest("نوع صورة غير مسموح");
            }

            var fileName = $"{Guid.NewGuid()}{ext}";
            var folder = Path.Combine(_env.WebRootPath, "uploads", "chat");
            Directory.CreateDirectory(folder);
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Json(new { url = $"/uploads/chat/{fileName}" });
        }
    }
}
