const RTSReplaySkin = window.RTSReplay;

RTSReplaySkin.formatSpeed = value => `${Number(value.toFixed(2))}x`;

RTSReplaySkin.loadFont = font => {
  const family = String(font || 'Inter').trim();
  if (!family) return;
  let link = document.getElementById('player-skin-font');
  if (!link) {
    link = document.createElement('link');
    link.id = 'player-skin-font';
    link.rel = 'stylesheet';
    document.head.appendChild(link);
  }
  link.href = `https://fonts.googleapis.com/css2?family=${encodeURIComponent(family).replace(/%20/g, '+')}:wght@400;600;700&display=swap`;
  RTSReplaySkin.player.style.setProperty('--player-skin-font', `'${family.replace(/'/g, "\\'")}', system-ui, sans-serif`);
};

RTSReplaySkin.clearSkin = () => {
  clearTimeout(RTSReplaySkin.titleTimer);
  RTSReplaySkin.title.textContent = '';
  RTSReplaySkin.player.classList.remove('has-title', 'has-speed');
  RTSReplaySkin.speedLabel.textContent = '';
  RTSReplaySkin.clearSlowMotion();
};

RTSReplaySkin.clearSlowMotion = () => {
  clearTimeout(RTSReplaySkin.slowHideTimer);
  RTSReplaySkin.slowMotion.classList.remove('slow-motion-active', 'slow-motion-flash');
  RTSReplaySkin.slowMotion.classList.add('slow-motion-hidden');
};

RTSReplaySkin.showSlowMotion = command => {
  const text = String(command.replaySlowMotionText || 'Slow Motion').trim() || 'Slow Motion';
  const speed = Number(command.replayPlaybackSpeed) || 1;
  const showSpeed = command.replaySlowMotionShowSpeed !== false;
  RTSReplaySkin.slowMotion.textContent = showSpeed ? `${text} ${RTSReplaySkin.formatSpeed(speed)}` : text;
  RTSReplaySkin.slowMotion.style.setProperty('--slow-fade-duration', `${Math.max(0, Number(command.replaySlowMotionFadeDuration) || 0.25)}s`);
  RTSReplaySkin.slowMotion.style.setProperty('--slow-flash-interval', `${Math.max(0.1, Number(command.replaySlowMotionFlashInterval) || 0.5)}s`);
  RTSReplaySkin.slowMotion.classList.remove('slow-motion-hidden');
  RTSReplaySkin.slowMotion.classList.toggle('slow-motion-no-fade', command.replaySlowMotionFade === false);
  RTSReplaySkin.slowMotion.classList.toggle('slow-motion-flash', command.replaySlowMotionFlash === true);
  requestAnimationFrame(() => RTSReplaySkin.slowMotion.classList.add('slow-motion-active'));
};

RTSReplaySkin.configure = command => {
  RTSReplaySkin.clearSkin();
  RTSReplaySkin.loadFont(command.replayPlayerFont || 'Inter');
  const title = String(command.replayTitle || '').trim();
  if (command.replayShowTitle !== false && title) {
    RTSReplaySkin.title.textContent = title;
    RTSReplaySkin.player.classList.add('has-title');
    const duration = Number(command.replayTitleDuration);
    if (Number.isFinite(duration) && duration > 0) RTSReplaySkin.titleTimer = setTimeout(() => RTSReplaySkin.player.classList.remove('has-title'), duration * 1000);
  }
  const speed = Number(command.replayPlaybackSpeed) || 1;
  if (speed < 0.999) RTSReplaySkin.showSlowMotion(command);
  else if (speed > 1.001) {
    RTSReplaySkin.speedLabel.textContent = RTSReplaySkin.formatSpeed(speed);
    RTSReplaySkin.player.classList.add('has-speed');
  }
};

RTSReplaySkin.hide = () => RTSReplaySkin.clearSlowMotion();
RTSReplaySkin.titleTimer = null;
RTSReplaySkin.slowHideTimer = null;
RTSReplaySkin.title = document.getElementById('player-title');
RTSReplaySkin.speedLabel = document.getElementById('player-speed-indicator');
RTSReplaySkin.slowMotion = document.getElementById('player-slow-motion');
window.RTSReplaySkin = RTSReplaySkin;
