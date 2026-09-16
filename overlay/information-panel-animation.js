const RTSInformationPanelAnimation = window.RTSInformationPanelAnimation || {};
let panelRunner = null;
let panelCommand = null;

const panelAdapter = {
  normaliseStep: step => ({
    position: step?.position || 'Centered',
    duration: Math.max(0, Number(step?.duration) || 0),
    delay: Math.max(0, Number(step?.delay) || 0),
    easing: step?.easing || 'ease-in-out'
  }),
  getPosition: name => RTSInformationPanels.normalise(RTSInformationPanels.getPosition(panelCommand, name)),
  positionsEqual: (a, b) => {
    if (!a || !b) return false;
    return ['scaleX', 'scaleY', 'x', 'y', 'rotateZ'].every(key => Number(a[key] ?? 0) === Number(b[key] ?? 0));
  },
  easing: name => {
    switch (String(name || 'ease-in-out').toLowerCase()) {
      case 'linear': return t => t;
      case 'ease-in': return t => t * t;
      case 'ease-out': return t => 1 - Math.pow(1 - t, 2);
      default: return t => t < .5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2;
    }
  },
  interpolatePosition: (from, to, progress) => ({
    scaleX: from.scaleX + (to.scaleX - from.scaleX) * progress,
    scaleY: from.scaleY + (to.scaleY - from.scaleY) * progress,
    x: from.x + (to.x - from.x) * progress,
    y: from.y + (to.y - from.y) * progress,
    rotateZ: from.rotateZ + (to.rotateZ - from.rotateZ) * progress
  }),
  applyPosition: (panel, position) => {
    if (!panel || !position) return;
    const offset = RTSInformationPanels.getViewportOffset(position);
    panel.style.left = offset ? `${offset.left}px` : `calc(50% + ${position.x}vw)`;
    panel.style.top = offset ? `${offset.top}px` : `calc(50% - ${position.y}vh)`;
    panel.style.setProperty('--panel-scale-x', position.scaleX / 100);
    panel.style.setProperty('--panel-scale-y', position.scaleY / 100);
    panel.style.setProperty('--panel-rotate-z', `${position.rotateZ}deg`);
  }
};

const getPanelRunner = () => {
  if (!panelRunner && window.RTSAnimationEngine) panelRunner = RTSAnimationEngine.createRunner(panelAdapter);
  return panelRunner;
};

RTSInformationPanelAnimation.cancel = () => getPanelRunner()?.cancel();
RTSInformationPanelAnimation.profile = command => {
  const raw = command?.replayPanelAnimation;
  if (!raw) return null;
  try {
    const profile = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return profile && typeof profile === 'object' ? profile : null;
  } catch (_) { return null; }
};

RTSInformationPanelAnimation.position = (command, name) => RTSInformationPanels.normalise(RTSInformationPanels.getPosition(command, name));
RTSInformationPanelAnimation.easing = name => panelAdapter.easing(name);
RTSInformationPanelAnimation.apply = (panel, position) => panelAdapter.applyPosition(panel, position);

RTSInformationPanelAnimation.run = (panel, command, sequence, complete) => {
  panelCommand = command || {};
  const runner = getPanelRunner();
  if (!runner) { complete?.(); return; }
  runner.run(sequence, complete);
};

RTSInformationPanelAnimation.show = (panel, command, name) => {
  panelCommand = command || {};
  const profile = RTSInformationPanelAnimation.profile(command);
  RTSInformationPanels.applySize(panel, command);
  panel.classList.add('show');
  panel.setAttribute('aria-hidden', 'false');
  if (Array.isArray(profile?.start) && profile.start.length) RTSInformationPanelAnimation.run(panel, command, profile.start);
  else RTSInformationPanels.applyPosition(panel, command, name);
};

RTSInformationPanelAnimation.hide = (panel, command) => {
  if (!panel) return;
  panelCommand = command || panelCommand || {};
  const profile = RTSInformationPanelAnimation.profile(panelCommand);
  const end = profile?.end;
  if (!Array.isArray(end) || !end.length) {
    RTSInformationPanelAnimation.cancel();
    panel.classList.remove('show');
    panel.setAttribute('aria-hidden', 'true');
    return;
  }
  const runner = getPanelRunner();
  runner?.setActive(RTSInformationPanelAnimation.position(panelCommand, panelCommand.replayPanelPosition || 'Centered'));
  runner?.runEnd(end, () => {
    panel.classList.remove('show');
    panel.setAttribute('aria-hidden', 'true');
  });
};

window.RTSInformationPanelAnimation = RTSInformationPanelAnimation;
