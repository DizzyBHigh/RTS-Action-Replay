const RTSReplayBroadcast = window.RTSReplay;
const broadcastTitle = document.getElementById('player-title');

RTSReplayBroadcast.stopBroadcastChevrons = () => {
  if (RTSReplayBroadcast.broadcastChevronSpawnTimer) clearTimeout(RTSReplayBroadcast.broadcastChevronSpawnTimer);
  RTSReplayBroadcast.broadcastChevronSpawnTimer = null;
  broadcastTitle?.querySelector('.broadcast-chevron-track')?.remove();
};

RTSReplayBroadcast.startBroadcastChevrons = () => {
  if (!broadcastTitle) return;
  RTSReplayBroadcast.stopBroadcastChevrons();

  const track = document.createElement('div');
  track.className = 'broadcast-chevron-track';
  broadcastTitle.append(track);
  RTSReplayBroadcast.broadcastChevronTrack = track;

  const colour = value => {
    const raw = String(value || '').trim();
    const match = raw.match(/^#([0-9a-f]{6}|[0-9a-f]{8})$/i);
    return match ? `#${match[1].slice(0, 6)}` : raw;
  };
  const number = (value, min, max, fallback) => Math.max(min, Math.min(max, Number(value) || fallback));
  const randomValue = (max, min) => min + Math.random() * Math.max(0, max - min);
  const styles = getComputedStyle(broadcastTitle);
  const primary = colour(styles.getPropertyValue('--title-primary')) || '#0384CB';
  const secondary = colour(styles.getPropertyValue('--title-secondary')) || '#FFD400';
  const heightSetting = number(RTSReplayBroadcast.command?.replayBroadcastChevronHeight, 1, track.clientHeight, 42);
  const widthSetting = number(RTSReplayBroadcast.command?.replayBroadcastChevronWidth, 1, 300, 90);
  const spacingSetting = number(RTSReplayBroadcast.command?.replayBroadcastChevronSpacing, 0, 200, 0);
  const randomHeight = RTSReplayBroadcast.command?.replayBroadcastRandomHeight === true;
  const randomWidth = RTSReplayBroadcast.command?.replayBroadcastRandomWidth === true;
  const randomSpacing = RTSReplayBroadcast.command?.replayBroadcastRandomSpacing === true;
  const speed = number(RTSReplayBroadcast.command?.replayBroadcastChevronSpeed, 10, 500, 95);
  const seedLeft = -120;
  const trackWidth = track.clientWidth;
  const getHeight = () => randomHeight ? randomValue(heightSetting, 1) : heightSetting;
  const getWidth = () => randomWidth ? randomValue(widthSetting, 1) : widthSetting;
  const getSpacing = () => randomSpacing ? randomValue(spacingSetting, 0) : spacingSetting;
  const dimensions = (width, height) => ({ width, height, strokeWidth: Math.max(6, Math.min(20, height * 0.18)) });
  const travelDelay = (width, height, spacing) => {
    const size = dimensions(width, height);
    return Math.max(0, (size.width + size.strokeWidth + spacing) / speed * 1000);
  };

  const createChevron = (left, width, height, index) => {
    const size = dimensions(width, height);
    const mover = document.createElement('span');
    mover.className = 'broadcast-chevron-mover';
    mover.style.left = `${left.toFixed(2)}px`;
    mover.style.width = `${size.width.toFixed(2)}px`;
    const chevron = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    chevron.classList.add('broadcast-chevron');
    chevron.setAttribute('viewBox', '0 0 50 100');
    chevron.setAttribute('width', size.width.toFixed(2));
    chevron.setAttribute('height', size.height.toFixed(2));
    chevron.style.setProperty('--chevron-width', `${size.width.toFixed(2)}px`);
    chevron.style.setProperty('--chevron-height', `${size.height.toFixed(2)}px`);
    const chevronColor = index % 2 ? secondary : primary;
    chevron.style.setProperty('--chevron-color', chevronColor);
    const path = document.createElementNS('http://www.w3.org/2000/svg', 'polyline');
    path.setAttribute('points', '0,3 50,50 0,97');
    path.setAttribute('fill', 'none');
    path.setAttribute('stroke', 'currentColor');
    path.setAttribute('stroke-width', String(size.strokeWidth));
    path.setAttribute('vector-effect', 'non-scaling-stroke');
    path.setAttribute('stroke-linecap', 'butt');
    path.setAttribute('stroke-linejoin', 'miter');
    chevron.append(path);
    mover.append(chevron);
    track.append(mover);
    return mover;
  };

  const animate = (mover, left) => {
    const distance = trackWidth - left + 60;
    const animation = mover.animate(
      [{ transform: 'translate3d(0,0,0)' }, { transform: `translate3d(${distance}px,0,0)` }],
      { duration: (distance / speed) * 1000, easing: 'linear', fill: 'forwards' }
    );
    animation.onfinish = () => mover.remove();
  };

  let index = 0;
  let pendingWidth = getWidth();
  let pendingHeight = getHeight();
  let pendingSpacing = getSpacing();

  const spawn = () => {
    if (!broadcastTitle.classList.contains('title-broadcast') || !broadcastTitle.classList.contains('visible') || track !== RTSReplayBroadcast.broadcastChevronTrack) {
      RTSReplayBroadcast.stopBroadcastChevrons();
      return;
    }
    const width = pendingWidth;
    const height = pendingHeight;
    const spacing = pendingSpacing;
    const mover = createChevron(seedLeft, width, height, index++);
    animate(mover, seedLeft);
    pendingWidth = getWidth();
    pendingHeight = getHeight();
    pendingSpacing = getSpacing();
    RTSReplayBroadcast.broadcastChevronSpawnTimer = setTimeout(spawn, travelDelay(width, height, spacing));
  };

  spawn();
};

RTSReplayBroadcast.observeBroadcastChevrons = () => {
  if (!broadcastTitle) return;
  const observer = new MutationObserver(() => {
    if (broadcastTitle.classList.contains('title-broadcast') && broadcastTitle.classList.contains('visible')) RTSReplayBroadcast.startBroadcastChevrons();
    else RTSReplayBroadcast.stopBroadcastChevrons();
  });
  observer.observe(broadcastTitle, { attributes: true, attributeFilter: ['class'] });
};

RTSReplayBroadcast.observeBroadcastChevrons();
