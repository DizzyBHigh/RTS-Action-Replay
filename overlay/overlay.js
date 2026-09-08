const RTS_OVERLAY = {
  host: '127.0.0.1',
  port: 8080,
  eventName: 'RTS-Action Replay',
  confirmAction: 'RTS Action Replay - Playback Confirm',
  reconnectDelay: 3000,
  messageDuration: 5000
};

const status = document.getElementById('status');
const messageCard = document.getElementById('message-card');
const messageText = document.getElementById('message-text');
const brandLogo = document.getElementById('brand-logo');
const brandFallback = document.getElementById('brand-fallback');
const video = document.getElementById('video');
let rtsSocket;
let reconnectTimer;
let messageTimer;

function setStatus(text, state = '') {
  status.textContent = text;
  status.className = state;
}

function confirmPlayback(replayId, userId, userName) {
  if (!replayId || !rtsSocket || rtsSocket.readyState !== WebSocket.OPEN) return;
  rtsSocket.send(JSON.stringify({
    request: 'DoAction',
    id: `rts-replay-confirm-${Date.now()}`,
    action: { name: RTS_OVERLAY.confirmAction },
    args: { replayId, userId: userId || '', userName: userName || '' }
  }));
}

function showMessage(command) {
  const text = command.replayMessage || '';
  if (!text) return;

  messageText.textContent = text;
  const logoUrl = command.replayLogoUrl || '';
  if (logoUrl) {
    brandLogo.onload = () => {
      brandLogo.style.display = 'block';
      brandFallback.style.display = 'none';
    };
    brandLogo.onerror = () => {
      brandLogo.style.display = 'none';
      brandFallback.style.display = 'block';
    };
    brandLogo.src = logoUrl;
  } else {
    brandLogo.removeAttribute('src');
    brandLogo.style.display = 'none';
    brandFallback.style.display = 'block';
  }

  clearTimeout(messageTimer);
  messageCard.classList.remove('show');
  void messageCard.offsetWidth;
  messageCard.classList.add('show');
  messageCard.setAttribute('aria-hidden', 'false');
  messageTimer = setTimeout(() => {
    messageCard.classList.remove('show');
    messageCard.setAttribute('aria-hidden', 'true');
  }, RTS_OVERLAY.messageDuration);
}

function playReplay(command) {
  video.play().then(() => {
    confirmPlayback(command.replayId, command.replayUserId, command.replayUserName);
  }).catch(error => console.warn('Replay play failed', error));
}

function loadReplay(command) {
  if (!command.replayUrl) return;
  video.src = command.replayUrl;
  video.style.display = 'block';
  video.load();
  if (command.replayAutoplay) {
    video.addEventListener('canplay', () => playReplay(command), { once: true });
  }
}

function handleReplayCommand(command) {
  if (command.replayCommand === 'message') showMessage(command);
  if (command.replayCommand === 'load') loadReplay(command);
  if (command.replayCommand === 'play') playReplay(command);
  if (command.replayCommand === 'pause') video.pause();
  if (command.replayCommand === 'stop') { video.pause(); video.currentTime = 0; }
  if (command.replayCommand === 'replay') {
    video.currentTime = 0;
    playReplay(command);
  }
}

function handleEvent(message) {
  if (message?.event?.source !== 'Custom' || message?.event?.type !== 'Event') return;
  const data = message.data;
  if (data?.eventName !== RTS_OVERLAY.eventName || !data.args) return;
  handleReplayCommand(data.args);
}

function connect() {
  clearTimeout(reconnectTimer);
  setStatus(`Connecting to Streamer.bot at ws://${RTS_OVERLAY.host}:${RTS_OVERLAY.port}/…`);
  rtsSocket = new WebSocket(`ws://${RTS_OVERLAY.host}:${RTS_OVERLAY.port}/`);

  rtsSocket.onopen = () => {
    rtsSocket.send(JSON.stringify({
      request: 'Subscribe',
      id: 'rts-action-replay',
      events: { Custom: ['Event'] }
    }));
    setStatus('Connected to Streamer.bot WebSocket', 'connected');
  };

  rtsSocket.onmessage = event => {
    try {
      handleEvent(JSON.parse(event.data));
    } catch (error) {
      console.warn('Invalid WebSocket message', error);
    }
  };

  rtsSocket.onerror = () => setStatus('Streamer.bot WebSocket connection error', 'error');
  rtsSocket.onclose = event => {
    setStatus(`Streamer.bot WebSocket closed (code ${event.code})`, 'error');
    reconnectTimer = setTimeout(connect, RTS_OVERLAY.reconnectDelay);
  };

  window.rtsSocket = rtsSocket;
}

window.rtsOverlay = RTS_OVERLAY;
window.testReplay = url => loadReplay({ replayUrl: url, replayAutoplay: false });
window.testMessage = text => showMessage({ replayMessage: text });
connect();
