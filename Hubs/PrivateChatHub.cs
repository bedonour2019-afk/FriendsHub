using FriendsHub.Data;
using FriendsHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Hubs
{
    [Authorize]
    public class PrivateChatHub : Hub
    {
        private readonly AppDbContext _db;
        private static readonly Dictionary<string, string> _userConnections = new();

        public PrivateChatHub(AppDbContext db)
        {
            _db = db;
        }

        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                _userConnections[Context.ConnectionId] = username;
                await Groups.AddToGroupAsync(Context.ConnectionId, $"pchat_user_{username}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _userConnections.Remove(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(int chatId, string content, string? filePath = null, string type = "text")
        {
            var senderUsername = Context.User?.Identity?.Name;
            if (string.IsNullOrEmpty(senderUsername)) return;
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(filePath)) return;

            var message = new PrivateChatMessage
            {
                PrivateChatId = chatId,
                SenderUsername = senderUsername,
                Content = (type == "text" || type == "sticker") ? content : null,
                FilePath = (type == "image" || type == "audio") ? filePath : null,
                Type = type,
                SentAt = DateTime.Now,
                IsRead = false
            };

            _db.PrivateChatMessages.Add(message);

            var chat = await _db.PrivateChats.FindAsync(chatId);
            if (chat != null)
            {
                if (chat.User1 != senderUsername && chat.User2 != senderUsername) return;
                chat.LastMessageAt = DateTime.Now;
            }
            else
            {
                return;
            }

            await _db.SaveChangesAsync();

            var otherUser = chat!.User1 == senderUsername ? chat.User2 : chat.User1;

            var payload = new
            {
                message.Id,
                message.PrivateChatId,
                message.SenderUsername,
                message.Content,
                message.FilePath,
                message.Type,
                SentAt = message.SentAt.ToString("HH:mm"),
                message.IsRead
            };

            await Clients.Group($"pchat_user_{otherUser}").SendAsync("ReceivePrivateMessage", payload);
            await Clients.Caller.SendAsync("ReceivePrivateMessage", payload);
        }

        public async Task MarkAsRead(int chatId)
        {
            var username = Context.User?.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return;

            var messages = await _db.PrivateChatMessages
                .Where(m => m.PrivateChatId == chatId && m.SenderUsername != username && !m.IsRead)
                .ToListAsync();

            foreach (var msg in messages)
            {
                msg.IsRead = true;
            }

            await _db.SaveChangesAsync();

            var chat = await _db.PrivateChats.FindAsync(chatId);
            if (chat != null)
            {
                var otherUser = chat.User1 == username ? chat.User2 : chat.User1;
                await Clients.Group($"pchat_user_{otherUser}").SendAsync("MessagesRead", chatId);
            }
        }
    }
}
