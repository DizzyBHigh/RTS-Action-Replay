const RTSReplayWebSocket = window.RTSReplay;
let pendingMessageCompletion = null;

RTSReplayWebSocket.requestAction = (action,args={},id='rts-action-'+Date.now()) => {
  const socket=RTSReplayWebSocket.socket;
  if(!socket || socket.readyState!==WebSocket.OPEN) return false;
  socket.send(JSON.stringify({request:'DoAction',id,action,args}));
  return true;
};

RTSReplayWebSocket.acknowledgeMessage = queueId => {
  if (!queueId) return false;
  if (!RTSReplayWebSocket.socket || RTSReplayWebSocket.socket.readyState !== WebSocket.OPEN) {
    pendingMessageCompletion = queueId;
    return false;
  }
  pendingMessageCompletion = null;
  return RTSReplayWebSocket.requestAction(
    {name:'RTS - Action Replay - Core - Messaging'},
    {messageQueueId:queueId,messageComplete:'true'},
    'rts-message-complete-'+queueId
  );
};

RTSReplayWebSocket.setStatus = (text, state = '') => {
  RTSReplayWebSocket.status.textContent = text;
  RTSReplayWebSocket.status.className = state;
};

RTSReplayWebSocket.connect = () => {
  clearTimeout(RTSReplayWebSocket.reconnectTimer);
  const { host, port } = RTSReplayWebSocket.config;
  RTSReplayWebSocket.setStatus(`Connecting to Streamer.bot at ws://${host}:${port}/...`);
  RTSReplayWebSocket.socket = new WebSocket(`ws://${host}:${port}/`);

  RTSReplayWebSocket.socket.onopen = () => {
    RTSReplayWebSocket.socket.send(JSON.stringify({
      request: 'Subscribe',
      id: 'rts-action-replay',
      events: { Custom: ['Event'] }
    }));
    RTSReplayWebSocket.setStatus('Connected to Streamer.bot WebSocket', 'connected');
    if (pendingMessageCompletion) RTSReplayWebSocket.acknowledgeMessage(pendingMessageCompletion);
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