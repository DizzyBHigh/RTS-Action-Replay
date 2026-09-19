(() => {
  const object = value => value && typeof value === 'object' && !Array.isArray(value) ? value : {};
  const list = value => Array.isArray(value) ? value : [];
  const positionsFor = (config, target) => object(config?.presets?.positions?.[target]);

  const sequence = (raw, positions, fallback) => list(raw).map(step => {
    const name = String(step?.position || fallback);
    return {
      position: name,
      duration: Number(step?.duration) || 0,
      delay: Number(step?.delay) || 0,
      easing: step?.easing || 'ease-in-out'
    };
  });

  const profilesFor = (config, target, profiles) => {
    const store = object(config?.animation?.[target]);
    return list(profiles).map(profile => {
      const id = String(profile?.id || 'default');
      const stored = object(store[id]);
      const targetName = target === 'clapperboard' ? 'clapperboard' : target;
      const positions = positionsFor(config, targetName);
      const fallback = target === 'panel' ? 'Centered' : 'Full Screen';
      return {
        id,
        name: profile?.name || id,
        start: sequence(stored.startSequence, positions, fallback),
        end: sequence(stored.endSequence, positions, fallback)
      };
    });
  };

  const buildCommand = config => {
    const player = object(config?.player);
    const panel = object(config?.panel);
    const clapper = object(config?.clapperboard);
    const globals = object(config?.globals);
    const command = { replayCommand: 'config-test' };

    command.replayPlayerPositions = JSON.stringify(positionsFor(config, 'player'));
    command.replayPanelPositions = JSON.stringify(positionsFor(config, 'panel'));
    command.replayClapperPositions = JSON.stringify(config?.clapperPositions || positionsFor(config, 'clapperboard'));

    command.replayAnimationProfiles = profilesFor(config, 'player', player.animationProfiles);
    command.replayPanelAnimationProfiles = profilesFor(config, 'panel', panel.animationProfiles);
    command.replayClapperAnimationProfiles = profilesFor(config, 'clapperboard', clapper.animationProfiles);

    const playerEntry = object(player.entryPoints?.play);
    const playerProfileId = String(playerEntry.animationProfile || player.animation?.selectedProfile || 'default');
    const playerProfile = command.replayAnimationProfiles.find(p => p.id === playerProfileId) || command.replayAnimationProfiles[0];
    command.replayAnimationProfile = playerProfile ? JSON.stringify(playerProfile) : '';
    command.replayAnimationProfileId = playerProfile?.id || 'default';
    command.replayStartPosition = playerProfile?.start?.[0]?.position || 'Full Screen';
    command.replayEndPosition = playerProfile?.end?.at(-1)?.position || command.replayStartPosition;

    const panelEntry = object(panel.entryPoints?.recent);
    const panelProfileId = String(panelEntry.animationProfile || panel.animation?.entryPoints?.recent || 'default');
    const panelProfile = command.replayPanelAnimationProfiles.find(p => p.id === panelProfileId) || command.replayPanelAnimationProfiles[0];
    command.replayPanelAnimation = panelProfile ? JSON.stringify(panelProfile) : '';
    command.replayPanelPosition = 'Centered';

    const clapperProfileId = String(clapper.animation?.selectedProfile || clapper.entryPoint?.animationProfile || 'default');
    const clapperProfile = command.replayClapperAnimationProfiles.find(p => p.id === clapperProfileId) || command.replayClapperAnimationProfiles[0];
    command.replayClapperAnimation = clapperProfile ? JSON.stringify(clapperProfile) : '';

    window.RTSOverlayConfigPresentation.apply(command, config, playerEntry);

    Object.assign(command, {
      replayShowControls: globals.showControls !== false,
      replayShowProgress: globals.showProgress !== false,
      replayPlaybackSpeed: Number(globals.playbackSpeed) || 1,
      replayPlaybackSpeedVisibility: globals.playbackSpeedVisibility || 'Only when greater or less than 1',
      replayFrameColor: globals.frameColor || '#0384CBFF',
      replayBorderGlow: globals.borderGlow !== false,
      replayBorderWidth: Number(globals.borderWidth) || 4,
      replayCornerRadius: Number(globals.cornerRadius) || 0,
      replayPanelWidth: Number(panel.width) || 500,
      replayPanelHeight: Number(panel.height) || 700,
      replayLogoUrl: globals.brandLogoUrl || command.replayBrandLogoUrl || '',
      replayClapperPosition: globals.clapperPosition || 'Centered',
      replayMessageBoardColor: globals.clapperBoardColor || '#101416',
      replayMessageTextColor: globals.clapperTextColor || '#0384CB',
      replayMessageStripeLight: globals.clapperStripeLight || '#EEEEEE',
      replayMessageStripeDark: globals.clapperStripeDark || '#111111',
      replayMessageAccent: globals.clapperAccent || '#0384CB',
      replayMessageFont: globals.clapperFont || 'Arial, sans-serif',
      replayMessage: globals.messagePlayText || 'CLAPPERBOARD ANIMATION TEST'
    });

    return command;
  };

  const apply = config => {
    if (!config || typeof config !== 'object') return;
    window.rtsOverlayConfig = config;
    const command = buildCommand(config);
    window.RTSReplaySettingsSync = { config, command };
    window.RTSReplay.command = command;
    window.RTSReplayVideo.currentCommand = command;
    window.RTSReplayVideo.configurePositions(command.replayPlayerPositions);
    window.RTSReplayVideo.applyPosition(window.RTSReplayVideo.getPosition(command.replayStartPosition), true);
    window.RTSReplayElements?.configure?.(command);
    window.RTSReplayControls?.configure?.(command);
    window.RTSInformationPanels?.applySize?.(window.RTSReplay.recentList, command);
    window.RTSDevToolbar?.refresh?.();
    window.RTSDevToolbar?.log?.('Overlay configuration applied to controls', {
      playerPositions: Object.keys(JSON.parse(command.replayPlayerPositions || '{}')).length,
      playerProfiles: command.replayAnimationProfiles.length,
      panelProfiles: command.replayPanelAnimationProfiles.length,
      clapperProfiles: command.replayClapperAnimationProfiles.length,
      panelWidth: command.replayPanelWidth,
      panelHeight: command.replayPanelHeight,
      playbackSpeed: command.replayPlaybackSpeed
    });
  };

  window.addEventListener('rts-overlay-config', event => apply(event.detail));
  window.RTSOverlayConfigSync = { apply, buildCommand };
})();
