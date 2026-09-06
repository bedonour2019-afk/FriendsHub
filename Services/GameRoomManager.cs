using System.Collections.Concurrent;

namespace FriendsHub.Services
{
    public class TicTacToeRoom
    {
        public string RoomId { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string? PlayerX { get; set; }
        public string? PlayerO { get; set; }
        public string?[] Board { get; set; } = new string?[9];
        public string CurrentTurn { get; set; } = "X";

        // WaitingForOpponent | Playing | Finished
        public string Status { get; set; } = "WaitingForOpponent";

        // X | O | Draw | null
        public string? Winner { get; set; }

        public int SpectatorsCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public object ToClientState() => new
        {
            roomId = RoomId,
            playerX = PlayerX,
            playerO = PlayerO,
            board = Board,
            currentTurn = CurrentTurn,
            status = Status,
            winner = Winner,
            spectatorsCount = SpectatorsCount
        };
    }

    public class GameRoomManager
    {
        private readonly ConcurrentDictionary<string, TicTacToeRoom> _rooms = new();
        private static readonly int[][] WinPatterns =
        {
            new[]{0,1,2}, new[]{3,4,5}, new[]{6,7,8},
            new[]{0,3,6}, new[]{1,4,7}, new[]{2,5,8},
            new[]{0,4,8}, new[]{2,4,6}
        };

        public TicTacToeRoom CreateRoom(string username)
        {
            var room = new TicTacToeRoom { PlayerX = username };
            _rooms[room.RoomId] = room;
            return room;
        }

        public TicTacToeRoom? GetRoom(string roomId) =>
            _rooms.TryGetValue(roomId, out var room) ? room : null;

        public List<TicTacToeRoom> GetOpenChallenges() =>
            _rooms.Values.Where(r => r.Status == "WaitingForOpponent")
                .OrderByDescending(r => r.CreatedAt).ToList();

        public List<TicTacToeRoom> GetActiveGames() =>
            _rooms.Values.Where(r => r.Status == "Playing")
                .OrderByDescending(r => r.CreatedAt).ToList();

        public object GetLobbyLists() => new
        {
            openChallenges = GetOpenChallenges().Select(r => new { r.RoomId, r.PlayerX }),
            activeGames = GetActiveGames().Select(r => new { r.RoomId, r.PlayerX, r.PlayerO, r.SpectatorsCount })
        };

        public (bool ok, string? error) JoinAsOpponent(string roomId, string username)
        {
            var room = GetRoom(roomId);
            if (room == null) return (false, "الغرفة مش موجودة");
            lock (room)
            {
                if (room.Status != "WaitingForOpponent") return (false, "التحدي ده اتقفل خلاص");
                if (room.PlayerX == username) return (false, "مينفعش تلعب ضد نفسك");

                room.PlayerO = username;
                room.Status = "Playing";
                room.CurrentTurn = "X";
            }
            return (true, null);
        }

        public (bool ok, string? error) MakeMove(string roomId, string username, int index)
        {
            var room = GetRoom(roomId);
            if (room == null) return (false, "الغرفة مش موجودة");

            lock (room)
            {
                if (room.Status != "Playing") return (false, "اللعبة خلصت");
                if (index < 0 || index > 8) return (false, "حركة غلط");
                if (room.Board[index] != null) return (false, "الخانة دي متملية");

                var mySymbol = room.PlayerX == username ? "X" : room.PlayerO == username ? "O" : null;
                if (mySymbol == null) return (false, "أنت مش لاعب في الغرفة دي");
                if (mySymbol != room.CurrentTurn) return (false, "مش دورك");

                room.Board[index] = mySymbol;

                var winner = CheckWinner(room.Board);
                if (winner != null)
                {
                    room.Status = "Finished";
                    room.Winner = winner;
                }
                else if (room.Board.All(c => c != null))
                {
                    room.Status = "Finished";
                    room.Winner = "Draw";
                }
                else
                {
                    room.CurrentTurn = mySymbol == "X" ? "O" : "X";
                }
            }

            return (true, null);
        }

        public (bool ok, string? error) Rematch(string roomId, string username)
        {
            var room = GetRoom(roomId);
            if (room == null) return (false, "الغرفة مش موجودة");

            lock (room)
            {
                if (room.Status != "Finished") return (false, "اللعبة لسه شغالة");
                if (room.PlayerX != username && room.PlayerO != username)
                    return (false, "المتفرجين مينفعش يطلبوا لعبة جديدة");

                room.Board = new string?[9];
                room.CurrentTurn = "X";
                room.Status = "Playing";
                room.Winner = null;
            }

            return (true, null);
        }

        public void AddSpectator(string roomId)
        {
            var room = GetRoom(roomId);
            if (room == null) return;
            lock (room) { room.SpectatorsCount++; }
        }

        private static string? CheckWinner(string?[] board)
        {
            foreach (var p in WinPatterns)
            {
                if (board[p[0]] != null && board[p[0]] == board[p[1]] && board[p[1]] == board[p[2]])
                    return board[p[0]];
            }
            return null;
        }
    }
}
