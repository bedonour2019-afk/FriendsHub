namespace FriendsHub.Models
{
    public class Story
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ستوري تختفي بعد 24 ساعة
        public bool IsExpired => DateTime.Now.Subtract(CreatedAt).TotalHours >= 24;
    }
}
