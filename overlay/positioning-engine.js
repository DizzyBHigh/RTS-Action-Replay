const RTSPositioningEngine = {
  version: '20260922-2',
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

    const tanHalfFov = Math.tan((fov * Math.PI / 180) / 2);

    // Match the deployed RtsUI.dll position preview. The DLL preview uses a
    // 640x360 position canvas and a camera distance of 360*tan(FOV/2).
    // WPF projection therefore scales screen-space translation by
    // (640/2/tan(FOV/2)) / (360*tan(FOV/2)).
    const perspective = Math.max(1, 360.0 * tanHalfFov);
    const projectionScale = (320.0 / tanHalfFov) / perspective;
    const depthFactor = (perspective - z) / perspective;
    const xValue = `${x * viewportWidth / 100 * projectionScale * depthFactor}px`;
    const yValue = `${-y * viewportHeight / 100 * projectionScale * depthFactor}px`;
    return `perspective(${perspective}px) translate3d(${xValue}, ${yValue}, ${z}px) rotateZ(${rotateZ}deg) rotateY(${rotateY}deg) rotateX(${rotateX}deg) scale3d(${scaleX}, ${scaleY}, 1)`;
  },

  apply(element, position) {
    if (!element) return null;
    const canvas = element.offsetParent || document.getElementById('rts-overlay') || document.body;
    const canvasWidth = Math.max(1, canvas.clientWidth || this.referenceWidth);
    const canvasHeight = Math.max(1, canvas.clientHeight || this.referenceHeight);
    const width = Math.max(0, element.offsetWidth || 0);
    const height = Math.max(0, element.offsetHeight || 0);
    element.style.left = `${(canvasWidth - width) / 2}px`;
    element.style.top = `${(canvasHeight - height) / 2}px`;

    const transform = this.transformFor(element, position);
    element.style.transform = transform;

    if (document.getElementById('rts-dev-stage')) {
      const name = String(position?.name || position?.tag || '');
      const z = Number(position?.z);
      if (/center.?hidden/i.test(name) || (Number.isFinite(z) && z <= -2500)) {
        const signature = [name, position?.x, position?.y, position?.z, position?.scaleX, position?.scaleY, position?.fov].join('|');
        if (this.diagnostics.get(element) !== signature) {
          this.diagnostics.set(element, signature);
          const screen = document.getElementById('rts-dev-screen');
          const rect = element.getBoundingClientRect();
          const canvasRect = canvas.getBoundingClientRect();
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
            canvasCenter: {
              x: canvasRect.left + canvasRect.width / 2,
              y: canvasRect.top + canvasRect.height / 2
            },
            screenCenter: screenRect ? {
              x: screenRect.left + screenRect.width / 2,
              y: screenRect.top + screenRect.height / 2
            } : null,
            computed: {
              width: getComputedStyle(element).width,
              height: getComputedStyle(element).height,
              transformOrigin: getComputedStyle(element).transformOrigin,
              canvasTransform: getComputedStyle(canvas).transform,
              canvasWidth,
              canvasHeight,
              anchor: {
                left: element.offsetLeft,
                top: element.offsetTop,
                width,
                height
              }
            }
          });
        }
      }
    }

    return transform;
  }
};

window.RTSPositioningEngine = RTSPositioningEngine;
