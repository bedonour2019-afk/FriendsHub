using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace FriendsHub.Services
{
    public class WatchPartyRoom
    {
        public string RoomId { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string HostUsername { get; set; } = string.Empty;
        public string VideoId { get; set; } = string.Empty;
        public bool IsPlaying { get; set; } = false;
        public double CurrentTime { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public HashSet<string> Participants { get; set; } = new();

        public object ToClientState() => new
        {
            roomId = RoomId,
            hostUsername = HostUsername,
            videoId = VideoId,
            isPlaying = IsPlaying,
            currentTime = CurrentTime,
            participants = Participants.ToList()
        };
    }

    public class WatchPartyManager
    {
        private readonly ConcurrentDictionary<string, WatchPartyRoom> _rooms = new();

        public WatchPartyRoom CreateRoom(string host, string videoId)
        {
            var room = new WatchPartyRoom { HostUsername = host, VideoId = videoId };
            room.Participants.Add(host);
            _rooms[room.RoomId] = room;
            return room;
        }

        public WatchPartyRoom? GetRoom(string roomId) =>
            _rooms.TryGetValue(roomId, out var r) ? r : null;

        public bool RemoveRoom(string roomId) =>
            _rooms.TryRemove(roomId, out _);

        public List<WatchPartyRoom> GetActiveRooms() =>
            _rooms.Values.OrderByDescending(r => r.CreatedAt).ToList();

        public object GetLobbyList() =>
            GetActiveRooms().Select(r => new
            {
                r.RoomId,
                r.HostUsername,
                ParticipantsCount = r.Participants.Count
            });

        public void AddParticipant(string roomId, string username)
        {
            var room = GetRoom(roomId);
            if (room == null) return;
            lock (room) { room.Participants.Add(username); }
        }

        public void RemoveParticipant(string roomId, string username)
        {
            var room = GetRoom(roomId);
            if (room == null) return;
            lock (room) { room.Participants.Remove(username); }
        }

        // بيقبل: رابط يوتيوب كامل، رابط مختصر، رابط embed، رابط shorts، أو الـ ID نفسه
        public static string? ExtractYoutubeId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            url = url.Trim();

            if (Regex.IsMatch(url, @"^[a-zA-Z0-9_-]{11}$"))
                return url;

            var patterns = new[]
            {
                @"(?:https?:\/\/)?(?:www\.)?youtu\.be\/([a-zA-Z0-9_-]{11})",
                @"(?:https?:\/\/)?(?:www\.)?youtube\.com\/watch\?.*v=([a-zA-Z0-9_-]{11})",
                @"(?:https?:\/\/)?(?:www\.)?youtube\.com\/embed\/([a-zA-Z0-9_-]{11})",
                @"(?:https?:\/\/)?(?:www\.)?youtube\.com\/shorts\/([a-zA-Z0-9_-]{11})",
                @"(?:https?:\/\/)?(?:www\.)?youtube\.com\/v\/([a-zA-Z0-9_-]{11})"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(url, pattern);
                if (match.Success) return match.Groups[1].Value;
            }

            return null;
        }
    }
}
