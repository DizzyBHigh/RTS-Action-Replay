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
  const widthSetting = number(RTSReplayBroadcast.command?.replayBroadcastChevronWidth, 1, 200, heightSetting);
  const spacingSetting = number(RTSReplayBroadcast.command?.replayBroadcastChevronSpacing, 0, 200, 0);
  const randomHeight = RTSReplayBroadcast.command?.replayBroadcastRandomHeight === true;
  const randomWidth = RTSReplayBroadcast.command?.replayBroadcastRandomWidth === true;
  const randomSpacing = RTSReplayBroadcast.command?.replayBroadcastRandomSpacing === true;
  const speed = number(RTSReplayBroadcast.command?.replayBroadcastChevronSpeed, 10, 500, 95);
  const seedLeft = -90;
  const trackWidth = track.clientWidth;
  const getHeight = () => randomHeight ? randomValue(heightSetting, 1) : heightSetting;
  const getWidth = () => randomWidth ? randomValue(widthSetting, 1) : widthSetting;
  const getSpacing = () => randomSpacing ? randomValue(spacingSetting, 0) : spacingSetting;
  const dimensions = (width, height) => {
    const scale = Math.min(width / 50, height / 100);
    return { width: 50 * scale, height: 100 * scale };
  };
  const pitch = (width, height, spacing) => Math.max(1, dimensions(width, height).width + spacing);

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
    chevron.style.setProperty('--chevron-color', index % 2 ? secondary : primary);
    const path = document.createElementNS('http://www.w3.org/2000/svg', 'polyline');
    path.setAttribute('points', '3,3 47,50 3,97');
    path.setAttribute('fill', 'none');
    path.setAttribute('stroke', 'currentColor');
    path.setAttribute('stroke-width', '7');
    path.setAttribute('stroke-linecap', 'round');
    path.setAttribute('stroke-linejoin', 'round');
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
  let left = seedLeft;
  let width = getWidth();
  let height = getHeight();
  let spacing = getSpacing();
  while (left < trackWidth) {
    createChevron(left, width, height, index++);
    animate(track.lastElementChild, left);
    left += pitch(width, height, spacing);
    width = getWidth();
    height = getHeight();
    spacing = getSpacing();
  }

  let pendingWidth = width;
  let pendingHeight = height;
  let pendingSpacing = spacing;
  const spawn = () => {
    if (!broadcastTitle.classList.contains('title-broadcast') || !broadcastTitle.classList.contains('visible') || track !== RTSReplayBroadcast.broadcastChevronTrack) {
      RTSReplayBroadcast.stopBroadcastChevrons();
      return;
    }
    const currentWidth = pendingWidth;
    const currentHeight = pendingHeight;
    const currentSpacing = pendingSpacing;
    const mover = createChevron(seedLeft, currentWidth, currentHeight, index++);
    animate(mover, seedLeft);
    pendingWidth = getWidth();
    pendingHeight = getHeight();
    pendingSpacing = getSpacing();
    const delay = Math.max(0, pitch(currentWidth, currentHeight, currentSpacing) / speed * 1000);
    RTSReplayBroadcast.broadcastChevronSpawnTimer = setTimeout(spawn, delay);
  };

  RTSReplayBroadcast.broadcastChevronSpawnTimer = setTimeout(spawn, Math.max(0, pitch(pendingWidth, pendingHeight, pendingSpacing) / speed * 1000));
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
