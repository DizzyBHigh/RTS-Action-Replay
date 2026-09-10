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
  RTSReplayPlayer.player.classList.add('player-transition');
};

RTSReplayPlayer.numberOr = (value, fallback) => {
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
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

  // RtsUI defines FOV against the horizontal preview dimension. Keep the
  // overlay's perspective calculation on the same basis.
  const viewportWidth = Math.max(1, window.innerWidth || 1920);
  const perspective = Math.max(1, (viewportWidth / 2) / Math.tan((fov * Math.PI / 180) / 2));

  const x = RTSReplayPlayer.numberOr(p?.x, 0);
  const y = RTSReplayPlayer.numberOr(p?.y, 0);
  RTSReplayPlayer.player.style.left = `calc(50% + ${x}vw)`;
  RTSReplayPlayer.player.style.top = `calc(50% - ${y}vh)`;

  // RtsUI's Transform3DGroup is built as Scale -> RotateX -> RotateY ->
  // RotateZ -> Translate. CSS transform functions are composed from right
  // to left, so reverse that order here while retaining the coordinate-axis
  // conversion above. This keeps the same world-space transform sequence.
  return `perspective(${perspective}px) translate(-50%, -50%) translateZ(${z}px) rotateZ(${rotateZ}deg) rotateY(${rotateY}deg) rotateX(${rotateX}deg) scale3d(${scaleX}, ${scaleY}, 1)`;
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
  if (immediate) RTSReplayPlayer.player.classList.remove('player-transition');
  RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(p);
};

RTSReplayPlayer.cancelPendingTransition = () => {
  RTSReplayPlayer.transitionToken = (RTSReplayPlayer.transitionToken || 0) + 1;
  RTSReplayPlayer.player.classList.remove('player-transition');
};

RTSReplayPlayer.animateIn = (startPosition, endPosition) => {
  const start = startPosition || RTSReplayPlayer.defaultPositions['Full Screen'];
  const end = endPosition || start;
  const token = (RTSReplayPlayer.transitionToken || 0) + 1;
  RTSReplayPlayer.transitionToken = token;
  RTSReplayPlayer.player.classList.remove('player-transition');
  RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(start);
  RTSReplayPlayer.player.classList.add('show');

  if (RTSReplayPlayer.positionsEqual(start, end)) {
    RTSReplayPlayer.activePosition = end;
    return;
  }

  requestAnimationFrame(() => {
    if (token !== RTSReplayPlayer.transitionToken) return;
    RTSReplayPlayer.configureTransition();
    requestAnimationFrame(() => {
      if (token !== RTSReplayPlayer.transitionToken) return;
      RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(end);
      RTSReplayPlayer.activePosition = end;
    });
  });
};

RTSReplayPlayer.animateOut = () => {
  const start = RTSReplayPlayer.activePosition || RTSReplayPlayer.defaultPositions['Full Screen'];
  const end = RTSReplayPlayer.getPosition(RTSReplayPlayer.currentCommand?.replayStartPosition || 'Full Screen');
  const token = (RTSReplayPlayer.transitionToken || 0) + 1;
  RTSReplayPlayer.transitionToken = token;

  if (RTSReplayPlayer.positionsEqual(start, end)) {
    RTSReplayPlayer.player.classList.remove('player-transition');
    RTSReplayPlayer.player.classList.remove('show');
    RTSReplayPlayer.activePosition = end;
    return;
  }

  RTSReplayPlayer.configureTransition();
  RTSReplayPlayer.player.classList.add('show');
  RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(start);

  const finish = event => {
    if (token !== RTSReplayPlayer.transitionToken) return;
    if (event && event.propertyName !== 'transform' && event.propertyName !== 'left' && event.propertyName !== 'top') return;
    RTSReplayPlayer.player.removeEventListener('transitionend', finish);
    RTSReplayPlayer.player.classList.remove('show');
    RTSReplayPlayer.player.classList.remove('player-transition');
    RTSReplayPlayer.activePosition = end;
  };

  RTSReplayPlayer.player.addEventListener('transitionend', finish);
  requestAnimationFrame(() => {
    if (token !== RTSReplayPlayer.transitionToken) {
      RTSReplayPlayer.player.removeEventListener('transitionend', finish);
      return;
    }
    RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(end);
  });
};
