const RTSInformationPanelStyling = window.RTSInformationPanelStyling || {};

RTSInformationPanelStyling.defaults = {
  primary: '#0384CBFF',
  background: '#101416FF',
  text: '#FFFFFFFF',
  alternateRow: '#182127FF',
  panel: { backgroundEnabled: true, borderEnabled: true, borderWidth: 3, radius: 0, glowEnabled: true, glowStrength: 24 },
  header: { font: 'Inter', size: 24, weight: '800', height: 88 },
  list: { font: 'Inter', size: 15, weight: '600', spacing: 0, radius: 0 },
  accent: { enabled: true, glow: true }
};

const merge = (base, value) => ({ ...base, ...(value && typeof value === 'object' ? value : {}) });

const normaliseStyle = source => ({
  ...RTSInformationPanelStyling.defaults,
  ...(source && typeof source === 'object' ? source : {}),
  panel: merge(RTSInformationPanelStyling.defaults.panel, source?.panel),
  header: merge(RTSInformationPanelStyling.defaults.header, source?.header),
  list: merge(RTSInformationPanelStyling.defaults.list, source?.list),
  accent: merge(RTSInformationPanelStyling.defaults.accent, source?.accent)
});

RTSInformationPanelStyling.getStyle = command => {
  try {
    const raw = command?.replayPanelStyle;
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return normaliseStyle(parsed);
  } catch (_) {
    return normaliseStyle(null);
  }
};

RTSInformationPanelStyling.apply = (panel, command) => {
  if (!panel) return;
  const style = RTSInformationPanelStyling.getStyle(command);
  const set = (key, value) => panel.style.setProperty(key, String(value));

  set('--panel-primary', style.primary);
  set('--panel-background', style.background);
  set('--panel-text', style.text);
  set('--panel-alternate-row', style.alternateRow);
  set('--panel-border-width', `${Number(style.panel.borderWidth) || 0}px`);
  set('--panel-radius', `${Number(style.panel.radius) || 0}px`);
  set('--panel-glow-strength', `${Number(style.panel.glowStrength) || 0}px`);
  set('--panel-header-font', style.header.font);
  set('--panel-header-size', `${Number(style.header.size) || 24}px`);
  set('--panel-header-weight', style.header.weight);
  set('--panel-header-height', `${Number(style.header.height) || 88}px`);
  set('--panel-list-font', style.list.font);
  set('--panel-list-size', `${Number(style.list.size) || 15}px`);
  set('--panel-list-weight', style.list.weight);
  set('--panel-row-spacing', `${Number(style.list.spacing) || 0}px`);
  set('--panel-row-radius', `${Number(style.list.radius) || 0}px`);

  panel.classList.toggle('rts-panel-no-background', !style.panel.backgroundEnabled);
  panel.classList.toggle('rts-panel-no-border', !style.panel.borderEnabled);
  panel.classList.toggle('rts-panel-no-glow', !style.panel.glowEnabled || Number(style.panel.glowStrength) <= 0);
  panel.classList.toggle('rts-panel-no-chevron', !style.accent.enabled);
  panel.classList.toggle('rts-panel-accent-glow', !!style.accent.glow);
};

window.RTSInformationPanelStyling = RTSInformationPanelStyling;
