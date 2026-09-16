const RTSReplayAnimation = window.RTSReplay;

RTSReplayAnimation.readProfile = command => {
  const raw = command?.replayAnimationProfile;
  if (!raw) return null;
  try {
    const profile = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return profile && typeof profile === 'object' ? profile : null;
  } catch (_) { return null; }
};

const playerAdapter = {
  normaliseStep: step => ({
    position: step?.position || step?.name || 'Full Screen',
    duration: normaliseDuration(step?.duration),
    delay: normaliseDuration(step?.delay),
    easing: step?.easing || 'ease-in-out'
  }),
  getPosition: name => RTSReplayAnimation.getPosition(name),
  positionsEqual: (a, b) => RTSReplayAnimation.positionsEqual(a, b),
  easing: name => RTSReplayAnimation.easing(name),
  interpolatePosition: (from, to, progress) => RTSReplayAnimation.interpolatePosition(from, to, progress),
  applyPosition: (position, immediate) => {
    RTSReplayAnimation.applyPosition(position, immediate);
    RTSReplayAnimation.activePosition = position;
  }
};

function normaliseDuration(value) {
  const duration = Number(value);
  if (!Number.isFinite(duration) || duration <= 0) return 0;
  return duration < 10 ? duration * 1000 : duration;
}

let playerRunner = null;
const getRunner = () => {
  if (!playerRunner && window.RTSAnimationEngine) playerRunner = window.RTSAnimationEngine.createRunner(playerAdapter);
  return playerRunner;
};

RTSReplayAnimation.cancelSequence = () => getRunner()?.cancel();
RTSReplayAnimation.runSequence = (sequence, onComplete) => getRunner()?.run(sequence, onComplete);
RTSReplayAnimation.runEndSequence = (sequence, onComplete) => {
  const runner = getRunner();
  if (!runner) { onComplete?.(); return; }
  runner.setActive(RTSReplayAnimation.activePosition);
  runner.runEnd(sequence, onComplete);
};

RTSReplayAnimation.sequenceToken = 0;
RTSReplayAnimation.sequenceTimer = null;
RTSReplayAnimation.sequenceFrame = null;
