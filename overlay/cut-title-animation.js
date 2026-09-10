const RTSReplayCut = window.RTSReplay;
const cutTitle = document.getElementById('player-title');

RTSReplayCut.stopCutBar = () => {
  if (RTSReplayCut.cutSpawnTimer) clearTimeout(RTSReplayCut.cutSpawnTimer);
  RTSReplayCut.cutSpawnTimer = null;
  cutTitle?.querySelectorAll('.cut-bar-track').forEach(track => track.remove());
};

RTSReplayCut.startCutBar = () => {
  if (!cutTitle) return;
  RTSReplayCut.stopCutBar();

  const bottomTrack = document.createElement('div');
  bottomTrack.className = 'cut-bar-track cut-bar-track-bottom';
  cutTitle.append(bottomTrack);
  RTSReplayCut.cutBar = bottomTrack;

  const colour = value => {
    const raw = String(value || '').trim();
    const match = raw.match(/^#([0-9a-f]{6}|[0-9a-f]{8})$/i);
    return match ? `#${match[1].slice(0, 6)}` : raw;
  };
  const styles = getComputedStyle(cutTitle);
  const primary = colour(styles.getPropertyValue('--title-primary')) || '#0384CB';
  const secondary = colour(styles.getPropertyValue('--title-secondary')) || '#FFD400';
  const speed = 90;
  const barWidth = cutTitle.clientWidth;

  const createBlock = (left, width, colourValue) => {
    const block = document.createElement('span');
    block.className = 'cut-bar-block';
    block.style.width = `${width.toFixed(1)}px`;
    block.style.backgroundColor = colourValue;
    block.style.left = `${left.toFixed(1)}px`;
    bottomTrack.appendChild(block);
    return block;
  };

  // Build a contiguous stream of primary/secondary blocks. Blocks deliberately
  // touch edge-to-edge so the strip is never interrupted by blank gaps.
  let seedLeft = -40;
  while (seedLeft < barWidth) {
    const width = 70 + Math.random() * 170;
    const colourValue = Math.random() < 0.5 ? primary : secondary;
    createBlock(seedLeft, width, colourValue);
    seedLeft += width;
  }

  [...bottomTrack.children].forEach(block => {
    const left = parseFloat(block.style.left);
    const width = parseFloat(block.style.width);
    const distance = barWidth + 80 + Math.max(0, left) + width;
    const animation = block.animate(
      [{ transform: 'translate3d(0,0,0)' }, { transform: `translate3d(-${distance}px,0,0)` }],
      { duration: (distance / speed) * 1000, easing: 'linear', fill: 'forwards' }
    );
    animation.onfinish = () => block.remove();
  });

  const spawn = () => {
    if (!cutTitle.classList.contains('title-cut') || !cutTitle.classList.contains('visible') || bottomTrack !== RTSReplayCut.cutBar) {
      RTSReplayCut.stopCutBar();
      return;
    }

    const width = 80 + Math.random() * 220;
    // Start the next block exactly at the right edge. Its spawn cadence is
    // based on its own width, so it follows the preceding block edge-to-edge.
    const left = barWidth;
    const distance = barWidth + width;
    const duration = (distance / speed) * 1000;
    const colourValue = Math.random() < 0.5 ? primary : secondary;
    const block = createBlock(left, width, colourValue);
    const animation = block.animate(
      [{ transform: 'translate3d(0,0,0)' }, { transform: `translate3d(-${distance}px,0,0)` }],
      { duration, easing: 'linear', fill: 'forwards' }
    );
    animation.onfinish = () => block.remove();

    RTSReplayCut.cutSpawnTimer = setTimeout(spawn, (width / speed) * 1000);
  };

  RTSReplayCut.cutSpawnTimer = setTimeout(spawn, 300);
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
