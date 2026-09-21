const RTSReplayWebSocket = window.RTSReplay;
let pendingMessageCompletion = null;\nlet pendingClapperboardCompletion = null;

RTSReplayWebSocket.acknowledgeMessage = queueId => {
  if (!queueId) return false;
  if (!RTSReplayWebSocket.socket || RTSReplayWebSocket.socket.readyState !== WebSocket.OPEN) {
    pendingMessageCompletion = queueId;
    return false;
  }
  pendingMessageCompletion = null;
  RTSReplayWebSocket.socket.send(JSON.stringify({
    request: 'DoAction',
    id: 'rts-message-complete-' + queueId,
    action: { name: 'RTS - Action Replay - Core - Messaging' },
    args: { messageQueueId: queueId, messageComplete: 'true' }
  }));
  return true;
};

RTSReplayWebSocket.acknowledgeClapperboard = replayId => {
  if (!replayId) return false;
  if (!RTSReplayWebSocket.socket || RTSReplayWebSocket.socket.readyState !== WebSocket.OPEN) return false;
  RTSReplayWebSocket.socket.send(JSON.stringify({
    request: "DoAction",
    id: "rts-clapper-complete-" + replayId,
    action: { name: "RTS - Action Replay - Core - Playlist" },
    args: { replayId, clapperboardComplete: "true" }
  }));
  return true;
};

RTSReplayWebSocket.setStatus = (text, state = '') => {
  RTSReplayWebSocket.status.textContent = text;
  RTSReplayWebSocket.status.className = state;
};

RTSReplayWebSocket.connect = () => {
  clearTimeout(RTSReplayWebSocket.reconnectTimer);
  const { host, port } = RTSReplayWebSocket.config;
  RTSReplayWebSocket.setStatus(`Connecting to Streamer.bot at ws://${host}:${port}/…`);
  RTSReplayWebSocket.socket = new WebSocket(`ws://${host}:${port}/`);

  RTSReplayWebSocket.socket.onopen = () => {
    RTSReplayWebSocket.socket.send(JSON.stringify({
      request: 'Subscribe',
      id: 'rts-action-replay',
      events: { Custom: ['Event'] }
    }));
    RTSReplayWebSocket.setStatus('Connected to Streamer.bot WebSocket', 'connected');
    if (pendingMessageCompletion) RTSReplayWebSocket.acknowledgeMessage(pendingMessageCompletion);\n    if (pendingClapperboardCompletion) RTSReplayWebSocket.acknowledgeClapperboard(pendingClapperboardCompletion);
  };

  RTSReplayWebSocket.socket.onmessage = event => {
    try {
      const message = JSON.parse(event.data);
      RTSReplayWebSocket.handleEvent(message);
    } catch (error) {
      console.warn('Invalid WebSocket message', error);
    }
  };

  RTSReplayWebSocket.socket.onerror = () => RTSReplayWebSocket.setStatus('Streamer.bot WebSocket connection error', 'error');
  RTSReplayWebSocket.socket.onclose = event => {
    RTSReplayWebSocket.setStatus(`Streamer.bot WebSocket closed (code ${event.code})`, 'error');
    RTSReplayWebSocket.reconnectTimer = setTimeout(RTSReplayWebSocket.connect, RTSReplayWebSocket.config.reconnectDelay);
  };

  window.rtsSocket = RTSReplayWebSocket.socket;
};
