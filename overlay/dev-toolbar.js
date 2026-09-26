(() => {
  const params = new URLSearchParams(window.location.search);
  if (params.get('dev') !== 'true') return;

  const screen = document.createElement('div');
  screen.id = 'rts-dev-screen';
  screen.innerHTML = '<div class="dev-screen-label">1920 × 1080</div><div class="dev-safe-area"></div>';
  document.body.prepend(screen);

  const bar = document.createElement('aside');
  bar.id = 'rts-dev-toolbar';
  const section = (key, title, extra = '') => `
    <section class="dev-section" data-target="${key}"><h3>${title}</h3>
      <div class="dev-grid"><button data-action="show">${title === 'Player' ? 'Show Player' : 'Show ' + title}</button>${title === 'Player' ? '' : '<button data-action="in">Play In</button>'}</div>
      ${title === 'Player' ? '' : '<div class="dev-grid"><button data-action="hide">Hide</button><button data-action="out">Play Out</button></div>'}
      <label>${title === 'Player' ? 'Platform Brand Preset' : 'Brand Preset'}<select data-control="brand"></select></label>
      <label>Animation Profile<select data-control="profile"></select></label>
      ${title === 'Player' ? '<div class="dev-grid"><button data-action="in">Play In</button><button data-action="out">Play Out</button><button data-action="complete">Complete</button></div>' : ''}
      <div class="dev-grid"><label>From<select data-control="from"></select></label><label>To<select data-control="to"></select></label></div>
      <div class="dev-grid"><label>Easing<select data-control="easing"><option>linear</option><option>ease-in</option><option selected>ease-in-out</option><option>ease-out</option></select></label><label>Duration (s)<input data-control="duration" type="number" min="0" max="10" step="0.1" value="1"></label></div>
      ${extra}
    </section>`;

  bar.innerHTML = `
    <div class="dev-toolbar-header"><strong>RTS DEV</strong><span>DEV CONTROLS</span></div>
    <div class="dev-toolbar-settings"><button data-action="refresh-settings">Get Settings</button><span>Load current Streamer.bot settings</span></div>
    ${section('player', 'Player', `
      <div class="dev-grid"><label>Title Style<select id="rts-dev-title-style"><option>broadcast</option><option>cinematic</option><option>cut</option><option>minimal</option></select></label><label>Title Position<select id="rts-dev-title-position"><option>top</option><option selected>bottom</option></select></label></div>
      <label>Playback Speed<select id="rts-dev-speed"><option>0.25</option><option>0.5</option><option>0.75</option><option selected>1</option><option>1.25</option><option>1.5</option><option>1.75</option><option>2</option></select></label>
      <label>Test Clip<select id="rts-dev-test-clip"></select></label>
      <label>Preview Title<input id="rts-dev-title" value="FIRST TEST — REPLAY CAPTURE"></label>
      <button data-action="title-toggle">Show Title</button>
      <button data-action="position-test">Test Animation</button>`)}
    ${section('panel', 'Panel', `<label>Panel Style<select data-control="style"><option>broadcast</option><option>cinematic</option><option>cut</option><option>minimal</option></select></label>`)}
    ${section('clapper', 'Clapperboard')}
    ${section('message', 'Message', `<label>Preview Message<input data-control="text" value="MESSAGE PREVIEW"></label>`)}
    <div class="dev-toolbar-footer"><span>?dev=true</span><div class="dev-grid"><button data-action="refresh">Refresh UI</button></div></div>`;
  document.body.prepend(bar);

  const targets = {
    player: { positions:'replayPlayerPositions', profiles:'replayAnimationProfiles', animation:'replayAnimationProfile', entry:()=>window.rtsOverlayConfig?.player?.entryPoints?.play, panel:null },
    panel: { positions:'replayPanelPositions', profiles:'replayPanelAnimationProfiles', animation:'replayPanelAnimation', entry:()=>window.rtsOverlayConfig?.panel?.entryPoints?.recent, panel:'panel' },
    clapper: { positions:'replayClapperPositions', profiles:'replayClapperAnimationProfiles', animation:'replayClapperAnimation', entry:()=>window.rtsOverlayConfig?.clapperboard?.entryPoint, panel:null },
    message: { positions:'replayMessagePositions', profiles:'replayMessageAnimationProfiles', animation:'replayMessageAnimation', entry:()=>window.rtsOverlayConfig?.message?.entryPoint, panel:'message' }
  };
  const easing = s => s.querySelector('[data-control="easing"]');
  const parse = value => { try { return typeof value === 'string' ? JSON.parse(value || '{}') : value; } catch (_) { return null; } };
  const command = () => ({ ...(window.RTSReplaySettingsSync?.command || RTSReplayVideo?.currentCommand || RTSReplay?.command || {}) });
  const sectionFor = target => bar.querySelector(`.dev-section[data-target="${target}"]`);
  const controls = (target, name) => sectionFor(target)?.querySelector(`[data-control="${name}"]`);
  const list = (target, name) => { const value = parse(command()[targets[target][name]]); return Array.isArray(value) ? value : []; };
  const positions = target => parse(command()[targets[target].positions]) || {};
  const selected = (target, name) => list(target, 'profiles').find(p => p.id === controls(target, 'profile')?.value) || list(target, 'profiles')[0];
  const fill = (target, name, items, value) => {
    const select = controls(target, name); if (!select) return;
    const old = select.value; select.replaceChildren(...items.map(item => new Option(item.name || item.id || item, item.id || item)));
    if (value && items.some(item => (item.id || item) === value)) select.value = value; else if (items.some(item => (item.id || item) === old)) select.value = old; else if (items[0]) select.value = items[0].id || items[0];
  };
  const refreshTarget = target => {
    const brands = window.rtsOverlayConfig?.presets?.branding || [];
    fill(target, 'brand', brands, targets[target].entry()?.brandingPreset || 'default');
    fill(target, 'profile', list(target, 'profiles'));
    const names = Object.keys(positions(target));
    fill(target, 'from', names);
    fill(target, 'to', names);
    const profile = selected(target, 'profile');
    const defaultPosition = target === 'player' ? 'Full Screen' : 'Centered';
    const start = profile?.start?.[0]?.position, end = profile?.end?.at(-1)?.position || start;
    const defaultName = names.find(name => name.toLowerCase() === defaultPosition.toLowerCase());
    if (defaultName) {
      controls(target, 'from').value = defaultName;
      controls(target, 'to').value = defaultName;
    } else {
      if (start && names.includes(start)) controls(target, 'from').value = start;
      if (end && names.includes(end)) controls(target, 'to').value = end;
    }
    const step = profile?.start?.[0];
    if (step?.easing) easing(sectionFor(target)).value = step.easing;
    if (step?.duration != null) controls(target, 'duration').value = Number(step.duration) / 1000;
  };
  const refreshTestClips = () => {
    const select = document.getElementById('rts-dev-test-clip');
    if (!select) return;
    const clips = Array.isArray(window.rtsOverlayConfig?.previewCatalog) ? window.rtsOverlayConfig.previewCatalog : [];
    const old = select.value;
    select.replaceChildren(...clips.map((clip, index) => {
      const id = String(clip?.id || '');
      const title = String(clip?.title || 'Untitled Replay');
      const creator = String(clip?.creator?.name || clip?.creator || 'Unknown Creator');
      const source = String(clip?.sourceType || clip?.sourcePlatform || '');
      const label = `#${index + 1} ${title} — ${creator}${source ? ` [${source}]` : ''}`;
      return new Option(label, id);
    }));
    if (clips.some(clip => String(clip?.id || '') === old)) select.value = old;
    else if (clips[0]?.id) select.value = String(clips[0].id);
  };
  const refresh = () => {
    Object.keys(targets).forEach(refreshTarget);
    refreshTestClips();
    const title = RTSReplayElements?.title;
    const toggle = bar.querySelector('[data-action="title-toggle"]');
    if (toggle) toggle.textContent = title?.classList.contains('visible') ? 'Hide Title' : 'Show Title';
  };
  const profileCommand = (target, reverse = false) => {
    const m = targets[target], base = command(), profile = selected(target, 'profile') || {};
    const from = controls(target, 'from').value, to = controls(target, 'to').value;
    const duration = Math.max(0, Number(controls(target, 'duration').value) || 0) * 1000;
    const ease = easing(sectionFor(target)).value || 'ease-in-out';
    const sequence = [{ position: from, duration: 0, delay: 0, easing: ease }, { position: to, duration, delay: 0, easing: ease }];
    const start = reverse ? [{ position: to, duration: 0, delay: 0, easing: ease }, { position: from, duration, delay: 0, easing: ease }] : sequence;
    const end = reverse ? sequence : [{ position: to, duration: 0, delay: 0, easing: ease }, { position: from, duration, delay: 0, easing: ease }];
    base[m.animation] = JSON.stringify({ id: profile.id || 'dev', name: profile.name || 'Dev Preview', start, end });
    return base;
  };
  const applyPresentation = (target, base) => {
    const config = window.rtsOverlayConfig, m = targets[target], entry = { ...(m.entry() || {}) };
    entry.brandingPreset = controls(target, 'brand').value || entry.brandingPreset || 'default'; if (target === 'panel') entry.designPreset = controls(target, 'style')?.value || entry.designPreset || 'broadcast';
    window.RTSOverlayConfigPresentation?.apply?.(base, config, entry);
    base[targets[target].animation] = profileCommand(target)[targets[target].animation];
    if (target === 'message') base.replayMessage = controls(target, 'text').value.trim() || 'MESSAGE PREVIEW';
    return base;
  };
  const setCommand = (target, custom = true) => {
    let next = applyPresentation(target, command());
    if (!custom) next = command();
    RTSReplay.command = next; RTSReplayVideo.currentCommand = next;
    if (target === 'player') { RTSReplayControls.configure(next); RTSReplayElements.configure(next); }
    return next;
  };

  const showPlayer = () => { const player = RTSReplay?.player; if (!player) return; player.classList.add('dev-player','show'); player.style.opacity='1'; player.style.visibility='visible'; RTSReplay.frame?.classList.add('dev-frame'); sectionFor('player').querySelector('[data-action="show"]').textContent='Hide Player'; };
  const hidePlayer = () => { const player = RTSReplay?.player; if (!player) return; RTSReplayVideo.cancelAnimation(); player.classList.remove('show','dev-player'); player.style.opacity=''; player.style.visibility=''; RTSReplay.frame?.classList.remove('dev-frame'); sectionFor('player').querySelector('[data-action="show"]').textContent='Show Player'; };
  const applyTitleSettings = show => {
    const next = setCommand('player');
    next.replayTitle = document.getElementById('rts-dev-title').value.trim() || 'FIRST TEST — REPLAY CAPTURE';
    next.replayTitleStyle = document.getElementById('rts-dev-title-style').value;
    next.replayTitlePosition = document.getElementById('rts-dev-title-position').value;
    RTSReplay.command = next; RTSReplayVideo.currentCommand = next;
    if (show) RTSReplayElements.showTitle(next); else RTSReplayElements.hideTitle();
    sectionFor('player').querySelector('[data-action="title-toggle"]').textContent = show ? 'Hide Title' : 'Show Title';
  };
  const completePlayer = () => {
    const next = animationCommand('player');
    const profile = selected('player', 'profile') || {};
    const replayId = document.getElementById('rts-dev-test-clip')?.value || '';
    showPlayer();
    RTSReplayVideo.runAnimationProfile(next, () => {
      const request = window.RTSReplay?.requestAction;
      if (!request) return;
      request(
        {name:'RTS - Action Replay - Core - Test'},
        {
          rtsDevTest:'TestVideo',
          rtsDevComplete:'true',
          rtsDevAnimationProfileId: profile.id || 'default',
          rtsDevReplayId: replayId
        },
        'rts-dev-complete-'+Date.now()
      );
    });
  };
  const hideDevPanels = () => {
    [RTSReplay?.recentList, RTSSearchPanel?.panel, RTSPlaylistList?.panel].forEach(panel => {
      if (!panel) return;
      RTSInformationPanels.hide(panel, panel._rtsPanelAnimationCommand || RTSReplayVideo?.currentCommand || {});
    });
  };
  const showPanel = target => {
    const panel = target === 'message' ? RTSReplay?.messageCard : RTSReplay?.recentList;
    if (!panel) return;
    let next = setCommand(target);
    if (target === 'message') {
      RTSReplayMessages.showMessage(next);
    } else {
      hideDevPanels();
      const type = controls('panel', 'panel-type')?.value || 'recent';
      next = { ...next, replayTest: true };
      if (type === 'search') {
        next.replayCommand = 'search-panel';
        next.replaySearchParameters = 'CATALOG';
        next.replaySearchHeader = 'SEARCH • 1/1 • 2';
        next.replaySearchRequester = 'RTS Dev';
        next.replaySearchRequesterPlatform = 'Twitch';
        next.replaySearchEntries = JSON.stringify([{number:'01',title:'Search Panel Preview',creator:'DuhBuhHuh',creatorPlatform:'Twitch',plays:12,rating:4.8,ratingCount:5},{number:'02',title:'Current Settings Loaded',creator:'RTS Dev',creatorPlatform:'Twitch',plays:8,rating:5,ratingCount:2}]);
        RTSSearchPanel.show(next);
      } else if (type === 'playlist') {
        next.replayCommand = 'playlist-panel';
        next.replayPlaylist = '#1 Playlist Preview — RTS Dev | #2 Current Settings Loaded — RTS Dev';
        RTSPlaylistList.show(next);
      } else {
        next.replayRecent = 'DEV PANEL PREVIEW';
        next.replayRecentData = JSON.stringify([{number:'01',title:'Panel Preview',requester:'RTS Dev',avatarUrl:''},{number:'02',title:'Current Settings Loaded',requester:'RTS Dev',avatarUrl:''}]);
        RTSReplay.showRecentList(next);
      }
    }
    sectionFor(target).querySelector('[data-action="show"]').textContent='Hide '+(target==='message'?'Message':'Panel');
  };
  const hidePanel = target => { const panel = target === 'message' ? RTSReplay?.messageCard : RTSReplay?.recentList; if (!panel) return; if(target==='message') RTSReplayMessages.hideMessage(RTSReplayVideo.currentCommand); else RTSInformationPanels.hide(panel,panel._rtsPanelAnimationCommand||RTSReplayVideo.currentCommand); sectionFor(target).querySelector('[data-action="show"]').textContent='Show '+(target==='message'?'Message':'Panel'); };
  const showClapper = () => { const next=setCommand('clapper'); RTSReplayMessages.showClapperboard({ ...next, replayMessage:next.replayMessage || 'CLAPPERBOARD PREVIEW' }); sectionFor('clapper').querySelector('[data-action="show"]').textContent='Hide Clapperboard'; };
  const hideClapper = () => { RTSReplayMessages.clapperboardTimer && clearTimeout(RTSReplayMessages.clapperboardTimer); RTSAnimationEngine.createRunner({target:RTSReplayMessages.clapperCard?.querySelector('.clapper-position')}).cancel(); RTSReplayMessages.clapperCard?.classList.remove('show'); RTSReplayMessages.clapperCard?.setAttribute('aria-hidden','true'); sectionFor('clapper').querySelector('[data-action="show"]').textContent='Show Clapperboard'; };

  const animationCommand = target => {
    const next = command();
    const profile = selected(target, 'profile') || {};
    next[targets[target].animation] = JSON.stringify({
      id: profile.id || 'default',
      name: profile.name || 'Default',
      start: Array.isArray(profile.start) ? profile.start : [],
      end: Array.isArray(profile.end) ? profile.end : []
    });
    return next;
  };
  const playIn = target => {
    const next = animationCommand(target);
    if(target==='player'){ showPlayer(); RTSReplayVideo.runAnimationProfile(next); }
    else if(target==='panel') {
      const panel=RTSReplay.recentList;
      if (!panel) return;
      panel._rtsPanelAnimationCommand=next;
      RTSInformationPanelAnimation.show(panel,next,next.replayPanelPosition || 'Centered');
    }
    else if(target==='message') RTSReplayMessages.showMessage(next);
    else showClapper();
  };
  const playOut = target => {
    const next = animationCommand(target);
    if(target==='player'){ showPlayer(); RTSReplayVideo.runEndAnimationProfile(next,hidePlayer); }
    else if(target==='panel') {
      const panel=RTSReplay.recentList;
      if (panel) RTSInformationPanels.hide(panel,next);
    }
    else if(target==='message') RTSReplayMessages.hideMessage(next);
    else {
      const card=RTSReplayMessages.clapperCard, profile=RTSAnimationEngine.readProfile(next.replayClapperAnimation);
      const runner=RTSAnimationEngine.createRunner({target:card?.querySelector('.clapper-position')});
      runner.configure(next.replayClapperPositions); runner.runEnd(profile?.end,()=>{card?.classList.remove('show');card?.setAttribute('aria-hidden','true');});
    }
  };
  const testPosition = () => { const next=setCommand('player'), from=controls('player','from').value, to=controls('player','to').value, duration=Math.max(.1,Number(controls('player','duration').value)||1)*1000, ease=easing(sectionFor('player')).value; next.replayStartPosition=from; next.replayEndPosition=to; next.replayAnimationDuration=duration; next.replayAnimationEasing=ease; RTSReplayVideo.currentCommand=next; RTSReplay.command=next; showPlayer(); RTSReplayVideo.animateIn(RTSReplayVideo.getPosition(from),RTSReplayVideo.getPosition(to)); };
  const previewTitle = () => applyTitleSettings(true);
  const refreshSettings = () => { const request = window.RTSReplay?.requestAction; if (!request) return false; const button = bar.querySelector('[data-action="refresh-settings"]'); const ok = request({name:'RTS - Action Replay - Core - Resolver'}, {rtsDevConfigRefresh:'true'}, 'rts-dev-config-'+Date.now()); if (button) { const label = button.textContent; button.textContent = ok ? 'Request Sent' : 'WebSocket Offline'; setTimeout(() => { button.textContent = label; }, 1200); } return ok; };
  const setSpeed = value => { const speed=Number(value); if(!Number.isFinite(speed)||!RTSReplay?.video)return; RTSReplay.video.playbackRate=speed; const next={...command(),replayPlaybackSpeed:speed}; RTSReplay.command=next; RTSReplayVideo.currentCommand=next; RTSReplayElements?.configureSpeed?.(next); };

  bar.addEventListener('click', event => {
    const button=event.target.closest('button'); if(!button)return;
    const section=button.closest('.dev-section'), target=section?.dataset.target, action=button.dataset.action;
    if(action==='show') target==='player' ? (RTSReplay.player?.classList.contains('show')?hidePlayer():(setCommand('player'),showPlayer())) : (target==='clapper' ? (RTSReplayMessages.clapperCard?.classList.contains('show')?hideClapper():showClapper()) : (target==='message'||target==='panel' ? ((target==='message'?RTSReplay.messageCard:RTSReplay.recentList)?.classList.contains('show')?hidePanel(target):showPanel(target)) : null));
    if(action==='hide') target==='player'?hidePlayer():target==='clapper'?hideClapper():hidePanel(target);
    if(action==='in') target==='player' ? playIn(target) : playIn(target);
    if(action==='out') target==='player' ? playOut(target) : playOut(target);
    if(action==='complete' && target==='player') completePlayer();
    if(action==='title') previewTitle();
    if(action==='title-toggle' && target==='player') {
      const title=RTSReplayElements?.title;
      applyTitleSettings(!title?.classList.contains('visible'));
    }
    if(action==='position-test') testPosition();
    if(action==='refresh') refresh(); if(action==='refresh-settings') refreshSettings();
  });
  bar.addEventListener('change', event => {
    const control=event.target.closest('[data-control]'), section=event.target.closest('.dev-section');
    if(!control||!section)return;
    const target=section.dataset.target;
    if(control.dataset.control==='profile'||control.dataset.control==='brand') {
      setCommand(target);
      if(target==='player' && RTSReplayElements?.title?.classList.contains('visible')) applyTitleSettings(true);
    }
    if(control.dataset.control==='style' && target==='panel' && (RTSReplay.recentList?.classList.contains('show')||RTSSearchPanel?.panel?.classList.contains('show')||RTSPlaylistList?.panel?.classList.contains('show'))) showPanel('panel');
    if(control.dataset.control==='panel-type' && target==='panel' && (RTSReplay.recentList?.classList.contains('show')||RTSSearchPanel?.panel?.classList.contains('show')||RTSPlaylistList?.panel?.classList.contains('show'))) showPanel('panel');
    if(control.dataset.control==='text' && target==='message' && RTSReplay.messageCard?.classList.contains('show')) showPanel('message');
  });
  bar.querySelector('#rts-dev-title-style').addEventListener('change',() => applyTitleSettings(true));
  bar.querySelector('#rts-dev-title-position').addEventListener('change',() => applyTitleSettings(true));
  bar.querySelector('#rts-dev-speed').addEventListener('change',event=>setSpeed(event.target.value));
  bar.querySelector('#rts-dev-test-clip').addEventListener('change',event => {
    const clip = (window.rtsOverlayConfig?.previewCatalog || []).find(item => String(item?.id || '') === String(event.target.value || ''));
    if (!clip) return;
    const next = { ...command(), replayTitle: String(clip.title || 'Test Replay') };
    RTSReplay.command = next; RTSReplayVideo.currentCommand = next;
  });
  const setPlayerVisibility = visible => {
    const button = sectionFor('player')?.querySelector('[data-action="show"]');
    if (button) button.textContent = visible ? 'Hide Player' : 'Show Player';
  };
  window.RTSDevToolbar = { ...(window.RTSDevToolbar || {}), refresh, setPlayerVisibility };
  refresh();