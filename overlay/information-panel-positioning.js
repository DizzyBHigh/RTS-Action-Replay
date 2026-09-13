const RTSInformationPanels = window.RTSInformationPanels || {};

RTSInformationPanels.defaultPositions = {
  "Centered": { scale: 100, x: 0, y: 0, rotateZ: 0 }
};

RTSInformationPanels.getPositions = command => {
  try {
    const raw = command?.replayPanelPositions;
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return parsed && typeof parsed === 'object' ? parsed : RTSInformationPanels.defaultPositions;
  } catch (_) { return RTSInformationPanels.defaultPositions; }
};

RTSInformationPanels.getPosition = (command, name) => {
  const positions = RTSInformationPanels.getPositions(command);
  if (positions[name]) return positions[name];
  const target = String(name || 'Centered').trim().toLowerCase();
  const match = Object.keys(positions).find(key => key.toLowerCase() === target || String(positions[key]?.tag || '').trim().toLowerCase() === target);
  return positions[match] || positions.Centered || RTSInformationPanels.defaultPositions.Centered;
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

RTSInformationPanels.getViewportOffset = position => {
  const screen = document.getElementById('rts-dev-screen');
  if (!screen) return null;
  const bounds = screen.getBoundingClientRect();
  return {
    left: bounds.left + bounds.width / 2 + position.x * bounds.width / 100,
    top: bounds.top + bounds.height / 2 - position.y * bounds.height / 100
  };
};

RTSInformationPanels.applyPosition = (panel, command, name) => {
  if (!panel) return;
  const position = RTSInformationPanels.normalise(RTSInformationPanels.getPosition(command, name));
  const offset = RTSInformationPanels.getViewportOffset(position);
  panel.style.left = offset ? `${offset.left}px` : `calc(50% + ${position.x}vw)`;
  panel.style.top = offset ? `${offset.top}px` : `calc(50% - ${position.y}vh)`;
  panel.style.setProperty('--panel-scale-x', position.scaleX / 100);
  panel.style.setProperty('--panel-scale-y', position.scaleY / 100);
  panel.style.setProperty('--panel-rotate-z', `${position.rotateZ}deg`);
};

RTSInformationPanels.show = (panel, command, name) => {
  if (window.RTSInformationPanelAnimation) {
    RTSInformationPanelAnimation.show(panel, command, name);
    return;
  }
  RTSInformationPanels.applyPosition(panel, command, name);
  panel.classList.add('show');
  panel.setAttribute('aria-hidden', 'false');
};

RTSInformationPanels.hide = (panel, command) => {
  if (!panel) return;
  if (window.RTSInformationPanelAnimation) {
    RTSInformationPanelAnimation.hide(panel, command);
    return;
  }
  panel.classList.remove('show');
  panel.setAttribute('aria-hidden', 'true');
};

window.RTSInformationPanels = RTSInformationPanels;
