(() => {
  const params = new URLSearchParams(window.location.search);
  if (params.get('dev') !== 'true') return;

  const bar = document.createElement('div');
  bar.id = 'rts-dev-toolbar';
  bar.innerHTML = `
    <span class="dev-label">RTS DEV</span>
    <button data-action="player">Show Player</button>
    <button data-action="clapper">Show Clapperboard</button>
    <button data-style="broadcast">Broadcast</button>
    <button data-style="cinematic">Cinematic</button>
    <button data-style="cut">Cut</button>
    <button data-style="minimal">Minimal</button>
    <button data-position="top">Top</button>
    <button data-position="bottom">Bottom</button>
    <label class="dev-speed-label" for="rts-dev-speed">Speed</label>
    <select id="rts-dev-speed" aria-label="Playback speed">
      <option value="0.25">0.25×</option>
      <option value="0.5">0.5×</option>
      <option value="0.75">0.75×</option>
      <option value="1" selected>1×</option>
      <option value="1.25">1.25×</option>
      <option value="1.5">1.5×</option>
      <option value="1.75">1.75×</option>
      <option value="2">2×</option>
    </select>
    <input id="rts-dev-title" value="FIRST TEST — REPLAY CAPTURE" aria-label="Preview title">
    <button data-action="show">Show Title</button>
    <button data-action="hide">Hide Title</button>
    <span class="spacer"></span>
    <span class="hint">?dev=true</span>
  `;
  document.body.prepend(bar);

  let style = 'broadcast';
  let position = 'bottom';
  let playerVisible = false;
  let clapperVisible = false;

  const formatDuration = seconds => {
    if (!Number.isFinite(seconds) || seconds < 0) return '—';
    const total = Math.round(seconds);
    return `${Math.floor(total / 60)}:${String(total % 60).padStart(2, '0')}`;
  };

  const updateClapper = command => {
    const current = command || RTSReplayVideo?.currentCommand || RTSReplay?.command || {};
    const number = current.replayNumber || 1;
    const title = current.replayTitle || 'FIRST TEST';
    const director = current.replayDirector || '—';
    const played = current.replayPlayedCount ?? '—';
    const length = formatDuration(RTSReplay?.video?.duration);
    const text = document.getElementById('message-text');
    if (text) text.textContent = `Playing replay #${number}: ${title}.`;
    document.getElementById('clapper-length').textContent = length;
    document.getElementById('clapper-director').textContent = director;
    document.getElementById('clapper-played').textContent = played;
  };

  window.RTSDevToolbar = { updateClapper };

  const showClapper = () => {
    const card = RTSReplay?.messageCard;
    if (!card) return;
    clapperVisible = true;
    updateClapper();
    card.classList.add('show');
    card.setAttribute('aria-hidden', 'false');
    bar.querySelector('[data-action="clapper"]').textContent = 'Hide Clapperboard';
  };

  const hideClapper = () => {
    const card = RTSReplay?.messageCard;
    if (!card) return;
    clapperVisible = false;
    card.classList.remove('show');
    card.setAttribute('aria-hidden', 'true');
    bar.querySelector('[data-action="clapper"]').textContent = 'Show Clapperboard';
  };

  const setSpeed = value => {
    const speed = Number(value);
    if (!Number.isFinite(speed) || !RTSReplay?.video) return;
    RTSReplay.video.playbackRate = speed;
    const command = Object.assign({}, RTSReplay.command || {}, {
      replayPlaybackSpeed: speed,
      replayPlaybackSpeedVisibility: RTSReplay.command?.replayPlaybackSpeedVisibility || 'Only when greater or less than 1'
    });
    RTSReplay.command = command;
    RTSReplayElements?.configureSpeed?.(command);
  };

  const showPlayer = () => {
    const player = RTSReplay?.player;
    if (!player) return;
    playerVisible = true;
    player.classList.add('dev-player', 'show');
    player.style.opacity = '1';
    player.style.visibility = 'visible';
    if (RTSReplay.frame) RTSReplay.frame.classList.add('dev-frame');
    bar.querySelector('[data-action="player"]').textContent = 'Hide Player';
  };

  const hidePlayer = () => {
    const player = RTSReplay?.player;
    if (!player) return;
    playerVisible = false;
    RTSReplay.hideTitle?.();
    player.classList.remove('show', 'dev-player');
    player.style.opacity = '';
    player.style.visibility = '';
    RTSReplay.frame?.classList.remove('dev-frame');
    bar.querySelector('[data-action="player"]').textContent = 'Show Player';
  };

  const preview = () => {
    showPlayer();
    setSpeed(document.getElementById('rts-dev-speed').value);
    const title = RTSReplay?.title;
    if (!title) return;
    RTSReplay.hideTitle?.();
    const text = document.getElementById('rts-dev-title').value.trim() || 'FIRST TEST — REPLAY CAPTURE';
    title.className = `title-${style} title-${position}`;
    title.textContent = text;
    title.classList.add('visible', 'title-enter');
  };

  bar.addEventListener('click', event => {
    const button = event.target.closest('button');
    if (!button) return;
    if (button.dataset.style) {
      style = button.dataset.style;
      bar.querySelectorAll('[data-style]').forEach(item => item.classList.toggle('active', item === button));
      preview();
    }
    if (button.dataset.position) {
      position = button.dataset.position;
      bar.querySelectorAll('[data-position]').forEach(item => item.classList.toggle('active', item === button));
      preview();
    }
    if (button.dataset.action === 'player') {
      playerVisible ? hidePlayer() : showPlayer();
      if (playerVisible) preview();
    }
    if (button.dataset.action === 'clapper') clapperVisible ? hideClapper() : showClapper();
    if (button.dataset.action === 'show') preview();
    if (button.dataset.action === 'hide') RTSReplay.hideTitle?.();
  });

  RTSReplay?.video?.addEventListener('loadedmetadata', () => {
    if (clapperVisible) updateClapper();
  });
  bar.querySelector('#rts-dev-speed').addEventListener('change', event => setSpeed(event.target.value));
  bar.querySelector('[data-style="broadcast"]').classList.add('active');
  bar.querySelector('[data-position="bottom"]').classList.add('active');
})();
