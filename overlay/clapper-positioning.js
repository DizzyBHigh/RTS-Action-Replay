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

RTSReplayClapper.applyPosition = (command, name) => {
  const position = RTSReplayClapper.getPosition(command, name || 'Centered');
  RTSPositioningEngine.apply(RTSReplayClapper.messageCard, position);
  RTSReplayClapper.activePosition = position;
};
