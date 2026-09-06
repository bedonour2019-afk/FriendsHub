(function () {
    const createBtn = document.getElementById("createChallengeBtn");
    const openChallengesEl = document.getElementById("openChallenges");
    const activeGamesEl = document.getElementById("activeGames");

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/gamesHub")
        .withAutomaticReconnect()
        .build();

    function renderLobby(data) {
        if (data.openChallenges && data.openChallenges.length > 0) {
            openChallengesEl.innerHTML = "";
            data.openChallenges.forEach(c => {
                const item = document.createElement("div");
                item.className = "challenge-item";
                const canJoin = c.playerX !== currentUser;
                item.innerHTML = `
                    <span>⚔️ <strong>${c.playerX}</strong> عايز خصم</span>
                    ${canJoin
                        ? `<button class="btn-primary join-btn" data-room="${c.roomId}">انضم كخصم</button>`
                        : `<span class="badge">تحديك أنت</span>`}
                `;
                openChallengesEl.appendChild(item);
            });
        } else {
            openChallengesEl.innerHTML = `<p class="empty-msg">مفيش تحديات مفتوحة دلوقتي</p>`;
        }

        if (data.activeGames && data.activeGames.length > 0) {
            activeGamesEl.innerHTML = "";
            data.activeGames.forEach(g => {
                const item = document.createElement("div");
                item.className = "challenge-item";
                item.innerHTML = `
                    <span>🎮 <strong>${g.playerX}</strong> ضد <strong>${g.playerO}</strong> (👀 ${g.spectatorsCount})</span>
                    <button class="btn-secondary watch-btn" data-room="${g.roomId}">اتفرج</button>
                `;
                activeGamesEl.appendChild(item);
            });
        } else {
            activeGamesEl.innerHTML = `<p class="empty-msg">مفيش مباريات شغالة دلوقتي</p>`;
        }

        document.querySelectorAll(".join-btn, .watch-btn").forEach(btn => {
            btn.addEventListener("click", () => {
                window.location.href = "/Games/TicTacToeRoom/" + btn.dataset.room;
            });
        });
    }

    connection.on("LobbyUpdated", renderLobby);

    connection.start()
        .then(() => connection.invoke("GetLobbyLists"))
        .then(renderLobby)
        .catch(err => console.error(err));

    createBtn.addEventListener("click", () => {
        connection.invoke("CreateChallenge")
            .then(roomId => {
                window.location.href = "/Games/TicTacToeRoom/" + roomId;
            })
            .catch(err => console.error(err));
    });
})();
