const RTSReplayVideo = window.RTSReplay;

RTSReplayVideo.confirmPlayback = (replayId, userId, userName) => {
  if (!replayId || !RTSReplayVideo.socket || RTSReplayVideo.socket.readyState !== WebSocket.OPEN) return;
  RTSReplayVideo.socket.send(JSON.stringify({
    request: 'DoAction', id: `rts-replay-confirm-${Date.now()}`,
    action: { name: RTSReplayVideo.config.confirmAction },
    args: { replayId, userId: userId || '', userName: userName || '' }
  }));
};

RTSReplayVideo.notifyPlaybackEnded = command => {
  if (!command?.replayId || !RTSReplayVideo.socket || RTSReplayVideo.socket.readyState !== WebSocket.OPEN) return;
  RTSReplayVideo.socket.send(JSON.stringify({
    request: 'DoAction', id: `rts-replay-ended-${Date.now()}`,
    action: { name: RTSReplayVideo.config.endedAction },
    args: { replayId: command.replayId, replayQueueEntryId: command.replayQueueEntryId || '' }
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

  const profile = RTSReplayAnimation.readProfile(command);
  const startSequence = profile?.start;
  const startSteps = Array.isArray(startSequence) ? startSequence : [];
  const startName = command.replayStartPosition || command.replayPosition || 'Full Screen';
  const endName = command.replayEndPosition || startName;
  const startPosition = startSteps.length ? RTSReplayAnimation.getPosition(startSteps[0].position) : RTSReplayVideo.getPosition(startName);
  const endPosition = startSteps.length ? RTSReplayAnimation.getPosition(startSteps[startSteps.length - 1].position) : RTSReplayVideo.getPosition(endName);

  RTSReplayVideo.video.src = command.replayUrl;
  RTSReplayVideo.video.style.display = 'block';
  RTSReplayVideo.visiblePosition = endPosition;
  const alreadyVisible = RTSReplayVideo.player.classList.contains('show');
  RTSReplayVideo.video.load();
  if (alreadyVisible) {
    RTSReplayAnimation.cancelSequence();
    RTSReplayVideo.applyPosition(endPosition, true);
    RTSReplayVideo.activePosition = endPosition;
  } else if (startSteps.length) {
    RTSReplayAnimation.runSequence(startSequence);
    RTSReplayVideo.player.classList.add('show');
  } else {
    RTSReplayVideo.animateIn(startPosition, endPosition);
  }

  if (command.replayAutoplay) {
    RTSReplayVideo.video.addEventListener('canplay', () => RTSReplayVideo.playReplay(command), { once: true });
  }
};

RTSReplayVideo.testTitle = command => {
  const params = new URLSearchParams(window.location.search);
  if (params.get('dev') !== 'true') return;
  RTSReplayVideo.currentCommand = command;
  RTSReplayControls.configure(command);
  RTSReplayElements.configure(command);
  RTSReplayVideo.player.classList.add('dev-player', 'show');
  RTSReplayVideo.player.style.opacity = '1';
  RTSReplayVideo.player.style.visibility = 'visible';
  RTSReplayVideo.frame?.classList.add('dev-frame');
  window.RTSDevToolbar?.refreshPositions?.();
};

RTSReplayVideo.moveReplay = command => {
  RTSReplayVideo.currentCommand = command;
  const position = RTSReplayVideo.getPosition(command.replayPosition || 'Full Screen');
  const current = RTSReplayVideo.activePosition;
  if (current && RTSReplayVideo.positionsEqual?.(current, position)) return;
  RTSReplayVideo.player.classList.add('show');
  if (!current) {
    RTSReplayVideo.applyPosition(position, true);
    RTSReplayVideo.activePosition = position;
    return;
  }
  RTSReplayVideo.animatePosition(current, position, () => {
    RTSReplayVideo.activePosition = position;
  });
};

RTSReplayVideo.showPlayer = () => {
  const target = RTSReplayVideo.visiblePosition || RTSReplayVideo.activePosition || RTSReplayVideo.getPosition('Full Screen');
  RTSReplayVideo.video.style.display = 'block';
  RTSReplayVideo.player.classList.add('show');
  RTSReplayVideo.applyPosition(target, true);
  RTSReplayVideo.activePosition = target;
  RTSReplayVideo.playReplay(RTSReplayVideo.currentCommand || {});
};

RTSReplayVideo.hideReplay = () => {
  RTSReplayAnimation.cancelSequence();
  RTSReplayVideo.video.pause();
  RTSReplayVideo.visiblePosition = RTSReplayVideo.activePosition;
  const profile = RTSReplayAnimation.readProfile(RTSReplayVideo.currentCommand || {});
  if (Array.isArray(profile?.end) && profile.end.length) {
    RTSReplayAnimation.runEndSequence(profile.end, () => RTSReplayVideo.player.classList.remove('show'));
    return;
  }
  RTSReplayVideo.animateOut();
};

RTSReplayVideo.handleReplayCommand = command => {
  if (command.replayCommand === 'message') RTSReplayVideo.showMessage(command);
  if (command.replayCommand === 'load') RTSReplayVideo.loadReplay(command);
  if (command.replayCommand === 'title-test') RTSReplayVideo.testTitle(command);
  if (command.replayCommand === 'play') RTSReplayVideo.playReplay(command);
  if (command.replayCommand === 'pause') RTSReplayVideo.video.pause();
  if (command.replayCommand === 'speed') {
    const speed = Math.max(0.25, Math.min(4, Number(command.replayPlaybackSpeed) || 1));
    RTSReplayVideo.video.playbackRate = speed;
  }
  if (command.replayCommand === 'move') RTSReplayVideo.moveReplay(command);
  if (command.replayCommand === 'hide') RTSReplayVideo.hideReplay();
  if (command.replayCommand === 'show') RTSReplayVideo.showPlayer();
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
  const command = RTSReplayVideo.currentCommand;
  if (command) RTSReplayVideo.notifyPlaybackEnded(command);
});
