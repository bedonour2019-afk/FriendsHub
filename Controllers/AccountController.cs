using System.Collections.Concurrent;
using System.Security.Claims;
using FriendsHub.Data;
using FriendsHub.Models;
using FriendsHub.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notifications;
        private readonly IWebHostEnvironment _env;
        private readonly PasswordHasher<AppUser> _hasher = new();

        // تخزين مؤقت لرموز استرجاع كلمة المرور (البريد -> الرمز وتاريخ الصلاحية)
        private static readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _resetCodes = new();

        public AccountController(AppDbContext db, NotificationService notifications, IWebHostEnvironment env)
        {
            _db = db;
            _notifications = notifications;
            _env = env;
        }

        // ---------- تسجيل دخول المستخدم العادي ----------
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // السماح بتسجيل الدخول باسم المستخدم أو البريد الإلكتروني
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                (u.Username == model.Username || u.Email == model.Username) && u.Role == "User");

            if (user == null || _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password)
                    == PasswordVerificationResult.Failed)
            {
                model.ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
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
                (u.Username == model.Username || u.Email == model.Username) && u.Role == "Admin");

            if (admin == null || _hasher.VerifyHashedPassword(admin, admin.PasswordHash, model.Password)
                    == PasswordVerificationResult.Failed)
            {
                model.ErrorMessage = "بيانات دخول الأدمن غير صحيحة";
                return View(model);
            }

            await SignInUserAsync(admin);
            return RedirectToAction("Index", "Home");
        }

        // ---------- تسجيل حساب جديد مع البريد الإلكتروني (جيميل) ----------
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

            // التحقق من كلمة السر الخاصة
            if (model.SecretCode != "bodyprograming")
            {
                ModelState.AddModelError("SecretCode", "كلمة السر غير صحيحة");
                return View(model);
            }

            var usernameExists = await _db.Users.AnyAsync(u => u.Username.ToLower() == model.Username.ToLower());
            if (usernameExists)
            {
                ModelState.AddModelError("Username", "اسم المستخدم ده مستخدم بالفعل، اختار اسم تاني");
                return View(model);
            }

            var emailExists = await _db.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == model.Email.ToLower());
            if (emailExists)
            {
                ModelState.AddModelError("Email", "البريد الإلكتروني ده مسجل بيه حساب قبل كده");
                return View(model);
            }

            var user = new AppUser
            {
                Username = model.Username.Trim(),
                Email = model.Email.Trim().ToLower(),
                Role = "User",
                CreatedAt = DateTime.Now,
                ProfilePicture = "/uploads/profiles/default.png"
            };

            // معالجة رفع صورة الملف الشخصي
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var imageExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();

                if (imageExts.Contains(ext))
                {
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var folder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                    Directory.CreateDirectory(folder);
                    var fullPath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(stream);
                    }

                    user.ProfilePicture = $"/uploads/profiles/{fileName}";
                }
            }

            user.PasswordHash = _hasher.HashPassword(user, model.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // إشعار للأدمن والجميع بتسجيل مستخدم جديد
            await _notifications.SendNotificationAsync(
                recipientUsername: null,
                actorUsername: user.Username,
                type: "new_user",
                title: "مستخدم جديد سجل في الموقع 👤",
                message: $"انضم {user.Username} ({user.Email}) إلى FriendsHub",
                linkUrl: "/Admin"
            );

            await SignInUserAsync(user);
            return RedirectToAction("Index", "Home");
        }

        // ---------- هل نسيت كلمة المرور؟ ----------
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim().ToLower();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email);

            if (user == null)
            {
                model.ErrorMessage = "لم يتم العثور على أي حساب مسجل بهذا البريد الإلكتروني.";
                return View(model);
            }

            // توليد رمز تحقق عشوائي مكون من 6 أرقام
            var code = new Random().Next(100000, 999999).ToString();
            _resetCodes[email] = (code, DateTime.Now.AddMinutes(15));

            model.SuccessMessage = $"تم توليد رمز استعادة كلمة المرور لحسابك ({user.Username}).";
            model.GeneratedResetCode = code;

            return View(model);
        }

        // ---------- إعادة تعيين كلمة المرور ----------
        [HttpGet]
        public IActionResult ResetPassword(string? email, string? code)
        {
            return View(new ResetPasswordViewModel
            {
                Email = email ?? "",
                ResetCode = code ?? ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim().ToLower();

            if (!_resetCodes.TryGetValue(email, out var stored) || stored.Expiry < DateTime.Now)
            {
                model.ErrorMessage = "رمز التحقق غير صالح أو انتهت صلاحيته. يرجى طلب رمز جديد.";
                return View(model);
            }

            if (stored.Code != model.ResetCode.Trim())
            {
                model.ErrorMessage = "رمز التحقق غير صحيح، تأكد من إدخال الأرقام الستة بشكل سليم.";
                return View(model);
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email);
            if (user == null)
            {
                model.ErrorMessage = "المستخدم غير موجود.";
                return View(model);
            }

            user.PasswordHash = _hasher.HashPassword(user, model.NewPassword);
            await _db.SaveChangesAsync();

            // إزالة الرمز بعد استخدامه
            _resetCodes.TryRemove(email, out _);

            TempData["Success"] = "تم تغيير كلمة المرور بنجاح! يمكنك الآن تسجيل الدخول بكلمة المرور الجديدة.";
            return RedirectToAction(nameof(Login));
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

        // ---------- إعدادات الحساب ----------
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Settings()
        {
            var currentUser = User.Identity?.Name;
            if (currentUser == null) return RedirectToAction("Login");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == currentUser);
            if (user == null) return RedirectToAction("Login");

            var model = new SettingsViewModel
            {
                Username = user.Username,
                Email = user.Email ?? "",
                CurrentProfilePicture = user.ProfilePicture
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SettingsViewModel model)
        {
            var currentUser = User.Identity?.Name;
            if (currentUser == null) return RedirectToAction("Login");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == currentUser);
            if (user == null) return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                model.CurrentProfilePicture = user.ProfilePicture;
                return View(model);
            }

            // تحديث الاسم والبريد
            if (model.Username != user.Username)
            {
                var usernameExists = await _db.Users.AnyAsync(u => u.Username.ToLower() == model.Username.ToLower() && u.Id != user.Id);
                if (usernameExists)
                {
                    ModelState.AddModelError("Username", "اسم المستخدم ده مستخدم بالفعل");
                    model.CurrentProfilePicture = user.ProfilePicture;
                    return View(model);
                }
                user.Username = model.Username.Trim();
            }

            user.Email = model.Email.Trim().ToLower();

            // تحديث كلمة المرور
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "لازم تكتب كلمة المرور الحالية عشان تغيرها");
                    model.CurrentProfilePicture = user.ProfilePicture;
                    return View(model);
                }

                if (_hasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("CurrentPassword", "كلمة المرور الحالية غلط");
                    model.CurrentProfilePicture = user.ProfilePicture;
                    return View(model);
                }

                user.PasswordHash = _hasher.HashPassword(user, model.NewPassword);
            }

            // تحديث صورة الملف الشخصي
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var imageExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();

                if (imageExts.Contains(ext))
                {
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var folder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                    Directory.CreateDirectory(folder);
                    var fullPath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(stream);
                    }

                    user.ProfilePicture = $"/uploads/profiles/{fileName}";
                }
            }

            await _db.SaveChangesAsync();

            // تحديث اسم المستخدم في session
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await SignInUserAsync(user);

            model.SuccessMessage = "تم تحديث الإعدادات بنجاح!";
            model.CurrentProfilePicture = user.ProfilePicture;
            return View(model);
        }

        private async Task SignInUserAsync(AppUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role),
                new("UserId", user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? "")
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
