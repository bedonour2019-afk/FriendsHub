using FriendsHub.Data;
using FriendsHub.Models;
using FriendsHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Controllers
{
    [Authorize]
    public class TeamsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notifications;

        public TeamsController(AppDbContext db, NotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pinnedTeams = await _db.Teams
                .Where(t => t.IsPinned)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.PinnedTeams = pinnedTeams;
            return View(new TeamsResultViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(TeamsInputViewModel input, bool pinTeam = false)
        {
            var names = (input.NamesRaw ?? string.Empty)
                .Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .Where(n => n.Length > 0)
                .Distinct()
                .ToList();

            var result = new TeamsResultViewModel();

            if (names.Count < 2)
            {
                ModelState.AddModelError(string.Empty, "اكتب اسمين على الأقل عشان نقسم فريقين");
                ViewBag.NamesRaw = input.NamesRaw;
                return View(result);
            }

            var rnd = new Random();
            var shuffled = names.OrderBy(_ => rnd.Next()).ToList();
            var half = (shuffled.Count + 1) / 2;

            result.TeamA = shuffled.Take(half).ToList();
            result.TeamB = shuffled.Skip(half).ToList();

            ViewBag.NamesRaw = input.NamesRaw;

            // حفظ الفريق إذا تم طلب التثبيت
            if (pinTeam)
            {
                var currentUser = User.Identity?.Name ?? "";
                var team = new Team
                {
                    CreatorUsername = currentUser,
                    TeamA_Members = string.Join(", ", result.TeamA),
                    TeamB_Members = string.Join(", ", result.TeamB),
                    IsPinned = true,
                    CreatedAt = DateTime.Now
                };

                _db.Teams.Add(team);
                await _db.SaveChangesAsync();

                // إرسال إشعار للجميع
                await _notifications.SendNotificationAsync(
                    recipientUsername: null,
                    actorUsername: currentUser,
                    type: "team_created",
                    title: "فريق جديد تم إنشاؤه ⚔️",
                    message: $"قام {currentUser} بإنشاء فريق جديد! اضغط لعرض التفاصيل.",
                    linkUrl: "/Teams"
                );
            }

            var pinnedTeams = await _db.Teams
                .Where(t => t.IsPinned)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync<Team>();

            ViewBag.PinnedTeams = pinnedTeams;
            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpinTeam(int id)
        {
            var team = await _db.Teams.FindAsync(id);
            if (team != null)
            {
                team.IsPinned = false;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
