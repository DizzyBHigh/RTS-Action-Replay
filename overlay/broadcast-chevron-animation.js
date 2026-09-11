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

  const styles = getComputedStyle(broadcastTitle);
  const primary = colour(styles.getPropertyValue('--title-primary')) || '#0384CB';
  const secondary = colour(styles.getPropertyValue('--title-secondary')) || '#FFD400';
  const speed = 95;
  const pitch = 72;
  const seedLeft = -90;
  const trackWidth = track.clientWidth;

  const createChevron = (left, index) => {
    const mover = document.createElement('span');
    mover.className = 'broadcast-chevron-mover';
    mover.style.left = `${left}px`;

    const chevron = document.createElement('span');
    chevron.className = 'broadcast-chevron';
    chevron.style.setProperty('--chevron-color', index % 2 ? secondary : primary);
    mover.append(chevron);
    track.append(mover);
    return mover;
  };

  let index = 0;
  for (let left = seedLeft; left < trackWidth; left += pitch) {
    const mover = createChevron(left, index++);
    const distance = trackWidth - left + 60;
    const animation = mover.animate(
      [{ transform: 'translate3d(0,0,0)' }, { transform: `translate3d(${distance}px,0,0)` }],
      { duration: (distance / speed) * 1000, easing: 'linear', fill: 'forwards' }
    );
    animation.onfinish = () => mover.remove();
  }

  const spawn = () => {
    if (!broadcastTitle.classList.contains('title-broadcast') || !broadcastTitle.classList.contains('visible') || track !== RTSReplayBroadcast.broadcastChevronTrack) {
      RTSReplayBroadcast.stopBroadcastChevrons();
      return;
    }

    const mover = createChevron(seedLeft, index++);
    const distance = trackWidth - seedLeft + 60;
    const animation = mover.animate(
      [{ transform: 'translate3d(0,0,0)' }, { transform: `translate3d(${distance}px,0,0)` }],
      { duration: (distance / speed) * 1000, easing: 'linear', fill: 'forwards' }
    );
    animation.onfinish = () => mover.remove();
    RTSReplayBroadcast.broadcastChevronSpawnTimer = setTimeout(spawn, (pitch / speed) * 1000);
  };

  RTSReplayBroadcast.broadcastChevronSpawnTimer = setTimeout(spawn, (pitch / speed) * 1000);
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
