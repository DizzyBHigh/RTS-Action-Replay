const RTSPositioningEngine = {
  version: '20260918-4',
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
    const xValue = `${x * viewportWidth / 100}px`;
    const yValue = `${-y * viewportHeight / 100}px`;
    return `perspective(${perspective}px) translate(-50%, -50%) translate3d(${xValue}, ${yValue}, ${z}px) rotateZ(${rotateZ}deg) rotateY(${rotateY}deg) rotateX(${rotateX}deg) scale3d(${scaleX}, ${scaleY}, 1)`;
  },

  apply(element, position) {
    if (!element) return null;
    const transform = this.transformFor(element, position);
    element.style.transform = transform;
    window.RTSDevToolbar?.log?.('Positioning engine applied', {
      version: this.version,
      elementId: element.id,
      transform,
      inlineTransform: element.style.transform
    });
    return transform;
  }
};

window.RTSPositioningEngine = RTSPositioningEngine;
