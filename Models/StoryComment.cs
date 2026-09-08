using System.ComponentModel.DataAnnotations;

namespace FriendsHub.Models
{
    public class StoryComment
    {
        public int Id { get; set; }

        public int StoryId { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
