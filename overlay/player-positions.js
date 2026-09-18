const RTSReplayPlayer = window.RTSReplay;

RTSReplayPlayer.defaultPositions = {
  "Full Screen": { scale: 100, scaleX: 100, scaleY: 100, x: 0, y: 0, z: 0, rotateX: 0, rotateY: 0, rotateZ: 0, fov: 90 }
};

RTSReplayPlayer.getPositions = () => {
  try {
    const command = RTSReplayPlayer.currentCommand;
    const raw = command?.replayPlayerPositions ?? command?.replayPositions;
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
    const positions = parsed && typeof parsed === 'object' ? parsed : RTSReplayPlayer.defaultPositions;
    window.RTSDevToolbar?.log?.('Player positions lookup', {
      commandPresent: !!command,
      payloadPresent: raw != null,
      payloadType: typeof raw,
      payloadLength: typeof raw === 'string' ? raw.length : 0,
      count: Object.keys(positions).length,
      keys: Object.keys(positions)
    });
    return positions;
  } catch (error) {
    window.RTSDevToolbar?.log?.('Player positions lookup failed', { error: String(error) });
    return RTSReplayPlayer.defaultPositions;
  }
};

RTSReplayPlayer.resolvePositionTag = name => {
  const command = RTSReplayPlayer.currentCommand;
  const raw = command?.replayPlayerPositions;
  let positions = null;
  try {
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
    positions = parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : null;
  } catch (error) {
    window.RTSDevToolbar?.log?.('Player position payload parse failed', { error: String(error) });
  }

  window.RTSDevToolbar?.log?.('Player position resolver data', {
    commandPresent: !!command,
    payloadPresent: raw != null,
    payloadLength: typeof raw === 'string' ? raw.length : 0,
    count: positions ? Object.keys(positions).length : 0,
    keys: positions ? Object.keys(positions) : []
  });

  if (!positions) return null;

  const target = String(name || '').trim().toLowerCase();
  const match = Object.keys(positions).find(key => {
    const position = positions[key];
    return key.toLowerCase() === target || String(position?.tag || '').trim().toLowerCase() === target;
  });

  if (match) {
    window.RTSDevToolbar?.log?.('Player position matched by tag', { requested: name, key: match, position: positions[match] });
    return positions[match];
  }

  window.RTSDevToolbar?.log?.('Player position tag not found', { requested: name });
  return null;
};

RTSReplayPlayer.getPosition = name => RTSReplayPlayer.resolvePositionTag(name);

RTSReplayPlayer.configureTransition = () => {
  const rawDuration = Number(RTSReplayPlayer.currentCommand?.replayAnimationDuration);
  const duration = Math.max(.1, Number.isFinite(rawDuration) ? (rawDuration < 10 ? rawDuration : rawDuration / 1000) : .5);
  RTSReplayPlayer.player.style.setProperty('--player-duration', `${duration}s`);
  RTSReplayPlayer.player.style.setProperty('--player-easing', RTSReplayPlayer.currentCommand?.replayAnimationEasing || 'ease-in-out');
};

RTSReplayPlayer.numberOr = (value, fallback) => {
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
};

RTSReplayPlayer.normalisePosition = p => {
  const legacyScale = RTSReplayPlayer.numberOr(p?.scale, 100);
  return {
    scaleX: RTSReplayPlayer.numberOr(p?.scaleX, legacyScale),
    scaleY: RTSReplayPlayer.numberOr(p?.scaleY, legacyScale),
    x: RTSReplayPlayer.numberOr(p?.x, 0),
    y: RTSReplayPlayer.numberOr(p?.y, 0),
    z: RTSReplayPlayer.numberOr(p?.z, 0),
    rotateX: RTSReplayPlayer.numberOr(p?.rotateX, 0),
    rotateY: RTSReplayPlayer.numberOr(p?.rotateY, 0),
    rotateZ: RTSReplayPlayer.numberOr(p?.rotateZ, 0),
    fov: Math.max(30, Math.min(120, RTSReplayPlayer.numberOr(p?.fov, 90)))
  };
};

RTSReplayPlayer.easing = value => {
  const easing = String(value || 'ease-in-out').trim().toLowerCase();
  if (easing === 'linear') return t => t;
  if (easing === 'ease-in') return t => t * t * t;
  if (easing === 'ease-out') return t => 1 - Math.pow(1 - t, 3);
  if (easing === 'ease') return t => {
    if (t < 0.5) return 4 * t * t * t;
    return 1 - Math.pow(-2 * t + 2, 3) / 2;
  };
  return t => t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
};

RTSReplayPlayer.interpolatePosition = (from, to, progress) => {
  const a = RTSReplayPlayer.normalisePosition(from);
  const b = RTSReplayPlayer.normalisePosition(to);
  const lerp = (x, y) => x + (y - x) * progress;
  return {
    scaleX: lerp(a.scaleX, b.scaleX),
    scaleY: lerp(a.scaleY, b.scaleY),
    x: lerp(a.x, b.x),
    y: lerp(a.y, b.y),
    z: lerp(a.z, b.z),
    rotateX: lerp(a.rotateX, b.rotateX),
    rotateY: lerp(a.rotateY, b.rotateY),
    rotateZ: lerp(a.rotateZ, b.rotateZ),
    fov: lerp(a.fov, b.fov)
  };
};

RTSReplayPlayer.positionsEqual = (a, b) => {
  if (!a || !b) return false;
  const defaults = { scale: 100, scaleX: null, scaleY: null, x: 0, y: 0, z: 0, rotateX: 0, rotateY: 0, rotateZ: 0, fov: 90 };
  const get = (p, key) => {
    if (key === 'scaleX' || key === 'scaleY') return Number(p[key] ?? p.scale ?? 100);
    return Number(p[key] ?? defaults[key]);
  };
  return ['scaleX', 'scaleY', 'x', 'y', 'z', 'rotateX', 'rotateY', 'rotateZ', 'fov'].every(key => get(a, key) === get(b, key));
};

RTSReplayPlayer.applyPosition = (position, immediate = false) => {
  const p = position || RTSReplayPlayer.defaultPositions['Full Screen'];
  if (immediate) RTSReplayPlayer.cancelPendingTransition();
  RTSPositioningEngine.apply(RTSReplayPlayer.player, p);
};

RTSReplayPlayer.cancelPendingTransition = () => {
  RTSReplayPlayer.transitionToken = (RTSReplayPlayer.transitionToken || 0) + 1;
  if (RTSReplayPlayer.animationFrame) {
    cancelAnimationFrame(RTSReplayPlayer.animationFrame);
    RTSReplayPlayer.animationFrame = null;
  }
  RTSReplayPlayer.player.classList.remove('player-transition');
  RTSReplayPlayer.stage.classList.remove('player-transition');
};

RTSReplayPlayer.animatePosition = (startPosition, endPosition, onComplete) => {
  const start = startPosition || RTSReplayPlayer.defaultPositions['Full Screen'];
  const end = endPosition || start;
  const token = (RTSReplayPlayer.transitionToken || 0) + 1;
  RTSReplayPlayer.transitionToken = token;
  if (RTSReplayPlayer.animationFrame) cancelAnimationFrame(RTSReplayPlayer.animationFrame);
  const rawDuration = Number(RTSReplayPlayer.currentCommand?.replayAnimationDuration);
  const duration = Math.max(100, Number.isFinite(rawDuration) ? (rawDuration < 10 ? rawDuration * 1000 : rawDuration) : 500);
  const easing = RTSReplayPlayer.easing(RTSReplayPlayer.currentCommand?.replayAnimationEasing);
  const started = performance.now();
  const frame = now => {
    if (token !== RTSReplayPlayer.transitionToken) return;
    const rawProgress = Math.min(1, Math.max(0, (now - started) / duration));
    const progress = easing(rawProgress);
    const current = RTSReplayPlayer.interpolatePosition(start, end, progress);
    RTSPositioningEngine.apply(RTSReplayPlayer.player, current);
    if (rawProgress < 1) { RTSReplayPlayer.animationFrame = requestAnimationFrame(frame); return; }
    RTSReplayPlayer.animationFrame = null;
    RTSPositioningEngine.apply(RTSReplayPlayer.player, end);
    if (onComplete) onComplete();
  };
  RTSReplayPlayer.player.classList.remove('player-transition');
  RTSReplayPlayer.stage.classList.remove('player-transition');
  RTSPositioningEngine.apply(RTSReplayPlayer.player, start);
  RTSReplayPlayer.animationFrame = requestAnimationFrame(frame);
};

RTSReplayPlayer.animateIn = (startPosition, endPosition) => {
  const start = startPosition || RTSReplayPlayer.defaultPositions['Full Screen'];
  const end = endPosition || start;
  RTSReplayPlayer.cancelPendingTransition();
  RTSReplayPlayer.player.classList.add('show');
  RTSReplayPlayer.activePosition = start;
  if (RTSReplayPlayer.positionsEqual(start, end)) {
    RTSReplayPlayer.applyPosition(end, true);
    RTSReplayPlayer.activePosition = end;
    return;
  }
  RTSReplayPlayer.animatePosition(start, end, () => { RTSReplayPlayer.activePosition = end; });
};

RTSReplayPlayer.animateOut = () => {
  const start = RTSReplayPlayer.activePosition || RTSReplayPlayer.defaultPositions['Full Screen'];
  const end = RTSReplayPlayer.getPosition(RTSReplayPlayer.currentCommand?.replayStartPosition || 'Full Screen');
  RTSReplayPlayer.cancelPendingTransition();
  if (RTSReplayPlayer.positionsEqual(start, end)) {
    RTSReplayPlayer.player.classList.remove('show');
    RTSReplayPlayer.activePosition = end;
    return;
  }
  RTSReplayPlayer.player.classList.add('show');
  RTSReplayPlayer.animatePosition(start, end, () => {
    RTSReplayPlayer.player.classList.remove('show');
    RTSReplayPlayer.activePosition = end;
  });
};

RTSReplayPlayer.stage = document.getElementById('replay-player-stage');
