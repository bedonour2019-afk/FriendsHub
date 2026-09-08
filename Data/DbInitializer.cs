using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext db)
        {
            db.Database.EnsureCreated();

            // التأكد من وجود عمود Email في جدول Users
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    ALTER TABLE Users ADD COLUMN Email TEXT NULL;
                ");
            }
            catch
            {
                // العمود موجود بالفعل
            }

            // التأكد من وجود الأعمدة الجديدة
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    ALTER TABLE Users ADD COLUMN ProfilePicture TEXT NULL;
                    ALTER TABLE Users ADD COLUMN IsOnline INTEGER NOT NULL DEFAULT 0;
                    ALTER TABLE Users ADD COLUMN LastSeen TEXT NULL;
                ");
            }
            catch
            {
                // الأعمدة موجودة بالفعل
            }

            // التأكد من إنشاء الجداول الجديدة إذا كانت قاعدة البيانات منشأة سابقاً
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS PostReactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PostId INTEGER NOT NULL,
                    Username TEXT NOT NULL,
                    ReactionType TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_PostReactions_PostId_Username ON PostReactions(PostId, Username);

                CREATE TABLE IF NOT EXISTS PostComments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PostId INTEGER NOT NULL,
                    Username TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ChatMessageReactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MessageId INTEGER NOT NULL,
                    Username TEXT NOT NULL,
                    ReactionType TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_ChatMessageReactions_MessageId_Username ON ChatMessageReactions(MessageId, Username);

                CREATE TABLE IF NOT EXISTS StoryComments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StoryId INTEGER NOT NULL,
                    Username TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Notifications (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RecipientUsername TEXT NULL,
                    ActorUsername TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    LinkUrl TEXT NULL,
                    IsRead INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_Notifications_RecipientUsername ON Notifications(RecipientUsername);

                CREATE TABLE IF NOT EXISTS PrivateChats (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    User1 TEXT NOT NULL,
                    User2 TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    LastMessageAt TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS PrivateChatMessages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PrivateChatId INTEGER NOT NULL,
                    SenderUsername TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    FilePath TEXT NULL,
                    Type TEXT NOT NULL,
                    SentAt TEXT NOT NULL,
                    IsRead INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS Teams (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CreatorUsername TEXT NOT NULL,
                    TeamA_Members TEXT NOT NULL,
                    TeamB_Members TEXT NOT NULL,
                    IsPinned INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL
                );
            ");

            // تحديث المستخدمين الموجودين مسبقاً
            var usersWithoutProfile = db.Users.Where(u => string.IsNullOrEmpty(u.ProfilePicture)).ToList();
            foreach (var user in usersWithoutProfile)
            {
                user.ProfilePicture = "/uploads/profiles/default.png";
                user.IsOnline = false;
                user.LastSeen = DateTime.Now;
            }
            db.SaveChanges();
        }
    }
}
