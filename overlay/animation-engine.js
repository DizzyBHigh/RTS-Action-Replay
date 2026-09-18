const RTSAnimationEngine = {
  normalisePosition(position, fallbackScale = 100) {
    const scale = Number(position?.scale ?? fallbackScale);
    const value = (key, fallback) => {
      const n = Number(position?.[key]);
      return Number.isFinite(n) ? n : fallback;
    };
    return {
      scaleX: value('scaleX', Number.isFinite(scale) ? scale : fallbackScale),
      scaleY: value('scaleY', Number.isFinite(scale) ? scale : fallbackScale),
      x: value('x', 0), y: value('y', 0), z: value('z', 0),
      rotateX: value('rotateX', 0), rotateY: value('rotateY', 0),
      rotateZ: value('rotateZ', 0), fov: Math.max(30, Math.min(120, value('fov', 90)))
    };
  },

  getPositions(raw, fallback = {}) {
    try {
      const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
      return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : fallback;
    } catch (_) {
      return fallback;
    }
  },

  resolvePosition(positions, name, fallback) {
    const target = String(name || '').trim().toLowerCase();
    const keys = Object.keys(positions || {});
    const key = keys.find(k => k.toLowerCase() === target ||
      String(positions[k]?.tag || '').trim().toLowerCase() === target);
    return key ? positions[key] : fallback;
  },

  positionsEqual(a, b) {
    if (!a || !b) return false;
    const x = this.normalisePosition(a);
    const y = this.normalisePosition(b);
    return ['scaleX','scaleY','x','y','z','rotateX','rotateY','rotateZ','fov']
      .every(key => x[key] === y[key]);
  },

  easing(name) {
    switch (String(name || 'ease-in-out').toLowerCase()) {
      case 'linear': return t => t;
      case 'ease-in': return t => t * t * t;
      case 'ease-out': return t => 1 - Math.pow(1 - t, 3);
      default: return t => t < .5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
    }
  },

  interpolatePosition(from, to, progress) {
    const a = this.normalisePosition(from);
    const b = this.normalisePosition(to);
    const lerp = (x, y) => x + (y - x) * progress;
    return Object.fromEntries(Object.keys(a).map(key => [key, lerp(a[key], b[key])]));
  },

  readProfile(raw) {
    if (!raw) return null;
    try {
      const profile = typeof raw === 'string' ? JSON.parse(raw) : raw;
      return profile && typeof profile === 'object' ? profile : null;
    } catch (error) {
      window.RTSDevToolbar?.log?.('Animation profile parse failed', { error: String(error) });
      return null;
    }
  },

  createRunner(options = {}) {
    const target = options.target;
    let positions = options.positions || {};
    const fallback = options.defaultPosition || { scale: 100, x: 0, y: 0, z: 0, rotateX: 0, rotateY: 0, rotateZ: 0, fov: 90 };
    let active = null, token = 0, timer = null, frame = null;
    const log = (message, details) => window.RTSDevToolbar?.log?.(message, details);
    const resolve = name => this.resolvePosition(positions, name, fallback);
    const apply = position => { if (target) RTSPositioningEngine.apply(target, position); active = position; };
    const cancel = () => {
      token++;
      if (timer) clearTimeout(timer);
      if (frame) cancelAnimationFrame(frame);
      timer = frame = null;
    };
    const normaliseStep = step => ({
      position: step?.position || step?.name || Object.keys(positions)[0],
      duration: Math.max(0, Number(step?.duration) || 0),
      delay: Math.max(0, Number(step?.delay) || 0),
      easing: step?.easing || 'ease-in-out'
    });
    const run = (sequence, complete, endRun = false) => {
      const steps = Array.isArray(sequence) ? sequence.map(normaliseStep) : [];
      if (!steps.length) { complete?.(); return; }
      cancel();
      const runToken = token;
      let current = endRun ? active || resolve(steps[0].position) : resolve(steps[0].position);
      if (!endRun) {
        apply(current);
        if (steps[0].delay > 0) timer = setTimeout(() => advance(1), steps[0].delay);
        else advance(1);
      } else advance(0);

      const advance = index => {
        if (runToken !== token) return;
        if (index >= steps.length) { active = current; complete?.(); return; }
        const step = steps[index], targetPosition = resolve(step.position), start = current;
        const equal = this.positionsEqual(start, targetPosition);
        log('Animation transition check', { index, position: step.position, duration: step.duration, delay: step.delay, equal, from: start, to: targetPosition });
        const finish = () => {
          current = targetPosition; active = current;
          if (step.delay > 0) timer = setTimeout(() => advance(index + 1), step.delay);
          else advance(index + 1);
        };
        if (step.duration <= 0 || equal) { apply(targetPosition); finish(); return; }
        const ease = this.easing(step.easing), started = performance.now();
        const draw = now => {
          if (runToken !== token) return;
          const progress = Math.min(1, Math.max(0, (now - started) / step.duration));
          apply(this.interpolatePosition(start, targetPosition, ease(progress)));
          if (progress < 1) frame = requestAnimationFrame(draw);
          else { frame = null; apply(targetPosition); finish(); }
        };
        frame = requestAnimationFrame(draw);
      };
    };
    return {
      configure(raw) { positions = RTSAnimationEngine.getPositions(raw, {}); active = null; },
      resolve,
      apply,
      animate(from, to, duration, easing, complete) {
        run([{ position: '__target__', duration, easing }], complete);
      },
      run(sequence, complete) { run(sequence, complete, false); },
      runEnd(sequence, complete) { run(sequence, complete, true); },
      cancel,
      getActive: () => active,
      setActive: value => { active = value; }
    };
  }
};

window.RTSAnimationEngine = RTSAnimationEngine;