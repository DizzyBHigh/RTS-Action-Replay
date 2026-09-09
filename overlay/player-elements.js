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

RTSReplayElements.colorWithAlpha = (value, alpha) => {
  const color = String(value || '').trim();
  const match = color.match(/^#([0-9a-f]{6}|[0-9a-f]{8})$/i);
  if (!match) return color;
  const hex = match[1].length === 8 ? match[1].slice(2) : match[1];
  return `rgba(${parseInt(hex.slice(0, 2), 16)},${parseInt(hex.slice(2, 4), 16)},${parseInt(hex.slice(4, 6), 16)},${Math.max(0, Math.min(1, alpha))})`;
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
  const titleFont = command.replayTitleFont || 'Inter';
  const primary = command.replayTitlePrimaryColor || '#0384CB';
  const secondary = command.replayTitleSecondaryColor || '#101416';
  RTSReplayElements.loadFont(titleFont, 'player-title-font');
  RTSReplayElements.layer.style.setProperty('--frame-color', command.replayFrameColor || '#0384CB');
  RTSReplayElements.layer.style.setProperty('--title-font', `'${String(titleFont).replace(/'/g, "\\'")}', system-ui, sans-serif`);
  RTSReplayElements.layer.style.setProperty('--title-font-size', `${Math.max(1, Number(command.replayTitleFontSize) || 34)}px`);
  RTSReplayElements.layer.style.setProperty('--title-color', command.replayTitleColor || '#FFFFFF');
  RTSReplayElements.layer.style.setProperty('--title-shadow-color', command.replayTitleShadowColor || '#000000');
  RTSReplayElements.layer.style.setProperty('--title-primary', primary);
  RTSReplayElements.layer.style.setProperty('--title-secondary', secondary);
  RTSReplayElements.layer.style.setProperty('--title-background', `linear-gradient(90deg, ${RTSReplayElements.colorWithAlpha(primary,.94)} 0%, ${RTSReplayElements.colorWithAlpha(secondary,.88)} 50%, ${RTSReplayElements.colorWithAlpha(primary,.94)} 100%)`);
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
  RTSReplayElements.speedLabel.textContent = `${Number(speedValue.toFixed(2))}x`;
  RTSReplayElements.speedLabel.classList.add('visible');
  RTSReplayElements.showTitle(command);
};

RTSReplayElements.clear = () => {
  RTSReplayElements.clearTitleTimer();
  RTSReplayElements.brand.innerHTML = '';
  RTSReplayElements.title.textContent = '';
  RTSReplayElements.speedLabel.textContent = '';
  RTSReplayElements.brand.classList.remove('visible');
  RTSReplayElements.title.classList.remove('visible', 'closing', 'title-top', 'title-bottom');
  RTSReplayElements.speedLabel.classList.remove('visible');
};

RTSReplayElements.hide = RTSReplayElements.clear;
RTSReplayElements.layer = document.getElementById('player-elements');
RTSReplayElements.brand = document.getElementById('player-branding');
RTSReplayElements.title = document.getElementById('player-title');
RTSReplayElements.speedLabel = document.getElementById('player-speed-indicator');
window.RTSReplayElements = RTSReplayElements;
