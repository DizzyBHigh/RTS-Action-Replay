(() => {
  const params = new URLSearchParams(window.location.search);
  if (params.get('dev') !== 'true') return;
  const bar = document.getElementById('rts-dev-toolbar');
  if (!bar) return;

  const targets = {
    player:{label:'Player',positions:'replayPlayerPositions',profiles:'replayAnimationProfiles',element:()=>RTSReplay?.player,target:()=>RTSReplay?.player},
    panel:{label:'Panel',positions:'replayPanelPositions',profiles:'replayPanelAnimationProfiles',element:()=>RTSReplay?.recentList,target:()=>RTSReplay?.recentList},
    clapperboard:{label:'Clapperboard',positions:'replayClapperPositions',profiles:'replayClapperAnimationProfiles',element:()=>RTSReplay?.clapperCard,target:()=>RTSReplay?.clapperCard?.querySelector('.clapper-position')},
    message:{label:'Message',positions:'replayMessagePositions',profiles:'replayMessageAnimationProfiles',element:()=>RTSReplay?.messageCard,target:()=>RTSReplay?.messageCard}
  };
  const state={};
  const command=()=>({...window.RTSReplaySettingsSync?.command,...RTSReplay?.command});
  const config=()=>window.rtsOverlayConfig||{};
  const parse=v=>{try{return typeof v==='string'?JSON.parse(v||'{}'):v}catch(_){return null}};
  const profiles=t=>Array.isArray(command()[t.profiles])?command()[t.profiles]:[];
  const positions=t=>parse(command()[t.positions])||{};
  const profile=(t,id)=>profiles(t).find(p=>String(p.id)===String(id))||profiles(t)[0];
  const branding=id=>(config().presets?.branding||[]).find(b=>String(b.id)===String(id))||(config().presets?.branding||[])[0];

  const applyBranding=(kind,id)=>{
    const b=branding(id); if(!b)return;
    const c={...command(),replayBrandingPresetId:b.id,replayBrandLogoUrl:b.logo||'',replayBrandFallbackText:b.fallbackText||'RTS',replayBrandLabel:b.brandLabel||'ACTION REPLAY',replayBrandFallbackTextColor:b.primaryColor||'#0384CBFF',replayBrandLabelColor:b.textColor||'#FFFFFFFF',replayBrandPrimaryColor:b.primaryColor||'#0384CBFF',replayBrandSecondaryColor:b.secondaryColor||'#101416FF',replayMessageBoardColor:b.textColor||'#FFFFFFFF',replayMessageTextColor:b.titleColor||'#FFFFFFFF',replayMessageStripeLight:b.primaryColor||'#0384CBFF',replayMessageStripeDark:b.secondaryColor||'#101416FF',replayMessageAccent:b.shadowColor||'#000000FF',replayMessageFont:b.font||'Inter'};
    RTSReplay.command=c; RTSReplay.currentCommand=c;
    if(kind==='player') RTSReplayElements?.configureBranding?.(c);
    if(kind==='panel'){Object.assign(c,{replayPanelPrimaryColor:b.primaryColor,replayPanelSecondaryColor:b.secondaryColor,replayPanelTitleFont:b.font||'Inter',replayPanelTitleColor:b.textColor||'#fff',replayPanelListColor:b.textColor||'#fff',replayPanelListShadowColor:b.shadowColor||'#000'}); RTSInformationPanelPresets?.apply?.(RTSReplay.recentList,c);}
    if(kind==='message') RTSReplayMessages?.showMessage?.({...c,replayMessage:'MESSAGE BRANDING PREVIEW',replayMessageDuration:999999});
    if(kind==='clapperboard') RTSReplayMessages?.showClapperboard?.({...c,replayMessage:'CLAPPERBOARD BRANDING PREVIEW',replayClapperDuration:999999});
    state[kind].command=c;
  };

  const show=t=>{
    const el=t.element(); if(!el)return;
    el.classList.add('show'); el.setAttribute('aria-hidden','false');
    if(t.label==='Player'){el.classList.add('dev-player');el.style.opacity='1';el.style.visibility='visible';RTSReplay.frame?.classList.add('dev-frame');}
    if(t.label==='Panel') RTSPositionPreview?.previewPanelPosition?.({...command(),replayPanelPosition:state.panel.position});
    if(t.label==='Message') RTSReplayMessages?.showMessage?.({...command(),replayMessage:'MESSAGE TEST PREVIEW',replayMessageDuration:999999});
    if(t.label==='Clapperboard') RTSReplayMessages?.showClapperboard?.({...command(),replayMessage:'CLAPPERBOARD TEST PREVIEW',replayClapperDuration:999999});
  };
  const runner=t=>{const target=t.target();if(!target)return null;const r=RTSAnimationEngine.createRunner({target});r.configure(command()[t.positions]);return r};
  const hide=t=>{
    const el=t.element(); if(!el)return;
    el.classList.remove('show');
    el.setAttribute('aria-hidden','true');
    if(t.label==='Player'){
      el.classList.remove('dev-player');
      el.style.opacity='';
      el.style.visibility='';
      RTSReplay.frame?.classList.remove('dev-frame');
    }
  };
  const animateTo=(kind,name,duration=500,easing='ease-in-out')=>{
    const t=targets[kind]; show(t); const r=runner(t); if(!r)return;
    const next=r.resolve(name); const start=r.getActive()||next; r.transition(start,next,duration,easing); state[kind].position=name;
  };
  const showDuration=kind=>{const c=command();if(kind==='message')return Math.max(0,Number(c.replayMessageDuration)||5000);if(kind==='clapperboard')return Math.max(0,Number(c.replayClapperDuration)||5000);if(kind==='player')return Number.isFinite(RTSReplay?.video?.duration)?Math.max(0,RTSReplay.video.duration*1000):5000;return 5000};
  const runProfile=(kind,mode)=>{
    const t=targets[kind], s=state[kind], p=profile(t,s.profile); if(!p)return; show(t); const r=runner(t);
    if(mode==='complete')r.run(p.start,()=>setTimeout(()=>r.runEnd(p.end),showDuration(kind)));
    else if(mode==='end')r.runEnd(p.end); else r.run(p.start);
  };

  const refreshUI=()=>{
    Object.entries(targets).forEach(([kind,t])=>{
      const root=bar.querySelector('[data-dev-element="'+kind+'"]');if(!root)return;
      const pos=root.querySelector('[data-pos]'),psel=root.querySelector('[data-profile]'),bsel=root.querySelector('[data-brand]');
      const names=Object.keys(positions(t));pos.replaceChildren(...names.map(n=>new Option(n,n)));
      if(names.length)pos.value=state[kind].position&&names.includes(state[kind].position)?state[kind].position:names[0];
      const ps=profiles(t);psel.replaceChildren(...ps.map(p=>new Option(p.name||p.id,p.id)));
      if(ps.length)psel.value=state[kind].profile&&ps.some(p=>p.id===state[kind].profile)?state[kind].profile:ps[0].id;
      state[kind].position=pos.value;state[kind].profile=psel.value;
      const bs=config().presets?.branding||[];bsel.replaceChildren(...bs.map(b=>new Option(b.name||b.id,b.id)));
      if(bs.length)bsel.value=state[kind].branding&&bs.some(b=>b.id===state[kind].branding)?state[kind].branding:(command().replayBrandingPresetId||bs[0].id);
      state[kind].branding=bsel.value;
    });
    bar.querySelector('[data-dev-status]').textContent=window.rtsOverlayConfig?'Configuration loaded from Streamer.bot':'Waiting for configuration';
  };

  const refreshFromStreamerBot=()=>{
    const log=window.RTSDevToolbar?.log||((...args)=>console.debug('[RTS DEV]',...args));
    const socket=window.RTSReplay?.socket;
    log('Refresh clicked', {
      replay:!!window.RTSReplay,
      requestAction:typeof window.RTSReplay?.requestAction,
      socket:!!socket,
      readyState:socket?.readyState,
      readyStateName:['CONNECTING','OPEN','CLOSING','CLOSED'][socket?.readyState]||'NONE'
    });
    if(!window.RTSReplay?.requestAction){
      log('Refresh failed: requestAction is unavailable');
      return false;
    }
    if(!socket||socket.readyState!==WebSocket.OPEN){
      log('Refresh failed: WebSocket is not open');
      return false;
    }
    const requested=window.RTSReplay.requestAction(
      {name:'RTS - Action Replay - Core - Resolver'},
      {rtsDevConfigRefresh:'true'},
      'rts-dev-config-'+Date.now()
    );
    log(requested?'Refresh DoAction sent':'Refresh DoAction failed');
    return requested;
  };

  bar.innerHTML='<div class="dev-toolbar-header"><strong>RTS DEV</strong><span>TEST HARNESS</span></div><div class="dev-harness-tools"><button class="dev-accent" data-refresh>Refresh Configuration</button><button data-theme>☾ Dark</button></div><div class="dev-harness"><div class="dev-status" data-dev-status>Waiting for configuration</div>'
    +Object.entries(targets).map(([kind,t])=>'<section class="dev-section" data-dev-element="'+kind+'"><div class="dev-section-head"><h3>'+t.label+'</h3><div class="dev-visibility"><button data-show>Show</button><button data-hide>Hide</button></div></div>'
    +'<div class="dev-subsection"><div class="dev-name"><span>Positions</span><small>animate to</small></div><div class="dev-row"><select data-pos></select><button data-position>Animate</button></div>'
    +'<div class="dev-row equal"><label>Start<select data-start></select></label><label>End<select data-end></select></label></div>'
    +'<div class="dev-row equal"><label>Easing<select data-easing><option>linear</option><option>ease-in</option><option>ease-out</option><option selected>ease-in-out</option></select></label><label>Duration (ms)<input data-duration type="number" min="0" step="50" value="500"></label></div>'
    +'<button data-transition>Animate Transition</button></div>'
    +'<div class="dev-subsection"><div class="dev-name"><span>Animation Presets</span><small>existing profiles</small></div><select data-profile></select><div class="dev-profile"><button data-start-profile>Play Start</button><button data-end-profile>Play End</button><button data-complete>Play Complete</button></div></div>'
    +'<div class="dev-subsection"><div class="dev-name"><span>Branding Presets</span><small>style element</small></div><div class="dev-row"><select data-brand></select><button data-brand-apply>Apply</button></div></div></section>').join('')
    +'</div>';

  Object.keys(targets).forEach(k=>state[k]={position:'',profile:'',branding:'',start:'',end:''});
  const setTheme=()=>{const light=bar.classList.toggle('dev-light');bar.querySelector('[data-theme]').textContent=light?'☀ Light':'☾ Dark';localStorage.setItem('rts-dev-theme',light?'light':'dark')};
  if(localStorage.getItem('rts-dev-theme')==='light')bar.classList.add('dev-light');

  bar.querySelector('[data-refresh]')?.addEventListener('click',e=>{
    e.preventDefault();
    refreshFromStreamerBot();
  });

  bar.addEventListener('click',e=>{
    const b=e.target.closest('button');if(!b)return;
    if(b.dataset.theme)setTheme();
    const root=b.closest('[data-dev-element]');if(!root)return;const kind=root.dataset.devElement;
    if(b.dataset.show)show(targets[kind]);
    if(b.dataset.hide)hide(targets[kind]);
    if(b.dataset.position)animateTo(kind,root.querySelector('[data-pos]').value);
    if(b.dataset.transition){animateTo(kind,root.querySelector('[data-start]').value,0);animateTo(kind,root.querySelector('[data-end]').value,Math.max(0,Number(root.querySelector('[data-duration]').value)||500),root.querySelector('[data-easing]').value)}
    if(b.dataset.startProfile)runProfile(kind,'start');if(b.dataset.endProfile)runProfile(kind,'end');if(b.dataset.complete)runProfile(kind,'complete');if(b.dataset.brandApply)applyBranding(kind,root.querySelector('[data-brand]').value);
  });
  bar.addEventListener('change',e=>{
    const root=e.target.closest('[data-dev-element]');if(!root)return;const kind=root.dataset.devElement;
    if(e.target.dataset.pos)state[kind].position=e.target.value;if(e.target.dataset.start)state[kind].start=e.target.value;if(e.target.dataset.end)state[kind].end=e.target.value;
    if(e.target.dataset.profile)state[kind].profile=e.target.value;if(e.target.dataset.brand)state[kind].branding=e.target.value;
  });

  const syncPositionSelectors=()=>bar.querySelectorAll('[data-dev-element]').forEach(root=>{
    const names=Object.keys(positions(targets[root.dataset.devElement]));
    ['[data-start]','[data-end]'].forEach(sel=>{const s=root.querySelector(sel),old=s.value;s.replaceChildren(...names.map(n=>new Option(n,n)));if(names.includes(old))s.value=old;else if(names.length)s.value=names[0]});
  });
  window.RTSDevToolbar={...(window.RTSDevToolbar||{}),refresh:()=>{refreshUI();syncPositionSelectors()},refreshFromStreamerBot};
  window.addEventListener('rts-overlay-config',()=>{refreshUI();syncPositionSelectors()});
  window.RTSDevToolbar.refresh();
})();