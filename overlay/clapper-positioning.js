const RTSReplayClapper = window.RTSReplay;

RTSReplayClapper.defaultPositions = {
  Centered: { scale: 50, scaleX: 50, scaleY: 50, x: 0, y: 0, z: 0, rotateX: 0, rotateY: 0, rotateZ: 0, fov: 90 }
};

RTSReplayClapper.numberOr = (value, fallback) => {
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
};

RTSReplayClapper.getPositions = command => {
  try {
    const raw = command?.replayClapperPositions;
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return parsed && typeof parsed === 'object' ? parsed : RTSReplayClapper.defaultPositions;
  } catch (_) { return RTSReplayClapper.defaultPositions; }
};

RTSReplayClapper.getPosition = (command, name) => {
  const positions = RTSReplayClapper.getPositions(command);
  if (positions[name]) return positions[name];
  const target = String(name || '').trim().toLowerCase();
  const match = Object.keys(positions).find(key => {
    const position = positions[key];
    return key.toLowerCase() === target || String(position?.tag || '').trim().toLowerCase() === target;
  });
  return match ? positions[match] : positions.Centered || RTSReplayClapper.defaultPositions.Centered;
};

RTSReplayClapper.transformFor = position => {
  const legacyScale = RTSReplayClapper.numberOr(position?.scale, 50) / 100;
  const scaleX = RTSReplayClapper.numberOr(position?.scaleX, legacyScale * 100) / 100;
  const scaleY = RTSReplayClapper.numberOr(position?.scaleY, legacyScale * 100) / 100;
  const x = RTSReplayClapper.numberOr(position?.x, 0);
  const y = RTSReplayClapper.numberOr(position?.y, 0);
  const z = RTSReplayClapper.numberOr(position?.z, 0);
  const rotateX = -RTSReplayClapper.numberOr(position?.rotateX, 0);
  const rotateY = RTSReplayClapper.numberOr(position?.rotateY, 0);
  const rotateZ = -RTSReplayClapper.numberOr(position?.rotateZ, 0);
  const fov = Math.max(30, Math.min(120, RTSReplayClapper.numberOr(position?.fov, 90)));
  const width = Math.max(1, window.innerWidth || 1920);
  const perspective = Math.max(1, (width / 2) / Math.tan((fov * Math.PI / 180) / 2));
  return `perspective(${perspective}px) translate(-50%, -50%) translate3d(${x}vw, ${-y}vh, ${z}px) rotateZ(${rotateZ}deg) rotateY(${rotateY}deg) rotateX(${rotateX}deg) scale3d(${scaleX}, ${scaleY}, 1)`;
};

RTSReplayClapper.applyPosition = (command, name) => {
  const position = RTSReplayClapper.getPosition(command, name || 'Centered');
  RTSReplayClapper.messageCard.style.transform = RTSReplayClapper.transformFor(position);
  RTSReplayClapper.activePosition = position;
};
