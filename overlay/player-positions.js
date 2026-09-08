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
  return positions[name] || positions['Full Screen'] || RTSReplayPlayer.defaultPositions['Full Screen'];
};

RTSReplayPlayer.configureTransition = () => {
  const duration = Math.max(.1, Number(RTSReplayPlayer.currentCommand?.replayAnimationDuration) || .5);
  RTSReplayPlayer.player.style.setProperty('--player-duration', `${duration}s`);
  RTSReplayPlayer.player.style.setProperty('--player-easing', RTSReplayPlayer.currentCommand?.replayAnimationEasing || 'ease-in-out');
  RTSReplayPlayer.player.classList.add('player-transition');
};

RTSReplayPlayer.transformFor = (p, scaleFactor = 1, offsetX = 0, offsetY = 0) => {
  const rawScale = Number(p.scale);
  const scale = (Number.isFinite(rawScale) ? rawScale : 100) / 100 * scaleFactor;
  const x = Number(p.x), y = Number(p.y);
  const rotateX = Number(p.rotateX), rotateY = Number(p.rotateY), rotateZ = Number(p.rotateZ);
  return `translate(-50%, -50%) translate(${(Number.isFinite(x) ? x : 0) + offsetX}%, ${(Number.isFinite(y) ? y : 0) + offsetY}%) scale(${scale}) rotateX(${Number.isFinite(rotateX) ? rotateX : 0}deg) rotateY(${Number.isFinite(rotateY) ? rotateY : 0}deg) rotateZ(${Number.isFinite(rotateZ) ? rotateZ : 0}deg)`;
};

RTSReplayPlayer.applyPosition = (position, immediate = false) => {
  const p = position || RTSReplayPlayer.defaultPositions['Full Screen'];
  if (immediate) RTSReplayPlayer.player.classList.remove('player-transition');
  RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(p);
  if (immediate) requestAnimationFrame(RTSReplayPlayer.configureTransition);
};

RTSReplayPlayer.getAnimation = type => RTSReplayPlayer.currentCommand?.[type] || 'None';

RTSReplayPlayer.animateIn = position => {
  const mode = RTSReplayPlayer.getAnimation('replayAnimationIn');
  RTSReplayPlayer.configureTransition();
  if (mode === 'Zoom In') RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(position, .001);
  else if (mode.startsWith('Slide From ')) {
    const offset = { Left: [-120, 0], Right: [120, 0], Top: [0, -120], Bottom: [0, 120] }[mode.slice(11)] || [0, 0];
    RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(position, 1, offset[0], offset[1]);
  } else RTSReplayPlayer.applyPosition(position);
  requestAnimationFrame(() => { RTSReplayPlayer.applyPosition(position); RTSReplayPlayer.player.classList.add('show'); });
};

RTSReplayPlayer.animateOut = () => {
  const mode = RTSReplayPlayer.getAnimation('replayAnimationOut');
  if (mode === 'None') { RTSReplayPlayer.player.classList.remove('show'); return; }
  const p = RTSReplayPlayer.activePosition || RTSReplayPlayer.defaultPositions['Full Screen'];
  RTSReplayPlayer.configureTransition();
  if (mode === 'Zoom Out') RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(p, .001);
  else if (mode.startsWith('Slide To ')) {
    const offset = { Left: [-120, 0], Right: [120, 0], Top: [0, -120], Bottom: [0, 120] }[mode.slice(9)] || [0, 0];
    RTSReplayPlayer.player.style.transform = RTSReplayPlayer.transformFor(p, 1, offset[0], offset[1]);
  }
  RTSReplayPlayer.player.addEventListener('transitionend', () => RTSReplayPlayer.player.classList.remove('show'), { once: true });
};
