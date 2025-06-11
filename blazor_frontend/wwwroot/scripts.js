// blazor_frontend/wwwroot/scripts.js
window.scrollChatToBottom = function () {
    const container = document.getElementById("chat-container");
    if (container) {
        container.scrollTop = container.scrollHeight;
    }
};