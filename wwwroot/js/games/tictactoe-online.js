(function () {
    const boardEl = document.getElementById("board");
    const statusEl = document.getElementById("status");
    const playerXNameEl = document.getElementById("playerXName");
    const playerONameEl = document.getElementById("playerOName");
    const spectatorsCountEl = document.getElementById("spectatorsCount");
    const rematchBtn = document.getElementById("rematchBtn");

    let myRole = null; // "X" | "O" | "Spectator"
    let currentState = null;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/gamesHub")
        .withAutomaticReconnect()
        .build();

    function renderBoard(state) {
        boardEl.innerHTML = "";
        state.board.forEach((value, index) => {
            const cell = document.createElement("div");
            cell.className = "ttt-cell";
            cell.textContent = value ?? "";

            const canPlay = myRole === state.currentTurn && state.status === "Playing" && !value;
            if (canPlay) {
                cell.classList.add("playable");
                cell.addEventListener("click", () => {
                    connection.invoke("MakeMove", roomId, index).catch(err => console.error(err));
                });
            }
            boardEl.appendChild(cell);
        });
    }

    function renderState(state) {
        currentState = state;

        playerXNameEl.textContent = state.playerX ?? "-";
        playerONameEl.textContent = state.playerO ?? "في انتظار خصم...";
        spectatorsCountEl.textContent = state.spectatorsCount ?? 0;

        renderBoard(state);

        if (state.status === "WaitingForOpponent") {
            statusEl.textContent = "⏳ في انتظار خصم ينضم للتحدي...";
            rematchBtn.style.display = "none";
        } else if (state.status === "Playing") {
            if (myRole === "Spectator") {
                statusEl.textContent = `دور اللاعب: ${state.currentTurn === "X" ? state.playerX : state.playerO} (${state.currentTurn})`;
            } else if (myRole === state.currentTurn) {
                statusEl.textContent = "🎯 دورك إنك تلعب!";
            } else {
                statusEl.textContent = "⏳ في انتظار الخصم يلعب...";
            }
            rematchBtn.style.display = "none";
        } else if (state.status === "Finished") {
            if (state.winner === "Draw") {
                statusEl.textContent = "🤝 تعادل!";
            } else {
                const winnerName = state.winner === "X" ? state.playerX : state.playerO;
                statusEl.textContent = `🎉 ${winnerName} كسب اللعبة!`;
            }
            rematchBtn.style.display = (myRole === "X" || myRole === "O") ? "inline-block" : "none";
        }
    }

    connection.on("RoomUpdated", renderState);

    connection.start()
        .then(() => connection.invoke("JoinRoomPage", roomId))
        .then(result => {
            if (result.error) {
                statusEl.textContent = "❌ " + result.error;
                return;
            }
            myRole = result.role;
            renderState(result.room);
        })
        .catch(err => console.error(err));

    rematchBtn.addEventListener("click", () => {
        connection.invoke("RequestRematch", roomId).catch(err => console.error(err));
    });
})();
