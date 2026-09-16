const RTSReplayPanelBroadcast = window.RTSReplay;

RTSReplayPanelBroadcast.stopPanelChevrons = panel => {
  if (panel?._rtsPanelChevronTimer) clearTimeout(panel._rtsPanelChevronTimer);
  if (panel?._rtsPanelChevronTrack) panel._rtsPanelChevronTrack.remove();
  panel._rtsPanelChevronTimer = null;
  panel._rtsPanelChevronTrack = null;
};

RTSReplayPanelBroadcast.startPanelChevrons = (panel, command) => {
  if (!panel?.classList.contains('panel-broadcast') || !panel.classList.contains('show')) return;
  const header = panel.querySelector('.rts-panel-header');
  if (!header) return;
  RTSReplayPanelBroadcast.stopPanelChevrons(panel);

  const colour = value => { const raw = String(value || '').trim(); const match = raw.match(/^#([0-9a-f]{6}|[0-9a-f]{8})$/i); return match ? `#${match[1].slice(0, 6)}` : raw; };
  const number = (value, min, max, fallback) => Math.max(min, Math.min(max, Number(value) || fallback));
  const randomValue = (max, min = 1) => min + Math.random() * Math.max(0, max - min);
  const primary = colour(command?.replayBroadcastPrimaryColor || '#0384CB');
  const secondary = colour(command?.replayBroadcastSecondaryColor || '#FFD400');
  const panelScale = Math.min(1, panel.clientWidth / Math.max(1, window.innerWidth));
  const heightSetting = number(command?.replayBroadcastChevronHeight, 1, 89, 42) * panelScale;
  const widthSetting = number(command?.replayBroadcastChevronWidth, 1, 300, 42) * panelScale;
  const spacingSetting = number(command?.replayBroadcastChevronSpacing, 0, 200, 0) * panelScale;
  const randomHeight = command?.replayBroadcastRandomHeight === true;
  const randomWidth = command?.replayBroadcastRandomWidth === true;
  const randomSpacing = command?.replayBroadcastRandomSpacing === true;
  const speed = number(command?.replayBroadcastChevronSpeed, 10, 500, 95);
  const track = document.createElement('div');
  track.className = 'panel-broadcast-chevron-track';
  header.append(track);
  panel._rtsPanelChevronTrack = track;
  panel.style.setProperty('--panel-chevron-height', `${heightSetting}px`);
  const trackWidth = track.clientWidth;
  if (trackWidth <= 0) return;

  const getHeight = () => randomHeight ? randomValue(heightSetting) : heightSetting;
  const getWidth = () => randomWidth ? randomValue(widthSetting) : widthSetting;
  const getSpacing = () => randomSpacing ? randomValue(spacingSetting, 0) : spacingSetting;
  const chevronArm = (width, height) => Math.min(height * 0.42, width * 0.45);
  const createChevron = (left, width, height, index) => {
    const mover = document.createElement('span'); mover.className = 'panel-broadcast-chevron-mover'; mover.style.left = `${left.toFixed(2)}px`;
    const chevron = document.createElementNS('http://www.w3.org/2000/svg', 'svg'); chevron.classList.add('panel-broadcast-chevron'); chevron.setAttribute('viewBox', `0 0 ${width} ${height}`); chevron.setAttribute('width', width.toFixed(2)); chevron.setAttribute('height', height.toFixed(2)); chevron.style.color = index % 2 ? secondary : primary;
    const arm = chevronArm(width, height); const polygon = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
    polygon.setAttribute('points', [`0,0`, `${width - arm},0`, `${width},${height / 2}`, `${width - arm},${height}`, `0,${height}`, `${arm},${height / 2}`].join(' ')); polygon.setAttribute('fill', 'currentColor');
    chevron.append(polygon); mover.append(chevron); track.append(mover); return mover;
  };
  const animate = (mover, left, width) => { const distance = trackWidth + width - left; const animation = mover.animate([{ transform: 'translate3d(0,0,0)' }, { transform: `translate3d(${distance}px,0,0)` }], { duration: distance / speed * 1000, easing: 'linear', fill: 'forwards' }); animation.onfinish = () => mover.remove(); };

  let index = 0, pendingWidth = getWidth(), pendingHeight = getHeight(), pendingSpacing = getSpacing();
  const spawn = () => {
    if (!panel.classList.contains('panel-broadcast') || !panel.classList.contains('show') || track !== panel._rtsPanelChevronTrack) { RTSReplayPanelBroadcast.stopPanelChevrons(panel); return; }
    const width = pendingWidth, height = pendingHeight, spacing = pendingSpacing;
    animate(createChevron(-width, width, height, index++), -width, width);
    pendingWidth = getWidth(); pendingHeight = getHeight(); pendingSpacing = getSpacing();
    const pitch = Math.max(1, width - chevronArm(width, height) + spacing);
    panel._rtsPanelChevronTimer = setTimeout(spawn, pitch / speed * 1000);
  };
  spawn();
};

window.RTSReplayPanelBroadcast = RTSReplayPanelBroadcast;
