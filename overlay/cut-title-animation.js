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
  const command = RTSReplayCut.command || {};
  const primary = colour(command.replayTitlePrimaryColor) || '#0384CB';
  const secondary = colour(command.replayTitleSecondaryColor) || '#FFD400';
  const speed = 115;
  const spawn = () => {
    if (!cutTitle.classList.contains('title-cut') || !cutTitle.classList.contains('visible') || track !== RTSReplayCut.cutBar) {
      RTSReplayCut.stopCutBar();
      return;
    }
    const width = 70 + Math.random() * 230;
    const gap = 18 + Math.random() * 55;
    const block = document.createElement('span');
    block.className = 'cut-bar-block';
    block.style.width = `${width.toFixed(1)}px`;
    block.style.backgroundColor = Math.random() < 0.5 ? primary : secondary;
    block.style.left = '100%';
    track.appendChild(block);

    const barWidth = cutTitle.clientWidth;
    const start = gap;
    const end = -(barWidth + width + gap);
    const distance = start - end;
    const animation = block.animate(
      [{ transform: `translate3d(${start}px,0,0)` }, { transform: `translate3d(${end}px,0,0)` }],
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
