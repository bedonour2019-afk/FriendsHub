using System.ComponentModel.DataAnnotations;

namespace FriendsHub.Models
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // "Admin" or "User"
        [Required, MaxLength(20)]
        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? ProfilePicture { get; set; }

        public bool IsOnline { get; set; } = false;

        public DateTime? LastSeen { get; set; }
    }
}
