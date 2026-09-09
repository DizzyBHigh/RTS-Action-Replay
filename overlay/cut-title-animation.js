const RTSReplayCut = window.RTSReplay;
const cutTitle = document.getElementById('player-title');

RTSReplayCut.startCutBar = () => {
  if (!cutTitle) return;
  RTSReplayCut.stopCutBar();
  const buildPattern = () => {
    const primary = getComputedStyle(cutTitle).getPropertyValue('--title-primary').trim() || '#0384CB';
    const secondary = getComputedStyle(cutTitle).getPropertyValue('--title-secondary').trim() || '#101416';
    const stops = [];
    let position = 0;
    while (position < 100) {
      const width = 7 + Math.random() * 13;
      const end = Math.min(100, position + width);
      const color = Math.random() < 0.48 ? primary : secondary;
      stops.push(`${color} ${position.toFixed(1)}% ${end.toFixed(1)}%`);
      position = end;
    }
    return `linear-gradient(90deg,${stops.join(',')})`;
  };
  const update = () => {
    if (!cutTitle.classList.contains('title-cut') || !cutTitle.classList.contains('visible')) return;
    cutTitle.style.setProperty('--cut-bar-pattern', buildPattern());
    RTSReplayCut.cutBarTimer = setTimeout(update, 3200 + Math.random() * 2200);
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
