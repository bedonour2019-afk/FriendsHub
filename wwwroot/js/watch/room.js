(function () {
    let ytApiReady = false;
    let joinResult = null;
    let player = null;
    let isHost = false;
    let hostUsername = null;
    let suppressEvents = false;

    const hostBadge = document.getElementById("hostBadge");
    const viewerNote = document.getElementById("viewerNote");
    const playerOverlay = document.getElementById("playerOverlay");
    const changeVideoForm = document.getElementById("changeVideoForm");
    const participantsList = document.getElementById("participantsList");
    const participantsCount = document.getElementById("participantsCount");
    const partyMessages = document.getElementById("partyMessages");
    const partyChatForm = document.getElementById("partyChatForm");
    const partyChatInput = document.getElementById("partyChatInput");

    // -------- SignalR --------
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/watchPartyHub")
        .withAutomaticReconnect()
        .build();

    connection.on("Play", (time) => {
        if (!player) return;
        suppressEvents = true;
        player.seekTo(time, true);
        player.playVideo();
    });

    connection.on("Pause", (time) => {
        if (!player) return;
        suppressEvents = true;
        player.seekTo(time, true);
        player.pauseVideo();
    });

    connection.on("Seek", (time) => {
        if (!player) return;
        suppressEvents = true;
        player.seekTo(time, true);
    });

    connection.on("Heartbeat", (time) => {
        if (!player || typeof player.getCurrentTime !== "function") return;
        const diff = Math.abs(player.getCurrentTime() - time);
        if (diff > 2) {
            suppressEvents = true;
            player.seekTo(time, true);
        }
    });

    connection.on("VideoChanged", (videoId) => {
        if (player) player.loadVideoById(videoId);
    });

    connection.on("ParticipantsUpdated", renderParticipants);
    connection.on("PartyMessage", appendPartyMessage);

    connection.start()
        .then(() => connection.invoke("JoinRoom", roomId))
        .then(result => {
            if (result.error) {
                alert(result.error);
                window.location.href = "/Watch";
                return;
            }
            isHost = result.isHost;
            hostUsername = result.room.hostUsername;
            joinResult = result;

            renderParticipants(result.room.participants);

            if (isHost) {
                hostBadge.style.display = "block";
                changeVideoForm.style.display = "flex";
            } else {
                viewerNote.style.display = "block";
                playerOverlay.style.display = "block";
            }

            tryInitPlayer();
        })
        .catch(err => console.error(err));

    // -------- YouTube IFrame API --------
    window.onYouTubeIframeAPIReady = function () {
        ytApiReady = true;
        tryInitPlayer();
    };

    function tryInitPlayer() {
        if (!ytApiReady || !joinResult || player) return;

        const room = joinResult.room;
        player = new YT.Player("player", {
            height: "390",
            width: "100%",
            videoId: room.videoId,
            playerVars: { rel: 0 },
            events: {
                onReady: (e) => {
                    if (room.currentTime > 0) e.target.seekTo(room.currentTime, true);
                    if (room.isPlaying) e.target.playVideo(); else e.target.pauseVideo();
                },
                onStateChange: onPlayerStateChange
            }
        });
    }

    function onPlayerStateChange(event) {
        if (!isHost) return;

        if (suppressEvents) {
            suppressEvents = false;
            return;
        }

        const time = player.getCurrentTime();
        if (event.data === YT.PlayerState.PLAYING) {
            connection.invoke("PlayVideo", roomId, time).catch(err => console.error(err));
        } else if (event.data === YT.PlayerState.PAUSED) {
            connection.invoke("PauseVideo", roomId, time).catch(err => console.error(err));
        }
    }

    setInterval(() => {
        if (isHost && player && typeof player.getPlayerState === "function"
            && player.getPlayerState() === YT.PlayerState.PLAYING) {
            connection.invoke("Heartbeat", roomId, player.getCurrentTime()).catch(err => console.error(err));
        }
    }, 5000);

    // -------- تغيير الفيديو (المضيف بس) --------
    changeVideoForm.addEventListener("submit", (e) => {
        e.preventDefault();
        const input = document.getElementById("newVideoUrlInput");
        const url = input.value.trim();
        if (!url) return;
        connection.invoke("ChangeVideo", roomId, url).catch(err => console.error(err));
        input.value = "";
    });

    // -------- قائمة المشاهدين --------
    function renderParticipants(list) {
        participantsCount.textContent = list.length;
        participantsList.innerHTML = "";
        list.forEach(name => {
            const chip = document.createElement("div");
            chip.className = "participant-chip";
            chip.textContent = (name === hostUsername ? "👑 " : "👤 ") + name;
            participantsList.appendChild(chip);
        });
    }

    // -------- الشات الجانبي --------
    partyChatForm.addEventListener("submit", (e) => {
        e.preventDefault();
        const text = partyChatInput.value.trim();
        if (!text) return;
        connection.invoke("SendPartyMessage", roomId, text).catch(err => console.error(err));
        partyChatInput.value = "";
    });

    function appendPartyMessage(username, content, time) {
        const mine = username === currentUser ? "mine" : "";
        const div = document.createElement("div");
        div.className = "party-msg " + mine;

        const senderEl = document.createElement("strong");
        senderEl.textContent = username + ": ";

        const textEl = document.createElement("span");
        textEl.textContent = content;

        const timeEl = document.createElement("span");
        timeEl.className = "party-msg-time";
        timeEl.textContent = time;

        div.appendChild(senderEl);
        div.appendChild(textEl);
        div.appendChild(timeEl);

        partyMessages.appendChild(div);
        partyMessages.scrollTop = partyMessages.scrollHeight;
    }
})();
