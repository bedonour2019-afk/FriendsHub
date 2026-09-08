namespace FriendsHub.Models
{
    public class PrivateChatListItemViewModel
    {
        public int ChatId { get; set; }
        public string OtherUser { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public string LastMessageTime { get; set; } = string.Empty;
        public int UnreadCount { get; set; }
    }

    public class ChatMessageViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public string? Content { get; set; }
        public string? FilePath { get; set; }
        public string SentAt { get; set; } = string.Empty;
    }
}
