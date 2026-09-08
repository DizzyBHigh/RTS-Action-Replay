const RTSReplay = window.RTSReplay;

RTSReplay.confirmPlayback = (replayId, userId, userName) => {
  if (!replayId || !RTSReplay.socket || RTSReplay.socket.readyState !== WebSocket.OPEN) return;
  RTSReplay.socket.send(JSON.stringify({
    request: 'DoAction',
    id: `rts-replay-confirm-${Date.now()}`,
    action: { name: RTSReplay.config.confirmAction },
    args: { replayId, userId: userId || '', userName: userName || '' }
  }));
};

RTSReplay.playReplay = command => {
  RTSReplay.video.play().then(() => {
    RTSReplay.confirmPlayback(command.replayId, command.replayUserId, command.replayUserName);
  }).catch(error => console.warn('Replay play failed', error));
};

RTSReplay.loadReplay = command => {
  if (!command.replayUrl) return;
  RTSReplay.video.src = command.replayUrl;
  RTSReplay.video.style.display = 'block';
  RTSReplay.video.load();
  if (command.replayAutoplay) {
    RTSReplay.video.addEventListener('canplay', () => RTSReplay.playReplay(command), { once: true });
  }
};

RTSReplay.handleReplayCommand = command => {
  if (command.replayCommand === 'message') RTSReplay.showMessage(command);
  if (command.replayCommand === 'load') RTSReplay.loadReplay(command);
  if (command.replayCommand === 'play') RTSReplay.playReplay(command);
  if (command.replayCommand === 'pause') RTSReplay.video.pause();
  if (command.replayCommand === 'stop') {
    RTSReplay.video.pause();
    RTSReplay.video.currentTime = 0;
  }
  if (command.replayCommand === 'replay') {
    RTSReplay.video.currentTime = 0;
    RTSReplay.playReplay(command);
  }
};
