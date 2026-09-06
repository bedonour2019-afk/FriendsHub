using FriendsHub.Data;
using FriendsHub.Hubs;
using FriendsHub.Models;
using FriendsHub.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddSingleton<GameRoomManager>();
builder.Services.AddSingleton<WatchPartyManager>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// إنشاء قاعدة البيانات وعمل أدمن افتراضي أول ما السيرفر يشتغل
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        var hasher = new PasswordHasher<AppUser>();
        var seedUsername = builder.Configuration["SeedAdmin:Username"] ?? "admin";
        var seedPassword = builder.Configuration["SeedAdmin:Password"] ?? "Admin@123";

        var admin = new AppUser { Username = seedUsername, Role = "Admin" };
        admin.PasswordHash = hasher.HashPassword(admin, seedPassword);

        db.Users.Add(admin);
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapHub<ChatHub>("/chatHub");
app.MapHub<GamesHub>("/gamesHub");
app.MapHub<WatchPartyHub>("/watchPartyHub");

app.Run();
