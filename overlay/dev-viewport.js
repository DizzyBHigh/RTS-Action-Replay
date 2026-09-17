(() => {
  const params = new URLSearchParams(window.location.search);
  if (params.get('dev') !== 'true') return;

  const screen = document.getElementById('rts-dev-screen');
  const overlay = document.getElementById('rts-overlay');
  const bar = document.getElementById('rts-dev-toolbar');
  const workspace = document.getElementById('rts-dev-workspace');
  if (!screen || !overlay || !bar || !workspace) return;

  let zoom = 1;
  let panX = Math.round(286 / 2);
  let panY = 0;
  let dragging = false;
  let startX = 0;
  let startY = 0;

  const controls = document.createElement('div');
  controls.className = 'dev-viewport-controls';
  controls.innerHTML = '<button type="button" data-viewport="reset">Reset View</button><button type="button" data-viewport="minus">−</button><button type="button" data-viewport="plus">+</button>';
  const zoomRow = document.createElement('div');
  zoomRow.className = 'dev-viewport-zoom';
  zoomRow.innerHTML = '<span data-viewport-zoom>100%</span>';
  const footer = bar.querySelector('.dev-toolbar-footer');
  if (footer) {
    footer.insertBefore(zoomRow, footer.firstChild);
    footer.insertBefore(controls, footer.firstChild);
  }

  const apply = () => {
    workspace.style.setProperty('--dev-pan-x', `${panX}px`);
    workspace.style.setProperty('--dev-pan-y', `${panY}px`);
    workspace.style.setProperty('--dev-zoom', zoom);
    const label = zoomRow.querySelector('[data-viewport-zoom]');
    if (label) label.textContent = `${Math.round(zoom * 100)}%`;
  };

  const reset = () => {
    zoom = 1;
    panX = Math.round(286 / 2);
    panY = 0;
    apply();
  };

  const setZoom = value => {
    zoom = Math.min(2, Math.max(0.25, Number(value) || 1));
    apply();
  };

  controls.addEventListener('click', event => {
    const action = event.target.closest('[data-viewport]')?.dataset.viewport;
    if (action === 'reset') reset();
    if (action === 'minus') setZoom(zoom - 0.1);
    if (action === 'plus') setZoom(zoom + 0.1);
  });

  workspace.addEventListener('pointerdown', event => {
    if (event.button !== 0 || event.target.closest('#rts-dev-screen')) return;
    if (event.target.closest('button, input, select, textarea, a')) return;
    dragging = true;
    startX = event.clientX - panX;
    startY = event.clientY - panY;
    workspace.setPointerCapture(event.pointerId);
  });

  workspace.addEventListener('pointermove', event => {
    if (!dragging) return;
    panX = event.clientX - startX;
    panY = event.clientY - startY;
    apply();
  });

  const stopDrag = event => {
    dragging = false;
    if (event.pointerId !== undefined && workspace.hasPointerCapture(event.pointerId)) workspace.releasePointerCapture(event.pointerId);
  };
  workspace.addEventListener('pointerup', stopDrag);
  workspace.addEventListener('pointercancel', stopDrag);

  workspace.addEventListener('wheel', event => {
    event.preventDefault();
    setZoom(zoom + (event.deltaY < 0 ? 0.1 : -0.1));
  }, { passive: false, capture: true });

  apply();
  window.RTSDevViewport = { reset, setZoom };
})();
