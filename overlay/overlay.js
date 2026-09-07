const RTS_OVERLAY = {
  host: '127.0.0.1',
  port: 8080,
  eventName: 'RTS-Action Replay',
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

function loadReplay(url) {
  if (!url) return;
  messageBox.textContent = `Replay: ${url}`;
  video.src = url;
  video.style.display = 'block';
  video.load();
}

function handleReplayCommand(command) {
  if (command.replayCommand === 'load') loadReplay(command.replayUrl);
  if (command.replayCommand === 'play') video.play().catch(error => console.warn('Replay play failed', error));
  if (command.replayCommand === 'pause') video.pause();
  if (command.replayCommand === 'stop') { video.pause(); video.currentTime = 0; }
  if (command.replayCommand === 'replay') {
    video.currentTime = 0;
    video.play().catch(() => {});
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
window.testReplay = loadReplay;
connect();
