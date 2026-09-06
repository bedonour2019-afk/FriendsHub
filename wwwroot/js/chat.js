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

    stickerBtn.addEventListener("click", () => {
        emojiPanel.classList.remove("open");
        stickerPanel.classList.toggle("open");
    });

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    function appendMessage(username, type, content, filePath, time) {
        const mine = username === currentUser ? "mine" : "";

        const row = document.createElement("div");
        row.className = "chat-bubble-row " + mine;

        const bubble = document.createElement("div");
        bubble.className = "chat-bubble " + mine + (type === "sticker" ? " sticker-bubble" : "");

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

        const timeEl = document.createElement("div");
        timeEl.className = "bubble-time";
        timeEl.textContent = time;
        bubble.appendChild(timeEl);

        row.appendChild(bubble);
        messagesBox.appendChild(row);
        messagesBox.scrollTop = messagesBox.scrollHeight;
    }

    connection.on("ReceiveMessage", function (username, type, content, filePath, time) {
        appendMessage(username, type, content, filePath, time);
    });

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
})();
