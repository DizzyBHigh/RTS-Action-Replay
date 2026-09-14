const RTSReplaySettingsSync = window.RTSReplay;

RTSReplaySettingsSync.parse = value => {
  try {
    const parsed = typeof value === 'string' ? JSON.parse(value) : value;
    return parsed && typeof parsed === 'object' ? parsed : null;
  } catch (_) { return null; }
};

RTSReplaySettingsSync.apply = command => {
  const settings = RTSReplaySettingsSync.parse(command?.replaySettings);
  if (!settings) return false;
  RTSReplaySettingsSync.settings = settings;
  const synced = settings.command && typeof settings.command === 'object' ? settings.command : {};
  RTSReplaySettingsSync.command = { ...(RTSReplaySettingsSync.command || {}), ...synced };
  RTSReplaySettingsSync.playerConfig = settings.player || null;
  RTSReplaySettingsSync.panelConfig = settings.panel || null;

  if (RTSReplaySettingsSync.player?.classList.contains('show')) {
    RTSReplaySettingsSync.currentCommand = { ...(RTSReplaySettingsSync.currentCommand || {}), ...synced };
    window.RTSReplayControls?.configure?.(RTSReplaySettingsSync.currentCommand);
    window.RTSReplayElements?.configure?.(RTSReplaySettingsSync.currentCommand);
  }

  const panel = RTSReplaySettingsSync.recentList;
  if (panel?.classList.contains('show') && window.RTSReplay?.showRecentList) {
    const panelCommand = { ...(panel._rtsPanelAnimationCommand || {}), ...synced };
    if (settings.panel?.width) panelCommand.replayPanelWidth = settings.panel.width;
    if (settings.panel?.height) panelCommand.replayPanelHeight = settings.panel.height;
    if (settings.panel?.positions) panelCommand.replayPanelPositions = JSON.stringify(settings.panel.positions);
    panel._rtsPanelAnimationCommand = panelCommand;
    window.RTSReplay.showRecentList(panelCommand);
  }

  window.RTSDevToolbar?.log?.('Overlay settings synced', {
    playerProfiles: settings.player?.animationProfiles?.length || 0,
    panelProfiles: settings.panel?.animationProfiles?.length || 0
  });
  return true;
};

window.RTSReplaySettingsSync = RTSReplaySettingsSync;
