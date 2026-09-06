using FriendsHub.Data;
using FriendsHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Controllers
{
    [Authorize]
    public class PostsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public PostsController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _db.Posts.OrderByDescending(p => p.CreatedAt).ToListAsync();

            var stories = await _db.Stories.OrderByDescending(s => s.CreatedAt).ToListAsync();
            var activeStories = stories.Where(s => !s.IsExpired).ToList();

            ViewBag.Stories = activeStories;
            return View(posts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(IFormFile image, string? caption)
        {
            var path = await SaveImageAsync(image, "posts");
            if (path == null)
            {
                TempData["Error"] = "لازم تختار صورة صحيحة";
                return RedirectToAction(nameof(Index));
            }

            _db.Posts.Add(new Post
            {
                Username = User.Identity!.Name ?? "مجهول",
                ImagePath = path,
                Caption = caption
            });
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStory(IFormFile image)
        {
            var path = await SaveImageAsync(image, "stories");
            if (path == null)
            {
                TempData["Error"] = "لازم تختار صورة صحيحة للستوري";
                return RedirectToAction(nameof(Index));
            }

            _db.Stories.Add(new Story
            {
                Username = User.Identity!.Name ?? "مجهول",
                ImagePath = path
            });
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> SaveImageAsync(IFormFile? image, string subFolder)
        {
            if (image == null || image.Length == 0) return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext)) return null;

            var fileName = $"{Guid.NewGuid()}{ext}";
            var folder = Path.Combine(_env.WebRootPath, "uploads", subFolder);
            Directory.CreateDirectory(folder);
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return $"/uploads/{subFolder}/{fileName}";
        }
    }
}
