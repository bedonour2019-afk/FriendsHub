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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}
