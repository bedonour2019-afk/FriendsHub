using FriendsHub.Data;
using FriendsHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _db;

        public ProfileController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string username)
        {
            var currentUser = User.Identity?.Name ?? "";
            var targetUsername = username ?? currentUser;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == targetUsername);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var posts = await _db.Posts
                .Where(p => p.Username == targetUsername)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var postIds = posts.Select(p => p.Id).ToList();
            var allReactions = await _db.PostReactions
                .Where(r => postIds.Contains(r.PostId))
                .ToListAsync();
            var allComments = await _db.PostComments
                .Where(c => postIds.Contains(c.PostId))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var postViewModels = posts.Select(p =>
            {
                var reactions = allReactions.Where(r => r.PostId == p.Id).ToList();
                var comments = allComments.Where(c => c.PostId == p.Id).ToList();
                var userReaction = reactions.FirstOrDefault(r => r.Username == currentUser)?.ReactionType;
                var reactionCounts = reactions
                    .GroupBy(r => r.ReactionType)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new PostItemViewModel
                {
                    Post = p,
                    Reactions = reactions,
                    Comments = comments,
                    CurrentUserReaction = userReaction,
                    ReactionCounts = reactionCounts,
                    CanDelete = (p.Username == currentUser || User.IsInRole("Admin"))
                };
            }).ToList();

            ViewBag.ProfileUser = user;
            ViewBag.IsOwnProfile = currentUser == targetUsername;
            ViewBag.CurrentUser = currentUser;

            return View(postViewModels);
        }
    }
}
