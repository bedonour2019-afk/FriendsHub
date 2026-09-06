using FriendsHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FriendsHub.Hubs
{
    [Authorize]
    public class WatchPartyHub : Hub
    {
        private readonly WatchPartyManager _rooms;
        private const string LobbyGroup = "WatchLobby";

        public WatchPartyHub(WatchPartyManager rooms)
        {
            _rooms = rooms;
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
            return new { roomId = room.RoomId };
        }

        public async Task<object> JoinRoom(string roomId)
        {
            var username = Context.User!.Identity!.Name!;
            var room = _rooms.GetRoom(roomId);
            if (room == null) return new { error = "الغرفة مش موجودة أو خلصت" };

            _rooms.AddParticipant(roomId, username);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            var isHost = room.HostUsername == username;
            await Clients.Group(roomId).SendAsync("ParticipantsUpdated", room.Participants.ToList());

            return new { isHost, room = room.ToClientState() };
        }

        public async Task PlayVideo(string roomId, double time)
        {
            var room = _rooms.GetRoom(roomId);
            if (room == null || !IsHost(room)) return;
            lock (room) { room.IsPlaying = true; room.CurrentTime = time; }
            await Clients.OthersInGroup(roomId).SendAsync("Play", time);
        }

        public async Task PauseVideo(string roomId, double time)
        {
            var room = _rooms.GetRoom(roomId);
            if (room == null || !IsHost(room)) return;
            lock (room) { room.IsPlaying = false; room.CurrentTime = time; }
            await Clients.OthersInGroup(roomId).SendAsync("Pause", time);
        }

        public async Task SeekVideo(string roomId, double time)
        {
            var room = _rooms.GetRoom(roomId);
            if (room == null || !IsHost(room)) return;
            lock (room) { room.CurrentTime = time; }
            await Clients.OthersInGroup(roomId).SendAsync("Seek", time);
        }

        // بيتبعت كل شوية ثواني من المضيف بس عشان يظبط أي فرق توقيت بسيط عند الباقي
        public async Task Heartbeat(string roomId, double time)
        {
            var room = _rooms.GetRoom(roomId);
            if (room == null || !IsHost(room)) return;
            lock (room) { room.CurrentTime = time; }
            await Clients.OthersInGroup(roomId).SendAsync("Heartbeat", time);
        }

        public async Task ChangeVideo(string roomId, string videoUrl)
        {
            var room = _rooms.GetRoom(roomId);
            if (room == null || !IsHost(room)) return;

            var videoId = WatchPartyManager.ExtractYoutubeId(videoUrl);
            if (videoId == null) return;

            lock (room) { room.VideoId = videoId; room.IsPlaying = false; room.CurrentTime = 0; }
            await Clients.Group(roomId).SendAsync("VideoChanged", videoId);
        }

        // شات جانبي خاص بغرفة المشاهدة (لايف بس، مش محفوظ في قاعدة البيانات)
        public async Task SendPartyMessage(string roomId, string content)
        {
            var username = Context.User!.Identity!.Name!;
            if (string.IsNullOrWhiteSpace(content)) return;

            await Clients.Group(roomId).SendAsync(
                "PartyMessage", username, content.Trim(), DateTime.Now.ToString("HH:mm"));
        }

        private bool IsHost(WatchPartyRoom room) =>
            room.HostUsername == Context.User!.Identity!.Name;
    }
}
