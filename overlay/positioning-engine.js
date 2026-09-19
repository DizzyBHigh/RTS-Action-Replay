const RTSPositioningEngine = {
  version: '20260919-5',
  diagnostics: new WeakMap(),
  referenceWidth: 1920,
  referenceHeight: 1080,

  transformFor(element, position) {
    const p = position || {};
    const number = (value, fallback) => {
      const result = Number(value);
      return Number.isFinite(result) ? result : fallback;
    };
    const x = number(p.x, 0), y = number(p.y, 0), z = number(p.z, 0);
    const scale = number(p.scale, 100) / 100;
    const scaleX = number(p.scaleX, scale * 100) / 100;
    const scaleY = number(p.scaleY, scale * 100) / 100;
    const rotateX = -number(p.rotateX, 0);
    const rotateY = number(p.rotateY, 0);
    const rotateZ = -number(p.rotateZ, 0);
    const fov = Math.max(30, Math.min(120, number(p.fov, 90)));
    const dev = Boolean(document.getElementById('rts-dev-stage'));
    const viewportWidth = dev ? this.referenceWidth : Math.max(1, window.innerWidth || this.referenceWidth);
    const viewportHeight = dev ? this.referenceHeight : Math.max(1, window.innerHeight || this.referenceHeight);
    const perspective = Math.max(1, (viewportWidth / 2) / Math.tan((fov * Math.PI / 180) / 2));
    // X/Y are screen-space positioning values. Compensate their
    // translation for Z depth so Z zooms around the existing screen position
    // instead of causing the element to drift toward/away from the center.
    const depthFactor = (perspective - z) / perspective;
    const xValue = `${x * viewportWidth / 100 * depthFactor}px`;
    const yValue = `${-y * viewportHeight / 100 * depthFactor}px`;
    return `perspective(${perspective}px) translate3d(${xValue}, ${yValue}, ${z}px) rotateZ(${rotateZ}deg) rotateY(${rotateY}deg) rotateX(${rotateX}deg) scale3d(${scaleX}, ${scaleY}, 1)`;
  },

  apply(element, position) {
    if (!element) return null;
    const transform = this.transformFor(element, position);
    element.style.transform = transform;

    if (document.getElementById('rts-dev-stage')) {
      const name = String(position?.name || position?.tag || '');
      const z = Number(position?.z);
      if (/center.?hidden/i.test(name) || (Number.isFinite(z) && z <= -2500)) {
        const signature = [name, position?.x, position?.y, position?.z, position?.scaleX, position?.scaleY, position?.fov].join('|');
        if (this.diagnostics.get(element) !== signature) {
          this.diagnostics.set(element, signature);
          const stage = document.getElementById('replay-player-stage');
          const screen = document.getElementById('rts-dev-screen');
          const rect = element.getBoundingClientRect();
          const stageRect = stage?.getBoundingClientRect();
          const screenRect = screen?.getBoundingClientRect();
          window.RTSDevToolbar?.log?.('Position geometry diagnostic', {
            name,
            position: {
              x: position?.x, y: position?.y, z: position?.z,
              scaleX: position?.scaleX, scaleY: position?.scaleY, fov: position?.fov
            },
            transform,
            rectCenter: {
              x: rect.left + rect.width / 2,
              y: rect.top + rect.height / 2
            },
            stageCenter: stageRect ? {
              x: stageRect.left + stageRect.width / 2,
              y: stageRect.top + stageRect.height / 2
            } : null,
            screenCenter: screenRect ? {
              x: screenRect.left + screenRect.width / 2,
              y: screenRect.top + screenRect.height / 2
            } : null,
            computed: {
              width: getComputedStyle(element).width,
              height: getComputedStyle(element).height,
              transformOrigin: getComputedStyle(element).transformOrigin,
              stageTransform: stage ? getComputedStyle(stage).transform : null,
              stagePerspective: stage ? getComputedStyle(stage).perspective : null,
              stagePerspectiveOrigin: stage ? getComputedStyle(stage).perspectiveOrigin : null
            }
          });
        }
      }
    }

    return transform;
  }
};

window.RTSPositioningEngine = RTSPositioningEngine;
