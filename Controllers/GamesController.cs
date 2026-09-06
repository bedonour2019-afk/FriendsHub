using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FriendsHub.Controllers
{
    [Authorize]
    public class GamesController : Controller
    {
        public IActionResult Index() => View();

        // ساحة تحدي إكس أو أونلاين (Lobby)
        public IActionResult TicTacToe() => View();

        // غرفة لعب محددة (لاعبين + متفرجين)
        public IActionResult TicTacToeRoom(string id)
        {
            ViewBag.RoomId = id;
            return View();
        }

        public IActionResult Memory() => View();
    }
}
