const RTSReplaySkin = window.RTSReplay;
RTSReplaySkin.formatSpeed = value => `${Number(value.toFixed(2))}x`;
RTSReplaySkin.loadFont = font => {
  const family = String(font || 'Inter').trim(); if (!family) return;
  let link = document.getElementById('player-skin-font');
  if (!link) { link = document.createElement('link'); link.id = 'player-skin-font'; link.rel = 'stylesheet'; document.head.appendChild(link); }
  link.href = `https://fonts.googleapis.com/css2?family=${encodeURIComponent(family).replace(/%20/g, '+')}:wght@400;600;700&display=swap`;
  RTSReplaySkin.player.style.setProperty('--player-skin-font', `'${family.replace(/'/g, "\\'")}', system-ui, sans-serif`);
};
RTSReplaySkin.configureFontSizes = command => {
  const titleSize = Math.max(1, Number(command.replayTitleFontSize) || 34);
  const speedSize = Math.max(1, Number(command.replaySpeedFontSize) || 30);
  RTSReplaySkin.player.style.setProperty('--title-font-size', `${titleSize}px`);
  RTSReplaySkin.player.style.setProperty('--speed-font-size', `${speedSize}px`);
};
RTSReplaySkin.elementDefaults = { Brand: { scale: 100, x: 0, y: 0 }, Title: { scale: 100, x: 0, y: 0 }, 'Play Speed Indicator': { scale: 100, x: 0, y: 0 } };
RTSReplaySkin.readElements = command => {
  const explicit = {
    Brand: { scale: command.replayBrandScale, x: command.replayBrandX, y: command.replayBrandY },
    Title: { scale: command.replayTitleScale, x: command.replayTitleX, y: command.replayTitleY },
    'Play Speed Indicator': { scale: command.replaySpeedScale, x: command.replaySpeedX, y: command.replaySpeedY }
  };
  const hasExplicit = Object.keys(explicit).some(name => Object.values(explicit[name]).some(value => value !== undefined && value !== null && value !== ''));
  if (hasExplicit) return explicit;
  let data = {};
  try { data = typeof command.replayPlayerElements === 'string' ? JSON.parse(command.replayPlayerElements || '{}') : (command.replayPlayerElements || {}); } catch { data = {}; }
  return data || {};
};
RTSReplaySkin.elementValue = (value, fallback) => Number.isFinite(Number(value)) ? Number(value) : fallback;
RTSReplaySkin.applyElementPosition = (element, data, defaults) => {
  const p = data || defaults; const scale = Math.max(0, RTSReplaySkin.elementValue(p.scale, defaults.scale));
  const x = Math.max(-50, Math.min(50, RTSReplaySkin.elementValue(p.x, defaults.x))); const y = Math.max(-50, Math.min(50, RTSReplaySkin.elementValue(p.y, defaults.y)));
  element.style.left = `${50 + x}%`; element.style.top = `${50 + y}%`; element.style.transform = `translate(-50%, -50%) scale(${scale / 100})`;
};
RTSReplaySkin.configureElements = command => {
  const elements = RTSReplaySkin.readElements(command); const speed = elements['Play Speed Indicator'] || RTSReplaySkin.elementDefaults['Play Speed Indicator'];
  RTSReplaySkin.applyElementPosition(RTSReplaySkin.brand, elements.Brand, RTSReplaySkin.elementDefaults.Brand); RTSReplaySkin.applyElementPosition(RTSReplaySkin.title, elements.Title, RTSReplaySkin.elementDefaults.Title); RTSReplaySkin.applyElementPosition(RTSReplaySkin.speedLabel, speed, RTSReplaySkin.elementDefaults['Play Speed Indicator']);
  const scale = Math.max(0, RTSReplaySkin.elementValue(speed.scale, 100)); const x = Math.max(-50, Math.min(50, RTSReplaySkin.elementValue(speed.x, 0))); const y = Math.max(-50, Math.min(50, RTSReplaySkin.elementValue(speed.y, 0)));
  RTSReplaySkin.slowMotion.style.left = `${50 + x}%`; RTSReplaySkin.slowMotion.style.top = `calc(${50 + y}% + ${1.2 * scale / 100}em)`; RTSReplaySkin.slowMotion.style.transform = `translate(-50%, 0) scale(${scale / 100})`;
};
RTSReplaySkin.configureFrame = command => {
  const frame = RTSReplaySkin.frame; frame.style.setProperty('--frame-color', command.replayFrameColor || '#0384CB'); frame.style.setProperty('--border-color', command.replayBorderColor || '#FFFFFF');
  frame.style.setProperty('--border-width', `${Math.max(0, Number(command.replayBorderWidth) || 0)}px`); frame.style.setProperty('--shadow-color', command.replayShadowColor || '#80000000');
  frame.classList.remove('border-none', 'border-solid', 'border-dashed', 'border-double', 'drop-shadow'); frame.classList.add(`border-${String(command.replayBorderStyle || 'Solid').toLowerCase()}`); if (command.replayDropShadow === true) frame.classList.add('drop-shadow');
};
RTSReplaySkin.clearSlowMotion = () => { RTSReplaySkin.slowMotion.classList.remove('slow-motion-active', 'slow-motion-flash'); RTSReplaySkin.slowMotion.classList.add('slow-motion-hidden'); };
RTSReplaySkin.clearSkin = () => { clearTimeout(RTSReplaySkin.titleTimer); RTSReplaySkin.title.textContent = ''; RTSReplaySkin.player.classList.remove('has-title', 'has-speed', 'has-branding'); RTSReplaySkin.speedLabel.textContent = ''; RTSReplaySkin.brand.textContent = ''; RTSReplaySkin.clearSlowMotion(); };
RTSReplaySkin.configure = command => {
  RTSReplaySkin.clearSkin(); RTSReplaySkin.configureFrame(command); RTSReplaySkin.configureFontSizes(command); RTSReplaySkin.configureElements(command); RTSReplaySkin.loadFont(command.replayPlayerFont || 'Inter');
  if (command.replayShowBranding !== false) { const logo = String(command.replayLogoUrl || '').trim(); if (logo) RTSReplaySkin.brand.innerHTML = `<img src="${logo.replace(/"/g, '&quot;')}" alt="">`; else RTSReplaySkin.brand.textContent = 'RTS'; RTSReplaySkin.player.classList.add('has-branding'); }
  const title = String(command.replayTitle || '').trim(); if (command.replayShowTitle !== false && title) { RTSReplaySkin.title.textContent = title; RTSReplaySkin.player.classList.add('has-title'); const duration = Number(command.replayTitleDuration); if (Number.isFinite(duration) && duration > 0) RTSReplaySkin.titleTimer = setTimeout(() => RTSReplaySkin.player.classList.remove('has-title'), duration * 1000); }
  const speed = Number(command.replayPlaybackSpeed) || 1; if (speed < 0.999) RTSReplaySkin.showSlowMotion(command); else if (speed > 1.001) { RTSReplaySkin.speedLabel.textContent = RTSReplaySkin.formatSpeed(speed); RTSReplaySkin.player.classList.add('has-speed'); }
};
RTSReplaySkin.showSlowMotion = command => {
  const text = String(command.replaySlowMotionText || 'Slow Motion').trim() || 'Slow Motion'; const speed = Number(command.replayPlaybackSpeed) || 1; const showSpeed = command.replaySlowMotionShowSpeed !== false;
  RTSReplaySkin.slowMotion.textContent = showSpeed ? `${text} ${RTSReplaySkin.formatSpeed(speed)}` : text; RTSReplaySkin.slowMotion.style.setProperty('--slow-fade-duration', `${Math.max(0, Number(command.replaySlowMotionFadeDuration) || 0.25)}s`); RTSReplaySkin.slowMotion.style.setProperty('--slow-flash-interval', `${Math.max(0.1, Number(command.replaySlowMotionFlashInterval) || 0.5)}s`);
  RTSReplaySkin.player.classList.add('has-speed'); RTSReplaySkin.slowMotion.classList.remove('slow-motion-hidden'); RTSReplaySkin.slowMotion.classList.toggle('slow-motion-no-fade', command.replaySlowMotionFade === false); RTSReplaySkin.slowMotion.classList.toggle('slow-motion-flash', command.replaySlowMotionFlash === true); requestAnimationFrame(() => RTSReplaySkin.slowMotion.classList.add('slow-motion-active'));
};
RTSReplaySkin.hide = () => RTSReplaySkin.clearSlowMotion(); RTSReplaySkin.titleTimer = null; RTSReplaySkin.slowMotion = document.getElementById('player-slow-motion'); RTSReplaySkin.slowHideTimer = null; RTSReplaySkin.title = document.getElementById('player-title'); RTSReplaySkin.speedLabel = document.getElementById('player-speed-indicator'); RTSReplaySkin.brand = document.getElementById('player-branding'); RTSReplaySkin.frame = document.getElementById('player-frame'); window.RTSReplaySkin = RTSReplaySkin;
