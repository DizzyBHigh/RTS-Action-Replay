const RTSReplayCut = window.RTSReplay;
const cutTitle = document.getElementById('player-title');

RTSReplayCut.startCutBar = () => {
  if (!cutTitle) return;
  RTSReplayCut.stopCutBar();
  const primary = getComputedStyle(cutTitle).getPropertyValue('--title-primary').trim() || '#0384CB';
  const secondary = getComputedStyle(cutTitle).getPropertyValue('--title-secondary').trim() || '#101416';
  const track = document.createElement('div');
  track.className = 'cut-bar-track';
  const blocks = [];
  let total = 0;
  while (total < 100) {
    const width = Math.min(12, 4 + Math.random() * 7, 100 - total);
    const color = Math.random() < 0.5 ? primary : secondary;
    const alt = color === primary ? secondary : primary;
    const block = document.createElement('span');
    block.className = 'cut-bar-block';
    block.style.width = `${width}%`;
    block.style.setProperty('--block-color', color);
    block.style.setProperty('--block-alt', alt);
    block.style.setProperty('--block-duration', `${2.8 + Math.random() * 3.5}s`);
    block.style.setProperty('--block-delay', `${-(Math.random() * 4.5).toFixed(2)}s`);
    blocks.push(block);
    total += width;
  }
  blocks.forEach(block => track.appendChild(block));
  blocks.forEach(block => track.appendChild(block.cloneNode(true)));
  cutTitle.querySelector('.cut-bar-track')?.remove();
  cutTitle.appendChild(track);
};

RTSReplayCut.stopCutBar = () => {
  cutTitle?.querySelector('.cut-bar-track')?.remove();
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
