(() => {
  const object = value => value && typeof value === 'object' && !Array.isArray(value) ? value : {};
  const list = value => Array.isArray(value) ? value : [];
  const findPreset = (presets, id) => list(presets).find(preset => String(preset?.id || '') === String(id || ''));
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
      const fallback = target === 'panel' || target === 'message' ? 'Centered' : 'Full Screen';
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
    const message = object(config?.message);
    const messageBrand = findPreset(config?.presets?.branding, message.entryPoint?.brandingPreset || 'default');
    const globals = object(config?.globals);
    const command = { replayCommand: 'config-test' };

    command.replayPlayerPositions = JSON.stringify(positionsFor(config, 'player'));
    command.replayPanelPositions = JSON.stringify(positionsFor(config, 'panel'));
    command.replayClapperPositions = JSON.stringify(positionsFor(config, 'clapperboard'));
    command.replayMessagePositions = JSON.stringify(positionsFor(config, 'message'));

    command.replayAnimationProfiles = profilesFor(config, 'player', player.animationProfiles);
    command.replayPanelAnimationProfiles = profilesFor(config, 'panel', panel.animationProfiles);
    command.replayClapperAnimationProfiles = profilesFor(config, 'clapperboard', clapper.animationProfiles);
    command.replayMessageAnimationProfiles = profilesFor(config, 'message', message.animationProfiles);

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
    const messageProfileId = String(message.animation?.selectedProfile || message.entryPoint?.animationProfile || 'default');
    const messageProfile = command.replayMessageAnimationProfiles.find(p => p.id === messageProfileId) || command.replayMessageAnimationProfiles[0];
    command.replayMessageAnimation = messageProfile ? JSON.stringify(messageProfile) : '';

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
      replayPanelWidth: Number(globals.panelWidth) || Number(panel.width) || 500,
      replayPanelHeight: Number(globals.panelHeight) || Number(panel.height) || 700,
      replayPanelCornerRadius: Number(globals.panelCornerRadius) || Number(panel.cornerRadius) || 0,
      replayMessageMinWidth: Number(globals.messageMinWidth) || Number(message.minWidth) || 500,
      replayMessageMinHeight: Number(globals.messageMinHeight) || Number(message.minHeight) || 120,
      replayMessageCornerRadius: Number(globals.messageCornerRadius) || Number(message.cornerRadius) || 0,
      replayMessageDuration: Number(globals.messageDuration) || 5000,
      replayClapperDuration: Number(globals.clapperDuration) || 5000,
      replayLogoUrl: globals.brandLogoUrl || command.replayBrandLogoUrl || '',
      replayMessageBoardColor: messageBrand?.textColor || '#FFFFFFFF',
      replayMessageTextColor: messageBrand?.titleColor || '#FFFFFFFF',
      replayMessageStripeLight: messageBrand?.primaryColor || '#0384CBFF',
      replayMessageStripeDark: messageBrand?.secondaryColor || '#101416FF',
      replayMessageAccent: messageBrand?.shadowColor || '#000000FF',
      replayMessageFont: messageBrand?.font || 'Inter',
      replayMessage: globals.messagePlayText || 'MESSAGE PREVIEW'
    });

    return command;
  };

  const apply = config => {
    if (!config || typeof config !== 'object') return;
    window.rtsOverlayConfig = config;
    const command = buildCommand(config);
    window.RTSReplaySettingsSync = { config, command };
    window.RTSReplay.command = command;
    window.RTSReplay.currentCommand = command;
    window.RTSReplay.configurePositions(command.replayPlayerPositions);
    window.RTSReplay.applyPosition(window.RTSReplay.getPosition(command.replayStartPosition), true);
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
