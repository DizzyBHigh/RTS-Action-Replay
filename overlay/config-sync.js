(() => {
  const parse = value => {
    try { return typeof value === 'string' ? JSON.parse(value || '{}') : value; }
    catch (_) { return null; }
  };

  const list = value => Array.isArray(value) ? value : [];
  const object = value => value && typeof value === 'object' && !Array.isArray(value) ? value : {};
  const find = (items, id) => list(items).find(item =>
    String(item?.id || '').toLowerCase() === String(id || '').toLowerCase()
  ) || null;

  const positionsFor = (config, target) =>
    object(config?.presets?.positions?.[target]);

  const sequence = (raw, positions, fallback) =>
    list(raw).map(step => {
      const name = String(step?.position || fallback);
      const position = positions[name] || Object.values(positions).find(p =>
        String(p?.tag || '').toLowerCase() === name.toLowerCase()
      );
      return {
        position: String(name),
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
      const positions = positionsFor(config, target === 'clapperboard' ? 'clapperboard' : target);
      const fallback = target === 'panel' ? 'Centered' : 'Full Screen';
      return {
        id,
        name: profile?.name || id,
        start: sequence(stored.startSequence, positions, fallback),
        end: sequence(stored.endSequence, positions, fallback)
      };
    });
  };

  const branding = (config, id) => find(config?.presets?.branding, id) || find(config?.presets?.branding, 'default') || {};
  const visual = (config, id) => find(config?.presets?.visual, id) || find(config?.presets?.visual, 'broadcast') || {};
  const title = (config, id) => find(config?.presets?.title, id) || find(config?.presets?.title, 'default') || {};

  const applyPresentation = (command, config, entry) => {
    const designId = entry?.designPreset || entry?.visualPreset || 'broadcast';
    const titleId = entry?.titlePreset || 'default';
    const brandId = entry?.brandingPreset || 'default';
    const d = visual(config, designId);
    const t = title(config, titleId);
    const b = branding(config, brandId);

    command.replayPanelPreset = d.design || d.id || 'broadcast';
    command.replayPanelPrimaryColor = b.primaryColor || '#0384CBFF';
    command.replayPanelSecondaryColor = b.secondaryColor || '#101416FF';
    command.replayPanelTitleFont = b.font || 'Inter';
    command.replayPanelTitleSize = b.fontSize || 34;
    command.replayPanelTitleColor = b.textColor || '#FFFFFFFF';
    command.replayPanelListColor = b.textColor || '#FFFFFFFF';
    command.replayPanelListShadowColor = b.shadowColor || '#000000FF';
    command.replayPanelBackgroundColor = d.backgroundColor || b.secondaryColor || '#101416FF';

    command.replayShowTitle = t.showTitle !== false;
    command.replayTitleDecorationPosition = t.decorationPosition || 'Prefix';
    command.replayTitleDecoration = t.decoration || 'Action Replay -';
    command.replayTitlePosition = t.position || 'Bottom';
    command.replayTitleAnimation = t.animation || 'Left to right';
    command.replayTitleDelay = Number(t.delay) || 0;
    command.replayTitleDuration = Number(t.duration) || 0;
    command.replayTitleAnimationDuration = Number(t.animationDuration) || 1000;

    command.replayTitleFont = b.font || 'Inter';
    command.replayTitleFontSize = b.fontSize || 34;
    command.replayTitleTextColor = b.textColor || '#FFFFFFFF';
    command.replayTitleShadowColor = b.shadowColor || '#000000FF';
    command.replayTitlePrimaryColor = b.primaryColor || '#0384CBFF';
    command.replayTitleSecondaryColor = b.secondaryColor || '#101416FF';
    command.replayBrandLogoUrl = b.logo || '';
    command.replayBrandFallbackText = b.fallbackText || 'RTS';
    command.replayBrandLabel = b.brandLabel || 'ACTION REPLAY';
    command.replayBrandFallbackTextColor = b.primaryColor || '#0384CBFF';
    command.replayBrandLabelColor = b.textColor || '#FFFFFFFF';

    command.replayBroadcastPrimaryColor = b.primaryColor || '#0384CBFF';
    command.replayBroadcastSecondaryColor = b.secondaryColor || '#101416FF';
    command.replayBroadcastChevronHeight = d.chevronHeight ?? 42;
    command.replayBroadcastChevronWidth = d.chevronWidth ?? 42;
    command.replayBroadcastChevronSpacing = d.chevronSpacing ?? 0;
    command.replayBroadcastChevronSpeed = d.chevronSpeed ?? 95;
    command.replayCutPrimaryColor = b.primaryColor || '#0384CBFF';
    command.replayCutSecondaryColor = b.secondaryColor || '#101416FF';
    command.replayCutBackgroundColor = d.backgroundColor || '#101416FF';
    command.replayCutBlockWidth = d.blockWidth ?? 170;
    command.replayCutRandomWidth = d.randomWidth !== false;
    command.replayCutBarHeight = d.barHeight ?? 5;
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
    const selectedPlayerProfile = String(playerEntry.animationProfile || player.animation?.selectedProfile || 'default');
    const playerProfile = command.replayAnimationProfiles.find(p => p.id === selectedPlayerProfile) || command.replayAnimationProfiles[0];
    command.replayAnimationProfile = playerProfile ? JSON.stringify(playerProfile) : '';
    command.replayAnimationProfileId = playerProfile?.id || 'default';
    command.replayStartPosition = playerProfile?.start?.[0]?.position || 'full-screen';
    command.replayEndPosition = playerProfile?.end?.at(-1)?.position || command.replayStartPosition;

    const panelEntry = object(panel.entryPoints?.recent);
    const panelProfileId = String(panelEntry.animationProfile || panel.animation?.entryPoints?.recent || 'default');
    const panelProfile = command.replayPanelAnimationProfiles.find(p => p.id === panelProfileId) || command.replayPanelAnimationProfiles[0];
    command.replayPanelAnimation = panelProfile ? JSON.stringify(panelProfile) : '';
    command.replayPanelPosition = 'Centered';

    const clapperProfileId = String(clapper.animation?.selectedProfile || clapper.entryPoint?.animationProfile || 'default');
    const clapperProfile = command.replayClapperAnimationProfiles.find(p => p.id === clapperProfileId) || command.replayClapperAnimationProfiles[0];
    command.replayClapperAnimation = clapperProfile ? JSON.stringify(clapperProfile) : '';

    applyPresentation(command, config, playerEntry);

    command.replayShowControls = globals.showControls !== false;
    command.replayShowProgress = globals.showProgress !== false;
    command.replayPlaybackSpeed = Number(globals.playbackSpeed) || 1;
    command.replayPlaybackSpeedVisibility = globals.playbackSpeedVisibility || 'Only when greater or less than 1';
    command.replayFrameColor = globals.frameColor || '#0384CBFF';
    command.replayBorderGlow = globals.borderGlow !== false;
    command.replayBorderWidth = Number(globals.borderWidth) || 4;
    command.replayCornerRadius = Number(globals.cornerRadius) || 0;
    command.replayPanelWidth = Number(panel.width) || 500;
    command.replayPanelHeight = Number(panel.height) || 700;

    command.replayLogoUrl = globals.brandLogoUrl || command.replayBrandLogoUrl || '';
    command.replayClapperPosition = globals.clapperPosition || 'Centered';
    command.replayMessageBoardColor = globals.clapperBoardColor || '#101416';
    command.replayMessageTextColor = globals.clapperTextColor || '#0384CB';
    command.replayMessageStripeLight = globals.clapperStripeLight || '#EEEEEE';
    command.replayMessageStripeDark = globals.clapperStripeDark || '#111111';
    command.replayMessageAccent = globals.clapperAccent || '#0384CB';
    command.replayMessageFont = globals.clapperFont || 'Arial, sans-serif';
    command.replayMessage = globals.messagePlayText || 'CLAPPERBOARD ANIMATION TEST';

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
    window.RTSReplayVideo.applyPosition(
      window.RTSReplayVideo.getPosition(command.replayStartPosition),
      true
    );
    window.RTSReplayVideo.currentCommand = command;
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
