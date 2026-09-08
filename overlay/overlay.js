const RTS_OVERLAY = {
  host: '127.0.0.1',
  port: 8080,
  eventName: 'RTS-Action Replay',
  confirmAction: 'RTS Action Replay - Playback Confirm',
  reconnectDelay: 3000
};

const status = document.getElementById('status');
const messageBox = document.getElementById('message');
const video = document.getElementById('video');
let rtsSocket;
let reconnectTimer;

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

function playReplay(command) {
  video.play().then(() => {
    confirmPlayback(command.replayId, command.replayUserId, command.replayUserName);
  }).catch(error => console.warn('Replay play failed', error));
}

function loadReplay(command) {
  if (!command.replayUrl) return;
  messageBox.textContent = `Replay: ${command.replayUrl}`;
  video.src = command.replayUrl;
  video.style.display = 'block';
  video.load();
  if (command.replayAutoplay) {
    video.addEventListener('canplay', () => playReplay(command), { once: true });
  }
}

function handleReplayCommand(command) {
  if (command.replayCommand === 'message') {
    messageBox.textContent = command.replayMessage || '';
    return;
  }
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
  messageBox.textContent = `Command: ${data.args.replayCommand || 'unknown'}`;
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
connect();
