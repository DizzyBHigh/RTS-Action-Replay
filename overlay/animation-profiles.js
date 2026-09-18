const RTSReplayAnimation = window.RTSReplay;

const animationDevLog = (message, details) => window.RTSDevToolbar?.log?.(message, details);

RTSReplayAnimation.readProfile = command => {
  const raw = command?.replayAnimationProfile;
  if (!raw) {
    animationDevLog('Animation profile missing', { replayId: command?.replayId, queueEntryId: command?.replayQueueEntryId });
    return null;
  }
  try {
    const profile = typeof raw === 'string' ? JSON.parse(raw) : raw;
    animationDevLog('Animation profile read', {
      replayId: command?.replayId,
      queueEntryId: command?.replayQueueEntryId,
      profileId: profile?.id,
      startSteps: Array.isArray(profile?.start) ? profile.start.length : 0,
      endSteps: Array.isArray(profile?.end) ? profile.end.length : 0
    });
    return profile && typeof profile === 'object' ? profile : null;
  } catch (error) {
    animationDevLog('Animation profile parse failed', { replayId: command?.replayId, error: String(error) });
    return null;
  }
};

const playerAdapter = {
  normaliseStep: step => ({
    position: step?.position || step?.name || 'Full Screen',
    duration: normaliseDuration(step?.duration),
    delay: normaliseDuration(step?.delay),
    easing: step?.easing || 'ease-in-out'
  }),
  getPosition: name => {
    const command = RTSReplayAnimation.currentCommand;
    const raw = command?.replayPlayerPositions ?? command?.replayPositions;
    const position = RTSReplayAnimation.resolvePositionTag(name);
    animationDevLog('Animation resolver boundary', {
      requested: name,
      commandPresent: !!command,
      payloadPresent: raw != null,
      payloadType: typeof raw,
      payloadLength: typeof raw === 'string' ? raw.length : 0,
      returned: position
    });
    return position;
  },
  positionsEqual: (a, b) => RTSReplayAnimation.positionsEqual(a, b),
  interpolatePosition: (from, to, progress) => RTSReplayAnimation.interpolatePosition(from, to, progress),
  easing: name => RTSReplayAnimation.easing(name),
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
  if (!playerRunner) animationDevLog('Animation engine unavailable');
  return playerRunner;
};

RTSReplayAnimation.cancelSequence = () => {
  animationDevLog('Animation sequence cancelled');
  getRunner()?.cancel();
};

RTSReplayAnimation.runSequence = (sequence, onComplete) => {
  const runner = getRunner();
  const steps = Array.isArray(sequence) ? sequence : [];
  animationDevLog('Animation start sequence requested', {
    replayId: RTSReplayAnimation.currentCommand?.replayId,
    steps: steps.map((step, index) => ({ index, position: step?.position || step?.name || 'Full Screen', duration: step?.duration ?? 0, delay: step?.delay ?? 0, easing: step?.easing || 'ease-in-out' }))
  });
  if (!runner) return;
  runner.run(steps, () => {
    animationDevLog('Animation start sequence completed', { replayId: RTSReplayAnimation.currentCommand?.replayId, steps: steps.length });
    onComplete?.();
  });
};

RTSReplayAnimation.runEndSequence = (sequence, onComplete) => {
  const runner = getRunner();
  const steps = Array.isArray(sequence) ? sequence : [];
  animationDevLog('Animation end sequence requested', {
    replayId: RTSReplayAnimation.currentCommand?.replayId,
    steps: steps.map((step, index) => ({ index, position: step?.position || step?.name || 'Full Screen', duration: step?.duration ?? 0, delay: step?.delay ?? 0, easing: step?.easing || 'ease-in-out' }))
  });
  if (!runner) { onComplete?.(); return; }
  runner.setActive(RTSReplayAnimation.activePosition);
  runner.runEnd(steps, () => {
    animationDevLog('Animation end sequence completed', { replayId: RTSReplayAnimation.currentCommand?.replayId, steps: steps.length });
    onComplete?.();
  });
};

RTSReplayAnimation.sequenceToken = 0;
RTSReplayAnimation.sequenceTimer = null;
RTSReplayAnimation.sequenceFrame = null;
