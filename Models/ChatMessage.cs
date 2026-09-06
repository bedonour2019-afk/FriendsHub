namespace FriendsHub.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;

        // text | image | audio | sticker
        public string Type { get; set; } = "text";

        // نص الرسالة أو الإيموجي (للـ text والـ sticker)
        public string? Content { get; set; }

        // مسار الملف (للصور والتسجيلات الصوتية)
        public string? FilePath { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}
