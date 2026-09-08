using System.ComponentModel.DataAnnotations;

namespace FriendsHub.Models
{
    public class Team
    {
        public int Id { get; set; }

        [Required]
        public string CreatorUsername { get; set; } = string.Empty;

        [Required]
        public string TeamA_Members { get; set; } = string.Empty;

        [Required]
        public string TeamB_Members { get; set; } = string.Empty;

        public bool IsPinned { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
