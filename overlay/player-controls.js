const RTSReplayControls = window.RTSReplay;

RTSReplayControls.formatTime = seconds => {
  if (!Number.isFinite(seconds) || seconds < 0) return '00:00';
  const total = Math.floor(seconds);
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  return h ? `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}` : `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
};

RTSReplayControls.updateControls = () => {
  const video = RTSReplayControls.video;
  const duration = Number.isFinite(video.duration) ? video.duration : 0;
  const current = Number.isFinite(video.currentTime) ? video.currentTime : 0;
  RTSReplayControls.state.textContent = video.paused ? '▶' : '❚❚';
  RTSReplayControls.time.textContent = `${RTSReplayControls.formatTime(current)} / ${RTSReplayControls.formatTime(duration)}`;
  RTSReplayControls.progressBar.style.width = duration ? `${(current / duration) * 100}%` : '0%';
};

RTSReplayControls.configure = command => {
  const player = RTSReplayControls.player;
  player.classList.toggle('controls-hidden', command.replayShowControls === false);
  player.classList.toggle('progress-hidden', command.replayShowProgress === false);
  player.style.setProperty('--frame-color', command.replayFrameColor || '#0384CB');
  player.style.setProperty('--border-color', command.replayBorderColor || '#FFFFFF');
  RTSReplayControls.frame.className = '';
  RTSReplayControls.frame.classList.add(`border-${String(command.replayBorderStyle || 'Solid').toLowerCase()}`);
  const speed = Number(command.replayPlaybackSpeed) || 1;
  RTSReplayControls.speed.textContent = `${speed}×`;
  RTSReplayControls.video.playbackRate = speed;
};

['loadedmetadata', 'timeupdate', 'play', 'pause', 'ended', 'durationchange'].forEach(event => {
  RTSReplayControls.video.addEventListener(event, RTSReplayControls.updateControls);
});
