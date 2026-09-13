(() => {
  const params = new URLSearchParams(window.location.search);
  if (params.get('dev') !== 'true') return;

  const bar = document.getElementById('rts-dev-toolbar');
  if (!bar) return;

  const button = document.createElement('button');
  button.dataset.action = 'log';
  button.textContent = 'Show Log';
  bar.insertBefore(button, bar.querySelector('.spacer'));

  const panel = document.createElement('div');
  panel.id = 'rts-dev-log';
  panel.setAttribute('aria-hidden', 'true');
  panel.innerHTML = '<div class="dev-log-header"><b>Replay Diagnostics</b><button type="button" data-log-clear>Clear</button></div><pre></pre>';
  document.body.appendChild(panel);

  const output = panel.querySelector('pre');
  const entries = [];
  let visible = false;

  const stringify = value => {
    if (value instanceof Error) return `${value.name}: ${value.message}`;
    if (typeof value === 'string') return value;
    try { return JSON.stringify(value); } catch { return String(value); }
  };

  const log = (message, details) => {
    const stamp = new Date().toLocaleTimeString();
    const line = `[${stamp}] ${message}${details === undefined ? '' : ` ${stringify(details)}`}`;
    entries.push(line);
    while (entries.length > 200) entries.shift();
    output.textContent = entries.join('\n');
    panel.scrollTop = panel.scrollHeight;
  };

  const toggle = () => {
    visible = !visible;
    panel.classList.toggle('show', visible);
    panel.setAttribute('aria-hidden', String(!visible));
    button.textContent = visible ? 'Hide Log' : 'Show Log';
  };

  const video = RTSReplay?.video;
  const videoEvent = name => {
    if (!video) return;
    video.addEventListener(name, () => {
      const error = video.error;
      log(`video ${name}`, {
        readyState: video.readyState,
        networkState: video.networkState,
        currentTime: video.currentTime,
        duration: Number.isFinite(video.duration) ? video.duration : null,
        error: error ? { code: error.code, message: error.message || '' } : null
      });
    });
  };

  ['loadstart', 'loadedmetadata', 'durationchange', 'loadeddata', 'canplay', 'canplaythrough', 'playing', 'waiting', 'stalled', 'suspend', 'abort', 'emptied', 'error', 'ended'].forEach(videoEvent);

  const previous = window.RTSDevToolbar || {};
  window.RTSDevToolbar = { ...previous, log };

  bar.addEventListener('click', event => {
    const target = event.target.closest('[data-action="log"]');
    if (!target) return;
    toggle();
  });

  panel.addEventListener('click', event => {
    const target = event.target.closest('[data-log-clear]');
    if (!target) return;
    entries.length = 0;
    output.textContent = '';
  });

  log('diagnostics ready');
})();
