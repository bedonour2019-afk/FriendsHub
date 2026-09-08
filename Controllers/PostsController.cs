using FriendsHub.Data;
using FriendsHub.Models;
using FriendsHub.Services;
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
        private readonly NotificationService _notifications;

        public PostsController(AppDbContext db, IWebHostEnvironment env, NotificationService notifications)
        {
            _db = db;
            _env = env;
            _notifications = notifications;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = User.Identity?.Name ?? "";
            var isAdmin = User.IsInRole("Admin");

            // 1. جلب المنشورات مع التعليقات والتفاعلات
            var posts = await _db.Posts.OrderByDescending(p => p.CreatedAt).ToListAsync();
            var postIds = posts.Select(p => p.Id).ToList();

            var allComments = await _db.PostComments
                .Where(c => postIds.Contains(c.PostId))
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            var allReactions = await _db.PostReactions
                .Where(r => postIds.Contains(r.PostId))
                .ToListAsync();

            var postViewModels = posts.Select(p =>
            {
                var comments = allComments.Where(c => c.PostId == p.Id).ToList();
                var reactions = allReactions.Where(r => r.PostId == p.Id).ToList();
                var userReaction = reactions.FirstOrDefault(r => r.Username == currentUser)?.ReactionType;

                var reactionCounts = reactions
                    .GroupBy(r => r.ReactionType)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new PostItemViewModel
                {
                    Post = p,
                    Comments = comments,
                    Reactions = reactions,
                    CurrentUserReaction = userReaction,
                    ReactionCounts = reactionCounts,
                    CanDelete = (p.Username == currentUser || isAdmin)
                };
            }).ToList();

            // 2. جلب الستوريهات النشطة فقط لآخر 24 ساعة
            var cutoff = DateTime.Now.AddHours(-24);
            var activeStories = await _db.Stories
                .Where(s => s.CreatedAt >= cutoff)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();

            var storyIds = activeStories.Select(s => s.Id).ToList();
            var allStoryComments = await _db.StoryComments
                .Where(sc => storyIds.Contains(sc.StoryId))
                .OrderBy(sc => sc.CreatedAt)
                .ToListAsync();

            // تجميع الستوري لكل مستخدم (نمط إنستغرام)
            var groupedStories = activeStories
                .GroupBy(s => s.Username)
                .Select(g => new UserStoryGroupDto
                {
                    Username = g.Key,
                    Stories = g.Select(s => new StoryItemDto
                    {
                        Id = s.Id,
                        Username = s.Username,
                        ImagePath = s.ImagePath,
                        CreatedAt = s.CreatedAt,
                        CanDelete = (s.Username == currentUser || isAdmin),
                        Comments = allStoryComments.Where(c => c.StoryId == s.Id).ToList()
                    }).ToList()
                })
                .ToList();

            var model = new PostsFeedViewModel
            {
                Posts = postViewModels,
                GroupedStories = groupedStories
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(IFormFile image, string? caption)
        {
            var username = User.Identity?.Name ?? "مجهول";
            var path = await SaveImageAsync(image, "posts");
            if (path == null)
            {
                TempData["Error"] = "لازم تختار صورة صحيحة للبوست";
                return RedirectToAction(nameof(Index));
            }

            var post = new Post
            {
                Username = username,
                ImagePath = path,
                Caption = caption,
                CreatedAt = DateTime.Now
            };

            _db.Posts.Add(post);
            await _db.SaveChangesAsync();

            // إشعار لجميع الأصدقاء
            await _notifications.SendNotificationAsync(
                recipientUsername: null,
                actorUsername: username,
                type: "post",
                title: "منشور جديد 📷",
                message: $"نشر {username} منشوراً جديداً",
                linkUrl: "/Posts"
            );

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStory(IFormFile image)
        {
            var username = User.Identity?.Name ?? "مجهول";
            var path = await SaveImageAsync(image, "stories");
            if (path == null)
            {
                TempData["Error"] = "لازم تختار صورة صحيحة للستوري";
                return RedirectToAction(nameof(Index));
            }

            var story = new Story
            {
                Username = username,
                ImagePath = path,
                CreatedAt = DateTime.Now
            };

            _db.Stories.Add(story);
            await _db.SaveChangesAsync();

            // إشعار للجميع
            await _notifications.SendNotificationAsync(
                recipientUsername: null,
                actorUsername: username,
                type: "story",
                title: "ستوري جديدة 🌟",
                message: $"أضاف {username} ستوري جديدة",
                linkUrl: "/Posts"
            );

            return RedirectToAction(nameof(Index));
        }

        // ---------- حذف بوست (لصاحبه أو للأدمن) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _db.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var currentUser = User.Identity?.Name ?? "";
            var isAdmin = User.IsInRole("Admin");

            if (post.Username != currentUser && !isAdmin)
            {
                return Forbid();
            }

            // حذف التعليقات والتفاعلات التابعة للبوست
            var comments = _db.PostComments.Where(c => c.PostId == id);
            _db.PostComments.RemoveRange(comments);

            var reactions = _db.PostReactions.Where(r => r.PostId == id);
            _db.PostReactions.RemoveRange(reactions);

            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();

            // حذف ملف الصورة من السيرفر
            DeleteLocalFile(post.ImagePath);

            return RedirectToAction(nameof(Index));
        }

        // ---------- حذف ستوري (لصاحبه أو للأدمن) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStory(int id)
        {
            var story = await _db.Stories.FindAsync(id);
            if (story == null) return NotFound();

            var currentUser = User.Identity?.Name ?? "";
            var isAdmin = User.IsInRole("Admin");

            if (story.Username != currentUser && !isAdmin)
            {
                return Forbid();
            }

            var comments = _db.StoryComments.Where(c => c.StoryId == id);
            _db.StoryComments.RemoveRange(comments);

            _db.Stories.Remove(story);
            await _db.SaveChangesAsync();

            DeleteLocalFile(story.ImagePath);

            return RedirectToAction(nameof(Index));
        }

        // ---------- تفاعل ريأكت فيسبوك على البوست ----------
        [HttpPost]
        public async Task<IActionResult> TogglePostReaction(int postId, string reactionType)
        {
            var currentUser = User.Identity?.Name ?? "";
            var validTypes = new[] { "like", "love", "haha", "sad", "angry" };
            if (!validTypes.Contains(reactionType)) return BadRequest("نوع التفاعل غير صحيح");

            var post = await _db.Posts.FindAsync(postId);
            if (post == null) return NotFound();

            var existing = await _db.PostReactions
                .FirstOrDefaultAsync(r => r.PostId == postId && r.Username == currentUser);

            string? activeReaction = null;

            if (existing != null)
            {
                if (existing.ReactionType == reactionType)
                {
                    // إلغاء الريأكت عند الضغط عليه مرة أخرى
                    _db.PostReactions.Remove(existing);
                }
                else
                {
                    // تغيير نوع الريأكت
                    existing.ReactionType = reactionType;
                    existing.CreatedAt = DateTime.Now;
                    activeReaction = reactionType;
                }
            }
            else
            {
                // إضافة ريأكت جديد
                var reaction = new PostReaction
                {
                    PostId = postId,
                    Username = currentUser,
                    ReactionType = reactionType,
                    CreatedAt = DateTime.Now
                };
                _db.PostReactions.Add(reaction);
                activeReaction = reactionType;

                // إشعار لصاحب البوست إن لم يكن هو نفسه
                if (post.Username != currentUser)
                {
                    var reactionEmoji = reactionType switch
                    {
                        "love" => "❤️",
                        "haha" => "😂",
                        "sad" => "😢",
                        "angry" => "😡",
                        _ => "👍"
                    };

                    await _notifications.SendNotificationAsync(
                        recipientUsername: post.Username,
                        actorUsername: currentUser,
                        type: "reaction",
                        title: "تفاعل جديد",
                        message: $"تفاعل {currentUser} بـ {reactionEmoji} على منشورك",
                        linkUrl: "/Posts"
                    );
                }
            }

            await _db.SaveChangesAsync();

            var updatedReactions = await _db.PostReactions
                .Where(r => r.PostId == postId)
                .GroupBy(r => r.ReactionType)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            return Json(new
            {
                success = true,
                userReaction = activeReaction,
                counts = updatedReactions
            });
        }

        // ---------- إضافة تعليق على بوست ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPostComment(int postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return BadRequest();
            var currentUser = User.Identity?.Name ?? "";

            var post = await _db.Posts.FindAsync(postId);
            if (post == null) return NotFound();

            var comment = new PostComment
            {
                PostId = postId,
                Username = currentUser,
                Content = content.Trim(),
                CreatedAt = DateTime.Now
            };

            _db.PostComments.Add(comment);
            await _db.SaveChangesAsync();

            // إشعار لصاحب البوست
            if (post.Username != currentUser)
            {
                await _notifications.SendNotificationAsync(
                    recipientUsername: post.Username,
                    actorUsername: currentUser,
                    type: "comment",
                    title: "تعليق جديد 💬",
                    message: $"علق {currentUser} على منشورك: \"{(content.Length > 30 ? content[..30] + "..." : content)}\"",
                    linkUrl: "/Posts"
                );
            }

            return RedirectToAction(nameof(Index));
        }

        // ---------- حذف تعليق على بوست ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePostComment(int commentId)
        {
            var comment = await _db.PostComments.FindAsync(commentId);
            if (comment == null) return NotFound();

            var currentUser = User.Identity?.Name ?? "";
            var isAdmin = User.IsInRole("Admin");

            var post = await _db.Posts.FindAsync(comment.PostId);

            // مسموح لصاحب الكومنت، صاحب البوست، أو الأدمن بالحذف
            if (comment.Username != currentUser && post?.Username != currentUser && !isAdmin)
            {
                return Forbid();
            }

            _db.PostComments.Remove(comment);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ---------- إضافة تعليق/رد على ستوري ----------
        [HttpPost]
        public async Task<IActionResult> AddStoryComment(int storyId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return BadRequest();
            var currentUser = User.Identity?.Name ?? "";

            var story = await _db.Stories.FindAsync(storyId);
            if (story == null) return NotFound();

            var comment = new StoryComment
            {
                StoryId = storyId,
                Username = currentUser,
                Content = content.Trim(),
                CreatedAt = DateTime.Now
            };

            _db.StoryComments.Add(comment);
            await _db.SaveChangesAsync();

            if (story.Username != currentUser)
            {
                await _notifications.SendNotificationAsync(
                    recipientUsername: story.Username,
                    actorUsername: currentUser,
                    type: "story_reply",
                    title: "رد على الستوري 🌟",
                    message: $"رد {currentUser} على ستوريك: \"{(content.Length > 30 ? content[..30] + "..." : content)}\"",
                    linkUrl: "/Posts"
                );
            }

            return Json(new { success = true, username = currentUser, content = comment.Content, time = comment.CreatedAt.ToString("HH:mm") });
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

        private void DeleteLocalFile(string relativePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativePath)) return;
                var trimmed = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var full = Path.Combine(_env.WebRootPath, trimmed);
                if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
            }
            catch
            {
                // تجاهل أخطاء مسح الملف
            }
        }

        // ---------- جلب المنشورات الحديثة ----------
        [HttpGet]
        public async Task<IActionResult> GetRecentPosts()
        {
            var posts = await _db.Posts
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new
                {
                    p.Id,
                    p.Username,
                    Content = p.Caption,
                    p.ImagePath,
                    p.CreatedAt
                })
                .ToListAsync();

            return Json(posts);
        }
    }
}
