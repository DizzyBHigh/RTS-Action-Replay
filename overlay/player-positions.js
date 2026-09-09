const RTSReplayPlayer = window.RTSReplay;

RTSReplayPlayer.defaultPositions = {
  "Full Screen": { scale: 100, x: 0, y: 0, rotateX: 0, rotateY: 0, rotateZ: 0 }
};

RTSReplayPlayer.getPositions = () => {
  try {
    const raw = RTSReplayPlayer.currentCommand?.replayPositions;
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return parsed && typeof parsed === 'object' ? parsed : RTSReplayPlayer.defaultPositions;
  } catch (_) { return RTSReplayPlayer.defaultPositions; }
};

RTSReplayPlayer.getPosition = name => {
  const positions = RTSReplayPlayer.getPositions();
  if (positions[name]) return positions[name];
  const target = String(name || '').trim().toLowerCase();
  if (target) {
    const match = Object.keys(positions).find(key => {
      const position = positions[key];
      return key.toLowerCase() === target || String(position?.tag || '').trim().toLowerCase() === target;
    });
    if (match) return positions[match];
  }
  return positions['Full Screen'] || RTSReplayPlayer.defaultPositions['Full Screen'];
};

RTSReplayPlayer.configureTransition = () => {
  const duration = Math.max(.1, Number(RTSReplayPlayer.currentCommand?.replayAnimationDuration) || .5);
  RTSReplayPlayer.player.style.setProperty('--player-duration', `${duration}s`);
  RTSReplayPlayer.player.style.setProperty('--player-easing', RTSReplayPlayer.currentCommand?.replayAnimationEasing || 'ease-in-out');
  RTSReplayPlayer.player.classList.add('player-transition');
};

RTSReplayPlayer.transformFor = (p, scaleFactor = 1) => {
  const rawScale = Number(p?.scale);
  const scale = (Number.isFinite(rawScale) ? rawScale : 100) / 100 * scaleFactor;
  const x = Number(p?.x), y = Number(p?.y);
  const rotateX = Number(p?.rotateX), rotateY = Number(p?.rotateY), rotateZ = Number(p?.rotateZ);
  return `translate(-50%, -50%) translate(${Number.isFinite(x) ? x : 0}%, ${Number.isFinite(y) ? y : 0}%) scale(${scale}) rotateX(${Number.isFinite(rotateX) ? rotateX : 0}deg) rotateY(${Number.isFinite(rotateY) ? rotateY : 0}deg) rotateZ(${Number.isFinite(rotateZ) ? rotateZ : 0}deg)`;
};

RTSReplayPlayer.applyPosition = (position, immediate = false) => {
  const p = position || RTSReplayPlayer.defaultPositions['Full Screen'];
  if (immediate) RTSReplayPlayer.player.classList.remove('player-transition');
  RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(p);
  if (immediate) requestAnimationFrame(RTSReplayPlayer.configureTransition);
};

RTSReplayPlayer.animateIn = (startPosition, endPosition) => {
  const start = startPosition || RTSReplayPlayer.defaultPositions['Full Screen'];
  const end = endPosition || start;
  RTSReplayPlayer.configureTransition();
  RTSReplayPlayer.player.classList.add('show');
  RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(start);
  requestAnimationFrame(() => requestAnimationFrame(() => {
    RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(end);
    RTSReplayPlayer.activePosition = end;
  }));
};

RTSReplayPlayer.animateOut = () => {
  const start = RTSReplayPlayer.activePosition || RTSReplayPlayer.defaultPositions['Full Screen'];
  const end = RTSReplayPlayer.getPosition(RTSReplayPlayer.currentCommand?.replayStartPosition || 'Full Screen');
  RTSReplayPlayer.configureTransition();
  RTSReplayPlayer.player.classList.add('show');
  RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(start);
  requestAnimationFrame(() => requestAnimationFrame(() => {
    RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(end);
  }));
  RTSReplayPlayer.player.addEventListener('transitionend', event => {
    if (event.propertyName === 'transform') RTSReplayPlayer.player.classList.remove('show');
  }, { once: true });
};
