using FriendsHub.Data;
using FriendsHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FriendsHub.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _db;
        private static readonly string[] ValidTypes = { "text", "image", "audio", "sticker" };

        public ChatHub(AppDbContext db)
        {
            _db = db;
        }

        // type: text | image | audio | sticker
        // content: نص الرسالة (text/sticker) أو رابط الملف (image/audio)
        public async Task SendMessage(string type, string content)
        {
            var username = Context.User!.Identity!.Name!;
            if (string.IsNullOrWhiteSpace(content)) return;
            if (!ValidTypes.Contains(type)) type = "text";

            var message = new ChatMessage
            {
                Username = username,
                Type = type,
                Content = (type == "text" || type == "sticker") ? content.Trim() : null,
                FilePath = (type == "image" || type == "audio") ? content : null,
                SentAt = DateTime.Now
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            await Clients.All.SendAsync(
                "ReceiveMessage",
                message.Username,
                message.Type,
                message.Content,
                message.FilePath,
                message.SentAt.ToString("HH:mm")
            );
        }
    }
}
