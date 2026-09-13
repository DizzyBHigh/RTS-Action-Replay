const RTSInformationPanelStyling = window.RTSInformationPanelStyling || {};

RTSInformationPanelStyling.defaults = {
  background: { enabled: true, primary: '#101416FF', secondary: '#0384CBFF' },
  border: { enabled: true, color: '#0384CBFF', width: 3, radius: 0 },
  glow: { enabled: true, color: '#0384CBFF', blur: 24, spread: 0 },
  header: { titleColor: '#FFFFFFFF', font: 'Inter', size: 24, weight: '800', shadowColor: '#000000FF', primary: '#0384CBFF', secondary: '#101416FF', angle: 135, height: 88 },
  list: { textColor: '#FFFFFFFF', secondaryTextColor: '#AAB4BAFF', font: 'Inter', size: 15, weight: '600', spacing: 0, primary: '#101416FF', secondary: '#0384CBFF', border: '#FFFFFF14', radius: 0, glowEnabled: false, glowColor: '#0384CBFF' },
  accent: { color: '#0384CBFF', chevron: true, glow: true }
};

const mergePanelStyle = source => {
  const merge = (base, value) => {
    const result = { ...base };
    if (!value || typeof value !== 'object') return result;
    Object.keys(base).forEach(key => {
      if (value[key] !== undefined) result[key] = value[key];
    });
    return result;
  };
  return {
    background: merge(RTSInformationPanelStyling.defaults.background, source?.background),
    border: merge(RTSInformationPanelStyling.defaults.border, source?.border),
    glow: merge(RTSInformationPanelStyling.defaults.glow, source?.glow),
    header: merge(RTSInformationPanelStyling.defaults.header, source?.header),
    list: merge(RTSInformationPanelStyling.defaults.list, source?.list),
    accent: merge(RTSInformationPanelStyling.defaults.accent, source?.accent)
  };
};

RTSInformationPanelStyling.getStyle = command => {
  try {
    const raw = command?.replayPanelStyle;
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return mergePanelStyle(parsed);
  } catch (_) { return mergePanelStyle(null); }
};

RTSInformationPanelStyling.apply = (panel, command) => {
  if (!panel) return;
  const style = RTSInformationPanelStyling.getStyle(command);
  const set = (key, value) => panel.style.setProperty(key, String(value));
  set('--panel-bg-primary', style.background.primary);
  set('--panel-bg-secondary', style.background.secondary);
  set('--panel-border-color', style.border.color);
  set('--panel-border-width', `${Number(style.border.width) || 0}px`);
  set('--panel-radius', `${Number(style.border.radius) || 0}px`);
  set('--panel-glow-color', style.glow.color);
  set('--panel-glow-blur', `${Number(style.glow.blur) || 0}px`);
  set('--panel-glow-spread', `${Number(style.glow.spread) || 0}px`);
  set('--panel-title-color', style.header.titleColor);
  set('--panel-title-font', style.header.font);
  set('--panel-title-size', `${Number(style.header.size) || 24}px`);
  set('--panel-title-weight', style.header.weight);
  set('--panel-title-shadow', style.header.shadowColor);
  set('--panel-header-primary', style.header.primary);
  set('--panel-header-secondary', style.header.secondary);
  set('--panel-header-angle', `${Number(style.header.angle) || 135}deg`);
  set('--panel-header-height', `${Number(style.header.height) || 88}px`);
  set('--panel-list-text', style.list.textColor);
  set('--panel-list-secondary-text', style.list.secondaryTextColor);
  set('--panel-list-font', style.list.font);
  set('--panel-list-size', `${Number(style.list.size) || 15}px`);
  set('--panel-list-weight', style.list.weight);
  set('--panel-row-spacing', `${Number(style.list.spacing) || 0}px`);
  set('--panel-row-primary', style.list.primary);
  set('--panel-row-secondary', style.list.secondary);
  set('--panel-row-border', style.list.border);
  set('--panel-row-radius', `${Number(style.list.radius) || 0}px`);
  set('--panel-row-glow-color', style.list.glowColor);
  set('--panel-accent-color', style.accent.color);
  panel.classList.toggle('rts-panel-no-background', !style.background.enabled);
  panel.classList.toggle('rts-panel-no-border', !style.border.enabled);
  panel.classList.toggle('rts-panel-no-glow', !style.glow.enabled);
  panel.classList.toggle('rts-panel-no-chevron', !style.accent.chevron);
  panel.classList.toggle('rts-panel-accent-glow', !!style.accent.glow);
  panel.classList.toggle('rts-panel-row-glow', !!style.list.glowEnabled);
};

window.RTSInformationPanelStyling = RTSInformationPanelStyling;
