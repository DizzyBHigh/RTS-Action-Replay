const RTSReplayPlayer = window.RTSReplay;

RTSReplayPlayer.defaultPositions = {
  "Full Screen": { scale: 100, scaleX: 100, scaleY: 100, x: 0, y: 0, z: 0, rotateX: 0, rotateY: 0, rotateZ: 0, fov: 90 }
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

RTSReplayPlayer.transformFor = (p, scaleFactor = 1) => {
  const legacyScale = RTSReplayPlayer.numberOr(p?.scale, 100) / 100;
  const scaleX = RTSReplayPlayer.numberOr(p?.scaleX, legacyScale * 100) / 100 * scaleFactor;
  const scaleY = RTSReplayPlayer.numberOr(p?.scaleY, legacyScale * 100) / 100 * scaleFactor;
  const z = RTSReplayPlayer.numberOr(p?.z, 0);

  // RtsUI uses +Y as up. CSS screen coordinates use +Y down. Convert the
  // coordinate-system handedness at the overlay boundary so stored position
  // values remain identical between the editor and the browser.
  const rotateX = -RTSReplayPlayer.numberOr(p?.rotateX, 0);
  const rotateY = RTSReplayPlayer.numberOr(p?.rotateY, 0);
  const rotateZ = -RTSReplayPlayer.numberOr(p?.rotateZ, 0);
  const fov = Math.max(30, Math.min(120, RTSReplayPlayer.numberOr(p?.fov, 90)));

  // RtsUI uses a PerspectiveCamera. Its distance is calculated from the
  // horizontal preview width, so use the same calculation for the browser stage.
  const viewportWidth = Math.max(1, window.innerWidth || 1920);
  const perspective = Math.max(1, (viewportWidth / 2) / Math.tan((fov * Math.PI / 180) / 2));
  RTSReplayPlayer.stage.style.perspective = `${perspective}px`;
  RTSReplayPlayer.stage.style.perspectiveOrigin = 'center center';

  // Screen positioning is kept outside the 3D transform. The stage is the
  // camera/screen plane and the player is the 3D surface at its center.
  const x = RTSReplayPlayer.numberOr(p?.x, 0);
  const y = RTSReplayPlayer.numberOr(p?.y, 0);
  RTSReplayPlayer.stage.style.left = `calc(50% + ${x}vw)`;
  RTSReplayPlayer.stage.style.top = `calc(50% - ${y}vh)`;

  // RtsUI's Transform3DGroup is Scale -> RotateX -> RotateY -> RotateZ ->
  // Translate. CSS transform functions are composed right-to-left, so the
  // 3D operations are written in the reverse order here.
  return `translate(-50%, -50%) translateZ(${z}px) rotateZ(${rotateZ}deg) rotateY(${rotateY}deg) rotateX(${rotateX}deg) scale3d(${scaleX}, ${scaleY}, 1)`;
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
  RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(p);
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
    RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(current);
    if (rawProgress < 1) {
      RTSReplayPlayer.animationFrame = requestAnimationFrame(frame);
      return;
    }
    RTSReplayPlayer.animationFrame = null;
    RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(end);
    if (onComplete) onComplete();
  };

  RTSReplayPlayer.player.classList.remove('player-transition');
  RTSReplayPlayer.stage.classList.remove('player-transition');
  RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(start);
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

  RTSReplayPlayer.animatePosition(start, end, () => {
    RTSReplayPlayer.activePosition = end;
  });
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
