const RTSReplayCut = window.RTSReplay;
const cutTitle = document.getElementById('player-title');

RTSReplayCut.startCutBar = () => {
  if (!cutTitle) return;
  RTSReplayCut.stopCutBar();
  const update = () => {
    if (!cutTitle.classList.contains('title-cut') || !cutTitle.classList.contains('visible')) return;
    const primary = getComputedStyle(cutTitle).getPropertyValue('--title-primary').trim() || '#0384CB';
    const secondary = getComputedStyle(cutTitle).getPropertyValue('--title-secondary').trim() || '#101416';
    const stops = [];
    let position = 0;
    while (position < 100) {
      const width = 6 + Math.random() * 16;
      const end = Math.min(100, position + width);
      const roll = Math.random();
      const color = roll < 0.5 ? primary : roll < 0.92 ? secondary : 'transparent';
      stops.push(`${color} ${position.toFixed(1)}% ${end.toFixed(1)}%`);
      position = end;
    }
    cutTitle.style.setProperty('--cut-bar-pattern', `linear-gradient(90deg,${stops.join(',')})`);
    RTSReplayCut.cutBarTimer = setTimeout(update, 2200 + Math.random() * 1800);
  };
  update();
};

RTSReplayCut.stopCutBar = () => {
  if (RTSReplayCut.cutBarTimer) clearTimeout(RTSReplayCut.cutBarTimer);
  RTSReplayCut.cutBarTimer = null;
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
