using FriendsHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FriendsHub.Hubs
{
    [Authorize]
    public class WatchPartyHub : Hub
    {
        private readonly WatchPartyManager _rooms;
        private readonly NotificationService _notifications;
        private const string LobbyGroup = "WatchLobby";

        public WatchPartyHub(WatchPartyManager rooms, NotificationService notifications)
        {
            _rooms = rooms;
            _notifications = notifications;
        }

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, LobbyGroup);
            await base.OnConnectedAsync();
        }

        public object GetActiveRooms() => _rooms.GetLobbyList();

        public async Task<object> CreateRoom(string videoUrl)
        {
            var username = Context.User!.Identity!.Name!;
            var videoId = WatchPartyManager.ExtractYoutubeId(videoUrl);
            if (videoId == null) return new { error = "رابط يوتيوب مش صحيح، حاول تلصق رابط الفيديو تاني" };

            var room = _rooms.CreateRoom(username, videoId);
            await Clients.Group(LobbyGroup).SendAsync("LobbyUpdated", _rooms.GetLobbyList());

            // إشعار للأصدقاء
            await _notifications.SendNotificationAsync(
                recipientUsername: null,
                actorUsername: username,
                type: "watch",
                title: "مشاهدة جماعية جديدة 🎬",
                message: $"{username} بدأ غرفة مشاهدة جماعية، انضم الآن!",
                linkUrl: $"/Watch/Room/{room.RoomId}"
            );

            return new { roomId = room.RoomId };
        }

        public async Task<object> JoinRoom(string roomId)
        {
            var username = Context.User!.Identity!.Name!;
            var room = _rooms.GetRoom(roomId);
            if (room == null) return new { error = "الغرفة مش موجودة أو تم إغلاقها" };

            _rooms.AddParticipant(roomId, username);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            var isHost = room.HostUsername == username || Context.User.IsInRole("Admin");
            await Clients.Group(roomId).SendAsync("ParticipantsUpdated", room.Participants.ToList());

            return new { isHost, room = room.ToClientState() };
        }

        // الجميع يستطيع تشغيل الفيديو
        public async Task PlayVideo(string roomId, double time)
        {
            var room = _rooms.GetRoom(roomId);
            if (room == null) return;
            lock (room) { room.IsPlaying = true; room.CurrentTime = time; }
            await Clients.OthersInGroup(roomId).SendAsync("Play", time);
        }

        // الجميع يستطيع إيقاف الفيديو مؤقتاً
        public async Task PauseVideo(string roomId, double time)
        {
            var room = _rooms.GetRoom(roomId);
            if (room == null) return;
            lock (room) { room.IsPlaying = false; room.CurrentTime = time; }
            await Clients.OthersInGroup(roomId).SendAsync("Pause", time);
        }

        // الجميع يستطيع تقديم/ترجيع الفيديو
        public async Task SeekVideo(string roomId, double time)
        {
            var room = _rooms.GetRoom(roomId);
            if (room == null) return;
            lock (room) { room.CurrentTime = time; }
            await Clients.OthersInGroup(roomId).SendAsync("Seek", time);
        }

        public async Task Heartbeat(string roomId, double time)
        {
            var room = _rooms.GetRoom(roomId);
            if (room == null) return;
            lock (room) { room.CurrentTime = time; }
            await Clients.OthersInGroup(roomId).SendAsync("Heartbeat", time);
        }

        // الجميع يستطيع تغيير الفيديو
        public async Task ChangeVideo(string roomId, string videoUrl)
        {
            var room = _rooms.GetRoom(roomId);
            if (room == null) return;

            var videoId = WatchPartyManager.ExtractYoutubeId(videoUrl);
            if (videoId == null) return;

            lock (room)
            {
                room.VideoId = videoId;
                room.IsPlaying = true;
                room.CurrentTime = 0;
            }

            await Clients.Group(roomId).SendAsync("VideoChanged", videoId);
        }

        // إنهاء الجلسة وإغلاق الغرفة (للمضيف أو الأدمن)
        public async Task EndSession(string roomId)
        {
            var username = Context.User!.Identity!.Name!;
            var isAdmin = Context.User.IsInRole("Admin");
            var room = _rooms.GetRoom(roomId);
            if (room == null) return;

            if (room.HostUsername != username && !isAdmin) return;

            _rooms.RemoveRoom(roomId);
            await Clients.Group(roomId).SendAsync("RoomEnded", "تم إنهاء الجلسة وإغلاق الغرفة من قبل المضيف.");
            await Clients.Group(LobbyGroup).SendAsync("LobbyUpdated", _rooms.GetLobbyList());
        }

        // مغادرة الغرفة لمشارك
        public async Task LeaveRoom(string roomId)
        {
            var username = Context.User!.Identity!.Name!;
            _rooms.RemoveParticipant(roomId, username);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

            var room = _rooms.GetRoom(roomId);
            if (room != null)
            {
                await Clients.Group(roomId).SendAsync("ParticipantsUpdated", room.Participants.ToList());
            }
            await Clients.Group(LobbyGroup).SendAsync("LobbyUpdated", _rooms.GetLobbyList());
        }

        // شات جانبي خاص بالغرفة
        public async Task SendPartyMessage(string roomId, string content)
        {
            var username = Context.User!.Identity!.Name!;
            if (string.IsNullOrWhiteSpace(content)) return;

            await Clients.Group(roomId).SendAsync(
                "PartyMessage", username, content.Trim(), DateTime.Now.ToString("HH:mm"));
        }
    }
}
