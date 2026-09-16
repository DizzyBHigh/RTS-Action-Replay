const RTSReplayClapperAnimation = window.RTSReplayClapperAnimation || {};
let clapperRunner = null;
let clapperCommand = null;

const clapperAdapter = {
  normaliseStep: step => ({
    position: step?.position || step?.name || 'Centered',
    duration: Math.max(0, Number(step?.duration) || 0),
    delay: Math.max(0, Number(step?.delay) || 0),
    easing: step?.easing || 'ease-in-out'
  }),
  getPosition: name => RTSReplayClapper.getPosition(clapperCommand, name),
  positionsEqual: (a, b) => {
    if (!a || !b) return false;
    const keys = ['scaleX', 'scaleY', 'x', 'y', 'z', 'rotateX', 'rotateY', 'rotateZ', 'fov'];
    return keys.every(key => Number(a[key] ?? a.scale ?? (key.startsWith('scale') ? 50 : key === 'fov' ? 90 : 0)) ===
      Number(b[key] ?? b.scale ?? (key.startsWith('scale') ? 50 : key === 'fov' ? 90 : 0)));
  },
  easing: name => {
    switch (String(name || 'ease-in-out').toLowerCase()) {
      case 'linear': return t => t;
      case 'ease-in': return t => t * t * t;
      case 'ease-out': return t => 1 - Math.pow(1 - t, 3);
      default: return t => t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
    }
  },
  interpolatePosition: (from, to, progress) => {
    const keys = ['scaleX', 'scaleY', 'x', 'y', 'z', 'rotateX', 'rotateY', 'rotateZ', 'fov'];
    const value = (p, key) => Number(p?.[key] ?? p?.scale ?? (key.startsWith('scale') ? 50 : key === 'fov' ? 90 : 0));
    return Object.fromEntries(keys.map(key => [key, value(from, key) + (value(to, key) - value(from, key)) * progress]));
  },
  applyPosition: (position, immediate) => {
    if (immediate) RTSReplayClapper.messageCard.style.transition = 'none';
    RTSReplayClapper.messageCard.style.transform = RTSReplayClapper.transformFor(position);
    RTSReplayClapper.activePosition = position;
  }
};

const getRunner = () => {
  if (!clapperRunner && window.RTSAnimationEngine) clapperRunner = RTSAnimationEngine.createRunner(clapperAdapter);
  return clapperRunner;
};

const triggerClap = () => {
  const stick = RTSReplayClapper.messageCard.querySelector('.clapstick');
  if (!stick) return;
  stick.classList.remove('clap');
  void stick.offsetWidth;
  stick.classList.add('clap');
};

RTSReplayClapperAnimation.show = command => {
  clapperCommand = command || {};
  const runner = getRunner();
  RTSReplayClapper.messageCard.classList.remove('show');
  void RTSReplayClapper.messageCard.offsetWidth;
  RTSReplayClapper.messageCard.classList.add('show');
  RTSReplayClapper.messageCard.setAttribute('aria-hidden', 'false');
  const profile = readClapperProfile(clapperCommand);
  if (profile?.start?.length && runner) runner.run(profile.start, triggerClap);
  else triggerClap();
};

RTSReplayClapperAnimation.hide = command => {
  clapperCommand = command || clapperCommand || {};
  const runner = getRunner();
  const profile = readClapperProfile(clapperCommand);
  if (profile?.end?.length && runner) {
    runner.setActive(RTSReplayClapper.activePosition);
    runner.runEnd(profile.end, () => {
      RTSReplayClapper.messageCard.classList.remove('show');
      RTSReplayClapper.messageCard.setAttribute('aria-hidden', 'true');
    });
    return;
  }
  runner?.cancel();
  RTSReplayClapper.messageCard.classList.remove('show');
  RTSReplayClapper.messageCard.setAttribute('aria-hidden', 'true');
};

RTSReplayClapperAnimation.cancel = () => getRunner()?.cancel();

function readClapperProfile(command) {
  const raw = command?.replayClapperAnimation;
  if (!raw) return null;
  try {
    const profile = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return profile && typeof profile === 'object' ? profile : null;
  } catch (_) { return null; }
}

window.RTSReplayClapperAnimation = RTSReplayClapperAnimation;
