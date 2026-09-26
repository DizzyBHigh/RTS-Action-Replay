const RTSReplayControls = window.RTSReplay;

RTSReplayControls.formatTime = seconds => {
  if (!Number.isFinite(seconds) || seconds < 0) return '00:00';
  const total = Math.floor(seconds);
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  return h ? `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}` : `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
};

RTSReplayControls.setPlaybackState = playing => {
  if (!RTSReplayControls.playPause) return;
  RTSReplayControls.playPause.setAttribute('aria-label', playing ? 'Pause' : 'Play');
  RTSReplayControls.playPause.setAttribute('aria-pressed', playing ? 'true' : 'false');
  RTSReplayControls.playPause.firstElementChild.textContent = playing ? '||' : '>';
};

RTSReplayControls.updateControls = () => {
  const video = RTSReplayControls.video;
  const duration = Number.isFinite(video.duration) ? video.duration : 0;
  const current = Number.isFinite(video.currentTime) ? video.currentTime : 0;
  RTSReplayControls.current.textContent = RTSReplayControls.formatTime(current);
  RTSReplayControls.duration.textContent = RTSReplayControls.formatTime(duration);
  RTSReplayControls.progressBar.style.width = duration ? `${(current / duration) * 100}%` : '0%';
  RTSReplayControls.progress.setAttribute('aria-valuemax', String(duration));
  RTSReplayControls.progress.setAttribute('aria-valuenow', String(current));
  RTSReplayControls.setPlaybackState(!video.paused && !video.ended);
};

RTSReplayControls.updateYouTubeControls = (current, duration) => {
  current = Number(current) || 0;
  duration = Number(duration) || 0;
  RTSReplayControls.current.textContent = RTSReplayControls.formatTime(Math.max(0, current));
  RTSReplayControls.duration.textContent = RTSReplayControls.formatTime(Math.max(0, duration));
  RTSReplayControls.progressBar.style.width = duration ? `${Math.max(0, Math.min(100, current / duration * 100))}%` : '0%';
  RTSReplayControls.progress.setAttribute('aria-valuemax', String(duration));
  RTSReplayControls.progress.setAttribute('aria-valuenow', String(Math.max(0, current)));
  RTSReplayControls.setPlaybackState(window.RTSReplayYouTubePlaying?.() === true);
};

RTSReplayControls.configure = command => {
  const player = RTSReplayControls.player;
  const showControls = command.replayShowControls !== false;
  player.classList.toggle('controls-hidden', !showControls);
  player.classList.toggle('progress-hidden', command.replayShowProgress === false);
  RTSReplayControls.controls.setAttribute('aria-hidden', showControls ? 'false' : 'true');
  player.style.setProperty('--frame-color', command.replayFrameColor || '#0384CB');
  const controlColor = command.replayControlColor || command.replayBrandPrimaryColor || command.replayTitlePrimaryColor || '#0384CB';
  player.style.setProperty('--control-color', controlColor, 'important');
  RTSReplayControls.controls.style.setProperty('--control-color', controlColor, 'important');
  RTSReplayControls.playPause.style.setProperty('color', controlColor, 'important');
  RTSReplayControls.playPause.style.setProperty('border-color', controlColor, 'important');
  RTSReplayControls.progressBar.style.setProperty('background-color', controlColor, 'important');
  RTSReplayControls.speed.style.setProperty('color', controlColor, 'important');
  const computed = getComputedStyle(player);
  const controlComputed = RTSReplayControls.playPause ? getComputedStyle(RTSReplayControls.playPause) : null;
  const progressComputed = RTSReplayControls.progressBar ? getComputedStyle(RTSReplayControls.progressBar) : null;
  window.RTSDevToolbar?.log?.('Player controls configured', {
    commandControlColor: command.replayControlColor || '<missing>',
    cssControlColor: player.style.getPropertyValue('--control-color') || '<missing>',
    inheritedControlColor: computed.getPropertyValue('--control-color') || '<missing>',
    buttonColor: controlComputed?.color || '<missing>',
    buttonBorderColor: controlComputed?.borderTopColor || '<missing>',
    progressBarColor: progressComputed?.backgroundColor || '<missing>'
  });
  player.style.setProperty('--border-color', command.replayBorderColor || '#FFFFFF');
  RTSReplayControls.frame.className = '';
  RTSReplayControls.frame.classList.add(`border-${String(command.replayBorderStyle || 'Solid').toLowerCase()}`);
  const speed = Number(command.replayPlaybackSpeed) || 1;
  RTSReplayControls.speed.textContent = `${speed}x`;
  RTSReplayControls.video.playbackRate = speed;
};

RTSReplayControls.configureControls = RTSReplayControls.configure;

['loadedmetadata', 'timeupdate', 'play', 'pause', 'ended', 'durationchange'].forEach(event => {
  RTSReplayControls.video.addEventListener(event, RTSReplayControls.updateControls);
});

RTSReplayControls.seekToPointer = event => {
  const rect = RTSReplayControls.progress.getBoundingClientRect();
  if (!rect.width) return;
  const fraction = Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width));
  const command = RTSReplayVideo.currentCommand;
  if (!command) return;
  if (command.replaySource?.toLowerCase() === 'youtube') {
    const start = Number(command.replayStartTime || 0);
    const duration = Number(command.replayDuration || 0);
    window.RTSReplayYouTubeSeek?.(start + duration * fraction);
  } else {
    const duration = Number(RTSReplayControls.video.duration);
    if (Number.isFinite(duration) && duration > 0) RTSReplayControls.video.currentTime = duration * fraction;
  }
};

RTSReplayControls.togglePlayback = command => {
  const activeCommand = command || RTSReplayVideo.currentCommand;
  if (!activeCommand) return;
  if (activeCommand.replaySource?.toLowerCase() === 'youtube') {
    const state = window.RTSReplayYouTubeState?.();
    if (state === 'playing' || state === 'buffering') {
      RTSReplayVideo.expectedPlaying = false;
      RTSReplayWatchdog?.stop?.();
      window.RTSReplayYouTubePause?.();
    } else RTSReplayVideo.playReplay(activeCommand);
    return;
  }
  if (RTSReplayControls.video.paused || RTSReplayControls.video.ended) RTSReplayVideo.playReplay(activeCommand);
  else {
    RTSReplayVideo.expectedPlaying = false;
    RTSReplayWatchdog?.stop?.();
    RTSReplayControls.video.pause();
  }
};

RTSReplayControls.playPause?.addEventListener('click', () => RTSReplayControls.togglePlayback());
let seeking = false;
RTSReplayControls.progress?.addEventListener('pointerdown', event => {
  seeking = true;
  RTSReplayControls.progress.setPointerCapture?.(event.pointerId);
  RTSReplayControls.seekToPointer(event);
});
RTSReplayControls.progress?.addEventListener('pointermove', event => { if (seeking) RTSReplayControls.seekToPointer(event); });
RTSReplayControls.progress?.addEventListener('pointerup', event => {
  seeking = false;
  RTSReplayControls.progress.releasePointerCapture?.(event.pointerId);
});
RTSReplayControls.progress?.addEventListener('pointercancel', () => { seeking = false; });
RTSReplayControls.progress?.addEventListener('keydown', event => {
  if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
  const command = RTSReplayVideo.currentCommand;
  if (!command) return;
  const step = event.shiftKey ? 10 : 5;
  if (command.replaySource?.toLowerCase() === 'youtube') {
    const start = Number(command.replayStartTime || 0);
    const duration = Number(command.replayDuration || 0);
    const current = Number(window.RTSReplayYouTubeCurrent?.() || 0);
    window.RTSReplayYouTubeSeek?.(start + Math.max(0, Math.min(duration, current + (event.key === 'ArrowLeft' ? -step : step))));
  } else {
    const duration = Number(RTSReplayControls.video.duration);
    const current = Number(RTSReplayControls.video.currentTime);
    RTSReplayControls.video.currentTime = Math.max(0, Math.min(duration, current + (event.key === 'ArrowLeft' ? -step : step)));
  }
  event.preventDefault();
});
