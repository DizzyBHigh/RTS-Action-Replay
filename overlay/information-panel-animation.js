const RTSInformationPanelAnimation = window.RTSInformationPanelAnimation || {};
let panelCommand = null;
let activePanel = null;

const getPanelRunner = (panel, command) => {
  if (!panel) return null;
  const runner = RTSAnimationEngine.createRunner({
    target: panel,
    defaultPosition: { scale: 100, x: 0, y: 0, z: 0, rotateX: 0, rotateY: 0, rotateZ: 0, fov: 90 }
  });
  runner.configure(command?.replayPanelPositions);
  return runner;
};

RTSInformationPanelAnimation.cancel = panel => {
  const runner = getPanelRunner(panel || activePanel, panelCommand);
  runner?.cancel();
};

RTSInformationPanelAnimation.profile = command =>
  RTSAnimationEngine.readProfile(command?.replayPanelAnimation);

RTSInformationPanelAnimation.position = (command, name) => {
  const positions = RTSAnimationEngine.getPositions(command?.replayPanelPositions, {});
  return RTSAnimationEngine.normalisePosition(
    RTSAnimationEngine.resolvePosition(positions, name || 'Centered', { scale: 100, x: 0, y: 0, z: 0, rotateX: 0, rotateY: 0, rotateZ: 0, fov: 90 })
  );
};

RTSInformationPanelAnimation.apply = (panel, position) => {
  activePanel = panel;
  const runner = getPanelRunner(panel, panelCommand);
  runner.apply(position);
};

RTSInformationPanelAnimation.run = (panel, command, sequence, complete) => {
  panelCommand = command || {};
  activePanel = panel;
  const runner = getPanelRunner(panel, command);
  if (!runner) { complete?.(); return; }
  runner.run(sequence, complete);
};

RTSInformationPanelAnimation.show = (panel, command, name) => {
  panelCommand = command || {};
  activePanel = panel;
  const profile = RTSInformationPanelAnimation.profile(command);
  if (command?.replayInformationPanelType === 'message') RTSInformationPanels.applyMessageSize(panel, command); else RTSInformationPanels.applySize(panel, command);
  panel.classList.add('show');
  panel.setAttribute('aria-hidden', 'false');
  if (Array.isArray(profile?.start) && profile.start.length) {
    RTSInformationPanelAnimation.run(panel, command, profile.start);
  } else {
    RTSInformationPanelAnimation.apply(panel, RTSInformationPanelAnimation.position(command, name));
  }
};

RTSInformationPanelAnimation.hide = (panel, command) => {
  if (!panel) return;
  const hideCommand = command || panelCommand || {};
  panelCommand = hideCommand;
  activePanel = panel;
  const profile = RTSInformationPanelAnimation.profile(hideCommand);
  const end = profile?.end;
  if (!Array.isArray(end) || !end.length) {
    RTSInformationPanelAnimation.cancel();
    panel.classList.remove('show');
    panel.setAttribute('aria-hidden', 'true');
    return;
  }
  const runner = getPanelRunner(panel, hideCommand);
  runner.runEnd(end, () => {
    panel.classList.remove('show');
    panel.setAttribute('aria-hidden', 'true');
  });
};

window.RTSInformationPanelAnimation = RTSInformationPanelAnimation;
