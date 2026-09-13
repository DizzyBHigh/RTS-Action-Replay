const RTSInformationPanelStyling = window.RTSInformationPanelStyling || {};

RTSInformationPanelStyling.defaults = {
  primary: '#0384CBFF', background: '#101416FF', text: '#FFFFFFFF', alternateRow: '#182127FF',
  panel: { backgroundEnabled: true, borderEnabled: true, borderWidth: 3, radius: 0, glowEnabled: true, glowStrength: 24 },
  header: { font: 'Inter', size: 24, weight: '800', height: 88 },
  list: { font: 'Inter', size: 15, weight: '600', spacing: 0, radius: 0 },
  accent: { enabled: true, glow: true }
};

const mergePanelStyle = source => {
  const merge = (base, value) => ({ ...base, ...(value && typeof value === 'object' ? value : {}) });
  return {
    ...RTSInformationPanelStyling.defaults,
    ...(source && typeof source === 'object' ? source : {}),
    panel: merge(RTSInformationPanelStyling.defaults.panel, source?.panel),
    header: merge(RTSInformationPanelStyling.defaults.header, source?.header),
    list: merge(RTSInformationPanelStyling.defaults.list, source?.list),
    accent: merge(RTSInformationPanelStyling.defaults.accent, source?.accent)
  };
};

RTSInformationPanelStyling.getStyle = command => {
  try {
    const raw = command?.replayPanelStyle;
    return mergePanelStyle(typeof raw === 'string' ? JSON.parse(raw) : raw);
  } catch (_) { return mergePanelStyle(null); }
};

RTSInformationPanelStyling.apply = (panel, command) => {
  if (!panel) return;
  const style = RTSInformationPanelStyling.getStyle(command);
  const set = (key, value) => panel.style.setProperty(key, String(value));
  set('--panel-primary', style.primary);
  set('--panel-bg-primary', style.background);
  set('--panel-bg-secondary', style.background);
  set('--panel-border-color', style.primary);
  set('--panel-border-width', `${Number(style.panel.borderWidth) || 0}px`);
  set('--panel-radius', `${Number(style.panel.radius) || 0}px`);
  set('--panel-glow-color', style.primary);
  set('--panel-glow-blur', `${Number(style.panel.glowStrength) || 0}px`);
  set('--panel-glow-spread', `${Math.round((Number(style.panel.glowStrength) || 0) / 8)}px`);
  set('--panel-title-color', style.text);
  set('--panel-title-font', style.header.font);
  set('--panel-title-size', `${Number(style.header.size) || 24}px`);
  set('--panel-title-weight', style.header.weight);
  set('--panel-title-shadow', '#000000FF');
  set('--panel-header-primary', style.primary);
  set('--panel-header-secondary', style.background);
  set('--panel-header-angle', '135deg');
  set('--panel-header-height', `${Number(style.header.height) || 88}px`);
  set('--panel-list-text', style.text);
  set('--panel-list-font', style.list.font);
  set('--panel-list-size', `${Number(style.list.size) || 15}px`);
  set('--panel-list-weight', style.list.weight);
  set('--panel-row-spacing', `${Number(style.list.spacing) || 0}px`);
  set('--panel-row-primary', style.background);
  set('--panel-row-secondary', style.alternateRow);
  set('--panel-row-radius', `${Number(style.list.radius) || 0}px`);
  set('--panel-row-border', `${style.primary}33`);
  set('--panel-row-glow-color', style.primary);
  set('--panel-accent-color', style.primary);
  panel.classList.toggle('rts-panel-no-background', !style.panel.backgroundEnabled);
  panel.classList.toggle('rts-panel-no-border', !style.panel.borderEnabled);
  panel.classList.toggle('rts-panel-no-glow', !style.panel.glowEnabled);
  panel.classList.toggle('rts-panel-no-chevron', !style.accent.enabled);
  panel.classList.toggle('rts-panel-accent-glow', !!style.accent.glow);
};

window.RTSInformationPanelStyling = RTSInformationPanelStyling;
