(() => {
  const params = new URLSearchParams(window.location.search);
  if (params.get('dev') !== 'true') return;

  const toolbar = document.getElementById('rts-dev-toolbar');
  const screen = document.getElementById('rts-dev-screen');
  const overlay = document.getElementById('rts-overlay');
  if (!toolbar || !screen || !overlay) return;

  const viewport = document.createElement('div');
  viewport.id = 'rts-dev-viewport';
  const stage = document.createElement('div');
  stage.id = 'rts-dev-stage';
  const controls = document.createElement('div');
  controls.id = 'rts-dev-viewport-controls';
  controls.innerHTML = '<button type="button" data-viewport-reset>Reset View</button><span>Drag to pan · Wheel to zoom · R to reset</span>';
  viewport.append(stage, controls);
  document.body.appendChild(viewport);

  stage.append(screen, overlay);

  let scale = 1;
  let x = 0;
  let y = 0;
  let dragging = false;
  let pointerX = 0;
  let pointerY = 0;

  const fitScale = () => Math.min(viewport.clientWidth / 1920, viewport.clientHeight / 1080);
  const minScale = () => fitScale() * 0.5;

  const clamp = () => {
    const effective = Math.max(scale, minScale());
    const width = 1920 * effective;
    const height = 1080 * effective;
    if (width <= viewport.clientWidth) x = (viewport.clientWidth - width) / 2;
    else x = Math.min(0, Math.max(viewport.clientWidth - width, x));
    if (height <= viewport.clientHeight) y = (viewport.clientHeight - height) / 2;
    else y = Math.min(0, Math.max(viewport.clientHeight - height, y));
  };

  const render = () => {
    clamp();
    stage.style.transform = `translate3d(${x}px,${y}px,0) scale(${scale})`;
    controls.querySelector('span').textContent = `Drag to pan · Wheel to zoom · ${Math.round(scale / fitScale() * 100)}% · R to reset`;
  };

  const reset = () => {
    scale = fitScale();
    x = 0;
    y = 0;
    render();
  };

  const zoom = (factor, clientX, clientY) => {
    const oldScale = scale;
    const nextScale = Math.min(3, Math.max(minScale(), oldScale * factor));
    if (nextScale === oldScale) return;
    const bounds = viewport.getBoundingClientRect();
    const localX = clientX - bounds.left;
    const localY = clientY - bounds.top;
    x = localX - (localX - x) * (nextScale / oldScale);
    y = localY - (localY - y) * (nextScale / oldScale);
    scale = nextScale;
    render();
  };

  viewport.addEventListener('pointerdown', event => {
    if (event.button !== 0 || event.target.closest('button')) return;
    dragging = true;
    pointerX = event.clientX;
    pointerY = event.clientY;
    viewport.setPointerCapture(event.pointerId);
    viewport.classList.add('dragging');
  });

  viewport.addEventListener('pointermove', event => {
    if (!dragging) return;
    x += event.clientX - pointerX;
    y += event.clientY - pointerY;
    pointerX = event.clientX;
    pointerY = event.clientY;
    render();
  });

  const stopDrag = event => {
    if (!dragging) return;
    dragging = false;
    if (viewport.hasPointerCapture(event.pointerId)) viewport.releasePointerCapture(event.pointerId);
    viewport.classList.remove('dragging');
  };
  viewport.addEventListener('pointerup', stopDrag);
  viewport.addEventListener('pointercancel', stopDrag);
  viewport.addEventListener('wheel', event => {
    event.preventDefault();
    zoom(event.deltaY < 0 ? 1.1 : 1 / 1.1, event.clientX, event.clientY);
  }, { passive: false });

  controls.querySelector('[data-viewport-reset]').addEventListener('click', reset);
  window.addEventListener('keydown', event => {
    if (event.key.toLowerCase() === 'r' && !/input|select|textarea/i.test(document.activeElement?.tagName || '')) reset();
  });
  window.addEventListener('resize', () => {
    if (scale <= fitScale() + 0.001) reset();
    else render();
  });

  reset();
})();
