namespace FriendsHub.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
