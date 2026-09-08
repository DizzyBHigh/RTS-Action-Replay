const RTSReplayWebSocket = window.RTSReplay;

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
  };

  RTSReplayWebSocket.socket.onmessage = event => {
    try {
      RTSReplayWebSocket.handleEvent(JSON.parse(event.data));
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
