const RTSReplayAnimation = window.RTSReplay;

RTSReplayAnimation.sequenceToken = 0;
RTSReplayAnimation.cancelSequence = () => {
  RTSReplayAnimation.sequenceToken += 1;
  if (RTSReplayAnimation.sequenceTimer) clearTimeout(RTSReplayAnimation.sequenceTimer);
  RTSReplayAnimation.sequenceTimer = null;
  if (RTSReplayAnimation.sequenceFrame) cancelAnimationFrame(RTSReplayAnimation.sequenceFrame);
  RTSReplayAnimation.sequenceFrame = null;
};

RTSReplayAnimation.readProfile = command => {
  const raw = command?.replayAnimationProfile;
  if (!raw) return null;
  try {
    const profile = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return profile && typeof profile === 'object' ? profile : null;
  } catch (_) { return null; }
};

RTSReplayAnimation.normaliseStep = step => ({
  position: step?.position || step?.name || 'Full Screen',
  duration: Math.max(0, Number(step?.duration) || 0),
  delay: Math.max(0, Number(step?.delay) || 0),
  easing: step?.easing || 'ease-in-out'
});

RTSReplayAnimation.animateStep = (from, step, token, done) => {
  const target = RTSReplayAnimation.getPosition(step.position);
  const duration = step.duration;
  if (duration <= 0 || RTSReplayAnimation.positionsEqual(from, target)) {
    RTSReplayAnimation.applyPosition(target, true);
    RTSReplayAnimation.activePosition = target;
    if (step.delay > 0) RTSReplayAnimation.sequenceTimer = setTimeout(() => done(target), step.delay);
    else done(target);
    return;
  }

  const start = RTSReplayAnimation.normalisePosition(from);
  const end = RTSReplayAnimation.normalisePosition(target);
  const easing = RTSReplayAnimation.easing(step.easing);
  const started = performance.now();
  const frame = now => {
    if (token !== RTSReplayAnimation.sequenceToken) return;
    const raw = Math.min(1, Math.max(0, (now - started) / duration));
    RTSReplayAnimation.player.style.transform = RTSReplayAnimation.transformFor(
      RTSReplayAnimation.interpolatePosition(start, end, easing(raw)));
    if (raw < 1) {
      RTSReplayAnimation.sequenceFrame = requestAnimationFrame(frame);
      return;
    }
    RTSReplayAnimation.sequenceFrame = null;
    RTSReplayAnimation.player.style.transform = RTSReplayAnimation.transformFor(target);
    RTSReplayAnimation.activePosition = target;
    if (step.delay > 0) RTSReplayAnimation.sequenceTimer = setTimeout(() => done(target), step.delay);
    else done(target);
  };
  RTSReplayAnimation.sequenceFrame = requestAnimationFrame(frame);
};

RTSReplayAnimation.runSequence = (sequence, onComplete) => {
  const steps = Array.isArray(sequence) ? sequence.map(RTSReplayAnimation.normaliseStep) : [];
  if (!steps.length) { if (onComplete) onComplete(); return; }
  RTSReplayAnimation.cancelSequence();
  const token = RTSReplayAnimation.sequenceToken;
  const first = RTSReplayAnimation.getPosition(steps[0].position);
  RTSReplayAnimation.applyPosition(first, true);
  RTSReplayAnimation.activePosition = first;

  const advance = index => {
    if (token !== RTSReplayAnimation.sequenceToken) return;
    if (index >= steps.length) { if (onComplete) onComplete(); return; }
    RTSReplayAnimation.animateStep(RTSReplayAnimation.activePosition, steps[index], token,
      () => advance(index + 1));
  };
  if (steps[0].delay > 0) RTSReplayAnimation.sequenceTimer = setTimeout(() => advance(1), steps[0].delay);
  else advance(1);
};

RTSReplayAnimation.runEndSequence = (sequence, onComplete) => {
  const steps = Array.isArray(sequence) ? sequence.map(RTSReplayAnimation.normaliseStep) : [];
  if (!steps.length) { if (onComplete) onComplete(); return; }
  RTSReplayAnimation.cancelSequence();
  const token = RTSReplayAnimation.sequenceToken;
  let current = RTSReplayAnimation.activePosition || RTSReplayAnimation.getPosition('Full Screen');
  const advance = index => {
    if (token !== RTSReplayAnimation.sequenceToken) return;
    if (index >= steps.length) { if (onComplete) onComplete(); return; }
    RTSReplayAnimation.animateStep(current, steps[index], token, next => {
      current = next;
      advance(index + 1);
    });
  };
  advance(0);
};
