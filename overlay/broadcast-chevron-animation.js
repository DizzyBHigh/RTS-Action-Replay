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
  const widthSetting = number(RTSReplayBroadcast.command?.replayBroadcastChevronWidth, 1, 200, 42);
  const spacingSetting = number(RTSReplayBroadcast.command?.replayBroadcastChevronSpacing, 0, 200, 13);
  const randomWidth = RTSReplayBroadcast.command?.replayBroadcastRandomWidth === true;
  const randomSpacing = RTSReplayBroadcast.command?.replayBroadcastRandomSpacing === true;
  const speed = 95;
  const seedLeft = -90;
  const trackWidth = track.clientWidth;

  const getWidth = () => randomWidth ? randomValue(widthSetting, 1) : widthSetting;
  const getSpacing = () => randomSpacing ? randomValue(spacingSetting, 0) : spacingSetting;
  const visualWidth = width => width * Math.SQRT2;
  const pitch = (currentWidth, nextWidth, spacing) =>
    currentWidth * (1 + 1 / Math.SQRT2) + nextWidth * (1 / Math.SQRT2 - 0.5) + spacing;

  const createChevron = (left, width, index) => {
    const mover = document.createElement('span');
    mover.className = 'broadcast-chevron-mover';
    mover.style.left = `${left.toFixed(2)}px`;

    const chevron = document.createElement('span');
    chevron.className = 'broadcast-chevron';
    chevron.style.setProperty('--chevron-size', `${width.toFixed(2)}px`);
    chevron.style.setProperty('--chevron-color', index % 2 ? secondary : primary);
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
  let nextWidth = getWidth();
  let spacing = getSpacing();
  while (left < trackWidth) {
    createChevron(left, width, index);
    animate(track.lastElementChild, left);
    index++;
    left += pitch(width, nextWidth, spacing);
    width = nextWidth;
    nextWidth = getWidth();
    spacing = getSpacing();
  }

  let pendingWidth = width;
  let pendingSpacing = spacing;

  const spawn = () => {
    if (!broadcastTitle.classList.contains('title-broadcast') || !broadcastTitle.classList.contains('visible') || track !== RTSReplayBroadcast.broadcastChevronTrack) {
      RTSReplayBroadcast.stopBroadcastChevrons();
      return;
    }

    const currentWidth = pendingWidth;
    const nextWidth = getWidth();
    const spacing = pendingSpacing;
    const delay = (pitch(currentWidth, nextWidth, spacing) / speed) * 1000;
    const mover = createChevron(seedLeft, currentWidth, index++);
    animate(mover, seedLeft);

    pendingWidth = nextWidth;
    pendingSpacing = getSpacing();
    RTSReplayBroadcast.broadcastChevronSpawnTimer = setTimeout(spawn, delay);
  };

  RTSReplayBroadcast.broadcastChevronSpawnTimer = setTimeout(spawn, (pitch(pendingWidth, getWidth(), pendingSpacing) / speed) * 1000);
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
