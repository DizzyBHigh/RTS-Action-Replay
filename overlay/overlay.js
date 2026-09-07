const status = document.getElementById('status');
const messageBox = document.getElementById('message');
const video = document.getElementById('video');
const host = '127.0.0.1';
const port = 8080;
let reconnectTimer;

function setStatus(text, state = '') {
  status.textContent = text;
  status.className = state;
}

function loadReplay(url) {
  if (!url) return;
  setStatus(`Loading replay: ${url}`);
  video.src = url;
  video.style.display = 'block';
  video.load();
}

function handleReplayCommand(command) {
  if (command.replayCommand === 'load') loadReplay(command.replayUrl);
  if (command.replayCommand === 'play') video.play().catch(error => console.warn('Replay play failed', error));
  if (command.replayCommand === 'pause') video.pause();
  if (command.replayCommand === 'stop') { video.pause(); video.currentTime = 0; }
  if (command.replayCommand === 'replay') { video.currentTime = 0; video.play().catch(() => {}); }
}

function connect() {
  clearTimeout(reconnectTimer);
  setStatus(`Connecting to Streamer.bot at ws://${host}:${port}/…`);

  const ws = new WebSocket(`ws://${host}:${port}/`);

  ws.onopen = () => {
    ws.send(JSON.stringify({
      request: 'Subscribe',
      id: 'rts-action-replay-poc',
      events: { Custom: ['Event'] }
    }));
    setStatus('Connected to Streamer.bot WebSocket', 'connected');
  };

  ws.onmessage = event => {
    messageBox.textContent = `Last WebSocket message: ${event.data}`;
    try {
      const message = JSON.parse(event.data);
      console.log('Streamer.bot message', message);
      const data = message.data;
      if (message.event?.source === 'Custom' &&
          message.event?.type === 'Event' &&
          data?.eventName === 'RTS-Action Replay' &&
          data.args) {
        handleReplayCommand(data.args);
      }
    } catch (error) {
      console.warn('Invalid WebSocket message', error);
    }
  };

  ws.onerror = () => setStatus('Streamer.bot WebSocket connection error', 'error');

  ws.onclose = event => {
    setStatus(`Streamer.bot WebSocket closed (code ${event.code})`, 'error');
    reconnectTimer = setTimeout(connect, 3000);
  };

  window.ws = ws;
}

window.testReplay = loadReplay;
connect();
