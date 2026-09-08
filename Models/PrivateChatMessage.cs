using System.ComponentModel.DataAnnotations;

namespace FriendsHub.Models
{
    public class PrivateChatMessage
    {
        public int Id { get; set; }

        [Required]
        public int PrivateChatId { get; set; }

        [Required]
        public string SenderUsername { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? FilePath { get; set; }

        public string Type { get; set; } = "text"; // text, image, audio

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}
