/**
 * Contoso Estimator — Chat UI Client
 *
 * Connects to the ASP.NET Core backend which proxies requests to Foundry Agent Service.
 * Uses Server-Sent Events (SSE) for real-time streaming of agent responses.
 */

// State
let currentConversationId = null;
let lastResponseId = null;
let isStreaming = false;
const conversations = [];

// DOM refs
const messagesEl    = document.getElementById("messages");
const chatForm      = document.getElementById("chat-form");
const userInput     = document.getElementById("user-input");
const sendBtn       = document.getElementById("send-btn");
const newChatBtn    = document.getElementById("new-chat-btn");
const chatHistoryEl = document.getElementById("chat-history");
const statusBadge   = document.getElementById("status-badge");

// Initialise
checkHealth();

chatForm.addEventListener("submit", (e) => { e.preventDefault(); sendMessage(); });

userInput.addEventListener("input", () => {
  autoResize(userInput);
  sendBtn.disabled = userInput.value.trim() === "" || isStreaming;
});

userInput.addEventListener("keydown", (e) => {
  if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); sendMessage(); }
});

newChatBtn.addEventListener("click", startNewChat);

document.addEventListener("click", (e) => {
  if (e.target.classList.contains("suggestion")) {
    userInput.value = e.target.dataset.prompt;
    sendBtn.disabled = false;
    sendMessage();
  }
});

// Health check
async function checkHealth() {
  try {
    const res = await fetch("/api/health");
    const data = await res.json();
    if (data.project_endpoint_configured) {
      setStatus("Ready", "connected");
    } else {
      setStatus("Not configured", "error");
    }
  } catch {
    setStatus("Offline", "error");
  }
}

function setStatus(text, state) {
  statusBadge.textContent = text;
  statusBadge.className = "status-badge " + (state || "");
}

// Chat management
function startNewChat() {
  currentConversationId = null;
  renderMessages([]);
  renderChatHistory();
}

// Send message & stream response via SSE
async function sendMessage() {
  const text = userInput.value.trim();
  if (!text || isStreaming) return;

  const welcome = messagesEl.querySelector(".welcome-message");
  if (welcome) welcome.remove();

  appendMessage("user", text);
  userInput.value = "";
  autoResize(userInput);
  sendBtn.disabled = true;
  isStreaming = true;

  const agentMsgEl = appendMessage("agent", null, true);

  try {
    const response = await fetch("/api/chat", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        message: text,
        conversationId: currentConversationId,
      }),
    });

    if (!response.ok) {
      const err = await response.json().catch(() => ({}));
      throw new Error(err.detail || `HTTP ${response.status}`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = "";
    let fullText = "";

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split("\n");
      buffer = lines.pop();

      let currentEvent = null;
      for (const line of lines) {
        if (line.startsWith("event:")) {
          currentEvent = line.slice(6).trim();
        } else if (line.startsWith("data:") && currentEvent) {
          const rawData = line.slice(5).trim();
          handleSSEEvent(currentEvent, rawData, agentMsgEl, (t) => { fullText += t; });
          currentEvent = null;
        } else if (line === "") {
          currentEvent = null;
        }
      }
    }

    removeTypingIndicator(agentMsgEl);
    renderMarkdown(agentMsgEl.querySelector(".message-content"), fullText);
    trackConversation(text, fullText);

  } catch (err) {
    removeTypingIndicator(agentMsgEl);
    agentMsgEl.querySelector(".message-content").textContent = `⚠️ Error: ${err.message}`;
  } finally {
    isStreaming = false;
    sendBtn.disabled = userInput.value.trim() === "";
  }
}

// SSE event handler
function handleSSEEvent(event, rawData, agentMsgEl, onDelta) {
  let data;
  try { data = JSON.parse(rawData); } catch { return; }

  switch (event) {
    case "conversation_id":
      currentConversationId = data.conversation_id;
      break;

    case "delta":
      removeTypingIndicator(agentMsgEl);
      onDelta(data.text);
      const content = agentMsgEl.querySelector(".message-content");
      content.textContent += data.text;
      scrollToBottom();
      break;

    case "response_id":
      lastResponseId = data.response_id;
      break;

    case "error":
      removeTypingIndicator(agentMsgEl);
      agentMsgEl.querySelector(".message-content").textContent = `⚠️ ${data.error}`;
      break;

    case "done":
      break;
  }
}

// DOM helpers
function appendMessage(role, text, typing = false) {
  const wrapper = document.createElement("div");
  wrapper.className = `message ${role}`;

  const avatar = document.createElement("div");
  avatar.className = "message-avatar";
  avatar.textContent = role === "user" ? "You" : "AI";

  const content = document.createElement("div");
  content.className = "message-content";

  if (typing) {
    content.innerHTML = `<div class="typing-indicator"><span></span><span></span><span></span></div>`;
  } else if (text) {
    if (role === "agent") { renderMarkdown(content, text); }
    else { content.textContent = text; }
  }

  wrapper.appendChild(avatar);
  wrapper.appendChild(content);
  messagesEl.appendChild(wrapper);
  scrollToBottom();
  return wrapper;
}

function removeTypingIndicator(el) {
  const indicator = el.querySelector(".typing-indicator");
  if (indicator) indicator.remove();
}

function scrollToBottom() { messagesEl.scrollTop = messagesEl.scrollHeight; }

function autoResize(textarea) {
  textarea.style.height = "auto";
  textarea.style.height = Math.min(textarea.scrollHeight, 150) + "px";
}

// Simple Markdown renderer
function renderMarkdown(el, text) {
  if (!text) { el.innerHTML = ""; return; }

  let html = escapeHtml(text);

  // Code blocks
  html = html.replace(/```(\w*)\n([\s\S]*?)```/g, (_, lang, code) =>
    `<pre><code>${code.trim()}</code></pre>`);

  // Inline code
  html = html.replace(/`([^`]+)`/g, "<code>$1</code>");

  // Bold & italic
  html = html.replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>");
  html = html.replace(/\*(.+?)\*/g, "<em>$1</em>");

  // Headings
  html = html.replace(/^### (.+)$/gm, "<h4>$1</h4>");
  html = html.replace(/^## (.+)$/gm, "<h3>$1</h3>");
  html = html.replace(/^# (.+)$/gm, "<h2>$1</h2>");

  // Tables
  html = html.replace(
    /^(\|.+\|)\n(\|[-| :]+\|)\n((?:\|.+\|\n?)+)/gm,
    (_, header, sep, body) => {
      const ths = header.split("|").filter(Boolean).map(c => `<th>${c.trim()}</th>`).join("");
      const rows = body.trim().split("\n").map(row => {
        const tds = row.split("|").filter(Boolean).map(c => `<td>${c.trim()}</td>`).join("");
        return `<tr>${tds}</tr>`;
      }).join("");
      return `<table><thead><tr>${ths}</tr></thead><tbody>${rows}</tbody></table>`;
    }
  );

  // Lists
  html = html.replace(/^[•\-\*] (.+)$/gm, "<li>$1</li>");
  html = html.replace(/(<li>.*<\/li>\n?)+/g, (match) => `<ul>${match}</ul>`);

  // Paragraphs
  html = html.replace(/\n{2,}/g, "</p><p>");
  html = `<p>${html}</p>`;

  html = html.replace(/<p>\s*(<(?:pre|table|ul|h[2-4]))/g, "$1");
  html = html.replace(/(<\/(?:pre|table|ul|h[2-4])>)\s*<\/p>/g, "$1");

  el.innerHTML = html;
}

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

// Conversation tracking (in-memory)
function trackConversation(userText, agentText) {
  let conv = conversations.find(c => c.id === currentConversationId);
  if (!conv) {
    conv = {
      id: currentConversationId,
      title: userText.slice(0, 40) + (userText.length > 40 ? "…" : ""),
      messages: [],
    };
    conversations.unshift(conv);
  }
  conv.messages.push({ role: "user", text: userText });
  conv.messages.push({ role: "agent", text: agentText });
  renderChatHistory();
}

function renderChatHistory() {
  chatHistoryEl.innerHTML = "";
  for (const conv of conversations) {
    const btn = document.createElement("button");
    btn.className = "chat-history-item" + (conv.id === currentConversationId ? " active" : "");
    btn.textContent = conv.title;
    btn.addEventListener("click", () => loadConversation(conv));
    chatHistoryEl.appendChild(btn);
  }
}

function loadConversation(conv) {
  currentConversationId = conv.id;
  renderMessages(conv.messages);
  renderChatHistory();
}

function renderMessages(messages) {
  messagesEl.innerHTML = "";
  if (messages.length === 0) {
    messagesEl.innerHTML = `
      <div class="welcome-message">
        <div class="welcome-icon">📐</div>
        <h2>Welcome to Contoso Estimator</h2>
        <p>I can help you with cost estimation, rate lookups, policy questions, and project calculations.</p>
        <div class="suggestions">
          <button class="suggestion" data-prompt="What is the concrete supply rate for 32MPa in NSW?">🏗️ Concrete rates NSW</button>
          <button class="suggestion" data-prompt="Calculate cost for 5000m³ of 40MPa concrete in VIC">💰 Cost calculation</button>
          <button class="suggestion" data-prompt="What is the approval threshold for tenders between $20M and $50M?">📋 Tender approvals</button>
          <button class="suggestion" data-prompt="What contingency percentage applies to projects over $100M?">📊 Contingency rates</button>
        </div>
      </div>`;
    return;
  }
  for (const msg of messages) {
    appendMessage(msg.role, msg.text);
  }
}
