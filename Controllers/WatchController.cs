using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendsHub.Controllers
{
    [Authorize]
    public class WatchController : Controller
    {
        // لوبي: عمل غرفة جديدة أو الانضمام لغرفة شغالة
        public IActionResult Index() => View();

        // غرفة المشاهدة نفسها
        public IActionResult Room(string id)
        {
            ViewBag.RoomId = id;
            return View();
        }
    }
}
