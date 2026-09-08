const RTSReplayVideo = window.RTSReplay;

RTSReplayVideo.confirmPlayback = (replayId, userId, userName) => {
  if (!replayId || !RTSReplayVideo.socket || RTSReplayVideo.socket.readyState !== WebSocket.OPEN) return;
  RTSReplayVideo.socket.send(JSON.stringify({ request: 'DoAction', id: `rts-replay-confirm-${Date.now()}`, action: { name: RTSReplayVideo.config.confirmAction }, args: { replayId, userId: userId || '', userName: userName || '' } }));
};

RTSReplayVideo.playReplay = command => {
  RTSReplayVideo.video.play().then(() => RTSReplayVideo.confirmPlayback(command.replayId, command.replayUserId, command.replayUserName)).catch(error => console.warn('Replay play failed', error));
};

RTSReplayVideo.loadReplay = command => {
  if (!command.replayUrl) return;
  RTSReplayVideo.currentCommand = command;
  RTSReplayVideo.player.classList.remove('preview');
  RTSReplayControls.configure(command);
  RTSReplaySkin.configure(command);
  const endPosition = command.replayEndPosition || 'Full Screen';
  const startPosition = command.replayStartPosition || endPosition;
  RTSReplayVideo.activePosition = RTSReplayVideo.getPosition(endPosition);
  RTSReplayVideo.video.src = command.replayUrl;
  RTSReplayVideo.video.style.display = 'block';
  RTSReplayVideo.video.playbackRate = Number(command.replayPlaybackSpeed) || 1;
  RTSReplayVideo.video.load();
  RTSReplayVideo.animateIn(RTSReplayVideo.getPosition(startPosition), RTSReplayVideo.getPosition(endPosition));
  if (command.replayAutoplay) RTSReplayVideo.video.addEventListener('canplay', () => RTSReplayVideo.playReplay(command), { once: true });
};

RTSReplayVideo.previewPosition = command => {
  RTSReplayVideo.currentCommand = command;
  RTSReplayControls.configure(command);
  RTSReplaySkin.configure(command);
  RTSReplayVideo.player.classList.add('preview', 'show');
  if (command.replayElementPreview === true) return;
  RTSReplayVideo.activePosition = RTSReplayVideo.getPosition(command.replayPosition || 'Full Screen');
  RTSReplayVideo.applyPosition(RTSReplayVideo.activePosition);
};

RTSReplayVideo.hidePreview = () => {
  RTSReplaySkin.clearSkin();
  RTSReplayVideo.player.classList.remove('preview', 'show');
};

RTSReplayVideo.moveReplay = command => {
  RTSReplayVideo.currentCommand = command;
  const position = RTSReplayVideo.getPosition(command.replayPosition || 'Full Screen');
  RTSReplayVideo.activePosition = position;
  RTSReplayVideo.applyPosition(position);
  RTSReplayVideo.player.classList.add('show');
};

RTSReplayVideo.handleReplayCommand = command => {
  if (command.replayCommand === 'message') RTSReplayVideo.showMessage(command);
  if (command.replayCommand === 'load') RTSReplayVideo.loadReplay(command);
  if (command.replayCommand === 'preview') RTSReplayVideo.previewPosition(command);
  if (command.replayCommand === 'previewHide') RTSReplayVideo.hidePreview();
  if (command.replayCommand === 'play') RTSReplayVideo.playReplay(command);
  if (command.replayCommand === 'pause') RTSReplayVideo.video.pause();
  if (command.replayCommand === 'move') RTSReplayVideo.moveReplay(command);
  if (command.replayCommand === 'hide') { RTSReplaySkin.hide(); RTSReplayVideo.animateOut(); }
  if (command.replayCommand === 'stop') { RTSReplaySkin.hide(); RTSReplayVideo.video.pause(); RTSReplayVideo.video.currentTime = 0; }
  if (command.replayCommand === 'replay') { RTSReplayVideo.video.currentTime = 0; RTSReplayVideo.playReplay(command); }
};