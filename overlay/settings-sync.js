const RTSReplaySettingsSync = window.RTSReplay;

RTSReplaySettingsSync.parse = value => {
  try {
    const parsed = typeof value === 'string' ? JSON.parse(value) : value;
    return parsed && typeof parsed === 'object' ? parsed : null;
  } catch (_) { return null; }
};

const profileCommand = (config, selectedId) => {
  const profiles = Array.isArray(config?.animationProfiles) ? config.animationProfiles : [];
  const selected = profiles.find(item => item?.id === selectedId) || profiles[0];
  if (!selected) return null;
  return {
    id: selected.id || 'default',
    name: selected.name || 'Default',
    start: selected.startSequence || selected.start || [],
    end: selected.endSequence || selected.end || []
  };
};

const json = value => value == null ? value : JSON.stringify(value);

RTSReplaySettingsSync.apply = command => {
  const settings = RTSReplaySettingsSync.parse(command?.replaySettings);
  if (!settings) return false;
  RTSReplaySettingsSync.settings = settings;
  const synced = settings.command && typeof settings.command === 'object' ? settings.command : {};
  RTSReplaySettingsSync.command = { ...synced };
  RTSReplaySettingsSync.playerConfig = settings.player || null;
  RTSReplaySettingsSync.panelConfig = settings.panel || null;
  RTSReplaySettingsSync.clapperConfig = settings.clapper || null;

  const player = settings.player;
  const panel = settings.panel;
  const clapper = settings.clapper;
  if (Array.isArray(player?.animationProfiles)) RTSReplaySettingsSync.command.replayAnimationProfiles = player.animationProfiles;
  if (player?.positions) RTSReplaySettingsSync.command.replayPlayerPositions = json(player.positions);
  if (Array.isArray(panel?.animationProfiles)) RTSReplaySettingsSync.command.replayPanelAnimationProfiles = panel.animationProfiles;
  if (panel?.positions) RTSReplaySettingsSync.command.replayPanelPositions = json(panel.positions);
  if (panel?.animation) RTSReplaySettingsSync.command.replayPanelAnimation = json(panel.animation);
  if (Array.isArray(clapper?.animationProfiles)) RTSReplaySettingsSync.command.replayClapperAnimationProfiles = clapper.animationProfiles;
  if (clapper?.positions) RTSReplaySettingsSync.command.replayClapperPositions = json(clapper.positions);

  const selectedClapper = profileCommand(clapper, clapper?.animation?.selectedProfile);
  if (selectedClapper) RTSReplaySettingsSync.command.replayClapperAnimation = json(selectedClapper);

  const selectedPlayer = profileCommand(player, player?.animation?.selectedProfile);
  if (selectedPlayer) RTSReplaySettingsSync.command.replayAnimationProfile = json(selectedPlayer);

  if (Number.isFinite(Number(RTSReplaySettingsSync.command.replayMessageDuration))) {
    RTSReplay.config.messageDuration = Number(RTSReplaySettingsSync.command.replayMessageDuration);
  }

  if (RTSReplaySettingsSync.player?.classList.contains('show')) {
    window.RTSReplay.command = { ...(window.RTSReplay.command || {}), ...RTSReplaySettingsSync.command };
    window.RTSReplay.configureControls?.(window.RTSReplay.command);
    window.RTSReplay.configure?.(window.RTSReplay.command);
  }

  const panelElement = RTSReplaySettingsSync.recentList;
  if (panelElement?.classList.contains('show') && window.RTSReplay?.showRecentList) {
    const panelCommand = { ...(panelElement._rtsPanelAnimationCommand || {}), ...RTSReplaySettingsSync.command };
    panelCommand.replayPanelWidth = panel?.width ?? panelCommand.replayPanelWidth;
    panelCommand.replayPanelHeight = panel?.height ?? panelCommand.replayPanelHeight;
    panelElement._rtsPanelAnimationCommand = panelCommand;
    window.RTSReplay.showRecentList(panelCommand);
  }

  window.RTSDevToolbar?.refresh?.();
  window.RTSDevToolbar?.log?.('Overlay settings synced', {
    playerProfiles: player?.animationProfiles?.length || 0,
    panelProfiles: panel?.animationProfiles?.length || 0,
    clapperProfiles: clapper?.animationProfiles?.length || 0
  });
  return true;
};

window.RTSReplaySettingsSync = RTSReplaySettingsSync;
