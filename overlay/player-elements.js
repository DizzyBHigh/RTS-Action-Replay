const RTSReplayElements = window.RTSReplay;

RTSReplayElements.loadFont = (font, id) => {
  const family = String(font || 'Inter').trim();
  if (!family) return;
  let link = document.getElementById(id);
  if (!link) {
    link = document.createElement('link');
    link.id = id;
    link.rel = 'stylesheet';
    document.head.appendChild(link);
  }
  link.href = `https://fonts.googleapis.com/css2?family=${encodeURIComponent(family).replace(/%20/g, '+')}:wght@400;600;700&display=swap`;
};

RTSReplayElements.toRgba = (value, opacity) => {
  const color = String(value || '').trim();
  const match = color.match(/^#([0-9a-f]{6}|[0-9a-f]{8})$/i);
  const alpha = Math.max(0, Math.min(1, Number(opacity) / 100));
  if (!match) return color || `rgba(16,20,22,${alpha})`;
  const hex = match[1].length === 8 ? match[1].slice(2) : match[1];
  return `rgba(${parseInt(hex.slice(0, 2), 16)},${parseInt(hex.slice(2, 4), 16)},${parseInt(hex.slice(4, 6), 16)},${alpha})`;
};

RTSReplayElements.clearTitleTimer = () => {
  if (RTSReplayElements.titleTimer) clearTimeout(RTSReplayElements.titleTimer);
  RTSReplayElements.titleTimer = null;
};

RTSReplayElements.hideTitle = () => {
  RTSReplayElements.clearTitleTimer();
  RTSReplayElements.title.classList.remove('visible', 'closing', 'title-top', 'title-bottom');
};

RTSReplayElements.showTitle = command => {
  RTSReplayElements.hideTitle();
  const title = String(command.replayTitle || '').trim();
  if (command.replayShowTitle === false || !title) return;
  const position = String(command.replayTitlePosition || 'Top').toLowerCase() === 'bottom' ? 'title-bottom' : 'title-top';
  const duration = Math.max(.1, Number(command.replayTitleAnimationDuration) || .45);
  const displayDuration = Math.max(0, Number(command.replayTitleDuration) || 0);
  RTSReplayElements.title.classList.add(position);
  RTSReplayElements.title.textContent = title;
  RTSReplayElements.title.classList.add('visible');
  if (displayDuration > 0) {
    RTSReplayElements.titleTimer = setTimeout(() => {
      RTSReplayElements.title.classList.add('closing');
      RTSReplayElements.titleTimer = setTimeout(RTSReplayElements.hideTitle, duration * 1000);
    }, displayDuration * 1000);
  }
};

RTSReplayElements.configure = command => {
  RTSReplayElements.currentCommand = command;
  const playerFont = command.replayPlayerFont || 'Inter';
  const titleFont = command.replayTitleFont || playerFont;
  RTSReplayElements.loadFont(playerFont, 'player-elements-font');
  RTSReplayElements.loadFont(titleFont, 'player-title-font');
  RTSReplayElements.layer.style.setProperty('--frame-color', command.replayFrameColor || '#0384CB');
  RTSReplayElements.layer.style.setProperty('--speed-font-size', `${Math.max(1, Number(command.replaySpeedFontSize) || 30)}px`);
  RTSReplayElements.layer.style.setProperty('--player-elements-font', `'${String(playerFont).replace(/'/g, "\\'")}', system-ui, sans-serif`);
  RTSReplayElements.layer.style.setProperty('--title-font', `'${String(titleFont).replace(/'/g, "\\'")}', system-ui, sans-serif`);
  RTSReplayElements.layer.style.setProperty('--title-font-size', `${Math.max(1, Number(command.replayTitleFontSize) || 34)}px`);
  RTSReplayElements.layer.style.setProperty('--title-color', command.replayTitleColor || '#FFFFFF');
  RTSReplayElements.layer.style.setProperty('--title-shadow-color', command.replayTitleShadowColor || '#000000');
  RTSReplayElements.layer.style.setProperty('--title-background', RTSReplayElements.toRgba(command.replayTitleBackgroundColor || '#101416', command.replayTitleBackgroundOpacity ?? 88));
  RTSReplayElements.layer.style.setProperty('--title-animation-duration', `${Math.max(.1, Number(command.replayTitleAnimationDuration) || .45)}s`);

  RTSReplayElements.brand.innerHTML = '';
  const showBranding = command.replayShowBranding !== false;
  const logo = String(command.replayLogoUrl || '').trim();
  RTSReplayElements.brand.classList.toggle('visible', showBranding);
  if (showBranding) {
    if (logo) RTSReplayElements.brand.innerHTML = `<img src="${logo.replace(/"/g, '&quot;')}" alt="">`;
    else RTSReplayElements.brand.textContent = 'RTS';
  }

  const speedValue = Number(command.replayPlaybackSpeed) || 1;
  RTSReplayElements.speedLabel.textContent = '';
  RTSReplayElements.slowMotion.textContent = '';
  RTSReplayElements.speedLabel.classList.remove('visible');
  RTSReplayElements.slowMotion.classList.remove('visible');
  if (speedValue < 0.999) {
    const text = String(command.replaySlowMotionText || 'Slow Motion').trim() || 'Slow Motion';
    const showSpeed = command.replaySlowMotionShowSpeed !== false;
    RTSReplayElements.slowMotion.textContent = showSpeed ? `${text} ${Number(speedValue.toFixed(2))}x` : text;
    RTSReplayElements.slowMotion.classList.add('visible');
  } else {
    RTSReplayElements.speedLabel.textContent = `${Number(speedValue.toFixed(2))}x`;
    RTSReplayElements.speedLabel.classList.add('visible');
  }
  RTSReplayElements.showTitle(command);
};

RTSReplayElements.clear = () => {
  RTSReplayElements.clearTitleTimer();
  RTSReplayElements.brand.innerHTML = '';
  RTSReplayElements.title.textContent = '';
  RTSReplayElements.speedLabel.textContent = '';
  RTSReplayElements.slowMotion.textContent = '';
  RTSReplayElements.brand.classList.remove('visible');
  RTSReplayElements.title.classList.remove('visible', 'closing', 'title-top', 'title-bottom');
  RTSReplayElements.speedLabel.classList.remove('visible');
  RTSReplayElements.slowMotion.classList.remove('visible');
};

RTSReplayElements.hide = RTSReplayElements.clear;
RTSReplayElements.layer = document.getElementById('player-elements');
RTSReplayElements.brand = document.getElementById('player-branding');
RTSReplayElements.title = document.getElementById('player-title');
RTSReplayElements.speedLabel = document.getElementById('player-speed-indicator');
RTSReplayElements.slowMotion = document.getElementById('player-slow-motion');
window.RTSReplayElements = RTSReplayElements;
