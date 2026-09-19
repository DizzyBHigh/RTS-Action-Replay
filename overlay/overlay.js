const RTSReplayOverlay = window.RTSReplay;

RTSReplayOverlay.handleEvent = message => {
  if (message?.event?.source !== 'Custom' || message?.event?.type !== 'Event') return;
  const data = message.data;
  if (data?.eventName !== RTSReplayOverlay.config.eventName || !data.args) return;
  const args = data.args;
  window.RTSDevToolbar?.log?.('Custom replay event accepted', {
    eventName: data.eventName,
    replayCommand: args?.replayCommand,
    replayId: args?.replayId,
    queueEntryId: args?.replayQueueEntryId
  });
  if (args.replayCommand === 'load') {
    let positions = null;
    const rawPositions = args.replayPlayerPositions ?? args.replayPositions;
    try { positions = typeof rawPositions === 'string' ? JSON.parse(rawPositions) : rawPositions; } catch (_) {}
    const positionKeys = positions && typeof positions === 'object' ? Object.keys(positions) : [];
    window.RTSDevToolbar?.log?.('Player position payload received', {
      present: rawPositions != null,
      type: typeof rawPositions,
      length: typeof rawPositions === 'string' ? rawPositions.length : 0,
      count: positionKeys.length,
      keys: positionKeys,
      requested: ['mini-right-hidden', 'mr-angled', 'center-large'].map(tag => {
        const key = positionKeys.find(k =>
          k.toLowerCase() === tag ||
          String(positions[k]?.tag || '').trim().toLowerCase() === tag
        );
        return key ? { tag, key, position: positions[key] } : { tag, missing: true };
      })
    });
    let profile = null;
    try { profile = typeof args.replayAnimationProfile === 'string' ? JSON.parse(args.replayAnimationProfile) : args.replayAnimationProfile; } catch (_) {}
    window.RTSDevToolbar?.log?.('Replay load payload', {
      animationProfileId: args.replayAnimationProfileId || profile?.id || '<missing>',
      animationName: profile?.name || '<missing>',
      startSteps: Array.isArray(profile?.start) ? profile.start.length : 0,
      endSteps: Array.isArray(profile?.end) ? profile.end.length : 0,
      brandingLogo: args.replayBrandLogoUrl || '<none>',
      brandingFallback: args.replayBrandFallbackText || '<missing>',
      brandingLabel: args.replayBrandLabel || '<missing>',
      brandingFallbackColor: args.replayBrandFallbackTextColor || '<missing>',
      brandingLabelColor: args.replayBrandLabelColor || '<missing>'
    });
  }
  if (args.replayCommand === 'avatar-response') {
    window.RTSSearchPanel?.handleAvatar?.(args);
    return;
  }
  if (args.replayCommand === 'search-panel') {
    window.RTSSearchPanel?.handle?.(args);
    return;
  }
  if (args.replayCommand === 'playlist-panel' || (args.replayCommand === 'message' && args.replayPlaylist)) {
    window.RTSPlaylistList?.handle?.(args);
    return;
  }
  if (args.replayCommand === 'leaderboard-panel') {
    window.RTSLeaderboardList?.handle?.(args);
    return;
  }
  RTSReplayOverlay.handleReplayCommand(args);
  if (args.replayCommand === 'recent-list') RTSReplayOverlay.showRecentList?.(args);
};

window.rtsOverlay = RTSReplayOverlay.config;
window.testReplay = url => RTSReplayOverlay.loadReplay({ replayUrl: url, replayAutoplay: false });
window.testMessage = text => RTSReplayOverlay.showMessage({ replayMessage: text });
RTSReplayOverlay.connect();
