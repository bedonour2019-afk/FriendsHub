using FriendsHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendsHub.Controllers
{
    [Authorize]
    public class TeamsController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View(new TeamsResultViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(TeamsInputViewModel input)
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
            return View(result);
        }
    }
}
