using System.Security.Claims;
using FriendsHub.Data;
using FriendsHub.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FriendsHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly PasswordHasher<AppUser> _hasher = new();

        public AccountController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // ---------- تسجيل دخول المستخدم العادي ----------
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Username == model.Username && u.Role == "User");

            if (user == null || _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password)
                    == PasswordVerificationResult.Failed)
            {
                model.ErrorMessage = "اسم المستخدم أو كلمة المرور غلط";
                return View(model);
            }

            await SignInUserAsync(user);
            return RedirectToAction("Index", "Home");
        }

        // ---------- تسجيل دخول الأدمن ----------
        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var admin = await _db.Users.FirstOrDefaultAsync(u =>
                u.Username == model.Username && u.Role == "Admin");

            if (admin == null || _hasher.VerifyHashedPassword(admin, admin.PasswordHash, model.Password)
                    == PasswordVerificationResult.Failed)
            {
                model.ErrorMessage = "بيانات دخول الأدمن غلط";
                return View(model);
            }

            await SignInUserAsync(admin);
            return RedirectToAction("Index", "Home");
        }

        // ---------- تسجيل حساب جديد (مستخدم عادي فقط) ----------
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. التحقق من كود المجموعة السرّي
            var correctPasscode = _config["AppSettings:GroupPasscode"];
            if (model.GroupPasscode != correctPasscode)
            {
                ModelState.AddModelError("GroupPasscode", "كود المجموعة غير صحيح! لا يمكنك إنشاء حساب");
                return View(model);
            }

            // 2. التحقق من وجود اسم المستخدم سابقاً
            var exists = await _db.Users.AnyAsync(u => u.Username == model.Username);
            if (exists)
            {
                ModelState.AddModelError("Username", "اسم المستخدم ده مستخدم بالفعل");
                return View(model);
            }

            var user = new AppUser
            {
                Username = model.Username,
                Role = "User"
            };
            user.PasswordHash = _hasher.HashPassword(user, model.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await SignInUserAsync(user);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInUserAsync(AppUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role),
                new("UserId", user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });
        }
    }
}