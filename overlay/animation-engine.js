const RTSAnimationEngine = {
  createRunner(adapter) {
    let token = 0;
    let timer = null;
    let frame = null;
    let active = null;
    const log = (message, details) => window.RTSDevToolbar?.log?.(message, details);

    const describe = position => position ? {
      x: position.x, y: position.y, z: position.z,
      scaleX: position.scaleX, scaleY: position.scaleY,
      rotationX: position.rotationX, rotationY: position.rotationY,
      rotationZ: position.rotationZ, fov: position.fov
    } : null;

    const resolve = (requested, index) => {
      const position = adapter.getPosition(requested);
      log('Animation position resolved', { index, requested, position: describe(position) });
      return position;
    };

    const logVisualState = (phase, details = {}) => {
      const visual = adapter.getVisualState?.();
      if (!visual) return;
      log('Animation visual state', { phase, ...visual, ...details });
    };

    const cancel = () => {
      token += 1;
      if (timer) clearTimeout(timer);
      timer = null;
      if (frame) cancelAnimationFrame(frame);
      frame = null;
    };

    const run = (sequence, onComplete) => {
      const steps = Array.isArray(sequence) ? sequence.map(adapter.normaliseStep) : [];
      if (!steps.length) { onComplete?.(); return; }
      cancel();
      const runToken = token;
      const first = resolve(steps[0].position, 0);
      active = first;
      adapter.applyPosition(first, true);
      logVisualState('initial', { position: describe(first) });

      const advance = index => {
        if (runToken !== token) return;
        if (index >= steps.length) { onComplete?.(); return; }
        const step = steps[index];
        const target = resolve(step.position, index);
        const start = active || target;
        const equal = adapter.positionsEqual?.(start, target);
        log('Animation transition check', { index, position: step.position, duration: step.duration, delay: step.delay, equal, from: describe(start), to: describe(target) });
        const finish = () => {
          if (runToken !== token) return;
          active = target;
          if (step.delay > 0) timer = setTimeout(() => advance(index + 1), step.delay);
          else advance(index + 1);
        };
        if (step.duration <= 0 || equal) {
          adapter.applyPosition(target, true);
          finish();
          return;
        }
        const easing = adapter.easing(step.easing);
        const started = performance.now();
        logVisualState('transition-start', { index, position: step.position, from: describe(start), to: describe(target) });
        setTimeout(() => {
          if (runToken === token) logVisualState('transition-mid', { index, position: step.position });
        }, step.duration / 2);
        const draw = now => {
          if (runToken !== token) return;
          const raw = Math.min(1, Math.max(0, (now - started) / step.duration));
          adapter.applyPosition(adapter.interpolatePosition(start, target, easing(raw)), true);
          if (raw < 1) frame = requestAnimationFrame(draw);
          else { frame = null; adapter.applyPosition(target, true); logVisualState('transition-end', { index, position: step.position, to: describe(target) }); finish(); }
        };
        frame = requestAnimationFrame(draw);
      };

      if (steps[0].delay > 0) timer = setTimeout(() => advance(1), steps[0].delay);
      else advance(1);
    };

    const runEnd = (sequence, onComplete) => {
      const steps = Array.isArray(sequence) ? sequence.map(adapter.normaliseStep) : [];
      if (!steps.length) { onComplete?.(); return; }
      cancel();
      const runToken = token;
      let current = active || resolve(steps[0].position, 0);
      const advance = index => {
        if (runToken !== token) return;
        if (index >= steps.length) { active = current; onComplete?.(); return; }
        const step = steps[index];
        const target = resolve(step.position, index);
        const start = current;
        const equal = adapter.positionsEqual?.(start, target);
        log('Animation transition check', { index, position: step.position, duration: step.duration, delay: step.delay, equal, from: describe(start), to: describe(target) });
        const finish = () => {
          if (runToken !== token) return;
          current = target;
          active = target;
          if (step.delay > 0) timer = setTimeout(() => advance(index + 1), step.delay);
          else advance(index + 1);
        };
        if (step.duration <= 0 || equal) {
          adapter.applyPosition(target, true);
          finish();
          return;
        }
        const easing = adapter.easing(step.easing);
        const started = performance.now();
        logVisualState('end-transition-start', { index, position: step.position, from: describe(start), to: describe(target) });
        setTimeout(() => {
          if (runToken === token) logVisualState('end-transition-mid', { index, position: step.position });
        }, step.duration / 2);
        const draw = now => {
          if (runToken !== token) return;
          const raw = Math.min(1, Math.max(0, (now - started) / step.duration));
          adapter.applyPosition(adapter.interpolatePosition(start, target, easing(raw)), true);
          if (raw < 1) frame = requestAnimationFrame(draw);
          else { frame = null; adapter.applyPosition(target, true); logVisualState('end-transition-end', { index, position: step.position, to: describe(target) }); finish(); }
        };
        frame = requestAnimationFrame(draw);
      };
      advance(0);
    };

    return { cancel, run, runEnd, getActive: () => active, setActive: value => { active = value; } };
  }
};

window.RTSAnimationEngine = RTSAnimationEngine;
