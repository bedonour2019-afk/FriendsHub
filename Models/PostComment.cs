using System.ComponentModel.DataAnnotations;

namespace FriendsHub.Models
{
    public class PostComment
    {
        public int Id { get; set; }

        public int PostId { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
