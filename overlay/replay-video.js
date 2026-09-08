const RTSReplayVideo = window.RTSReplay;

RTSReplayVideo.confirmPlayback = (replayId, userId, userName) => {
  if (!replayId || !RTSReplayVideo.socket || RTSReplayVideo.socket.readyState !== WebSocket.OPEN) return;
  RTSReplayVideo.socket.send(JSON.stringify({
    request: 'DoAction',
    id: `rts-replay-confirm-${Date.now()}`,
    action: { name: RTSReplayVideo.config.confirmAction },
    args: { replayId, userId: userId || '', userName: userName || '' }
  }));
};

RTSReplayVideo.playReplay = command => {
  RTSReplayVideo.video.play().then(() => {
    RTSReplayVideo.confirmPlayback(command.replayId, command.replayUserId, command.replayUserName);
  }).catch(error => console.warn('Replay play failed', error));
};

RTSReplayVideo.loadReplay = command => {
  if (!command.replayUrl) return;
  RTSReplayVideo.video.src = command.replayUrl;
  RTSReplayVideo.video.style.display = 'block';
  RTSReplayVideo.video.load();
  if (command.replayAutoplay) {
    RTSReplayVideo.video.addEventListener('canplay', () => RTSReplayVideo.playReplay(command), { once: true });
  }
};

RTSReplayVideo.handleReplayCommand = command => {
  if (command.replayCommand === 'message') RTSReplayVideo.showMessage(command);
  if (command.replayCommand === 'load') RTSReplayVideo.loadReplay(command);
  if (command.replayCommand === 'play') RTSReplayVideo.playReplay(command);
  if (command.replayCommand === 'pause') RTSReplayVideo.video.pause();
  if (command.replayCommand === 'stop') {
    RTSReplayVideo.video.pause();
    RTSReplayVideo.video.currentTime = 0;
  }
  if (command.replayCommand === 'replay') {
    RTSReplayVideo.video.currentTime = 0;
    RTSReplayVideo.playReplay(command);
  }
};
