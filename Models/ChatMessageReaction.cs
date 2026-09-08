using System.ComponentModel.DataAnnotations;

namespace FriendsHub.Models
{
    public class ChatMessageReaction
    {
        public int Id { get; set; }

        public int MessageId { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        // like | love | haha | sad | angry
        [Required, MaxLength(20)]
        public string ReactionType { get; set; } = "like";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
