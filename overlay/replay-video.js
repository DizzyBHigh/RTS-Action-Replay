const RTSReplayVideo = window.RTSReplay;

RTSReplayVideo.confirmPlayback = (replayId, userId, userName) => {
  if (!replayId || !RTSReplayVideo.socket || RTSReplayVideo.socket.readyState !== WebSocket.OPEN) return;
  RTSReplayVideo.socket.send(JSON.stringify({
    request: 'DoAction', id: `rts-replay-confirm-${Date.now()}`,
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

  RTSReplayVideo.currentCommand = command;
  window.RTSDevToolbar?.updateClapper?.(command);
  RTSReplayControls.configure(command);
  RTSReplayElements.configure(command);

  const startName = command.replayStartPosition || command.replayPosition || 'Full Screen';
  const endName = command.replayEndPosition || startName;
  const startPosition = RTSReplayVideo.getPosition(startName);
  const endPosition = RTSReplayVideo.getPosition(endName);

  RTSReplayVideo.video.src = command.replayUrl;
  RTSReplayVideo.video.style.display = 'block';
  RTSReplayVideo.video.load();

  // Every LOAD is a new animation cycle. Do not reuse the previous visible
  // position here: doing so caused a profile configured as Hidden -> Mini to
  // appear at Mini and then animate back to Hidden when the clip ended.
  // Reset the player to the configured start position while hidden, then show
  // it and animate from start -> end.
  RTSReplayVideo.cancelPendingTransition?.();
  RTSReplayVideo.player.classList.remove('show');
  RTSReplayVideo.applyPosition(startPosition, true);
  void RTSReplayVideo.player.offsetWidth;
  RTSReplayVideo.player.classList.add('show');
  RTSReplayVideo.activePosition = startPosition;

  if (RTSReplayVideo.positionsEqual?.(startPosition, endPosition)) {
    RTSReplayVideo.applyPosition(endPosition, true);
    RTSReplayVideo.activePosition = endPosition;
  } else {
    requestAnimationFrame(() => {
      RTSReplayVideo.configureTransition();
      requestAnimationFrame(() => {
        RTSReplayVideo.player.style.transform = RTSReplayVideo.transformFor(endPosition);
        RTSReplayVideo.activePosition = endPosition;
      });
    });
  }

  if (command.replayAutoplay) {
    RTSReplayVideo.video.addEventListener('canplay', () => RTSReplayVideo.playReplay(command), { once: true });
  }
};

RTSReplayVideo.moveReplay = command => {
  RTSReplayVideo.currentCommand = command;
  const position = RTSReplayVideo.getPosition(command.replayPosition || 'Full Screen');
  const current = RTSReplayVideo.activePosition;

  if (current && RTSReplayVideo.positionsEqual?.(current, position)) return;

  RTSReplayVideo.activePosition = position;
  RTSReplayVideo.applyPosition(position);
  RTSReplayVideo.player.classList.add('show');
};

RTSReplayVideo.handleReplayCommand = command => {
  if (command.replayCommand === 'message') RTSReplayVideo.showMessage(command);
  if (command.replayCommand === 'load') RTSReplayVideo.loadReplay(command);
  if (command.replayCommand === 'play') RTSReplayVideo.playReplay(command);
  if (command.replayCommand === 'pause') RTSReplayVideo.video.pause();
  if (command.replayCommand === 'move') RTSReplayVideo.moveReplay(command);
  if (command.replayCommand === 'hide') RTSReplayVideo.animateOut();
  if (command.replayCommand === 'stop') {
    RTSReplayVideo.video.pause();
    RTSReplayVideo.video.currentTime = 0;
  }
  if (command.replayCommand === 'replay') {
    RTSReplayVideo.video.currentTime = 0;
    RTSReplayVideo.playReplay(command);
  }
};

RTSReplayVideo.video.addEventListener('ended', () => {
  if (!RTSReplayVideo.currentCommand) return;
  RTSReplayVideo.animateOut();
});
