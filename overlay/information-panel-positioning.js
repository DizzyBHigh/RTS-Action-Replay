const RTSInformationPanels = window.RTSInformationPanels || {};

RTSInformationPanels.defaultPositions = {
  "Center": { scale: 100, x: 0, y: 0, rotateZ: 0 },
  "Top": { scale: 100, x: 0, y: 32, rotateZ: 0 },
  "Bottom": { scale: 100, x: 0, y: -32, rotateZ: 0 },
  "Top Left": { scale: 100, x: -36, y: 28, rotateZ: 0 },
  "Top Right": { scale: 100, x: 36, y: 28, rotateZ: 0 },
  "Bottom Left": { scale: 100, x: -36, y: -28, rotateZ: 0 },
  "Bottom Right": { scale: 100, x: 36, y: -28, rotateZ: 0 }
};

RTSInformationPanels.getPositions = command => {
  try {
    const raw = command?.replayPanelPositions;
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return parsed && typeof parsed === 'object' ? parsed : RTSInformationPanels.defaultPositions;
  } catch (_) {
    return RTSInformationPanels.defaultPositions;
  }
};

RTSInformationPanels.getPosition = (command, name) => {
  const positions = RTSInformationPanels.getPositions(command);
  if (positions[name]) return positions[name];
  const target = String(name || 'Center').trim().toLowerCase();
  const match = Object.keys(positions).find(key => key.toLowerCase() === target || String(positions[key]?.tag || '').trim().toLowerCase() === target);
  return positions[match] || positions.Center || RTSInformationPanels.defaultPositions.Center;
};

RTSInformationPanels.normalise = position => {
  const scale = Number(position?.scale ?? 100);
  return {
    scaleX: Number.isFinite(Number(position?.scaleX)) ? Number(position.scaleX) : scale,
    scaleY: Number.isFinite(Number(position?.scaleY)) ? Number(position.scaleY) : scale,
    x: Number.isFinite(Number(position?.x)) ? Number(position.x) : 0,
    y: Number.isFinite(Number(position?.y)) ? Number(position.y) : 0,
    rotateZ: Number.isFinite(Number(position?.rotateZ)) ? Number(position.rotateZ) : 0
  };
};

RTSInformationPanels.applyPosition = (panel, command, name) => {
  if (!panel) return;
  const position = RTSInformationPanels.normalise(RTSInformationPanels.getPosition(command, name));
  panel.style.left = `calc(50% + ${position.x}vw)`;
  panel.style.top = `calc(50% - ${position.y}vh)`;
  panel.style.transform = `translate(-50%, -50%) scale3d(${position.scaleX / 100}, ${position.scaleY / 100}, 1) rotateZ(${position.rotateZ}deg)`;
};

RTSInformationPanels.show = (panel, command, name) => {
  RTSInformationPanels.applyPosition(panel, command, name);
  panel.classList.add('show');
  panel.setAttribute('aria-hidden', 'false');
};

RTSInformationPanels.hide = panel => {
  if (!panel) return;
  panel.classList.remove('show');
  panel.setAttribute('aria-hidden', 'true');
};

window.RTSInformationPanels = RTSInformationPanels;
