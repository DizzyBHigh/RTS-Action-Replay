(() => {
  const params = new URLSearchParams(window.location.search);
  if (params.get('dev') !== 'true') return;

  const screen = document.createElement('div');
  screen.id = 'rts-dev-screen';
  screen.innerHTML = '<div class="dev-screen-label">1920 × 1080</div><div class="dev-safe-area"></div>';
  document.body.prepend(screen);

  const bar = document.createElement('aside');
  bar.id = 'rts-dev-toolbar';
  bar.innerHTML = '<div class="dev-toolbar-header"><strong>RTS DEV</strong><span>TEST HARNESS</span></div>';
  document.body.prepend(bar);

  window.RTSDevToolbar = window.RTSDevToolbar || {};
})();