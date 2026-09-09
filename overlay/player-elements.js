const RTSReplayElements = window.RTSReplay;

RTSReplayElements.clearTitleTimer = () => {
  if (RTSReplayElements.titleTimer) clearTimeout(RTSReplayElements.titleTimer);
  RTSReplayElements.titleTimer = null;
};

RTSReplayElements.toRgba = (value, opacity = 1) => {
  const color = String(value || '').trim();
  const match = color.match(/^#([0-9a-f]{6}|[0-9a-f]{8})$/i);
  if (!match) return color || `rgba(0,0,0,${opacity})`;
  let hex = match[1];
  let alpha = opacity;
  if (hex.length === 8) {
    alpha *= parseInt(hex.slice(0, 2), 16) / 255;
    hex = hex.slice(2);
  }
  return `rgba(${parseInt(hex.slice(0, 2), 16)},${parseInt(hex.slice(2, 4), 16)},${parseInt(hex.slice(4, 6), 16)},${Math.max(0, Math.min(1, alpha))})`;
};

RTSReplayElements.loadFont = font => {
  const name = String(font || 'Inter').trim();
  if (!name) return;
  const id = `rts-font-${name.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`;
  if (document.getElementById(id)) return;
  const link = document.createElement('link');
  link.id = id;
  link.rel = 'stylesheet';
  link.href = `https://fonts.googleapis.com/css2?family=${encodeURIComponent(name).replace(/%20/g, '+')}:wght@400;500;600;700;800&display=swap`;
  document.head.appendChild(link);
};

RTSReplayElements.clearTitleTimer = RTSReplayElements.clearTitleTimer;

RTSReplayElements.hideTitle = () => {
  RTSReplayElements.clearTitleTimer();
  const title = RTSReplayElements.title;
  if (!title || !title.classList.contains('visible')) return;
  title.classList.remove('title-enter');
  title.classList.add('title-exit');
  title.addEventListener('animationend', () => title.classList.remove('visible', 'title-exit'), { once: true });
};

RTSReplayElements.showTitle = command => {
  const title = RTSReplayElements.title;
  if (!title || command.replayShowTitle === false) return;
  const text = String(command.replayTitle || '').trim();
  if (!text) return;

  RTSReplayElements.hideTitle();
  const style = String(command.replayTitleStyle || 'Broadcast').toLowerCase();
  const position = String(command.replayTitlePosition || 'Bottom').toLowerCase();
  const animation = String(command.replayTitleAnimation || 'Slide up/down').toLowerCase();
  const className = ['broadcast', 'cinematic', 'cut', 'minimal'].includes(style) ? style : 'broadcast';
  const direction = animation === 'left to right' ? 'from-left' : animation === 'right to left' ? 'from-right' : animation === 'fade' ? 'fade' : position === 'top' ? 'from-top' : 'from-bottom';

  title.className = `title-${className} title-${position === 'top' ? 'top' : 'bottom'} title-${direction}`;
  title.textContent = text;
  title.style.setProperty('--title-font', `'${String(command.replayTitleFont || 'Inter').replace(/'/g, '')}', system-ui, sans-serif`);
  title.style.setProperty('--title-size', `${Math.max(12, Number(command.replayTitleFontSize) || 34)}px`);
  title.style.setProperty('--title-text', RTSReplayElements.toRgba(command.replayTitleTextColor, 1));
  title.style.setProperty('--title-shadow', RTSReplayElements.toRgba(command.replayTitleShadowColor, 1));
  title.style.setProperty('--title-bg', RTSReplayElements.toRgba(command.replayTitleBackgroundColor, Math.max(0, Math.min(1, (Number(command.replayTitleBackgroundOpacity) || 0) / 100))));
  title.style.setProperty('--title-accent', RTSReplayElements.toRgba(command.replayTitleAccentColor, 1));
  title.style.setProperty('--title-animation-duration', `${Math.max(0.1, Number(command.replayTitleAnimationDuration) || 0.45)}s`);
  RTSReplayElements.loadFont(command.replayTitleFont);
  title.classList.add('visible', 'title-enter');

  const displayDuration = Math.max(0, Number(command.replayTitleDuration) || 0);
  if (displayDuration > 0) RTSReplayElements.titleTimer = setTimeout(() => RTSReplayElements.hideTitle(), displayDuration * 1000);
};

RTSReplayElements.configure = command => {
  RTSReplayElements.command = command;
  if (RTSReplayElements.speed) RTSReplayElements.speed.textContent = `${Number(command.replayPlaybackSpeed || 1).toFixed(2).replace(/\.00$/, '')}×`;
  if (RTSReplayElements.branding) RTSReplayElements.branding.classList.toggle('visible', command.replayShowBranding !== false);
  if (command.replayTitle) RTSReplayElements.showTitle(command);
};

RTSReplayElements.title = document.getElementById('player-title');
RTSReplayElements.branding = document.getElementById('player-branding');
RTSReplayElements.speed = document.getElementById('player-speed');
