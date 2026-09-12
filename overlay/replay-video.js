const RTSReplayVideo = window.RTSReplay;

const replayDevLog = (message, details) => window.RTSDevToolbar?.log?.(message, details);

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
  replayDevLog('play() requested', { replayId: command?.replayId, src: RTSReplayVideo.video.currentSrc, readyState: RTSReplayVideo.video.readyState });
  RTSReplayVideo.video.play().then(() => {
    replayDevLog('play() resolved', { replayId: command?.replayId, currentTime: RTSReplayVideo.video.currentTime });
    RTSReplayVideo.confirmPlayback(command.replayId, command.replayUserId, command.replayUserName);
  }).catch(error => {
    replayDevLog('play() rejected', { name: error?.name, message: error?.message });
    console.warn('Replay play failed', error);
  });
};

RTSReplayVideo.loadReplay = command => {
  if (!command.replayUrl) {
    replayDevLog('loadReplay skipped: missing replayUrl');
    return;
  }
  replayDevLog('loadReplay entered', { replayId: command.replayId, url: command.replayUrl, autoplay: !!command.replayAutoplay });
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
  replayDevLog('video src assigned', { currentSrc: RTSReplayVideo.video.currentSrc, readyState: RTSReplayVideo.video.readyState, networkState: RTSReplayVideo.video.networkState });
  RTSReplayVideo.video.style.display = 'block';
  RTSReplayVideo.visiblePosition = endPosition;
  const alreadyVisible = RTSReplayVideo.player.classList.contains('show');
  RTSReplayVideo.video.load();
  replayDevLog('video.load() called', { readyState: RTSReplayVideo.video.readyState, networkState: RTSReplayVideo.video.networkState, alreadyVisible, hasStartSequence: startSteps.length > 0 });
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
    replayDevLog('waiting for canplay');
    RTSReplayVideo.video.addEventListener('canplay', () => {
      replayDevLog('canplay handler fired', { readyState: RTSReplayVideo.video.readyState });
      RTSReplayVideo.playReplay(command);
    }, { once: true });
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
  RTSReplayVideo.currentCommand = { ...(RTSReplayVideo.currentCommand || {}), ...command };
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
  const command = RTSReplayVideo.currentCommand || {};
  const profile = RTSReplayAnimation.readProfile(command);
  replayDevLog('hideReplay entered', {
    replayId: command.replayId,
    showClass: RTSReplayVideo.player.classList.contains('show'),
    activePosition: RTSReplayVideo.activePosition,
    visiblePosition: RTSReplayVideo.visiblePosition,
    hasEndSequence: Array.isArray(profile?.end) && profile.end.length > 0
  });
  RTSReplayAnimation.cancelSequence();
  RTSReplayVideo.video.pause();
  RTSReplayVideo.visiblePosition = RTSReplayVideo.activePosition;
  if (Array.isArray(profile?.end) && profile.end.length) {
    replayDevLog('hideReplay starting end sequence', { steps: profile.end.length });
    RTSReplayAnimation.runEndSequence(profile.end, () => {
      RTSReplayVideo.player.classList.remove('show');
      replayDevLog('hideReplay end sequence complete', { showClass: RTSReplayVideo.player.classList.contains('show'), activePosition: RTSReplayVideo.activePosition });
    });
    return;
  }
  replayDevLog('hideReplay using animateOut');
  RTSReplayVideo.animateOut();
};

RTSReplayVideo.handleReplayCommand = command => {
  replayDevLog('command received', { replayCommand: command?.replayCommand, replayId: command?.replayId });
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
  replayDevLog('video ended', { replayId: command?.replayId, currentTime: RTSReplayVideo.video.currentTime });
  if (command) RTSReplayVideo.notifyPlaybackEnded(command);
});
