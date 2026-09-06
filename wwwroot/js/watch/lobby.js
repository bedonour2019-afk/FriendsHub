(function () {
    const form = document.getElementById("createRoomForm");
    const videoUrlInput = document.getElementById("videoUrlInput");
    const errorBox = document.getElementById("createRoomError");
    const activeRoomsEl = document.getElementById("activeRooms");

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/watchPartyHub")
        .withAutomaticReconnect()
        .build();

    function renderRooms(rooms) {
        if (!rooms || rooms.length === 0) {
            activeRoomsEl.innerHTML = `<p class="empty-msg">مفيش غرف مشاهدة شغالة دلوقتي</p>`;
            return;
        }

        activeRoomsEl.innerHTML = "";
        rooms.forEach(r => {
            const item = document.createElement("div");
            item.className = "challenge-item";
            item.innerHTML = `
                <span>🎬 غرفة <strong>${r.hostUsername}</strong> (👀 ${r.participantsCount})</span>
                <button class="btn-primary join-room-btn" data-room="${r.roomId}">انضم للمشاهدة</button>
            `;
            activeRoomsEl.appendChild(item);
        });

        document.querySelectorAll(".join-room-btn").forEach(btn => {
            btn.addEventListener("click", () => {
                window.location.href = "/Watch/Room/" + btn.dataset.room;
            });
        });
    }

    connection.on("LobbyUpdated", renderRooms);

    connection.start()
        .then(() => connection.invoke("GetActiveRooms"))
        .then(renderRooms)
        .catch(err => console.error(err));

    form.addEventListener("submit", (e) => {
        e.preventDefault();
        errorBox.style.display = "none";

        const url = videoUrlInput.value.trim();
        if (!url) return;

        connection.invoke("CreateRoom", url)
            .then(result => {
                if (result.error) {
                    errorBox.textContent = result.error;
                    errorBox.style.display = "block";
                    return;
                }
                window.location.href = "/Watch/Room/" + result.roomId;
            })
            .catch(err => console.error(err));
    });
})();
