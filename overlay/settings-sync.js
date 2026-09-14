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
  RTSReplaySettingsSync.command.replayPlayerPositions = settings.player?.positions ? JSON.stringify(settings.player.positions) : RTSReplaySettingsSync.command.replayPlayerPositions;
  RTSReplaySettingsSync.command.replayPanelPositions = settings.panel?.positions ? JSON.stringify(settings.panel.positions) : RTSReplaySettingsSync.command.replayPanelPositions;
  RTSReplaySettingsSync.command.replayPanelAnimation = settings.panel?.animation ? JSON.stringify(settings.panel.animation) : RTSReplaySettingsSync.command.replayPanelAnimation;

  if (RTSReplaySettingsSync.player?.classList.contains('show')) {
    window.RTSReplay.command = { ...(window.RTSReplay.command || {}), ...RTSReplaySettingsSync.command };
    window.RTSReplay.configureControls?.(window.RTSReplay.command);
    window.RTSReplay.configure?.(window.RTSReplay.command);
  }

  const panel = RTSReplaySettingsSync.recentList;
  if (panel?.classList.contains('show') && window.RTSReplay?.showRecentList) {
    const panelCommand = { ...(panel._rtsPanelAnimationCommand || {}), ...RTSReplaySettingsSync.command };
    panelCommand.replayPanelWidth = settings.panel?.width ?? panelCommand.replayPanelWidth;
    panelCommand.replayPanelHeight = settings.panel?.height ?? panelCommand.replayPanelHeight;
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
