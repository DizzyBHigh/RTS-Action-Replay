(() => {
  const params = new URLSearchParams(window.location.search);
  if (params.get('dev') !== 'true') return;

  const screen = document.createElement('div');
  screen.id = 'rts-dev-screen';
  screen.innerHTML = '<div class="dev-screen-label">1920 x 1080</div><div class="dev-safe-area"></div>';
  document.body.prepend(screen);

  const bar = document.createElement('aside');
  bar.id = 'rts-dev-toolbar';
  const devClapperStyle = document.createElement('style');
  devClapperStyle.textContent = `
    #clapper-card.dev-clapper-cinematic .slate { border-width:1px; border-radius:4px; box-shadow:inset 0 0 0 1px #ffffff12,0 18px 50px #000b; }
    #clapper-card.dev-clapper-cinematic .clapstick { border-width:1px; box-shadow:0 5px 12px #000b; }
    #clapper-card.dev-clapper-cut .slate { border-radius:0; border-width:4px; box-shadow:inset 0 -14px 0 var(--accent-color),0 12px 32px #000b; }
    #clapper-card.dev-clapper-cut .clapstick { border-radius:0; transform:rotate(0deg); box-shadow:none; }
    #clapper-card.dev-clapper-minimal .slate { background:color-mix(in srgb,var(--board-color) 94%,black); border-width:1px; border-radius:3px; box-shadow:0 0 30px #0009; }
    #clapper-card.dev-clapper-minimal .clapstick { border-width:1px; box-shadow:none; }
    #clapper-card.dev-clapper-minimal .hinge { border-width:1px; box-shadow:none; }
  `;
  document.head.appendChild(devClapperStyle);
  const section = (key, title, extra = '') => `
    <section class="dev-section" data-target="${key}"><h3>${title}</h3>
      ${title === 'Panel' ? '<label>Panel Type<select data-control="panel-type"><option value="recent">Recent Clips</option><option value="search">Search Results</option><option value="playlist">Playlist</option></select></label>' : ''}
      <label>${title === 'Player' ? 'Platform Brand Preset' : 'Brand Preset'}<select data-control="brand"></select></label>
      ${title === 'Panel' || title === 'Clapperboard' ? '<label>Design Style<select data-control="style"><option>broadcast</option><option>cinematic</option><option>cut</option><option>minimal</option></select></label>' : ''}
      <label>Animation Profile<select data-control="profile"></select></label>
      <div class="dev-grid"><button data-action="show">${title === 'Player' ? 'Show Player' : 'Show ' + title}</button>${title === 'Player' ? '' : '<button data-action="in">Play In</button>'}</div>
      ${title === 'Player' ? '' : '<div class="dev-grid"><button data-action="hide">Hide</button><button data-action="out">Play Out</button></div>'}
      ${title === 'Player' ? '<div class="dev-grid"><button data-action="in">Play In</button><button data-action="out">Play Out</button><button data-action="complete">Complete</button></div>' : ''}
      <div class="dev-separator"></div>
      <div class="dev-grid"><label>From<select data-control="from"></select></label><label>To<select data-control="to"></select></label></div>
      <div class="dev-grid"><label>Easing<select data-control="easing"><option>linear</option><option>ease-in</option><option selected>ease-in-out</option><option>ease-out</option></select></label><label>Duration (ms)<input data-control="duration" type="number" min="0" max="10000" step="100" value="1000"></label></div>
      ${title === 'Player' || title === 'Panel' || title === 'Clapperboard' ? '<button data-action="position-test">Test Animation</button><label class="dev-checkbox"><input type="checkbox" data-control="reset-to"> Move From to To after Test Animation</label><div class="dev-separator"></div>' : ''}
      ${extra}
    </section>`;

  bar.innerHTML = `
    <div class="dev-toolbar-header"><strong>RTS DEV</strong><span>DEV CONTROLS</span></div>
    <div class="dev-toolbar-settings"><button data-action="refresh-settings">Get Settings</button><span>Load current Streamer.bot settings</span></div>
    ${section('player', 'Player', `
      <div class="dev-grid"><label>Title Style<select id="rts-dev-title-style"><option>broadcast</option><option>cinematic</option><option>cut</option><option>minimal</option></select></label><label>Title Position<select id="rts-dev-title-position"><option>top</option><option selected>bottom</option></select></label></div>
      <div class="dev-grid"><label>Title Delay (s)<input id="rts-dev-title-delay" type="number" min="0" max="120" step="0.1" value="2"></label><label>Title Duration (s)<input id="rts-dev-title-duration" type="number" min="0" max="120" step="0.1" value="10"></label></div>
      <label>Playback Speed<select id="rts-dev-speed"><option>0.25</option><option>0.5</option><option>0.75</option><option selected>1</option><option>1.25</option><option>1.5</option><option>1.75</option><option>2</option></select></label>
      <label>Test Clip<select id="rts-dev-test-clip"></select></label>
      <label>Preview Title<input id="rts-dev-title" value="FIRST TEST - REPLAY CAPTURE"></label>
      <button data-action="title-toggle">Show Title</button>`)}
    ${section('panel', 'Panel')}
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
  const command = () => ({ ...(RTSReplayVideo?.currentCommand || RTSReplay?.command || window.RTSReplaySettingsSync?.command || {}) });
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
    if (start && names.includes(start)) controls(target, 'from').value = start;
    else if (defaultName) controls(target, 'from').value = defaultName;
    if (end && names.includes(end)) controls(target, 'to').value = end;
    else if (defaultName) controls(target, 'to').value = defaultName;
    const step = profile?.start?.[0];
    if (step?.easing) easing(sectionFor(target)).value = step.easing;
    if (step?.duration != null) controls(target, 'duration').value = Number(step.duration);
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
      const label = `#${index + 1} ${title} - ${creator}${source ? ` [${source}]` : ''}`;
      return new Option(label, id);
    }));
    if (clips.some(clip => String(clip?.id || '') === old)) select.value = old;
    else if (clips[0]?.id) select.value = String(clips[0].id);
  };
  const refresh = () => {
    Object.keys(targets).forEach(refreshTarget);
    const current = command();
    const delay = document.getElementById('rts-dev-title-delay');
    const duration = document.getElementById('rts-dev-title-duration');
    if (delay) delay.value = Math.max(0, Number(current.replayTitleDelay) || 0) / 1000;
    if (duration) duration.value = Math.max(0, Number(current.replayTitleDuration) || 0) / 1000;
    refreshTestClips();
    const title = RTSReplayElements?.title;
    const toggle = bar.querySelector('[data-action="title-toggle"]');
    if (toggle) toggle.textContent = title?.classList.contains('visible') ? 'Hide Title' : 'Show Title';
  };
  const profileCommand = (target, reverse = false) => {
    const m = targets[target], base = command(), profile = selected(target, 'profile') || {};
    const from = controls(target, 'from').value, to = controls(target, 'to').value;
    const duration = Math.max(0, Number(controls(target, 'duration').value) || 0);
    const ease = easing(sectionFor(target)).value || 'ease-in-out';
    const sequence = [{ position: from, duration: 0, delay: 0, easing: ease }, { position: to, duration, delay: 0, easing: ease }];
    const start = reverse ? [{ position: to, duration: 0, delay: 0, easing: ease }, { position: from, duration, delay: 0, easing: ease }] : sequence;
    const end = reverse ? sequence : [{ position: to, duration: 0, delay: 0, easing: ease }, { position: from, duration, delay: 0, easing: ease }];
    base[m.animation] = JSON.stringify({ id: profile.id || 'dev', name: profile.name || 'Dev Preview', start, end });
    return base;
  };
  const applyPresentation = (target, base) => {
    const config = window.rtsOverlayConfig, m = targets[target], entry = { ...(m.entry() || {}) };
    entry.brandingPreset = controls(target, 'brand').value || entry.brandingPreset || 'default';
    if (target === 'panel') entry.designPreset = controls(target, 'style')?.value || entry.designPreset || 'broadcast';
    window.RTSOverlayConfigPresentation?.apply?.(base, config, entry);
    if (target === 'clapper') base.replayClapperPreset = controls(target, 'style')?.value || 'broadcast';
    if (target === 'message') base.replayMessage = controls(target, 'text').value.trim() || 'MESSAGE PREVIEW';
    return base;
  };
  const setCommand = (target, custom = true) => {
    let next = applyPresentation(target, animationCommand(target));
    if (!custom) next = command();
    RTSReplay.command = next; RTSReplayVideo.currentCommand = next;
    if (window.RTSReplaySettingsSync) window.RTSReplaySettingsSync.command = next;
    if (target === 'player') { RTSReplayControls.configure(next); RTSReplayElements.configure(next); }
    return next;
  };
  const showPlayer = () => { const player = RTSReplay?.player; if (!player) return; player.classList.add('dev-player','show'); player.style.opacity='1'; player.style.visibility='visible'; RTSReplay.frame?.classList.add('dev-frame'); sectionFor('player').querySelector('[data-action="show"]').textContent='Hide Player'; };
  const hidePlayer = () => { const player = RTSReplay?.player; if (!player) return; RTSReplayVideo.cancelAnimation(); player.classList.remove('show','dev-player'); player.style.opacity=''; player.style.visibility=''; RTSReplay.frame?.classList.remove('dev-frame'); sectionFor('player').querySelector('[data-action="show"]').textContent='Show Player'; };
  const applyTitleSettings = show => {
    const next = setCommand('player');
    next.replayTitle = document.getElementById('rts-dev-title').value.trim() || 'FIRST TEST - REPLAY CAPTURE';
    next.replayTitleDelay = Math.max(0, Number(document.getElementById('rts-dev-title-delay').value) || 0) * 1000;
    next.replayTitleDuration = Math.max(0, Number(document.getElementById('rts-dev-title-duration').value) || 0) * 1000;
    next.replayTitleStyle = document.getElementById('rts-dev-title-style').value;
    next.replayTitlePosition = document.getElementById('rts-dev-title-position').value;
    RTSReplay.command = next; RTSReplayVideo.currentCommand = next;
    RTSReplayElements.clearTitleDelay?.();
    RTSReplayElements.clearTitleTimer?.();
    if (show) RTSReplayElements.showTitle(next); else RTSReplayElements.hideTitle();
    sectionFor('player').querySelector('[data-action="title-toggle"]').textContent = show ? 'Hide Title' : 'Show Title';
  };
  const completePlayer = () => {
    const next = animationCommand('player');
    const profile = selected('player', 'profile') || {};
    const replayId = document.getElementById('rts-dev-test-clip')?.value || '';
    RTSReplayVideo.resetDevPlaybackSurface?.();
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
  const hideDevPanels = except => {
    [RTSReplay?.recentList, RTSSearchPanel?.panel, RTSPlaylistList?.panel].forEach(panel => {
      if (!panel || panel === except) return;
      RTSInformationPanels.hide(panel, panel._rtsPanelAnimationCommand || RTSReplayVideo?.currentCommand || {});
    });
  };

  const renderDevPanel = (panel, type, command, animate = true) => {
    if (!panel) return null;
    const next = { ...command, replayTest: true };
    panel.dataset.rtsInformationPanel = type;
    if (type === 'search') {
      panel.innerHTML = '<div class="rts-panel-header"><span class="rts-panel-kicker">CATALOG SEARCH</span><strong class="rts-search-type">Full Catalog</strong><span class="rts-search-summary">Page 1 of 1 | 2 Clips</span><span class="rts-search-requester"><span class="rts-search-requester-label">Requested By </span><span class="rts-search-requester-platform rts-search-requester-platform--twitch">Twitch:</span><span class="rts-search-requester-name">RTS Dev</span></span></div><div class="rts-panel-list"><div class="rts-panel-entry"><span class="rts-panel-number">01</span><div class="rts-search-result-content"><span class="rts-panel-title">Search Panel Preview</span><span class="rts-search-result-creator rts-search-requester-platform--twitch"><span class="rts-search-result-creator-name">DuhBuhHuh</span></span></div><span class="rts-search-stats">12 views | * 4.8 (5)</span></div><div class="rts-panel-entry"><span class="rts-panel-number">02</span><div class="rts-search-result-content"><span class="rts-panel-title">Current Settings Loaded</span><span class="rts-search-result-creator rts-search-requester-platform--twitch"><span class="rts-search-result-creator-name">RTS Dev</span></span></div><span class="rts-search-stats">8 views | * 5.0 (2)</span></div></div>';
    } else if (type === 'playlist') {
      panel.innerHTML = '<div class="rts-panel-header"><span class="rts-panel-kicker">ACTION REPLAY</span><strong>PLAYLIST</strong></div><div class="rts-panel-list"><div class="rts-panel-entry"><span class="rts-panel-number">01</span><span class="rts-playlist-content"><strong class="rts-panel-title">Playlist Preview</strong><small class="rts-playlist-requester">RTS Dev</small></span></div><div class="rts-panel-entry"><span class="rts-panel-number">02</span><span class="rts-playlist-content"><strong class="rts-panel-title">Current Settings Loaded</strong><small class="rts-playlist-requester">RTS Dev</small></span></div></div>';
    } else {
      panel.innerHTML = '<div class="rts-panel-header"><span class="rts-panel-kicker">ACTION REPLAY</span><strong>RECENT REPLAYS</strong></div><div class="rts-panel-list"><div class="rts-panel-entry"><span class="rts-panel-number">01</span><span class="rts-panel-title">Panel Preview</span></div><div class="rts-panel-entry"><span class="rts-panel-number">02</span><span class="rts-panel-title">Current Settings Loaded</span></div></div>';
    }
    panel.classList.remove('show');
    panel.setAttribute('aria-hidden', 'true');
    void panel.offsetWidth;
    panel._rtsPanelAnimationCommand = next;
    devPanelAnimation = { panel, command: next };
    RTSReplay.command = next;
    RTSReplayVideo.currentCommand = next;
    if (window.RTSReplaySettingsSync) window.RTSReplaySettingsSync.command = next;
    if (animate) RTSInformationPanelAnimation.show(panel, next, next.replayPanelPosition || 'Centered');
    return next;
  };

  const showPanel = target => {
    const panel = target === 'message' ? RTSReplay?.messageCard : getDevPanel();
    if (!panel) return;
    if (target === 'message') {
      const next = setCommand(target);
      RTSReplayMessages.showMessage(next);
      sectionFor(target).querySelector('[data-action="show"]').textContent = 'Hide Message';
      return;
    }
    const type = controls('panel', 'panel-type')?.value || 'recent';
    const next = setCommand('panel');
    hideDevPanels(panel);
    renderDevPanel(panel, type, next);
    sectionFor(target).querySelector('[data-action="show"]').textContent = 'Hide Panel';
  };
  const hidePanel = target => { const panel = target === 'message' ? RTSReplay?.messageCard : getDevPanel(); if (!panel) return; if(target==='message') RTSReplayMessages.hideMessage(RTSReplayVideo.currentCommand); else RTSInformationPanels.hide(panel,panel._rtsPanelAnimationCommand||RTSReplayVideo.currentCommand); sectionFor(target).querySelector('[data-action="show"]').textContent='Show '+(target==='message'?'Message':'Panel'); };
  const showClapper = () => {
    const next=setCommand('clapper');
    const card=RTSReplayMessages.clapperCard;
    const style=controls('clapper','style')?.value || 'broadcast';
    RTSReplayMessages.showClapperboard({ ...next, replayMessage:next.replayMessage || 'CLAPPERBOARD PREVIEW' });
    card?.classList.remove('dev-clapper-broadcast','dev-clapper-cinematic','dev-clapper-cut','dev-clapper-minimal');
    card?.classList.add('dev-clapper-' + style);
    sectionFor('clapper').querySelector('[data-action="show"]').textContent='Hide Clapperboard';
  };
  const hideClapper = () => { RTSReplayMessages.clapperboardTimer && clearTimeout(RTSReplayMessages.clapperboardTimer); RTSAnimationEngine.createRunner({target:RTSReplayMessages.clapperCard?.querySelector('.clapper-position')}).cancel(); RTSReplayMessages.clapperCard?.classList.remove('show','dev-clapper-broadcast','dev-clapper-cinematic','dev-clapper-cut','dev-clapper-minimal'); RTSReplayMessages.clapperCard?.setAttribute('aria-hidden','true'); sectionFor('clapper').querySelector('[data-action="show"]').textContent='Show Clapperboard'; };

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
  const getDevPanel = () => {
    const type=controls('panel','panel-type')?.value || 'recent';
    if(type==='search') return RTSSearchPanel?.panel;
    if(type==='playlist') return RTSPlaylistList?.panel;
    return RTSReplay?.recentList;
  };
  let devPanelAnimation = { panel: null, command: null };
  const playIn = target => {
    if(target==='player'){ const next=animationCommand(target); showPlayer(); RTSReplayVideo.runAnimationProfile(next); return; }
    if(target==='panel'){
      const panel=getDevPanel();
      if (!panel) return;
      const next=setCommand('panel');
      hideDevPanels(panel);
      renderDevPanel(panel,controls('panel','panel-type')?.value || 'recent',next);
      return;
    }
    const next=animationCommand(target);
    if(target==='message') RTSReplayMessages.showMessage(next);
    else showClapper();
  };
  const playOut = target => {
    const next = animationCommand(target);
    if(target==='player'){ showPlayer(); RTSReplayVideo.runEndAnimationProfile(next,hidePlayer); }
    else if(target==='panel') {
      const panel=devPanelAnimation.panel;
      const storedCommand=devPanelAnimation.command || panel?._rtsPanelAnimationCommand;
      if (!panel || !storedCommand) return;
      panel._rtsPanelAnimationCommand = storedCommand;
      panel.classList.add('show');
      panel.setAttribute('aria-hidden', 'false');
      const profile = RTSInformationPanelAnimation.profile(storedCommand);
      window.RTSDevToolbar?.log?.('Dev panel Play Out', {
        panelType: panel.dataset.rtsInformationPanel || '',
        profile: profile?.id || '',
        endSteps: Array.isArray(profile?.end) ? profile.end.length : 0,
        active: RTSAnimationEngine.createRunner({ target: panel }).getActive?.() || null
      });
      RTSInformationPanelAnimation.hide(panel, storedCommand, () => {
        window.RTSDevToolbar?.log?.('Dev panel Play Out complete', {
          panelType: panel.dataset.rtsInformationPanel || '',
          visible: panel.classList.contains('show'),
          ariaHidden: panel.getAttribute('aria-hidden')
        });
        if (devPanelAnimation.panel === panel) devPanelAnimation = { panel: null, command: null };
      });
    }
    else if(target==='message') RTSReplayMessages.hideMessage(next);
    else {
      const card=RTSReplayMessages.clapperCard, profile=RTSAnimationEngine.readProfile(next.replayClapperAnimation);
      const runner=RTSAnimationEngine.createRunner({target:card?.querySelector('.clapper-position')});
      runner.configure(next.replayClapperPositions); runner.runEnd(profile?.end,()=>{card?.classList.remove('show');card?.setAttribute('aria-hidden','true');});
    }
  };
  const testPosition = target => {
     if (target === 'player') {
       const next=setCommand('player'), from=controls('player','from').value, to=controls('player','to').value;
       const duration=Math.max(0,Number(controls('player','duration').value)||0), ease=easing(sectionFor('player')).value;
       next.replayStartPosition=from; next.replayEndPosition=to; next.replayAnimationDuration=duration; next.replayAnimationEasing=ease;
       RTSReplayVideo.currentCommand=next; RTSReplay.command=next; showPlayer();
       const start=RTSReplayVideo.getPosition(from), end=RTSReplayVideo.getPosition(to);
       const complete=()=>{
         if(!controls('player','reset-to')?.checked) return;
         const fromControl=controls('player','from');
         fromControl.value=to;
         fromControl.dispatchEvent(new Event('change',{bubbles:true}));
         next.replayStartPosition=to;
         RTSReplayVideo.currentCommand={...RTSReplayVideo.currentCommand,replayStartPosition:to};
         RTSReplay.command={...RTSReplay.command,replayStartPosition:to};
       };
       RTSReplayVideo.cancelAnimation?.();
       RTSReplayVideo.applyPosition(start,true);
       if(RTSReplayVideo.positionsEqual(start,end)) complete();
       else RTSReplayVideo.animatePosition(start,end,complete);
       return;
     }
     if (target === 'clapper') {
       const card=RTSReplayMessages.clapperCard;
       const targetElement=card?.querySelector('.clapper-position');
       if (!card || !targetElement) return;
       const next=setCommand('clapper');
       const from=controls('clapper','from').value, to=controls('clapper','to').value;
       const duration=Math.max(0,Number(controls('clapper','duration').value)||0);
       const ease=easing(sectionFor('clapper')).value || 'ease-in-out';
       const runner=RTSAnimationEngine.createRunner({target:targetElement});
       runner.configure(next.replayClapperPositions);
       const start=runner.resolve(from), end=runner.resolve(to);
       window.RTSDevToolbar?.log?.('Dev clapper Test Animation', {
         from, to, duration, easing: ease,
         start, end
       });
       clearTimeout(RTSReplayMessages.clapperboardTimer);
       card.classList.add('show');
       card.style.opacity='1';
       card.style.visibility='visible';
       card.style.zIndex='55';
       runner.run([
         { position: from, duration: 0, delay: 0, easing: ease },
         { position: to, duration, delay: 0, easing: ease }
       ], () => {
         if(!controls('clapper','reset-to')?.checked) return;
         const fromControl=controls('clapper','from');
         fromControl.value=to;
         fromControl.dispatchEvent(new Event('change',{bubbles:true}));
       });
       return;
     }
     if (target !== 'panel') return;
     const panel=getDevPanel();
     if (!panel) return;
     const next=setCommand('panel');
     hideDevPanels(panel);
     renderDevPanel(panel,controls('panel','panel-type')?.value || 'recent',next,false);
     const from=controls('panel','from').value, to=controls('panel','to').value;
     const duration=Math.max(0,Number(controls('panel','duration').value)||0);
     const ease=easing(sectionFor('panel')).value || 'ease-in-out';
     const runner=RTSAnimationEngine.createRunner({target:panel});
     runner.configure(next.replayPanelPositions);
     const start=runner.resolve(from), end=runner.resolve(to);
     window.RTSDevToolbar?.log?.('Dev panel Test Animation', {
       from, to, duration, easing: ease,
       start, end
     });
     panel.classList.add('show');
     const complete=()=>{
       if(!controls('panel','reset-to')?.checked) return;
       const fromControl=controls('panel','from');
       fromControl.value=to;
       fromControl.dispatchEvent(new Event('change',{bubbles:true}));
     };
     runner.run([
       { position: from, duration: 0, delay: 0, easing: ease },
       { position: to, duration, delay: 0, easing: ease }
     ], complete);
   };
  const previewTitle = () => applyTitleSettings(true);
  const refreshSettings = () => { const request = window.RTSReplay?.requestAction; if (!request) return false; const button = bar.querySelector('[data-action="refresh-settings"]'); const ok = request({name:'RTS - Action Replay - Core - Resolver'}, {rtsDevConfigRefresh:'true'}, 'rts-dev-config-'+Date.now()); if (button) { const label = button.textContent; button.textContent = ok ? 'Request Sent' : 'WebSocket Offline'; setTimeout(() => { button.textContent = label; }, 1200); } return ok; };
  const setSpeed = value => { const speed=Number(value); if(!Number.isFinite(speed)||!RTSReplay?.video)return; RTSReplay.video.playbackRate=speed; const next={...command(),replayPlaybackSpeed:speed}; RTSReplay.command=next; RTSReplayVideo.currentCommand=next; RTSReplayElements?.configureSpeed?.(next); };

  bar.addEventListener('click', event => {
    const button=event.target.closest('button'); if(!button)return;
    const section=button.closest('.dev-section'), target=section?.dataset.target, action=button.dataset.action;
    if(action==='show') target==='player' ? (RTSReplay.player?.classList.contains('show')?hidePlayer():(setCommand('player'),showPlayer())) : (target==='clapper' ? (RTSReplayMessages.clapperCard?.classList.contains('show')?hideClapper():showClapper()) : (target==='message'||target==='panel' ? ((target==='message'?RTSReplay.messageCard:getDevPanel())?.classList.contains('show')?hidePanel(target):showPanel(target)) : null));
    if(action==='hide') target==='player'?hidePlayer():target==='clapper'?hideClapper():hidePanel(target);
    if(action==='in') target==='player' ? playIn(target) : playIn(target);
    if(action==='out') target==='player' ? playOut(target) : playOut(target);
    if(action==='complete' && target==='player') completePlayer();
    if(action==='title') previewTitle();
    if(action==='title-toggle' && target==='player') {
      const title=RTSReplayElements?.title;
      applyTitleSettings(!title?.classList.contains('visible'));
    }
    if(action==='position-test') testPosition(target);
    if(action==='refresh') refresh(); if(action==='refresh-settings') refreshSettings();
  });
  bar.addEventListener('change', event => {
    const control=event.target.closest('[data-control]'), section=event.target.closest('.dev-section');
    if(!control||!section)return;
    const target=section.dataset.target;
    if(control.dataset.control==='profile'||control.dataset.control==='brand') {
      setCommand(target);
      if (target === 'panel' && getDevPanel()?.classList.contains('show')) showPanel('panel');
      if(target==='player' && RTSReplayElements?.title?.classList.contains('visible')) applyTitleSettings(true);
    }
    if(control.dataset.control==='style' && target==='clapper' && RTSReplayMessages.clapperCard?.classList.contains('show')) showClapper();
    if((control.dataset.control==='style'||control.dataset.control==='panel-type') && target==='panel' && (RTSReplay.recentList?.classList.contains('show')||RTSSearchPanel?.panel?.classList.contains('show')||RTSPlaylistList?.panel?.classList.contains('show'))) showPanel('panel');
    if(control.dataset.control==='text' && target==='message' && RTSReplay.messageCard?.classList.contains('show')) showPanel('message');
  });
  bar.querySelector('#rts-dev-title').addEventListener('input',() => applyTitleSettings(true));
  bar.querySelector('#rts-dev-title-style').addEventListener('change',() => applyTitleSettings(true));
  bar.querySelector('#rts-dev-title-position').addEventListener('change',() => applyTitleSettings(true));
  bar.querySelector('#rts-dev-title-delay').addEventListener('change',() => applyTitleSettings(true));
  bar.querySelector('#rts-dev-title-duration').addEventListener('change',() => applyTitleSettings(true));
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
})();
