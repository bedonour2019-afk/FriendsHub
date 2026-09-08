(function () {
    let ytApiReady = false;
    let joinResult = null;
    let player = null;
    let isHost = false;
    let hostUsername = null;
    let suppressEvents = false;

    const endSessionBtn = document.getElementById("endSessionBtn");
    const leaveRoomBtn = document.getElementById("leaveRoomBtn");
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
        if (diff > 3) {
            suppressEvents = true;
            player.seekTo(time, true);
        }
    });

    // استلام تغيير الفيديو
    connection.on("VideoChanged", (videoId) => {
        if (player && typeof player.loadVideoById === "function") {
            suppressEvents = true;
            player.loadVideoById({
                videoId: videoId,
                startSeconds: 0
            });
            player.playVideo();
        }
    });

    // إغلاق الجلسة
    connection.on("RoomEnded", (msg) => {
        alert(msg || "تم إنهاء الجلسة.");
        window.location.href = "/Watch";
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

            if (isHost || isAdmin) {
                endSessionBtn.style.display = "inline-block";
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
            height: "420",
            width: "100%",
            videoId: room.videoId,
            playerVars: {
                rel: 0,
                autoplay: 1,
                modestbranding: 1
            },
            events: {
                onReady: (e) => {
                    if (room.currentTime > 0) e.target.seekTo(room.currentTime, true);
                    if (room.isPlaying) e.target.playVideo(); else e.target.pauseVideo();
                },
                onStateChange: onPlayerStateChange
            }
        });
    }

    // التحكم متاح للجميع
    function onPlayerStateChange(event) {
        if (suppressEvents) {
            suppressEvents = false;
            return;
        }

        if (!player || typeof player.getCurrentTime !== "function") return;
        const time = player.getCurrentTime();

        if (event.data === YT.PlayerState.PLAYING) {
            connection.invoke("PlayVideo", roomId, time).catch(err => console.error(err));
        } else if (event.data === YT.PlayerState.PAUSED) {
            connection.invoke("PauseVideo", roomId, time).catch(err => console.error(err));
        }
    }

    // إرسال نبضات المزامنة الدورية
    setInterval(() => {
        if (player && typeof player.getPlayerState === "function"
            && player.getPlayerState() === YT.PlayerState.PLAYING) {
            connection.invoke("Heartbeat", roomId, player.getCurrentTime()).catch(err => console.error(err));
        }
    }, 5000);

    // -------- تغيير الفيديو (متاح للجميع) --------
    changeVideoForm.addEventListener("submit", (e) => {
        e.preventDefault();
        const input = document.getElementById("newVideoUrlInput");
        const url = input.value.trim();
        if (!url) return;

        connection.invoke("ChangeVideo", roomId, url)
            .then(() => {
                input.value = "";
            })
            .catch(err => alert("حدث خطأ أثناء تغيير الفيديو: " + err));
    });

    // -------- إنهاء الجلسة --------
    endSessionBtn.addEventListener("click", () => {
        if (confirm("هل أنت متأكد من إنهاء جلسة المشاهدة وإغلاق الغرفة للجميع؟")) {
            connection.invoke("EndSession", roomId).catch(err => console.error(err));
        }
    });

    // -------- مغادرة الغرفة --------
    leaveRoomBtn.addEventListener("click", () => {
        connection.invoke("LeaveRoom", roomId)
            .then(() => {
                window.location.href = "/Watch";
            })
            .catch(() => {
                window.location.href = "/Watch";
            });
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
