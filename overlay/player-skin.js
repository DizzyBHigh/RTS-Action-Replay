const RTSReplaySkin = window.RTSReplay;

RTSReplaySkin.clearSkin = () => {
  clearTimeout(RTSReplaySkin.titleTimer);
  RTSReplaySkin.title.textContent = '';
  RTSReplaySkin.player.classList.remove('has-title', 'slow-motion', 'has-speed');
  RTSReplaySkin.speedLabel.textContent = '';
};

RTSReplaySkin.configure = command => {
  RTSReplaySkin.clearSkin();
  const title = String(command.replayTitle || '').trim();
  if (title) {
    RTSReplaySkin.title.textContent = title;
    RTSReplaySkin.player.classList.add('has-title');
    const duration = Number(command.replayTitleDuration);
    if (Number.isFinite(duration) && duration > 0) {
      RTSReplaySkin.titleTimer = setTimeout(() => {
        RTSReplaySkin.player.classList.remove('has-title');
      }, duration * 1000);
    }
  }

  const speed = Number(command.replayPlaybackSpeed) || 1;
  if (Math.abs(speed - 1) > 0.001) {
    RTSReplaySkin.speedLabel.textContent = `${speed}×`;
    RTSReplaySkin.player.classList.add('has-speed');
    if (speed < 1) RTSReplaySkin.player.classList.add('slow-motion');
  }
};

RTSReplaySkin.titleTimer = null;
RTSReplaySkin.title = document.getElementById('player-title');
RTSReplaySkin.speedLabel = document.getElementById('player-speed-indicator');

window.RTSReplaySkin = RTSReplaySkin;
