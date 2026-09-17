(() => {
  const params = new URLSearchParams(window.location.search);
  if (params.get('dev') !== 'true') return;

  const screen = document.getElementById('rts-dev-screen');
  const bar = document.getElementById('rts-dev-toolbar');
  if (!screen || !bar) return;

  let zoom = 1;
  let panX = Math.max(0, Math.round((window.innerWidth - 286) / 2 - window.innerWidth / 2));
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
    screen.style.setProperty('--dev-zoom', zoom);
    screen.style.setProperty('--dev-pan-x', `${panX}px`);
    screen.style.setProperty('--dev-pan-y', `${panY}px`);
    const label = zoomRow.querySelector('[data-viewport-zoom]');
    if (label) label.textContent = `${Math.round(zoom * 100)}%`;
  };

  const reset = () => {
    zoom = 1;
    panX = Math.max(0, Math.round((window.innerWidth - 286) / 2 - window.innerWidth / 2));
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

  screen.addEventListener('pointerdown', event => {
    if (event.button !== 0) return;
    dragging = true;
    startX = event.clientX - panX;
    startY = event.clientY - panY;
    screen.setPointerCapture(event.pointerId);
  });

  screen.addEventListener('pointermove', event => {
    if (!dragging) return;
    panX = event.clientX - startX;
    panY = event.clientY - startY;
    apply();
  });

  const stopDrag = event => {
    dragging = false;
    if (event.pointerId !== undefined && screen.hasPointerCapture(event.pointerId)) screen.releasePointerCapture(event.pointerId);
  };
  screen.addEventListener('pointerup', stopDrag);
  screen.addEventListener('pointercancel', stopDrag);

  screen.addEventListener('wheel', event => {
    event.preventDefault();
    setZoom(zoom + (event.deltaY < 0 ? 0.1 : -0.1));
  }, { passive: false });

  window.addEventListener('resize', () => {
    if (!dragging && Math.abs(panY) < 1) panX = Math.max(0, Math.round((window.innerWidth - 286) / 2 - window.innerWidth / 2));
    apply();
  });

  apply();
  window.RTSDevViewport = { reset, setZoom };
})();