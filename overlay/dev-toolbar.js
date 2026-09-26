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
      <div class="dev-grid"><button data-action="show">${title === 'Player' ? 'Show Player' : 'Show ' + title}</button><button data-action="in">Play In</button></div>
      <div class="dev-grid"><button data-action="hide">Hide</button><button data-action="out">Play Out</button></div>
      <label>Brand Preset<select data-control="brand"></select></label>
      <label>Animation Profile<select data-control="profile"></select></label>
      <div class="dev-grid"><label>From<select data-control="from"></select></label><label>To<select data-control="to"></select></label></div>
      <div class="dev-grid"><label>Easing<select data-control="easing"><option>linear</option><option>ease-in</option><option selected>ease-in-out</option><option>ease-out</option></select></label><label>Duration (s)<input data-control="duration" type="number" min="0" max="10" step="0.1" value="1"></label></div>
      ${extra}
    </section>`;

  bar.innerHTML = `
    <div class="dev-toolbar-header"><strong>RTS DEV</strong><span>DEV CONTROLS</span></div>
    ${section('player', 'Player', `
      <div class="dev-grid"><label>Title Style<select id="rts-dev-title-style"><option>broadcast</option><option>cinematic</option><option>cut</option><option>minimal</option></select></label><label>Title Position<select id="rts-dev-title-position"><option>top</option><option selected>bottom</option></select></label></div>
      <label>Playback Speed<select id="rts-dev-speed"><option>0.25</option><option>0.5</option><option>0.75</option><option selected>1</option><option>1.25</option><option>1.5</option><option>1.75</option><option>2</option></select></label>
      <label>Preview Title<input id="rts-dev-title" value="FIRST TEST — REPLAY CAPTURE"></label><button data-action="title">Preview Title</button>
      <button data-action="position-test">Test Position</button>`)}
    ${section('panel', 'Panel', `<label>Panel Style<select data-control="style"><option>broadcast</option><option>cinematic</option><option>cut</option><option>minimal</option></select></label>`)}
    ${section('clapper', 'Clapperboard')}
    ${section('message', 'Message', `<label>Preview Message<input data-control="text" value="MESSAGE PREVIEW"></label>`)}
    <div class="dev-toolbar-footer"><span>?dev=true</span><button data-action="refresh">Refresh Config</button></div>`;
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
    const start = profile?.start?.[0]?.position, end = profile?.end?.at(-1)?.position || start;
    if (start && names.includes(start)) controls(target, 'from').value = start;
    if (end && names.includes(end)) controls(target, 'to').value = end;
    const step = profile?.start?.[0];
    if (step?.easing) easing(sectionFor(target)).value = step.easing;
    if (step?.duration != null) controls(target, 'duration').value = Number(step.duration) / 1000;
  };
  const refresh = () => Object.keys(targets).forEach(refreshTarget);
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
    entry.brandingPreset = controls(target, 'brand').value || entry.brandingPreset || 'default';
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
  const showPanel = target => { const panel = target === 'message' ? RTSReplay?.messageCard : RTSReplay?.recentList; if (!panel) return; const next=setCommand(target); if(target==='message') RTSReplayMessages.showMessage(next); else RTSReplay.showRecentList(next); sectionFor(target).querySelector('[data-action="show"]').textContent='Hide '+(target==='message'?'Message':'Panel'); };
  const hidePanel = target => { const panel = target === 'message' ? RTSReplay?.messageCard : RTSReplay?.recentList; if (!panel) return; if(target==='message') RTSReplayMessages.hideMessage(RTSReplayVideo.currentCommand); else RTSInformationPanels.hide(panel,panel._rtsPanelAnimationCommand||RTSReplayVideo.currentCommand); sectionFor(target).querySelector('[data-action="show"]').textContent='Show '+(target==='message'?'Message':'Panel'); };
  const showClapper = () => { const next=setCommand('clapper'); RTSReplayMessages.showClapperboard({ ...next, replayMessage:next.replayMessage || 'CLAPPERBOARD PREVIEW' }); sectionFor('clapper').querySelector('[data-action="show"]').textContent='Hide Clapperboard'; };
  const hideClapper = () => { RTSReplayMessages.clapperboardTimer && clearTimeout(RTSReplayMessages.clapperboardTimer); RTSAnimationEngine.createRunner({target:RTSReplayMessages.clapperCard?.querySelector('.clapper-position')}).cancel(); RTSReplayMessages.clapperCard?.classList.remove('show'); RTSReplayMessages.clapperCard?.setAttribute('aria-hidden','true'); sectionFor('clapper').querySelector('[data-action="show"]').textContent='Show Clapperboard'; };

  const playIn = target => {
    const next=setCommand(target);
    if(target==='player'){ showPlayer(); RTSReplayVideo.runAnimationProfile(next); }
    else if(target==='panel') { const panel=RTSReplay.recentList; panel._rtsPanelAnimationCommand=next; RTSInformationPanelAnimation.show(panel,next,next.replayPanelPosition); }
    else if(target==='message') RTSReplayMessages.showMessage(next);
    else showClapper();
  };
  const playOut = target => {
    const next=setCommand(target);
    if(target==='player'){ showPlayer(); RTSReplayVideo.runEndAnimationProfile(next,hidePlayer); }
    else if(target==='panel') RTSInformationPanels.hide(RTSReplay.recentList,next);
    else if(target==='message') RTSReplayMessages.hideMessage(next);
    else {
      const card=RTSReplayMessages.clapperCard, profile=RTSAnimationEngine.readProfile(next.replayClapperAnimation);
      const runner=RTSAnimationEngine.createRunner({target:card?.querySelector('.clapper-position')});
      runner.configure(next.replayClapperPositions); runner.runEnd(profile?.end,()=>{card?.classList.remove('show');card?.setAttribute('aria-hidden','true');});
    }
  };
  const testPosition = () => { const next=setCommand('player'), from=controls('player','from').value, to=controls('player','to').value, duration=Math.max(.1,Number(controls('player','duration').value)||1)*1000, ease=easing(sectionFor('player')).value; next.replayStartPosition=from; next.replayEndPosition=to; next.replayAnimationDuration=duration; next.replayAnimationEasing=ease; RTSReplayVideo.currentCommand=next; RTSReplay.command=next; showPlayer(); RTSReplayVideo.animateIn(RTSReplayVideo.getPosition(from),RTSReplayVideo.getPosition(to)); };
  const previewTitle = () => { showPlayer(); const title=RTSReplay?.title; if(!title)return; RTSReplay.hideTitle?.(); title.className=`title-${document.getElementById('rts-dev-title-style').value} title-${document.getElementById('rts-dev-title-position').value}`; title.textContent=document.getElementById('rts-dev-title').value.trim()||'FIRST TEST — REPLAY CAPTURE'; title.classList.add('visible','title-enter'); };
  const setSpeed = value => { const speed=Number(value); if(!Number.isFinite(speed)||!RTSReplay?.video)return; RTSReplay.video.playbackRate=speed; const next={...command(),replayPlaybackSpeed:speed}; RTSReplay.command=next; RTSReplayVideo.currentCommand=next; RTSReplayElements?.configureSpeed?.(next); };

  bar.addEventListener('click', event => {
    const button=event.target.closest('button'); if(!button)return;
    const section=button.closest('.dev-section'), target=section?.dataset.target, action=button.dataset.action;
    if(action==='show') target==='player' ? (RTSReplay.player?.classList.contains('show')?hidePlayer():(setCommand('player'),showPlayer())) : (target==='clapper' ? (RTSReplayMessages.clapperCard?.classList.contains('show')?hideClapper():showClapper()) : (target==='message'||target==='panel' ? ((target==='message'?RTSReplay.messageCard:RTSReplay.recentList)?.classList.contains('show')?hidePanel(target):showPanel(target)) : null));
    if(action==='hide') target==='player'?hidePlayer():target==='clapper'?hideClapper():hidePanel(target);
    if(action==='in') playIn(target);
    if(action==='out') playOut(target);
    if(action==='title') previewTitle();
    if(action==='position-test') testPosition();
    if(action==='refresh') refresh();
  });
  bar.addEventListener('change', event => {
    const control=event.target.closest('[data-control]'), section=event.target.closest('.dev-section');
    if(!control||!section)return;
    const target=section.dataset.target;
    if(control.dataset.control==='profile'||control.dataset.control==='brand') setCommand(target);
    if(control.dataset.control==='style' && target==='panel' && RTSReplay.recentList?.classList.contains('show')) showPanel('panel');
    if(control.dataset.control==='text' && target==='message' && RTSReplay.messageCard?.classList.contains('show')) showPanel('message');
  });
  bar.querySelector('#rts-dev-title-style').addEventListener('change',previewTitle);
  bar.querySelector('#rts-dev-title-position').addEventListener('change',previewTitle);
  bar.querySelector('#rts-dev-speed').addEventListener('change',event=>setSpeed(event.target.value));
  refresh();
})();