// Private Chat JavaScript
document.addEventListener('DOMContentLoaded', function () {
    const messageInput = document.getElementById('messageInput');
    const sendForm = document.getElementById('sendForm');
    const messagesBox = document.getElementById('messagesBox');
    const imageBtn = document.getElementById('imageBtn');
    const imageInput = document.getElementById('imageInput');
    const micBtn = document.getElementById('micBtn');
    const recordingIndicator = document.getElementById('recordingIndicator');

    let mediaRecorder = null;
    let audioChunks = [];

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/privateChatHub")
        .withAutomaticReconnect()
        .build();

    connection.on("ReceivePrivateMessage", function (message) {
        addMessageToChat(message);
    });

    connection.on("MessagesRead", function (chatId) {
    });

    connection.start().catch(err => console.error(err));

    sendForm.addEventListener('submit', async function (e) {
        e.preventDefault();
        const content = messageInput.value.trim();
        if (!content) return;

        try {
            await connection.invoke("SendMessage", chatId, content, null, "text");
        } catch (err) {
            console.error('Send error:', err);
        }
        messageInput.value = '';
    });

    imageBtn.addEventListener('click', () => imageInput.click());

    imageInput.addEventListener('change', async function () {
        const file = this.files[0];
        if (!file) return;

        const formData = new FormData();
        formData.append('file', file);
        formData.append('type', 'image');

        try {
            const response = await fetch('/PrivateChat/UploadAttachment', {
                method: 'POST',
                body: formData
            });
            const data = await response.json();

            await connection.invoke("SendMessage", chatId, '', data.url, 'image');
        } catch (err) {
            console.error(err);
        }

        this.value = '';
    });

    micBtn.addEventListener('click', async function () {
        if (mediaRecorder && mediaRecorder.state === 'recording') {
            mediaRecorder.stop();
            micBtn.classList.remove('recording');
            recordingIndicator.style.display = 'none';
        } else {
            try {
                const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                mediaRecorder = new MediaRecorder(stream);
                audioChunks = [];

                mediaRecorder.ondataavailable = event => {
                    audioChunks.push(event.data);
                };

                mediaRecorder.onstop = async () => {
                    const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
                    const formData = new FormData();
                    formData.append('file', audioBlob, 'recording.webm');
                    formData.append('type', 'audio');

                    try {
                        const response = await fetch('/PrivateChat/UploadAttachment', {
                            method: 'POST',
                            body: formData
                        });
                        const data = await response.json();

                        await connection.invoke("SendMessage", chatId, '', data.url, 'audio');
                    } catch (err) {
                        console.error(err);
                    }

                    stream.getTracks().forEach(track => track.stop());
                };

                mediaRecorder.start();
                micBtn.classList.add('recording');
                recordingIndicator.style.display = 'block';
            } catch (err) {
                console.error('Microphone error:', err);
                alert('مشكلة في الميكروفون');
            }
        }
    });

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text || '';
        return div.innerHTML;
    }

    function addMessageToChat(message) {
        const sender = message.SenderUsername || message.senderUsername;
        const msgType = message.Type || message.type;
        const msgContent = message.Content || message.content;
        const msgFile = message.FilePath || message.filePath;
        const msgTime = message.SentAt || message.sentAt;

        const mine = sender === currentUser ? 'mine' : '';
        const bubble = document.createElement('div');
        bubble.className = `chat-bubble-row ${mine}`;

        let contentHtml = '';
        if (msgType === 'text') {
            contentHtml = `<div class="bubble-text">${escapeHtml(msgContent)}</div>`;
        } else if (msgType === 'image') {
            contentHtml = `<img class="bubble-image" src="${escapeHtml(msgFile)}" alt="Image" />`;
        } else if (msgType === 'audio') {
            contentHtml = `<audio class="bubble-audio" controls src="${escapeHtml(msgFile)}"></audio>`;
        }

        let timeDisplay = msgTime || '';
        try {
            const d = new Date(msgTime);
            if (!isNaN(d.getTime())) {
                timeDisplay = d.toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' });
            }
        } catch { }

        bubble.innerHTML = `
            <div class="chat-bubble ${mine}">
                ${contentHtml}
                <div class="bubble-time">${timeDisplay}</div>
            </div>
        `;

        messagesBox.appendChild(bubble);
        messagesBox.scrollTop = messagesBox.scrollHeight;

        if (!mine) {
            try {
                connection.invoke("MarkAsRead", chatId);
            } catch { }
        }
    }

    messagesBox.scrollTop = messagesBox.scrollHeight;
});
