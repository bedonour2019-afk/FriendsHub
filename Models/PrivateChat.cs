using System.ComponentModel.DataAnnotations;

namespace FriendsHub.Models
{
    public class PrivateChat
    {
        public int Id { get; set; }

        [Required]
        public string User1 { get; set; } = string.Empty;

        [Required]
        public string User2 { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastMessageAt { get; set; }
    }
}
