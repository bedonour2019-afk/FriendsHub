using FriendsHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FriendsHub.Hubs
{
    [Authorize]
    public class GamesHub : Hub
    {
        private readonly GameRoomManager _rooms;
        private const string LobbyGroup = "Lobby";

        public GamesHub(GameRoomManager rooms)
        {
            _rooms = rooms;
        }

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, LobbyGroup);
            await base.OnConnectedAsync();
        }

        public object GetLobbyLists() => _rooms.GetLobbyLists();

        public async Task<string> CreateChallenge()
        {
            var username = Context.User!.Identity!.Name!;
            var room = _rooms.CreateRoom(username);
            await Clients.Group(LobbyGroup).SendAsync("LobbyUpdated", _rooms.GetLobbyLists());
            return room.RoomId;
        }

        // بيتنادى لما حد يفتح صفحة الغرفة - بيحدد هو لاعب ولا متفرج
        public async Task<object> JoinRoomPage(string roomId)
        {
            var username = Context.User!.Identity!.Name!;
            var room = _rooms.GetRoom(roomId);
            if (room == null) return new { error = "الغرفة مش موجودة أو خلصت" };

            string role;

            if (room.PlayerX == username)
            {
                role = "X";
            }
            else if (room.PlayerO == username)
            {
                role = "O";
            }
            else if (room.Status == "WaitingForOpponent")
            {
                var (ok, error) = _rooms.JoinAsOpponent(roomId, username);
                if (!ok) return new { error };
                role = "O";
                await Clients.Group(LobbyGroup).SendAsync("LobbyUpdated", _rooms.GetLobbyLists());
            }
            else
            {
                role = "Spectator";
                _rooms.AddSpectator(roomId);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            var state = room.ToClientState();
            await Clients.OthersInGroup(roomId).SendAsync("RoomUpdated", state);

            return new { role, room = state };
        }

        public async Task MakeMove(string roomId, int index)
        {
            var username = Context.User!.Identity!.Name!;
            var (ok, _) = _rooms.MakeMove(roomId, username, index);
            if (!ok) return;

            var room = _rooms.GetRoom(roomId);
            if (room == null) return;

            await Clients.Group(roomId).SendAsync("RoomUpdated", room.ToClientState());

            if (room.Status == "Finished")
                await Clients.Group(LobbyGroup).SendAsync("LobbyUpdated", _rooms.GetLobbyLists());
        }

        public async Task RequestRematch(string roomId)
        {
            var username = Context.User!.Identity!.Name!;
            var (ok, _) = _rooms.Rematch(roomId, username);
            if (!ok) return;

            var room = _rooms.GetRoom(roomId);
            if (room == null) return;

            await Clients.Group(roomId).SendAsync("RoomUpdated", room.ToClientState());
            await Clients.Group(LobbyGroup).SendAsync("LobbyUpdated", _rooms.GetLobbyLists());
        }
    }
}
