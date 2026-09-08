(function () {
    const messagesBox = document.getElementById("messagesBox");
    const messageInput = document.getElementById("messageInput");
    const sendForm = document.getElementById("sendForm");
    const emojiBtn = document.getElementById("emojiBtn");
    const stickerBtn = document.getElementById("stickerBtn");
    const imageBtn = document.getElementById("imageBtn");
    const imageInput = document.getElementById("imageInput");
    const micBtn = document.getElementById("micBtn");
    const emojiPanel = document.getElementById("emojiPanel");
    const stickerPanel = document.getElementById("stickerPanel");
    const recordingIndicator = document.getElementById("recordingIndicator");

    const EMOJIS = ["😀","😂","😍","😎","😢","😡","👍","👎","🙏","🔥","🎉","❤️",
        "😅","😴","🤔","😱","👏","💪","🥳","😇","😭","🤣","😜","🙄",
        "😏","😳","👌","✌️","🤝","💔","🌹","⚽","🍕","☕","🌙","⭐"];

    const STICKERS = ["😂😂😂","🔥🔥🔥","👑","🎊🎉🎊","💯","😴💤","🤝✅","❤️‍🔥","🥇","👀","🙏🙏","😂🤙"];

    EMOJIS.forEach(e => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "picker-item";
        btn.textContent = e;
        btn.addEventListener("click", () => {
            messageInput.value += e;
            messageInput.focus();
        });
        emojiPanel.appendChild(btn);
    });

    STICKERS.forEach(s => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "picker-item sticker-item";
        btn.textContent = s;
        btn.addEventListener("click", () => {
            sendMessage("sticker", s);
            stickerPanel.classList.remove("open");
        });
        stickerPanel.appendChild(btn);
    });

    emojiBtn.addEventListener("click", () => {
        stickerPanel.classList.remove("open");
        emojiPanel.classList.toggle("open");
    });

    // تحميل المنشورات الحديثة
    async function loadRecentPosts() {
        try {
            const response = await fetch('/Posts/GetRecentPosts');
            const posts = await response.json();

            const recentPostsDiv = document.getElementById('recentPosts');
            if (!recentPostsDiv) return;

            if (posts.length === 0) {
                recentPostsDiv.innerHTML = '<p style="color: #a0a0a0; font-size: 12px; text-align: center;">مفيش منشورات حديثة</p>';
                return;
            }

            let html = '';
            posts.forEach(post => {
                const time = new Date(post.createdAt).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' });
                html += `
                    <div style="background: #30475e; padding: 8px; border-radius: 8px; margin-bottom: 8px; cursor: pointer;"
                         onclick="window.location.href='/Posts'">
                        <div style="font-size: 12px; color: #e0e0e0; font-weight: bold;">${post.username}</div>
                        <div style="font-size: 11px; color: #a0a0a0; margin-top: 4px;">${post.content || 'صورة'}</div>
                        <div style="font-size: 10px; color: #888; margin-top: 4px;">${time}</div>
                    </div>
                `;
            });
            recentPostsDiv.innerHTML = html;
        } catch (error) {
            console.error('Error loading recent posts:', error);
        }
    }

    // تحميل المنشورات عند التحميل
    loadRecentPosts();

    stickerBtn.addEventListener("click", () => {
        emojiPanel.classList.remove("open");
        stickerPanel.classList.toggle("open");
    });

    // -------- SignalR --------
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    function appendMessage(id, username, type, content, filePath, time) {
        const mine = username === currentUser ? "mine" : "";
        const canDelete = username === currentUser || isAdmin;

        const row = document.createElement("div");
        row.className = "chat-bubble-row " + mine;
        row.id = `msg-row-${id}`;

        const bubble = document.createElement("div");
        bubble.className = "chat-bubble " + mine + (type === "sticker" ? " sticker-bubble" : "");
        bubble.id = `msg-bubble-${id}`;

        const sender = document.createElement("div");
        sender.className = "bubble-sender";
        sender.textContent = username;
        bubble.appendChild(sender);

        if (type === "text") {
            const textEl = document.createElement("div");
            textEl.className = "bubble-text";
            textEl.textContent = content;
            bubble.appendChild(textEl);
        } else if (type === "sticker") {
            const stickerEl = document.createElement("div");
            stickerEl.className = "bubble-sticker";
            stickerEl.textContent = content;
            bubble.appendChild(stickerEl);
        } else if (type === "image") {
            const img = document.createElement("img");
            img.className = "bubble-image";
            img.src = filePath;
            bubble.appendChild(img);
        } else if (type === "audio") {
            const audio = document.createElement("audio");
            audio.className = "bubble-audio";
            audio.controls = true;
            audio.src = filePath;
            bubble.appendChild(audio);
        }

        const bottomRow = document.createElement("div");
        bottomRow.style.display = "flex";
        bottomRow.style.justifyContent = "space-between";
        bottomRow.style.alignItems = "center";
        bottomRow.style.marginTop = "2px";

        const timeEl = document.createElement("div");
        timeEl.className = "bubble-time";
        timeEl.textContent = time;
        bottomRow.appendChild(timeEl);

        const actions = document.createElement("div");
        actions.className = "chat-bubble-actions";

        // Reactions row
        const reactionsRow = document.createElement("div");
        reactionsRow.className = "msg-reactions-row";
        reactionsRow.id = `msg-reactions-${id}`;
        actions.appendChild(reactionsRow);

        // Palette wrapper
        const paletteWrapper = document.createElement("span");
        paletteWrapper.className = "reaction-btn-wrapper";
        paletteWrapper.style.position = "relative";
        paletteWrapper.style.display = "inline-block";

        const paletteBtn = document.createElement("button");
        paletteBtn.type = "button";
        paletteBtn.className = "msg-reaction-btn";
        paletteBtn.title = "تفاعل";
        paletteBtn.textContent = "➕";

        const palette = document.createElement("div");
        palette.className = "reactions-palette";
        palette.style.right = "0";

        ["like:👍", "love:❤️", "haha:😂", "sad:😢", "angry:😡"].forEach(item => {
            const [t, emoji] = item.split(":");
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "palette-item";
            btn.textContent = emoji;
            btn.onclick = () => window.reactMessage(id, t);
            palette.appendChild(btn);
        });

        paletteWrapper.appendChild(paletteBtn);
        paletteWrapper.appendChild(palette);
        actions.appendChild(paletteWrapper);

        if (canDelete) {
            const delBtn = document.createElement("button");
            delBtn.type = "button";
            delBtn.className = "btn-msg-delete";
            delBtn.title = "حذف الرسالة";
            delBtn.textContent = "🗑️";
            delBtn.onclick = () => window.deleteMessage(id);
            actions.appendChild(delBtn);
        }

        bottomRow.appendChild(actions);
        bubble.appendChild(bottomRow);

        row.appendChild(bubble);
        messagesBox.appendChild(row);
        messagesBox.scrollTop = messagesBox.scrollHeight;
    }

    // استقبال الرسائل
    connection.on("ReceiveMessage", function (id, username, type, content, filePath, time) {
        appendMessage(id, username, type, content, filePath, time);
    });

    // حذف الرسالة اللحظي
    connection.on("MessageDeleted", function (messageId) {
        const row = document.getElementById(`msg-row-${messageId}`);
        if (row) {
            row.style.opacity = "0";
            row.style.transition = "opacity 0.3s";
            setTimeout(() => row.remove(), 300);
        }
    });

    // تحديث ريأكت الرسالة اللحظي
    connection.on("MessageReactionUpdated", function (messageId, counts) {
        const row = document.getElementById(`msg-reactions-${messageId}`);
        if (!row) return;

        let html = '';
        let total = 0;
        const emojiMap = { like: '👍', love: '❤️', haha: '😂', sad: '😢', angry: '😡' };

        for (const [type, count] of Object.entries(counts)) {
            if (count > 0) {
                html += `<span>${emojiMap[type] || ''}</span>`;
                total += count;
            }
        }

        if (total > 0) {
            html += `<span>${total}</span>`;
        }
        row.innerHTML = html;
    });

    // دوال الحذف والريأكت العامة
    window.deleteMessage = function (id) {
        if (!confirm("هل أنت متأكد من حذف هذه الرسالة؟")) return;
        connection.invoke("DeleteMessage", id).catch(err => console.error(err));
    };

    window.reactMessage = function (id, reactionType) {
        connection.invoke("ToggleMessageReaction", id, reactionType).catch(err => console.error(err));
    };

    connection.start().then(function () {
        messagesBox.scrollTop = messagesBox.scrollHeight;
    }).catch(err => console.error(err));

    function sendMessage(type, content) {
        if (!content) return;
        connection.invoke("SendMessage", type, content).catch(err => console.error(err));
    }

    sendForm.addEventListener("submit", function (e) {
        e.preventDefault();
        const text = messageInput.value.trim();
        if (!text) return;
        sendMessage("text", text);
        messageInput.value = "";
        emojiPanel.classList.remove("open");
    });

    // -------- رفع صورة --------
    imageBtn.addEventListener("click", () => imageInput.click());

    imageInput.addEventListener("change", async () => {
        const file = imageInput.files[0];
        if (!file) return;

        const url = await uploadFile(file, "image");
        if (url) sendMessage("image", url);
        imageInput.value = "";
    });

    // -------- تسجيل صوتي --------
    let mediaRecorder = null;
    let audioChunks = [];
    let isRecording = false;

    micBtn.addEventListener("click", async () => {
        if (!isRecording) {
            try {
                const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                mediaRecorder = new MediaRecorder(stream);
                audioChunks = [];

                mediaRecorder.ondataavailable = e => audioChunks.push(e.data);
                mediaRecorder.onstop = async () => {
                    const blob = new Blob(audioChunks, { type: "audio/webm" });
                    const url = await uploadFile(blob, "audio", "voice.webm");
                    if (url) sendMessage("audio", url);
                    stream.getTracks().forEach(t => t.stop());
                };

                mediaRecorder.start();
                isRecording = true;
                micBtn.classList.add("recording");
                recordingIndicator.style.display = "block";
            } catch (err) {
                alert("محتاج تسمح للموقع يستخدم المايك عشان تسجل صوت");
            }
        } else {
            mediaRecorder.stop();
            isRecording = false;
            micBtn.classList.remove("recording");
            recordingIndicator.style.display = "none";
        }
    });

    async function uploadFile(fileOrBlob, type, fileName) {
        const formData = new FormData();
        formData.append("file", fileOrBlob, fileName || fileOrBlob.name);
        formData.append("type", type);

        try {
            const res = await fetch("/Chat/UploadAttachment", {
                method: "POST",
                body: formData
            });
            if (!res.ok) return null;
            const data = await res.json();
            return data.url;
        } catch (err) {
            console.error(err);
            return null;
        }
    }

    // ========================================================
    // مكالمات الصوت والفيديو (WebRTC Calling)
    // ========================================================
    let localStream = null;
    let peerConnection = null;
    let activeCallType = "video";
    let targetPeerUser = null;

    const rtcConfig = {
        iceServers: [{ urls: "stun:stun.l.google.com:19302" }]
    };

    const callModal = document.getElementById("callModal");
    const incomingCallModal = document.getElementById("incomingCallModal");
    const localVideo = document.getElementById("localVideo");
    const remoteVideo = document.getElementById("remoteVideo");
    const remoteUserLabel = document.getElementById("remoteUserLabel");
    const callStatusTitle = document.getElementById("callStatusTitle");

    const startAudioCallBtn = document.getElementById("startAudioCallBtn");
    const startVideoCallBtn = document.getElementById("startVideoCallBtn");
    const acceptCallBtn = document.getElementById("acceptCallBtn");
    const rejectCallBtn = document.getElementById("rejectCallBtn");
    const endCallBtn = document.getElementById("endCallBtn");
    const muteAudioBtn = document.getElementById("muteAudioBtn");
    const toggleVideoBtn = document.getElementById("toggleVideoBtn");

    startAudioCallBtn.addEventListener("click", () => initiateCall("audio"));
    startVideoCallBtn.addEventListener("click", () => initiateCall("video"));

    async function initiateCall(type) {
        activeCallType = type;
        callStatusTitle.textContent = type === "video" ? "📹 جاري الاتصال فيديو..." : "📞 جاري الاتصال صوتي...";
        remoteUserLabel.textContent = "في انتظار الرد...";

        try {
            localStream = await navigator.mediaDevices.getUserMedia({
                audio: true,
                video: type === "video"
            });
            localVideo.srcObject = localStream;
            callModal.classList.add("open");

            await connection.invoke("StartCall", type);
        } catch (err) {
            alert("تعذر الوصول إلى الكاميرا أو الميكروفون: " + err.message);
        }
    }

    // استقبال مكالمة واردة
    connection.on("IncomingCall", function (callerUsername, callType) {
        if (callerUsername === currentUser) return;
        targetPeerUser = callerUsername;
        activeCallType = callType;

        document.getElementById("incomingCallerName").textContent = callerUsername;
        document.getElementById("incomingCallTypeLabel").textContent = callType === "video" ? "مكالمة فيديو واردة 📹" : "مكالمة صوتية واردة 📞";
        incomingCallModal.classList.add("open");
    });

    acceptCallBtn.addEventListener("click", async () => {
        incomingCallModal.classList.remove("open");
        callStatusTitle.textContent = "📞 مكالمة جارية";
        remoteUserLabel.textContent = targetPeerUser;

        try {
            localStream = await navigator.mediaDevices.getUserMedia({
                audio: true,
                video: activeCallType === "video"
            });
            localVideo.srcObject = localStream;
            callModal.classList.add("open");

            createPeerConnection();
            localStream.getTracks().forEach(track => peerConnection.addTrack(track, localStream));

            await connection.invoke("AcceptCall", targetPeerUser);
        } catch (err) {
            alert("تعذر فتح الوسائط: " + err.message);
        }
    });

    rejectCallBtn.addEventListener("click", () => {
        incomingCallModal.classList.remove("open");
        if (targetPeerUser) {
            connection.invoke("RejectCall", targetPeerUser);
            targetPeerUser = null;
        }
    });

    connection.on("CallAccepted", async function (acceptorUsername) {
        targetPeerUser = acceptorUsername;
        callStatusTitle.textContent = "📞 مكالمة جارية";
        remoteUserLabel.textContent = acceptorUsername;

        createPeerConnection();
        localStream.getTracks().forEach(track => peerConnection.addTrack(track, localStream));

        // Create Offer
        const offer = await peerConnection.createOffer();
        await peerConnection.setLocalDescription(offer);

        connection.invoke("SendSignal", targetPeerUser, { sdp: offer });
    });

    connection.on("CallRejected", function (rejector) {
        alert(`${rejector} رفض المكالمة.`);
        endCurrentCall();
    });

    connection.on("ReceiveSignal", async function (senderUsername, data) {
        targetPeerUser = senderUsername;

        if (data.sdp) {
            if (!peerConnection) {
                createPeerConnection();
                if (localStream) {
                    localStream.getTracks().forEach(track => peerConnection.addTrack(track, localStream));
                }
            }

            await peerConnection.setRemoteDescription(new RTCSessionDescription(data.sdp));

            if (data.sdp.type === "offer") {
                const answer = await peerConnection.createAnswer();
                await peerConnection.setLocalDescription(answer);
                connection.invoke("SendSignal", targetPeerUser, { sdp: answer });
            }
        } else if (data.candidate) {
            if (peerConnection) {
                try {
                    await peerConnection.addIceCandidate(new RTCIceCandidate(data.candidate));
                } catch (e) {
                    console.error("Error adding candidate", e);
                }
            }
        }
    });

    function createPeerConnection() {
        peerConnection = new RTCPeerConnection(rtcConfig);

        peerConnection.onicecandidate = (event) => {
            if (event.candidate && targetPeerUser) {
                connection.invoke("SendSignal", targetPeerUser, { candidate: event.candidate });
            }
        };

        peerConnection.ontrack = (event) => {
            remoteVideo.srcObject = event.streams[0];
        };
    }

    endCallBtn.addEventListener("click", () => {
        if (targetPeerUser) {
            connection.invoke("EndCall", targetPeerUser);
        } else {
            connection.invoke("EndCall", "");
        }
        endCurrentCall();
    });

    connection.on("CallEnded", function (sender) {
        endCurrentCall();
    });

    function endCurrentCall() {
        if (peerConnection) {
            peerConnection.close();
            peerConnection = null;
        }
        if (localStream) {
            localStream.getTracks().forEach(track => track.stop());
            localStream = null;
        }
        callModal.classList.remove("open");
        incomingCallModal.classList.remove("open");
        targetPeerUser = null;
    }

    muteAudioBtn.addEventListener("click", () => {
        if (!localStream) return;
        const audioTrack = localStream.getAudioTracks()[0];
        if (audioTrack) {
            audioTrack.enabled = !audioTrack.enabled;
            muteAudioBtn.classList.toggle("active", !audioTrack.enabled);
            muteAudioBtn.textContent = audioTrack.enabled ? "🎤" : "🔇";
        }
    });

    toggleVideoBtn.addEventListener("click", () => {
        if (!localStream) return;
        const videoTrack = localStream.getVideoTracks()[0];
        if (videoTrack) {
            videoTrack.enabled = !videoTrack.enabled;
            toggleVideoBtn.classList.toggle("active", !videoTrack.enabled);
            toggleVideoBtn.textContent = videoTrack.enabled ? "📹" : "🚫";
        }
    });
})();
