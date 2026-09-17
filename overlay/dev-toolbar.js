(() => {
  const params = new URLSearchParams(window.location.search);
  if (params.get('dev') !== 'true') return;

  const screen = document.createElement('div');
  screen.id = 'rts-dev-screen';
  screen.innerHTML = '<div class="dev-screen-label">1920 × 1080</div><div class="dev-safe-area"></div>';
  document.body.prepend(screen);

  const bar = document.createElement('aside');
  bar.id = 'rts-dev-toolbar';
  bar.innerHTML = `
    <div class="dev-toolbar-header"><strong>RTS DEV</strong><span>DEV CONTROLS</span></div>
    <section class="dev-section"><h3>Player</h3>
      <div class="dev-grid"><button data-action="player">Show Player</button><button data-action="player-profile">Play Profile</button></div>
      <div class="dev-grid"><button data-action="player-profile-hide">Play End</button><button data-action="position-test">Test Position</button></div>
      <label>Animation Profile<select id="rts-dev-player-profile"></select></label>
      <div class="dev-grid"><label>From<select id="rts-dev-from"></select></label><label>To<select id="rts-dev-to"></select></label></div>
      <div class="dev-grid"><label>Easing<select id="rts-dev-easing"><option value="linear">Linear</option><option value="ease-in">Ease In</option><option value="ease-out">Ease Out</option><option value="ease-in-out" selected>Ease In Out</option><option value="ease">Ease</option></select></label><label>Duration<input id="rts-dev-duration" type="number" min="0.1" max="10" step="0.1" value="1"></label></div>
      <label>Title Style<select id="rts-dev-title-style"><option value="broadcast">Broadcast</option><option value="cinematic">Cinematic</option><option value="cut">Cut</option><option value="minimal">Minimal</option></select></label>
      <label>Title Position<select id="rts-dev-title-position"><option value="top">Top</option><option value="bottom" selected>Bottom</option></select></label>
      <label>Playback Speed<select id="rts-dev-speed"><option value="0.25">0.25×</option><option value="0.5">0.5×</option><option value="0.75">0.75×</option><option value="1" selected>1×</option><option value="1.25">1.25×</option><option value="1.5">1.5×</option><option value="1.75">1.75×</option><option value="2">2×</option></select></label>
      <label>Preview Title<input id="rts-dev-title" value="FIRST TEST — REPLAY CAPTURE"></label><button data-action="title">Preview Title</button>
    </section>
    <section class="dev-section"><h3>Panel</h3>
      <div class="dev-grid"><button data-action="panel">Show Panel</button><button data-action="panel-profile">Play Profile</button></div>
      <button data-action="panel-profile-hide">Play End</button>
      <label>Animation Profile<select id="rts-dev-panel-profile"></select></label>
      <label>Panel Style<select id="rts-dev-panel-style"><option value="broadcast">Broadcast</option><option value="cinematic">Cinematic</option><option value="cut">Cut</option><option value="minimal">Minimal</option></select></label>
      <label>Panel Position<select id="rts-dev-panel-position"></select></label>
    </section>
    <section class="dev-section"><h3>Clapperboard</h3>
      <div class="dev-grid"><button data-action="clapper">Show Clapperboard</button><button data-action="clapper-profile">Play Profile</button></div>
      <button data-action="clapper-profile-hide">Play End</button>
      <label>Animation Profile<select id="rts-dev-clapper-profile"></select></label>
    </section>
    <div class="dev-toolbar-footer"><span>?dev=true</span><button data-action="refresh">Refresh Config</button></div>`;
  document.body.prepend(bar);

  let playerVisible = false, panelVisible = false, clapperVisible = false;
  const formatDuration = seconds => { if (!Number.isFinite(seconds) || seconds < 0) return '—'; const total = Math.round(seconds); return `${Math.floor(total / 60)}:${String(total % 60).padStart(2, '0')}`; };
  const currentCommand = () => ({ ...(window.RTSReplaySettingsSync?.command || RTSReplayVideo?.currentCommand || RTSReplay?.command || {}) });
  const parse = value => { try { return typeof value === 'string' ? JSON.parse(value || '{}') : value; } catch (_) { return null; } };
  const profiles = key => { const list = parse(currentCommand()[key]); return Array.isArray(list) ? list : []; };
  const selectedProfile = (list, select) => list.find(item => item?.id === select.value) || list[0];
  const profileCommand = (profile, base) => profile ? { ...base, id: profile.id, name: profile.name, start: profile.startSequence || profile.start || [], end: profile.endSequence || profile.end || [] } : base;
  const fillProfiles = (id, key) => { const select = document.getElementById(id), list = profiles(key), old = select.value; select.replaceChildren(...list.map(item => new Option(item.name || item.id || 'Default', item.id || 'default'))); if (list.some(item => item.id === old)) select.value = old; else if (list[0]) select.value = list[0].id; };
  const setProfile = (command, selectId, profilesKey, outputKey) => { const profile = selectedProfile(profiles(profilesKey), document.getElementById(selectId)); command[outputKey] = JSON.stringify(profileCommand(profile, command)); return command; };
  const refreshProfiles = () => { fillProfiles('rts-dev-player-profile', 'replayAnimationProfiles'); fillProfiles('rts-dev-panel-profile', 'replayPanelAnimationProfiles'); fillProfiles('rts-dev-clapper-profile', 'replayClapperAnimationProfiles'); };
  const refreshPanelPositions = () => { const select = document.getElementById('rts-dev-panel-position'), positions = parse(currentCommand().replayPanelPositions) || {}, names = Object.keys(positions); select.replaceChildren(...names.map(name => new Option(name, name))); if (names.length) select.value = currentCommand().replayPanelPosition && names.includes(currentCommand().replayPanelPosition) ? currentCommand().replayPanelPosition : names[0]; };
  const refreshPositions = () => {
    const command = currentCommand(), positions = parse(command.replayPlayerPositions) || RTSReplayVideo?.getPositions?.() || RTSReplay?.getPositions?.() || {}, names = Object.keys(positions);
    ['rts-dev-from', 'rts-dev-to'].forEach(id => { const select = document.getElementById(id), old = select.value; select.replaceChildren(...names.map(name => new Option(name, name))); if (names.includes(old)) select.value = old; });
    if (command.replayStartPosition && names.includes(command.replayStartPosition)) document.getElementById('rts-dev-from').value = command.replayStartPosition;
    if (command.replayEndPosition && names.includes(command.replayEndPosition)) document.getElementById('rts-dev-to').value = command.replayEndPosition;
    const duration = Number(command.replayAnimationDuration); if (Number.isFinite(duration)) document.getElementById('rts-dev-duration').value = duration < 10 ? duration : duration / 1000;
    if (command.replayAnimationEasing) document.getElementById('rts-dev-easing').value = command.replayAnimationEasing;
    refreshPanelPositions();
  };
  const updateClapper = command => { const current = command || currentCommand(); const text = document.getElementById('message-text'); if (text) text.textContent = current.replayMessage || `Playing replay #${current.replayNumber || 1}: ${current.replayTitle || 'FIRST TEST'}.`; const length = document.getElementById('clapper-length'); if (length) length.textContent = formatDuration(RTSReplay?.video?.duration); const director = document.getElementById('clapper-director'); if (director) director.textContent = current.replayDirector || '—'; const played = document.getElementById('clapper-played'); if (played) played.textContent = current.replayPlayedCount ?? '—'; };
  const refresh = () => { refreshProfiles(); refreshPositions(); };
  window.RTSDevToolbar = { updateClapper, refreshPositions, refresh, log: window.RTSDevToolbar?.log };

  const showPlayer = () => { const player = RTSReplay?.player; if (!player) return; playerVisible = true; player.classList.add('dev-player', 'show'); player.style.opacity = '1'; player.style.visibility = 'visible'; RTSReplay.frame?.classList.add('dev-frame'); bar.querySelector('[data-action="player"]').textContent = 'Hide Player'; };
  const hidePlayer = () => { const player = RTSReplay?.player; if (!player) return; playerVisible = false; RTSReplayAnimation.cancelSequence(); player.classList.remove('show', 'dev-player'); player.style.opacity = ''; player.style.visibility = ''; RTSReplay.frame?.classList.remove('dev-frame'); bar.querySelector('[data-action="player"]').textContent = 'Show Player'; };
  const applyPlayerProfile = () => { const command = setProfile(currentCommand(), 'rts-dev-player-profile', 'replayAnimationProfiles', 'replayAnimationProfile'); RTSReplayVideo.currentCommand = command; RTSReplay.command = command; RTSReplayControls.configure(command); RTSReplayElements.configure(command); return command; };
  const playPlayerProfile = () => { const command = applyPlayerProfile(), profile = parse(command.replayAnimationProfile); showPlayer(); if (profile?.start?.length) RTSReplayAnimation.runSequence(profile.start); };
  const playPlayerEnd = () => { const command = applyPlayerProfile(), profile = parse(command.replayAnimationProfile); showPlayer(); if (profile?.end?.length) RTSReplayAnimation.runEndSequence(profile.end, hidePlayer); };
  const previewTitle = () => { showPlayer(); const title = RTSReplay?.title; if (!title) return; RTSReplay.hideTitle?.(); const text = document.getElementById('rts-dev-title').value.trim() || 'FIRST TEST — REPLAY CAPTURE'; const position = document.getElementById('rts-dev-title-position').value; title.className = `title-${document.getElementById('rts-dev-title-style').value} title-${position}`; title.textContent = text; title.classList.add('visible', 'title-enter'); };
  const setSpeed = value => { const speed = Number(value); if (!Number.isFinite(speed) || !RTSReplay?.video) return; RTSReplay.video.playbackRate = speed; const command = { ...currentCommand(), replayPlaybackSpeed: speed }; RTSReplay.command = command; RTSReplayVideo.currentCommand = command; RTSReplayElements?.configureSpeed?.(command); };
  const testPosition = () => { const command = applyPlayerProfile(), from = document.getElementById('rts-dev-from').value, to = document.getElementById('rts-dev-to').value, duration = Math.max(0.1, Number(document.getElementById('rts-dev-duration').value) || 1), easing = document.getElementById('rts-dev-easing').value; command.replayStartPosition = from; command.replayEndPosition = to; command.replayAnimationDuration = duration; command.replayAnimationEasing = easing; RTSReplayVideo.currentCommand = command; RTSReplay.command = command; showPlayer(); RTSReplayVideo.animateIn(RTSReplayVideo.getPosition(from), RTSReplayVideo.getPosition(to)); };

  const showClapper = () => { const command = setProfile(currentCommand(), 'rts-dev-clapper-profile', 'replayClapperAnimationProfiles', 'replayClapperAnimation'); RTSReplay.command = command; RTSReplayVideo.currentCommand = command; RTSReplayMessages.applyMessageStyle(command); RTSReplayMessages.messageText.textContent = command.replayMessage || 'CLAPPERBOARD ANIMATION TEST'; clapperVisible = true; updateClapper(command); RTSReplayClapperAnimation.show(command); bar.querySelector('[data-action="clapper"]').textContent = 'Hide Clapperboard'; };
  const hideClapper = () => { const command = currentCommand(); clapperVisible = false; RTSReplayClapperAnimation.hide(command); bar.querySelector('[data-action="clapper"]').textContent = 'Show Clapperboard'; };
  const playClapperEnd = () => hideClapper();

  const buildPanelCommand = () => { const command = setProfile(currentCommand(), 'rts-dev-panel-profile', 'replayPanelAnimationProfiles', 'replayPanelAnimation'); command.replayRecent = 'dev-preview'; command.replayRecentData = JSON.stringify(Array.from({ length: 8 }, (_, i) => ({ number: String(i + 1).padStart(2, '0'), title: ['FIRST TEST — REPLAY CAPTURE', 'GTA V — ACTION REPLAY', 'CINEMATIC DRIVE', 'NIGHT SHIFT', 'HIGHWAY RUN', 'MISSION COMPLETE', 'STREAM HIGHLIGHT', 'LAST CALL'][i], avatarUrl: '' }))); command.replayPanelPreset = document.getElementById('rts-dev-panel-style').value; command.replayPanelPosition = document.getElementById('rts-dev-panel-position').value || command.replayPanelPosition || 'Centered'; return command; };
  const showPanel = () => { const panel = RTSReplay?.recentList; if (!panel || !RTSReplay?.showRecentList) return; const command = buildPanelCommand(); panel._rtsPanelAnimationCommand = command; RTSReplay.showRecentList(command); panelVisible = true; bar.querySelector('[data-action="panel"]').textContent = 'Hide Panel'; };
  const informationPanels = () => [RTSReplay?.recentList, document.getElementById('search-panel'), document.getElementById('playlist-list'), document.getElementById('leaderboard-list')].filter(Boolean);
  const visiblePanel = () => informationPanels().find(panel => panel.classList.contains('show')) || RTSReplay?.recentList;
  const hidePanel = () => { const panel = visiblePanel(); if (!panel) return; panelVisible = false; RTSInformationPanelAnimation.cancel(); panel.classList.remove('show'); panel.setAttribute('aria-hidden', 'true'); bar.querySelector('[data-action="panel"]').textContent = 'Show Panel'; };
  const playPanelProfile = () => { const panel = RTSReplay?.recentList; if (!panel) return; const command = buildPanelCommand(); panel._rtsPanelAnimationCommand = command; panel.classList.add('show'); panel.setAttribute('aria-hidden', 'false'); RTSInformationPanelAnimation.show(panel, command, command.replayPanelPosition); panelVisible = true; };
  const playPanelEnd = () => { const panel = RTSReplay?.recentList; if (!panel) return; RTSInformationPanels.hide(panel, panel._rtsPanelAnimationCommand || buildPanelCommand()); panelVisible = false; };

  bar.addEventListener('click', event => { const button = event.target.closest('button'); if (!button) return; const action = button.dataset.action; if (action === 'player') playerVisible ? hidePlayer() : (showPlayer(), applyPlayerProfile()); if (action === 'player-profile') playPlayerProfile(); if (action === 'player-profile-hide') playPlayerEnd(); if (action === 'title') previewTitle(); if (action === 'position-test') testPosition(); if (action === 'clapper') clapperVisible ? hideClapper() : showClapper(); if (action === 'clapper-profile') showClapper(); if (action === 'clapper-profile-hide') playClapperEnd(); if (action === 'panel') informationPanels().some(panel => panel.classList.contains('show')) ? hidePanel() : showPanel(); if (action === 'panel-profile') playPanelProfile(); if (action === 'panel-profile-hide') playPanelEnd(); if (action === 'refresh') refresh(); });
  bar.querySelector('#rts-dev-title-style').addEventListener('change', previewTitle);
  bar.querySelector('#rts-dev-title-position').addEventListener('change', previewTitle);
  bar.querySelector('#rts-dev-speed').addEventListener('change', event => setSpeed(event.target.value));
  bar.querySelector('#rts-dev-panel-style').addEventListener('change', () => { if (panelVisible) showPanel(); });
  bar.querySelector('#rts-dev-panel-position').addEventListener('change', () => { if (panelVisible) showPanel(); });
  bar.querySelector('#rts-dev-player-profile').addEventListener('change', applyPlayerProfile);
  bar.querySelector('#rts-dev-clapper-profile').addEventListener('change', () => { if (clapperVisible) showClapper(); });
  bar.querySelector('#rts-dev-panel-profile').addEventListener('change', () => { if (panelVisible) showPanel(); });
  RTSReplay?.video?.addEventListener('loadedmetadata', () => { if (clapperVisible) updateClapper(); });
  refresh();
})();
