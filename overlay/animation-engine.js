const RTSAnimationEngine = {
  createRunner(adapter) {
    let token = 0;
    let timer = null;
    let frame = null;
    let active = null;

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
      const first = adapter.getPosition(steps[0].position);
      active = first;
      adapter.applyPosition(first, true);

      const advance = index => {
        if (runToken !== token) return;
        if (index >= steps.length) { onComplete?.(); return; }
        const step = steps[index];
        const target = adapter.getPosition(step.position);
        const start = active || target;
        const finish = () => {
          if (runToken !== token) return;
          active = target;
          if (step.delay > 0) timer = setTimeout(() => advance(index + 1), step.delay);
          else advance(index + 1);
        };
        if (step.duration <= 0 || adapter.positionsEqual?.(start, target)) {
          adapter.applyPosition(target, true);
          finish();
          return;
        }
        const easing = adapter.easing(step.easing);
        const started = performance.now();
        const draw = now => {
          if (runToken !== token) return;
          const raw = Math.min(1, Math.max(0, (now - started) / step.duration));
          adapter.applyPosition(adapter.interpolatePosition(start, target, easing(raw)), true);
          if (raw < 1) frame = requestAnimationFrame(draw);
          else { frame = null; adapter.applyPosition(target, true); finish(); }
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
      let current = active || adapter.getPosition(steps[0].position);
      const advance = index => {
        if (runToken !== token) return;
        if (index >= steps.length) { active = current; onComplete?.(); return; }
        const step = steps[index];
        const target = adapter.getPosition(step.position);
        const start = current;
        const finish = () => {
          if (runToken !== token) return;
          current = target;
          active = target;
          if (step.delay > 0) timer = setTimeout(() => advance(index + 1), step.delay);
          else advance(index + 1);
        };
        if (step.duration <= 0 || adapter.positionsEqual?.(start, target)) {
          adapter.applyPosition(target, true);
          finish();
          return;
        }
        const easing = adapter.easing(step.easing);
        const started = performance.now();
        const draw = now => {
          if (runToken !== token) return;
          const raw = Math.min(1, Math.max(0, (now - started) / step.duration));
          adapter.applyPosition(adapter.interpolatePosition(start, target, easing(raw)), true);
          if (raw < 1) frame = requestAnimationFrame(draw);
          else { frame = null; adapter.applyPosition(target, true); finish(); }
        };
        frame = requestAnimationFrame(draw);
      };
      advance(0);
    };

    return { cancel, run, runEnd, getActive: () => active, setActive: value => { active = value; } };
  }
};

window.RTSAnimationEngine = RTSAnimationEngine;
