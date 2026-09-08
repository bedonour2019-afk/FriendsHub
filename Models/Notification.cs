using System.ComponentModel.DataAnnotations;

namespace FriendsHub.Models
{
    public class Notification
    {
        public int Id { get; set; }

        // اسم المستخدم المستلم (أو فارغ/null إذا كان إشعار عام للكل)
        [MaxLength(50)]
        public string? RecipientUsername { get; set; }

        [Required, MaxLength(50)]
        public string ActorUsername { get; set; } = string.Empty;

        // chat | post | story | game | watch | call
        [Required, MaxLength(30)]
        public string Type { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? LinkUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
