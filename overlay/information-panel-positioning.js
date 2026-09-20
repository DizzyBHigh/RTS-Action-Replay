const RTSInformationPanels = window.RTSInformationPanels || {};

RTSInformationPanels.defaultPositions = {
  Centered: { scale: 100, x: 0, y: 0, z: 0, rotateX: 0, rotateY: 0, rotateZ: 0, fov: 90 }
};

RTSInformationPanels.getPositions = command =>
  RTSAnimationEngine.getPositions(command?.replayPanelPositions, RTSInformationPanels.defaultPositions);

RTSInformationPanels.getPosition = (command, name) => {
  const positions = RTSInformationPanels.getPositions(command);
  return RTSAnimationEngine.resolvePosition(
    positions,
    name || 'Centered',
    positions.Centered || RTSInformationPanels.defaultPositions.Centered
  );
};

RTSInformationPanels.normalise = position => RTSAnimationEngine.normalisePosition(position);

RTSInformationPanels.applySize = (panel, command) => {
  if (!panel) return;
  const width = Number(command?.replayPanelWidth);
  const height = Number(command?.replayPanelHeight);
  const radius = Number(command?.replayPanelCornerRadius);
  if (Number.isFinite(width) && width > 0) panel.style.width = `${width}px`;
  if (Number.isFinite(height) && height > 0) panel.style.height = `${height}px`;
  if (Number.isFinite(radius) && radius >= 0) panel.style.borderRadius = `${radius}px`;
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
  RTSPositioningEngine.apply(panel, RTSInformationPanels.getPosition(command, name));
};

RTSInformationPanels.show = (panel, command, name) => {
  if (!panel) return;
  RTSInformationPanels.applySize(panel, command);
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
