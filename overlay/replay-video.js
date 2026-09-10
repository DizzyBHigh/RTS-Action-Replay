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
  RTSDevToolbar?.updateClapper?.(command);
  RTSReplayControls.configure(command);
  RTSReplayElements.configure(command);

  const startName = command.replayStartPosition || command.replayPosition || 'Full Screen';
  const endName = command.replayEndPosition || startName;
  const startPosition = RTSReplayVideo.getPosition(startName);
  const endPosition = RTSReplayVideo.getPosition(endName);

  RTSReplayVideo.activePosition = startPosition;
  RTSReplayVideo.video.src = command.replayUrl;
  RTSReplayVideo.video.style.display = 'block';
  RTSReplayVideo.video.load();

  // A replay must enter at the configured Start Position, not briefly appear
  // at the player's default Full Screen transform before the animation begins.
  // Apply the start position while hidden and with transitions disabled, then
  // reveal the player and animate to the configured End Position.
  if (RTSReplayVideo.player.classList.contains('show')) {
    RTSReplayVideo.applyPosition(startPosition, true);
    requestAnimationFrame(() => {
      RTSReplayVideo.configureTransition();
      RTSReplayVideo.player.style.transform = RTSReplayVideo.transformFor(endPosition);
      RTSReplayVideo.activePosition = endPosition;
    });
  } else {
    RTSReplayVideo.player.classList.remove('player-transition');
    RTSReplayVideo.applyPosition(startPosition, true);
    RTSReplayVideo.player.classList.add('show');
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

// A replay is a complete player lifecycle: load -> enter -> play -> exit.
// When the video reaches its natural end, return it to the configured Start
// Position using the same animation duration and easing, then hide it.
RTSReplayVideo.video.addEventListener('ended', () => {
  if (!RTSReplayVideo.currentCommand) return;
  RTSReplayVideo.animateOut();
});
