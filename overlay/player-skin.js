const RTSReplaySkin = window.RTSReplay;
RTSReplaySkin.formatSpeed = value => `${Number(value.toFixed(2))}x`;
RTSReplaySkin.loadFont = font => {
  const family = String(font || 'Inter').trim(); if (!family) return;
  let link = document.getElementById('player-skin-font');
  if (!link) { link = document.createElement('link'); link.id = 'player-skin-font'; link.rel = 'stylesheet'; document.head.appendChild(link); }
  link.href = `https://fonts.googleapis.com/css2?family=${encodeURIComponent(family).replace(/%20/g, '+')}:wght@400;600;700&display=swap`;
  RTSReplaySkin.player.style.setProperty('--player-skin-font', `'${family.replace(/'/g, "\\'")}', system-ui, sans-serif`);
};
RTSReplaySkin.elementDefaults = { Brand: { scale: 100, x: 0, y: 0 }, Title: { scale: 100, x: 0, y: 0 }, 'Play Speed Indicator': { scale: 100, x: 0, y: 0 } };
RTSReplaySkin.readElements = command => {
  const raw = command && command.replayPlayerElements;
  if (raw && typeof raw === 'object') return raw;
  if (typeof raw === 'string' && raw.trim()) {
    try { return JSON.parse(raw); } catch (error) { console.warn('[RTS Action Replay] Invalid player element JSON', raw, error); }
  }
  return {};
};
RTSReplaySkin.applyElementPosition = (element, data, defaults) => {
  if (!element) return;
  const p = data || defaults; const scale = Math.max(0, Number(p.scale) || 0); const x = Math.max(-50, Math.min(150, Number(p.x) || 0)); const y = Math.max(-50, Math.min(150, Number(p.y) || 0));
  element.style.left = `${50 + x}%`; element.style.top = `${50 + y}%`; element.style.transform = `translate(-50%, -50%) scale(${scale / 100})`;
};
RTSReplaySkin.configureElements = command => {
  const elements = RTSReplaySkin.readElements(command); const speed = elements['Play Speed Indicator'] || RTSReplaySkin.elementDefaults['Play Speed Indicator'];
  RTSReplaySkin.applyElementPosition(RTSReplaySkin.brand, elements.Brand, RTSReplaySkin.elementDefaults.Brand);
  RTSReplaySkin.applyElementPosition(RTSReplaySkin.title, elements.Title, RTSReplaySkin.elementDefaults.Title);
  RTSReplaySkin.applyElementPosition(RTSReplaySkin.speedLabel, speed, RTSReplaySkin.elementDefaults['Play Speed Indicator']);
  const scale = Math.max(0, Number(speed.scale) || 0); const x = Math.max(-50, Math.min(150, Number(speed.x) || 0)); const y = Math.max(-50, Math.min(150, Number(speed.y) || 0));
  RTSReplaySkin.slowMotion.style.left = `${50 + x}%`; RTSReplaySkin.slowMotion.style.top = `calc(${50 + y}% + ${1.2 * scale / 100}em)`; RTSReplaySkin.slowMotion.style.transform = `translate(-50%, 0) scale(${scale / 100})`;
};
RTSReplaySkin.configureFrame = command => {
  const frame = RTSReplaySkin.frame;
  frame.style.setProperty('--frame-color', command.replayFrameColor || '#0384CB'); frame.style.setProperty('--border-color', command.replayBorderColor || '#FFFFFF');
  frame.style.setProperty('--border-width', `${Math.max(0, Number(command.replayBorderWidth) || 0)}px`); frame.style.setProperty('--shadow-color', command.replayShadowColor || '#80000000');
  frame.classList.remove('border-none', 'border-solid', 'border-dashed', 'border-double', 'drop-shadow'); frame.classList.add(`border-${String(command.replayBorderStyle || 'Solid').toLowerCase()}`);
  if (command.replayDropShadow === true) frame.classList.add('drop-shadow');
};
RTSReplaySkin.clearSlowMotion = () => { RTSReplaySkin.slowMotion.classList.remove('slow-motion-active', 'slow-motion-flash'); RTSReplaySkin.slowMotion.classList.add('slow-motion-hidden'); };
RTSReplaySkin.clearSkin = () => {
  clearTimeout(RTSReplaySkin.titleTimer); RTSReplaySkin.title.textContent = ''; RTSReplaySkin.player.classList.remove('has-title', 'has-speed', 'has-branding'); RTSReplaySkin.speedLabel.textContent = ''; RTSReplaySkin.brand.innerHTML = ''; RTSReplaySkin.clearSlowMotion();
};
RTSReplaySkin.configure = command => {
  RTSReplaySkin.clearSkin(); RTSReplaySkin.configureFrame(command); RTSReplaySkin.configureElements(command); RTSReplaySkin.loadFont(command.replayPlayerFont || 'Inter');
  const showBranding = command.replayShowBranding !== false;
  const logo = String(command.replayLogoUrl || '').trim();
  if (showBranding) {
    if (logo) RTSReplaySkin.brand.innerHTML = `<img src="${logo.replace(/"/g, '&quot;')}" alt="">`; else RTSReplaySkin.brand.textContent = 'RTS';
    RTSReplaySkin.player.classList.add('has-branding');
  }
  const title = String(command.replayTitle || '').trim();
  if (command.replayShowTitle !== false && title) {
    RTSReplaySkin.title.textContent = title; RTSReplaySkin.player.classList.add('has-title'); const duration = Number(command.replayTitleDuration);
    if (Number.isFinite(duration) && duration > 0) RTSReplaySkin.titleTimer = setTimeout(() => RTSReplaySkin.player.classList.remove('has-title'), duration * 1000);
  }
  const speed = Number(command.replayPlaybackSpeed) || 1;
  if (speed < 0.999) RTSReplaySkin.showSlowMotion(command);
  else if (speed > 1.001) { RTSReplaySkin.speedLabel.textContent = RTSReplaySkin.formatSpeed(speed); RTSReplaySkin.player.classList.add('has-speed'); }
};
RTSReplaySkin.showSlowMotion = command => {
  const text = String(command.replaySlowMotionText || 'Slow Motion').trim() || 'Slow Motion'; const speed = Number(command.replayPlaybackSpeed) || 1; const showSpeed = command.replaySlowMotionShowSpeed !== false;
  RTSReplaySkin.slowMotion.textContent = showSpeed ? `${text} ${RTSReplaySkin.formatSpeed(speed)}` : text;
  RTSReplaySkin.slowMotion.style.setProperty('--slow-fade-duration', `${Math.max(0, Number(command.replaySlowMotionFadeDuration) || 0.25)}s`);
  RTSReplaySkin.slowMotion.style.setProperty('--slow-flash-interval', `${Math.max(0.1, Number(command.replaySlowMotionFlashInterval) || 0.5)}s`);
  RTSReplaySkin.player.classList.add('has-speed'); RTSReplaySkin.slowMotion.classList.remove('slow-motion-hidden'); RTSReplaySkin.slowMotion.classList.toggle('slow-motion-no-fade', command.replaySlowMotionFade === false); RTSReplaySkin.slowMotion.classList.toggle('slow-motion-flash', command.replaySlowMotionFlash === true);
  requestAnimationFrame(() => RTSReplaySkin.slowMotion.classList.add('slow-motion-active'));
};
RTSReplaySkin.hide = () => RTSReplaySkin.clearSlowMotion();
RTSReplaySkin.titleTimer = null;
RTSReplaySkin.slowMotion = document.getElementById('player-slow-motion'); RTSReplaySkin.slowHideTimer = null;
RTSReplaySkin.title = document.getElementById('player-title'); RTSReplaySkin.speedLabel = document.getElementById('player-speed-indicator'); RTSReplaySkin.brand = document.getElementById('player-branding'); RTSReplaySkin.frame = document.getElementById('player-frame');
window.RTSReplaySkin = RTSReplaySkin;
