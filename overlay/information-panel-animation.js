const RTSInformationPanelAnimation = window.RTSInformationPanelAnimation || {};

RTSInformationPanelAnimation.token = 0;
RTSInformationPanelAnimation.timer = null;
RTSInformationPanelAnimation.frame = null;

RTSInformationPanelAnimation.cancel = () => {
  RTSInformationPanelAnimation.token += 1;
  if (RTSInformationPanelAnimation.timer) clearTimeout(RTSInformationPanelAnimation.timer);
  RTSInformationPanelAnimation.timer = null;
  if (RTSInformationPanelAnimation.frame) cancelAnimationFrame(RTSInformationPanelAnimation.frame);
  RTSInformationPanelAnimation.frame = null;
};

RTSInformationPanelAnimation.profile = command => {
  const raw = command?.replayPanelAnimation;
  if (!raw) return null;
  try {
    const profile = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return profile && typeof profile === 'object' ? profile : null;
  } catch (_) { return null; }
};

RTSInformationPanelAnimation.normaliseStep = step => ({
  position: step?.position || 'Center',
  duration: Math.max(0, Number(step?.duration) || 0),
  delay: Math.max(0, Number(step?.delay) || 0),
  easing: step?.easing || 'ease-in-out'
});

RTSInformationPanelAnimation.position = (command, name) => {
  const special = {
    'Hidden Left': { scale: 100, x: -120, y: 0, rotateZ: 0 },
    'Hidden Right': { scale: 100, x: 120, y: 0, rotateZ: 0 },
    'Hidden Top': { scale: 100, x: 0, y: 120, rotateZ: 0 },
    'Hidden Bottom': { scale: 100, x: 0, y: -120, rotateZ: 0 }
  };
  if (special[name]) return RTSInformationPanels.normalise(special[name]);
  return RTSInformationPanels.normalise(RTSInformationPanels.getPosition(command, name));
};

RTSInformationPanelAnimation.easing = name => {
  switch (name) {
    case 'linear': return value => value;
    case 'ease': return value => value < .5 ? 2 * value * value : 1 - Math.pow(-2 * value + 2, 2) / 2;
    case 'ease-in': return value => value * value;
    case 'ease-out': return value => 1 - Math.pow(1 - value, 2);
    default: return value => value < .5 ? 2 * value * value : 1 - Math.pow(-2 * value + 2, 2) / 2;
  }
};

RTSInformationPanelAnimation.apply = (panel, position) => {
  if (!panel || !position) return;
  panel.style.left = `calc(50% + ${position.x}vw)`;
  panel.style.top = `calc(50% - ${position.y}vh)`;
  panel.style.setProperty('--panel-scale-x', position.scaleX / 100);
  panel.style.setProperty('--panel-scale-y', position.scaleY / 100);
  panel.style.setProperty('--panel-rotate-z', `${position.rotateZ}deg`);
};

RTSInformationPanelAnimation.run = (panel, command, sequence, complete) => {
  const steps = Array.isArray(sequence) ? sequence.map(RTSInformationPanelAnimation.normaliseStep) : [];
  if (!steps.length) { if (complete) complete(); return; }
  RTSInformationPanelAnimation.cancel();
  const token = RTSInformationPanelAnimation.token;
  let current = RTSInformationPanelAnimation.position(command, steps[0].position);
  RTSInformationPanelAnimation.apply(panel, current);

  const advance = index => {
    if (token !== RTSInformationPanelAnimation.token) return;
    if (index >= steps.length) { if (complete) complete(); return; }
    const step = steps[index];
    const target = RTSInformationPanelAnimation.position(command, step.position);
    const start = current;
    if (step.duration <= 0) {
      RTSInformationPanelAnimation.apply(panel, target);
      current = target;
      if (step.delay) RTSInformationPanelAnimation.timer = setTimeout(() => advance(index + 1), step.delay);
      else advance(index + 1);
      return;
    }
    const easing = RTSInformationPanelAnimation.easing(step.easing);
    const started = performance.now();
    const frame = now => {
      if (token !== RTSInformationPanelAnimation.token) return;
      const raw = Math.min(1, Math.max(0, (now - started) / step.duration));
      const amount = easing(raw);
      RTSInformationPanelAnimation.apply(panel, {
        scaleX: start.scaleX + (target.scaleX - start.scaleX) * amount,
        scaleY: start.scaleY + (target.scaleY - start.scaleY) * amount,
        x: start.x + (target.x - start.x) * amount,
        y: start.y + (target.y - start.y) * amount,
        rotateZ: start.rotateZ + (target.rotateZ - start.rotateZ) * amount
      });
      if (raw < 1) RTSInformationPanelAnimation.frame = requestAnimationFrame(frame);
      else {
        RTSInformationPanelAnimation.frame = null;
        current = target;
        if (step.delay) RTSInformationPanelAnimation.timer = setTimeout(() => advance(index + 1), step.delay);
        else advance(index + 1);
      }
    };
    RTSInformationPanelAnimation.frame = requestAnimationFrame(frame);
  };
  advance(1);
};

RTSInformationPanelAnimation.show = (panel, command, name) => {
  const profile = RTSInformationPanelAnimation.profile(command);
  const start = profile?.start;
  panel.classList.add('show');
  panel.setAttribute('aria-hidden', 'false');
  if (Array.isArray(start) && start.length) RTSInformationPanelAnimation.run(panel, command, start);
  else RTSInformationPanels.applyPosition(panel, command, name);
};

RTSInformationPanelAnimation.hide = (panel, command) => {
  if (!panel) return;
  const profile = RTSInformationPanelAnimation.profile(command);
  const end = profile?.end;
  if (!Array.isArray(end) || !end.length) {
    RTSInformationPanelAnimation.cancel();
    panel.classList.remove('show');
    panel.setAttribute('aria-hidden', 'true');
    return;
  }
  RTSInformationPanelAnimation.run(panel, command, end, () => {
    panel.classList.remove('show');
    panel.setAttribute('aria-hidden', 'true');
  });
};

window.RTSInformationPanelAnimation = RTSInformationPanelAnimation;
