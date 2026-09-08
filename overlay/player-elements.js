const RTSReplayElements = window.RTSReplay;

RTSReplayElements.defaultPositions = {
  Brand: { name: 'Brand', scale: 100, x: 0, y: 0, rotateX: 0, rotateY: 0, rotateZ: 0 },
  Title: { name: 'Title', scale: 100, x: 0, y: 0, rotateX: 0, rotateY: 0, rotateZ: 0 },
  'Play Speed Indicator': { name: 'Play Speed Indicator', scale: 100, x: 0, y: 0, rotateX: 0, rotateY: 0, rotateZ: 0 }
};

RTSReplayElements.getPositions = () => {
  try {
    const raw = RTSReplayElements.currentCommand?.replayElementPositions;
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return parsed && typeof parsed === 'object' ? parsed : RTSReplayElements.defaultPositions;
  } catch (_) { return RTSReplayElements.defaultPositions; }
};

RTSReplayElements.getPosition = name => {
  const positions = RTSReplayElements.getPositions();
  return positions[name] || RTSReplayElements.defaultPositions[name];
};

RTSReplayElements.transformFor = (p, scaleFactor = 1) => {
  const rawScale = Number(p?.scale);
  const scale = (Number.isFinite(rawScale) ? rawScale : 100) / 100 * scaleFactor;
  const x = Number(p?.x), y = Number(p?.y);
  const rotateX = Number(p?.rotateX), rotateY = Number(p?.rotateY), rotateZ = Number(p?.rotateZ);
  return `translate(-50%, -50%) translate(${Number.isFinite(x) ? x : 0}%, ${Number.isFinite(y) ? y : 0}%) scale(${scale}) rotateX(${Number.isFinite(rotateX) ? rotateX : 0}deg) rotateY(${Number.isFinite(rotateY) ? rotateY : 0}deg) rotateZ(${Number.isFinite(rotateZ) ? rotateZ : 0}deg)`;
};

RTSReplayElements.applyPosition = (element, position) => {
  if (!element) return;
  element.style.transform = RTSReplayElements.transformFor(position || {});
};

RTSReplayElements.configure = command => {
  RTSReplayElements.currentCommand = command;
  const positions = RTSReplayElements.getPositions();
  RTSReplayElements.applyPosition(RTSReplayElements.brand, positions.Brand || RTSReplayElements.defaultPositions.Brand);
  RTSReplayElements.applyPosition(RTSReplayElements.title, positions.Title || RTSReplayElements.defaultPositions.Title);
  const speed = positions['Play Speed Indicator'] || RTSReplayElements.defaultPositions['Play Speed Indicator'];
  RTSReplayElements.applyPosition(RTSReplayElements.speedLabel, speed);
  RTSReplayElements.applyPosition(RTSReplayElements.slowMotion, speed);

  RTSReplayElements.brand.innerHTML = '';
  const showBranding = command.replayShowBranding !== false;
  const logo = String(command.replayLogoUrl || '').trim();
  RTSReplayElements.brand.classList.toggle('visible', showBranding);
  if (showBranding) {
    if (logo) RTSReplayElements.brand.innerHTML = `<img src="${logo.replace(/"/g, '&quot;')}" alt="">`;
    else RTSReplayElements.brand.textContent = 'RTS';
  }

  RTSReplayElements.title.textContent = '';
  const title = String(command.replayTitle || '').trim();
  RTSReplayElements.title.classList.toggle('visible', command.replayShowTitle !== false && !!title);
  if (command.replayShowTitle !== false && title) RTSReplayElements.title.textContent = title;

  RTSReplayElements.speedLabel.textContent = '';
  RTSReplayElements.slowMotion.textContent = '';
  const speedValue = Number(command.replayPlaybackSpeed) || 1;
  if (speedValue > 1.001) {
    RTSReplayElements.speedLabel.textContent = `${Number(speedValue.toFixed(2))}x`;
    RTSReplayElements.speedLabel.classList.add('visible');
    RTSReplayElements.slowMotion.classList.remove('visible');
  } else if (speedValue < 0.999) {
    const text = String(command.replaySlowMotionText || 'Slow Motion').trim() || 'Slow Motion';
    const showSpeed = command.replaySlowMotionShowSpeed !== false;
    RTSReplayElements.slowMotion.textContent = showSpeed ? `${text} ${Number(speedValue.toFixed(2))}x` : text;
    RTSReplayElements.slowMotion.classList.add('visible');
    RTSReplayElements.speedLabel.classList.remove('visible');
  } else {
    RTSReplayElements.speedLabel.classList.remove('visible');
    RTSReplayElements.slowMotion.classList.remove('visible');
  }
};

RTSReplayElements.clear = () => {
  RTSReplayElements.brand.innerHTML = '';
  RTSReplayElements.title.textContent = '';
  RTSReplayElements.speedLabel.textContent = '';
  RTSReplayElements.slowMotion.textContent = '';
  RTSReplayElements.brand.classList.remove('visible');
  RTSReplayElements.title.classList.remove('visible');
  RTSReplayElements.speedLabel.classList.remove('visible');
  RTSReplayElements.slowMotion.classList.remove('visible');
};

RTSReplayElements.hide = RTSReplayElements.clear;
RTSReplayElements.brand = document.getElementById('player-branding');
RTSReplayElements.title = document.getElementById('player-title');
RTSReplayElements.speedLabel = document.getElementById('player-speed-indicator');
RTSReplayElements.slowMotion = document.getElementById('player-slow-motion');
window.RTSReplayElements = RTSReplayElements;
