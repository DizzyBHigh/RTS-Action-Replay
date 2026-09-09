const RTSReplayCut = window.RTSReplay;
const cutTitle = document.getElementById('player-title');

RTSReplayCut.stopCutBar = () => {
  if (RTSReplayCut.cutSpawnTimer) clearTimeout(RTSReplayCut.cutSpawnTimer);
  RTSReplayCut.cutSpawnTimer = null;
  cutTitle?.querySelector('.cut-bar-track')?.remove();
};

RTSReplayCut.startCutBar = () => {
  if (!cutTitle) return;
  RTSReplayCut.stopCutBar();
  const track = document.createElement('div');
  track.className = 'cut-bar-track';
  cutTitle.appendChild(track);
  RTSReplayCut.cutBar = track;

  const colour = value => {
    const raw = String(value || '').trim();
    const match = raw.match(/^#([0-9a-f]{6}|[0-9a-f]{8})$/i);
    return match ? `#${match[1].slice(0, 6)}` : raw;
  };
  const styles = getComputedStyle(cutTitle);
  const primary = colour(styles.getPropertyValue('--title-primary')) || '#0384CB';
  const secondary = colour(styles.getPropertyValue('--title-secondary')) || '#FFD400';
  const speed = 90;

  const spawn = () => {
    if (!cutTitle.classList.contains('title-cut') || !cutTitle.classList.contains('visible') || track !== RTSReplayCut.cutBar) {
      RTSReplayCut.stopCutBar();
      return;
    }

    const width = 80 + Math.random() * 220;
    const gap = 20 + Math.random() * 60;
    const barWidth = cutTitle.clientWidth;
    const block = document.createElement('span');
    block.className = 'cut-bar-block';
    block.style.width = `${width.toFixed(1)}px`;
    block.style.backgroundColor = Math.random() < 0.5 ? primary : secondary;
    block.style.left = `${barWidth + gap}px`;
    track.appendChild(block);

    const distance = barWidth + gap + width + gap;
    const animation = block.animate(
      [{ transform: 'translate3d(0,0,0)' }, { transform: `translate3d(-${distance}px,0,0)` }],
      { duration: (distance / speed) * 1000, easing: 'linear', fill: 'forwards' }
    );
    animation.onfinish = () => block.remove();

    RTSReplayCut.cutSpawnTimer = setTimeout(spawn, ((width + gap) / speed) * 1000);
  };

  spawn();
};

RTSReplayCut.observeCutBar = () => {
  if (!cutTitle) return;
  const observer = new MutationObserver(() => {
    if (cutTitle.classList.contains('title-cut') && cutTitle.classList.contains('visible')) RTSReplayCut.startCutBar();
    else RTSReplayCut.stopCutBar();
  });
  observer.observe(cutTitle, { attributes: true, attributeFilter: ['class'] });
};

RTSReplayCut.observeCutBar();
