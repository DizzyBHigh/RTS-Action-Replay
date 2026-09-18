const RTSInformationPanels = window.RTSInformationPanels || {};

RTSInformationPanels.defaultPositions = {
  "Centered": { scale: 100, x: 0, y: 0, rotateZ: 0 }
};

RTSInformationPanels.getPositions = command => {
  try {
    return RTSAnimationEngine.getPositions(command?.replayPanelPositions, RTSInformationPanels.defaultPositions);
};

RTSInformationPanels.getPosition = (command, name) => {
  return RTSAnimationEngine.resolvePosition(positions, name || 'Centered', positions.Centered || RTSInformationPanels.defaultPositions.Centered);
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

RTSInformationPanels.applySize = (panel, command) => {
  if (!panel) return;
  const width = Number(command?.replayPanelWidth);
  const height = Number(command?.replayPanelHeight);
  if (Number.isFinite(width) && width > 0) panel.style.width = `${width}px`;
  if (Number.isFinite(height) && height > 0) panel.style.height = `${height}px`;
  if ((Number.isFinite(width) && width > 0) || (Number.isFinite(height) && height > 0)) {
    panel.style.maxWidth = 'none';
    panel.style.maxHeight = 'none';
  }
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
  RTSPositioningEngine.apply(panel, position);
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
