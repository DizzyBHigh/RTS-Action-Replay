(() => {
  const params=new URLSearchParams(location.search); if(params.get('dev')!=='true')return;
  const screen=document.createElement('div'); screen.id='rts-dev-screen'; screen.innerHTML='<div class="dev-screen-label">1920 × 1080</div><div class="dev-safe-area"></div>'; document.body.prepend(screen);
  const bar=document.createElement('aside'); bar.id='rts-dev-toolbar'; document.body.prepend(bar);
  const targets={
    player:{label:'Player',pos:'replayPlayerPositions',profiles:'replayAnimationProfiles',el:()=>RTSReplay?.player,target:()=>RTSReplay?.player},
    panel:{label:'Panel',pos:'replayPanelPositions',profiles:'replayPanelAnimationProfiles',el:()=>RTSReplay?.recentList,target:()=>RTSReplay?.recentList},
    message:{label:'Message',pos:'replayMessagePositions',profiles:'replayMessageAnimationProfiles',el:()=>RTSReplay?.messageCard,target:()=>RTSReplay?.messageCard},
    clapper:{label:'Clapperboard',pos:'replayClapperPositions',profiles:'replayClapperAnimationProfiles',el:()=>RTSReplay?.clapperCard,target:()=>RTSReplay?.clapperCard?.querySelector('.clapper-position')}
  };
  const state=Object.fromEntries(Object.keys(targets).map(k=>[k,{position:'',profile:'',branding:''}]));
  const parse=v=>{try{return typeof v==='string'?JSON.parse(v||'{}'):v}catch(_){return null}};
  const command=()=>({...window.RTSReplaySettingsSync?.command,...RTSReplay?.command});
  const positions=t=>parse(command()[t.pos])||{}; const profiles=t=>{const p=parse(command()[t.profiles]);return Array.isArray(p)?p:[]};
  const branding=()=>window.rtsOverlayConfig?.presets?.branding||[];
  const selected=(t)=>profiles(t).find(p=>String(p.id)===String(state[t.key].profile))||profiles(t)[0];
  const runner=t=>{const target=t.target();if(!target)return null;const r=RTSAnimationEngine.createRunner({target});r.configure(command()[t.pos]);return r};
  const show=(kind)=>{
    const t=targets[kind],c={...command()};
    if(kind==='player'){const e=RTSReplay?.player;if(!e)return;e.classList.add('dev-player','show');e.style.opacity='1';e.style.visibility='visible';e.setAttribute('aria-hidden','false');RTSReplay.frame?.classList.add('dev-frame');}
    if(kind==='panel'){const panel=RTSReplay?.recentList;if(!panel||!RTSReplay?.showRecentList)return;const cmd={...c,replayRecent:'dev-preview',replayRecentData:JSON.stringify([{number:'01',title:'FIRST TEST — REPLAY CAPTURE'},{number:'02',title:'GTA V — ACTION REPLAY'},{number:'03',title:'CINEMATIC DRIVE'},{number:'04',title:'LATEST REPLAY'}]),replayPanelPosition:state[kind].position||'Centered'};panel._rtsPanelAnimationCommand=cmd;RTSReplay.showRecentList(cmd);}
    if(kind==='message')RTSReplayMessages?.showMessage?.({...c,replayMessage:'MESSAGE TEST PREVIEW',replayMessageDuration:999999});
    if(kind==='clapper')RTSReplayMessages?.showClapperboard?.({...c,replayMessage:'CLAPPERBOARD TEST PREVIEW',replayClapperDuration:999999});
  };
  const hide=kind=>{
    if(kind==='player'){const e=RTSReplay?.player;if(!e)return;RTSReplayVideo.cancelAnimation();e.classList.remove('show','dev-player');e.style.opacity='';e.style.visibility='';e.setAttribute('aria-hidden','true');RTSReplay.frame?.classList.remove('dev-frame');return;}
    if(kind==='panel'){const e=RTSReplay?.recentList;if(!e)return;RTSInformationPanelAnimation?.cancel?.();e.classList.remove('show');e.setAttribute('aria-hidden','true');return;}
    if(kind==='message'){RTSReplayMessages?.hideMessage?.(command());return;}
    if(kind==='clapper'){const e=RTSReplayMessages?.clapperCard;if(!e)return;clearTimeout(RTSReplay.clapperboardTimer);RTSAnimationEngine?.createRunner?.({target:e.querySelector('.clapper-position')})?.cancel?.();e.classList.remove('show');e.style.opacity='';e.style.visibility='';e.style.zIndex='';e.setAttribute('aria-hidden','true');return;}
  };
  const playProfile=(kind,mode)=>{
    const t=targets[kind],p=selected(t);if(!p)return;show(kind);const r=runner(t);if(!r)return;
    const start=p.startSequence||p.start||[],end=p.endSequence||p.end||[];
    if(mode==='start')r.run(start);else if(mode==='end')r.runEnd(end);else r.run(start,()=>setTimeout(()=>r.runEnd(end),5000));
  };
  const applyBranding=(kind)=>{
    const b=branding().find(x=>String(x.id)===String(state[kind].branding))||branding()[0];if(!b)return;
    const c={...command(),replayBrandingPresetId:b.id,replayBrandLogoUrl:b.logo||'',replayBrandFallbackText:b.fallbackText||'RTS',replayBrandLabel:b.brandLabel||'ACTION REPLAY',replayBrandPrimaryColor:b.primaryColor,replayBrandSecondaryColor:b.secondaryColor,replayBrandFallbackTextColor:b.primaryColor,replayBrandLabelColor:b.textColor,replayMessageTextColor:b.titleColor,replayMessageStripeLight:b.primaryColor,replayMessageStripeDark:b.secondaryColor,replayMessageAccent:b.shadowColor,replayMessageFont:b.font||'Inter'};
    RTSReplay.command=c; if(kind==='player')RTSReplayElements?.configureBranding?.(c);
    if(kind==='panel')RTSInformationPanelPresets?.apply?.(RTSReplay.recentList,c);
    if(kind==='message')RTSReplayMessages?.showMessage?.({...c,replayMessage:'MESSAGE BRANDING PREVIEW',replayMessageDuration:999999});
    if(kind==='clapper')RTSReplayMessages?.showClapperboard?.({...c,replayMessage:'CLAPPERBOARD BRANDING PREVIEW',replayClapperDuration:999999});
  };
  const animate=(kind,name,duration=500,easing='ease-in-out')=>{show(kind);const r=runner(targets[kind]);if(!r)return;r.transition(r.getActive()||r.resolve(name),r.resolve(name),duration,easing);state[kind].position=name};
  const refresh=()=>{
    Object.entries(targets).forEach(([kind,t])=>{const root=bar.querySelector('[data-dev-element="'+kind+'"]');if(!root)return;
      const pos=root.querySelector('[data-pos]'),prof=root.querySelector('[data-profile]'),brand=root.querySelector('[data-brand]'),names=Object.keys(positions(t)),ps=profiles(t),bs=branding();
      pos.replaceChildren(...names.map(n=>new Option(n,n)));prof.replaceChildren(...ps.map(p=>new Option(p.name||p.id,p.id)));brand.replaceChildren(...bs.map(b=>new Option(b.name||b.id,b.id)));
      if(names.length)pos.value=state[kind].position&&names.includes(state[kind].position)?state[kind].position:names[0];if(ps.length)prof.value=state[kind].profile&&ps.some(p=>p.id===state[kind].profile)?state[kind].profile:ps[0].id;if(bs.length)brand.value=state[kind].branding&&bs.some(b=>b.id===state[kind].branding)?state[kind].branding:(command().replayBrandingPresetId||bs[0].id);
      state[kind].position=pos.value;state[kind].profile=prof.value;state[kind].branding=brand.value;
    }); bar.querySelector('[data-dev-status]').textContent=window.rtsOverlayConfig?'Configuration loaded from Streamer.bot':'Waiting for configuration';
  };
  bar.innerHTML='<div class="dev-toolbar-header"><strong>RTS DEV</strong><span>DEV CONTROLS</span></div><div class="dev-harness-tools"><button class="dev-accent" data-refresh>Refresh Configuration</button></div><div class="dev-harness"><div class="dev-status" data-dev-status>Waiting for configuration</div>'+
    Object.entries(targets).map(([kind,t])=>'<section class="dev-section" data-dev-element="'+kind+'"><div class="dev-section-head"><h3>'+t.label+'</h3><div class="dev-visibility"><button data-show>Show</button><button data-hide>Hide</button></div></div><div class="dev-subsection"><div class="dev-name"><span>Positions</span><small>existing positions</small></div><div class="dev-row"><select data-pos></select><button data-position>Animate</button></div><div class="dev-row equal"><label>Start<select data-start></select></label><label>End<select data-end></select></label></div><div class="dev-row equal"><label>Easing<select data-easing><option>linear</option><option>ease-in</option><option>ease-out</option><option selected>ease-in-out</option></select></label><label>Duration<input data-duration type="number" min="0" step="50" value="500"></label></div><button data-transition>Animate Transition</button></div><div class="dev-subsection"><div class="dev-name"><span>Animation Presets</span><small>existing profiles</small></div><select data-profile></select><div class="dev-profile"><button data-start-profile>Play Start</button><button data-end-profile>Play End</button><button data-complete>Play Complete</button></div></div><div class="dev-subsection"><div class="dev-name"><span>Branding Presets</span><small>existing branding</small></div><div class="dev-row"><select data-brand></select><button data-brand-apply>Apply</button></div></div></section>').join('')+
    '</div>';
  Object.keys(targets).forEach(k=>state[k].key=k);
  const refreshFromStreamerBot=()=>{const socket=window.RTSReplay?.socket;if(!window.RTSReplay?.requestAction||!socket||socket.readyState!==WebSocket.OPEN)return false;return window.RTSReplay.requestAction({name:'RTS - Action Replay - Core - Resolver'},{rtsDevConfigRefresh:'true'},'rts-dev-config-'+Date.now())};
  bar.querySelector('[data-refresh]').onclick=e=>{e.preventDefault();refreshFromStreamerBot()};
  bar.addEventListener('click',e=>{const b=e.target.closest('button');if(!b)return;const root=b.closest('[data-dev-element]');if(!root)return;const k=root.dataset.devElement;
    if(b.dataset.show)show(k);if(b.dataset.hide)hide(k);if(b.dataset.position)animate(k,root.querySelector('[data-pos]').value);
    if(b.dataset.transition){animate(k,root.querySelector('[data-start]').value,0);animate(k,root.querySelector('[data-end]').value,Number(root.querySelector('[data-duration]').value)||500,root.querySelector('[data-easing]').value)}
    if(b.dataset.startProfile)playProfile(k,'start');if(b.dataset.endProfile)playProfile(k,'end');if(b.dataset.complete)playProfile(k,'complete');if(b.dataset.brandApply)applyBranding(k);
  });
  bar.addEventListener('change',e=>{const root=e.target.closest('[data-dev-element]');if(!root)return;const k=root.dataset.devElement;if(e.target.dataset.pos)state[k].position=e.target.value;if(e.target.dataset.profile)state[k].profile=e.target.value;if(e.target.dataset.brand)state[k].branding=e.target.value;if(e.target.dataset.start)state[k].start=e.target.value;if(e.target.dataset.end)state[k].end=e.target.value});
  window.addEventListener('rts-overlay-config',refresh);
  window.RTSDevToolbar={...(window.RTSDevToolbar||{}),refresh,refreshFromStreamerBot};
  refresh();
})();