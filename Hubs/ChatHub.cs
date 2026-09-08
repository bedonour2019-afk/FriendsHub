using FriendsHub.Data;
using FriendsHub.Models;
using FriendsHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FriendsHub.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notifications;
        private static readonly string[] ValidTypes = { "text", "image", "audio", "sticker" };
        private static readonly string[] ValidReactions = { "like", "love", "haha", "sad", "angry" };

        public ChatHub(AppDbContext db, NotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_user_{username}");
            }
            await base.OnConnectedAsync();
        }

        // type: text | image | audio | sticker
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
                message.Id,
                message.Username,
                message.Type,
                message.Content,
                message.FilePath,
                message.SentAt.ToString("HH:mm")
            );

            // إشعار للأصدقاء
            var snippet = type switch
            {
                "image" => "صورة 🖼️",
                "audio" => "تسجيل صوتي 🎤",
                "sticker" => $"ستيكر {content}",
                _ => content
            };

            await _notifications.SendNotificationAsync(
                recipientUsername: null,
                actorUsername: username,
                type: "chat",
                title: $"رسالة جديدة من {username} 💬",
                message: snippet.Length > 35 ? snippet[..35] + "..." : snippet,
                linkUrl: "/Chat"
            );
        }

        // ---------- حذف رسالة (لصاحبها أو للأدمن) ----------
        public async Task DeleteMessage(int messageId)
        {
            var username = Context.User!.Identity!.Name!;
            var isAdmin = Context.User.IsInRole("Admin");

            var message = await _db.Messages.FindAsync(messageId);
            if (message == null) return;

            if (message.Username != username && !isAdmin) return;

            var reactions = _db.ChatMessageReactions.Where(r => r.MessageId == messageId);
            _db.ChatMessageReactions.RemoveRange(reactions);

            _db.Messages.Remove(message);
            await _db.SaveChangesAsync();

            await Clients.All.SendAsync("MessageDeleted", messageId);
        }

        // ---------- تفاعل ريأكت فيسبوك على رسالة ----------
        public async Task ToggleMessageReaction(int messageId, string reactionType)
        {
            var username = Context.User!.Identity!.Name!;
            if (!ValidReactions.Contains(reactionType)) return;

            var message = await _db.Messages.FindAsync(messageId);
            if (message == null) return;

            var existing = await _db.ChatMessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.Username == username);

            if (existing != null)
            {
                if (existing.ReactionType == reactionType)
                {
                    _db.ChatMessageReactions.Remove(existing);
                }
                else
                {
                    existing.ReactionType = reactionType;
                    existing.CreatedAt = DateTime.Now;
                }
            }
            else
            {
                _db.ChatMessageReactions.Add(new ChatMessageReaction
                {
                    MessageId = messageId,
                    Username = username,
                    ReactionType = reactionType,
                    CreatedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();

            var counts = await _db.ChatMessageReactions
                .Where(r => r.MessageId == messageId)
                .GroupBy(r => r.ReactionType)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            await Clients.All.SendAsync("MessageReactionUpdated", messageId, counts);
        }

        // ========================================================
        // WebRTC Signaling للمكالمات الصوتية والمرئية
        // ========================================================

        // بدء اتصال (صوتي أو فيديو) للجروب
        public async Task StartCall(string callType)
        {
            var username = Context.User!.Identity!.Name!;
            await Clients.Others.SendAsync("IncomingCall", username, callType);
        }

        public async Task AcceptCall(string callerUsername)
        {
            var username = Context.User!.Identity!.Name!;
            await Clients.Group($"chat_user_{callerUsername}").SendAsync("CallAccepted", username);
        }

        public async Task RejectCall(string callerUsername)
        {
            var username = Context.User!.Identity!.Name!;
            await Clients.Group($"chat_user_{callerUsername}").SendAsync("CallRejected", username);
        }

        public async Task SendSignal(string targetUsername, object signalData)
        {
            var senderUsername = Context.User!.Identity!.Name!;
            await Clients.Group($"chat_user_{targetUsername}").SendAsync("ReceiveSignal", senderUsername, signalData);
        }

        public async Task EndCall(string targetUsername)
        {
            var senderUsername = Context.User!.Identity!.Name!;
            if (!string.IsNullOrEmpty(targetUsername))
            {
                await Clients.Group($"chat_user_{targetUsername}").SendAsync("CallEnded", senderUsername);
            }
            else
            {
                await Clients.Others.SendAsync("CallEnded", senderUsername);
            }
        }
    }
}
