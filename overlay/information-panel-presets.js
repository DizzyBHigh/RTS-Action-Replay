const RTSInformationPanelPresets = window.RTSInformationPanelPresets || {};

RTSInformationPanelPresets.toCssColor = value => {
  const raw = String(value || '').trim();
  return /^#[0-9a-f]{6,8}$/i.test(raw) ? `#${raw.slice(1, 7)}` : raw;
};

RTSInformationPanelPresets.loadFont = font => {
  const name = String(font || 'Inter').trim();
  if (!name) return;
  const id = `rts-panel-font-${name.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`;
  if (document.getElementById(id)) return;
  const link = document.createElement('link');
  link.id = id;
  link.rel = 'stylesheet';
  link.href = `https://fonts.googleapis.com/css2?family=${encodeURIComponent(name).replace(/%20/g, '+')}:wght@400;500;600;700;800&display=swap`;
  document.head.appendChild(link);
};

RTSInformationPanelPresets.apply = (panel, command) => {
  if (!panel) return;
  const preset = String(command?.replayPanelPreset || 'Broadcast').toLowerCase();
  const primary = RTSInformationPanelPresets.toCssColor(command?.replayPanelPrimaryColor || '#0384CBFF');
  const secondary = RTSInformationPanelPresets.toCssColor(command?.replayPanelSecondaryColor || '#101416FF');
  const titleFont = String(command?.replayPanelTitleFont || 'Inter').trim();
  const titleSize = Math.max(12, Number(command?.replayPanelTitleSize) || 24);
  const titleColor = RTSInformationPanelPresets.toCssColor(command?.replayPanelTitleColor || '#FFFFFFFF');
  const listSize = Math.max(8, Number(command?.replayPanelListSize) || 15);
  const listColor = RTSInformationPanelPresets.toCssColor(command?.replayPanelListColor || '#FFFFFFFF');
  const className = ['broadcast', 'cinematic', 'cut', 'minimal'].includes(preset) ? preset : 'broadcast';
  panel.classList.remove('panel-broadcast', 'panel-cinematic', 'panel-cut', 'panel-minimal');
  panel.classList.add(`panel-${className}`);
  panel.style.setProperty('--panel-primary', primary);
  panel.style.setProperty('--panel-secondary', secondary);
  panel.style.setProperty('--panel-title-font', `'${titleFont.replace(/'/g, '')}', system-ui, sans-serif`);
  panel.style.setProperty('--panel-title-size', `${titleSize}px`);
  panel.style.setProperty('--panel-title-color', titleColor);
  panel.style.setProperty('--panel-list-size', `${listSize}px`);
  panel.style.setProperty('--panel-list-color', listColor);
  RTSInformationPanelPresets.loadFont(titleFont);
};

const originalShow = window.RTSInformationPanelAnimation?.show;
if (originalShow) {
  window.RTSInformationPanelAnimation.show = (panel, command, name) => {
    RTSInformationPanelPresets.apply(panel, command);
    return originalShow(panel, command, name);
  };
}

window.RTSInformationPanelPresets = RTSInformationPanelPresets;
