using FriendsHub.Models;
using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<ChatMessage> Messages => Set<ChatMessage>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Story> Stories => Set<Story>();
        public DbSet<PostReaction> PostReactions => Set<PostReaction>();
        public DbSet<PostComment> PostComments => Set<PostComment>();
        public DbSet<ChatMessageReaction> ChatMessageReactions => Set<ChatMessageReaction>();
        public DbSet<StoryComment> StoryComments => Set<StoryComment>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<PrivateChat> PrivateChats => Set<PrivateChat>();
        public DbSet<PrivateChatMessage> PrivateChatMessages => Set<PrivateChatMessage>();
        public DbSet<Team> Teams => Set<Team>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<PostReaction>()
                .HasIndex(r => new { r.PostId, r.Username })
                .IsUnique();

            modelBuilder.Entity<ChatMessageReaction>()
                .HasIndex(r => new { r.MessageId, r.Username })
                .IsUnique();

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.RecipientUsername);
        }
    }
}
