const RTSReplay = window.RTSReplay;

RTSReplay.setStatus = (text, state = '') => {
  RTSReplay.status.textContent = text;
  RTSReplay.status.className = state;
};

RTSReplay.connect = () => {
  clearTimeout(RTSReplay.reconnectTimer);
  const { host, port } = RTSReplay.config;
  RTSReplay.setStatus(`Connecting to Streamer.bot at ws://${host}:${port}/…`);
  RTSReplay.socket = new WebSocket(`ws://${host}:${port}/`);

  RTSReplay.socket.onopen = () => {
    RTSReplay.socket.send(JSON.stringify({
      request: 'Subscribe',
      id: 'rts-action-replay',
      events: { Custom: ['Event'] }
    }));
    RTSReplay.setStatus('Connected to Streamer.bot WebSocket', 'connected');
  };

  RTSReplay.socket.onmessage = event => {
    try {
      RTSReplay.handleEvent(JSON.parse(event.data));
    } catch (error) {
      console.warn('Invalid WebSocket message', error);
    }
  };

  RTSReplay.socket.onerror = () => RTSReplay.setStatus('Streamer.bot WebSocket connection error', 'error');
  RTSReplay.socket.onclose = event => {
    RTSReplay.setStatus(`Streamer.bot WebSocket closed (code ${event.code})`, 'error');
    RTSReplay.reconnectTimer = setTimeout(RTSReplay.connect, RTSReplay.config.reconnectDelay);
  };

  window.rtsSocket = RTSReplay.socket;
};
