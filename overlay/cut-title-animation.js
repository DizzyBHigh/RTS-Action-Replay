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

  const spawn = () => {
    if (!cutTitle.classList.contains('title-cut') || !cutTitle.classList.contains('visible')) {
      RTSReplayCut.stopCutBar();
      return;
    }
    const styles = getComputedStyle(cutTitle);
    const primary = styles.getPropertyValue('--title-primary').trim() || '#0384CB';
    const secondary = styles.getPropertyValue('--title-secondary').trim() || '#101416';
    const width = 70 + Math.random() * 230;
    const gap = 12 + Math.random() * 65;
    const speed = 65 + Math.random() * 15;
    const startX = cutTitle.clientWidth + gap;
    const block = document.createElement('span');
    block.className = 'cut-bar-block';
    block.style.width = `${width.toFixed(1)}px`;
    block.style.backgroundColor = Math.random() < 0.5 ? primary : secondary;
    track.appendChild(block);
    const distance = startX + width + 50;
    const duration = (distance / speed) * 1000;
    const animation = block.animate(
      [{ transform: `translate3d(${startX}px,0,0)` }, { transform: `translate3d(${-width - 50}px,0,0)` }],
      { duration, easing: 'linear', fill: 'forwards' }
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
