const RTSInformationPanelPresets = window.RTSInformationPanelPresets || {};

RTSInformationPanelPresets.toCssColor = value => {
  const raw = String(value || '').trim();
  return /^#[0-9a-f]{6,8}$/i.test(raw) ? `#${raw.slice(1, 7)}` : raw;
};

RTSInformationPanelPresets.loadFont = font => {
  const name = String(font || 'Inter').trim();
  if (!name) return;
  const id = `rts-panel-font-${name.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`;
  if (document.getElementById(id)) return;
  const link = document.createElement('link');
  link.id = id;
  link.rel = 'stylesheet';
  link.href = `https://fonts.googleapis.com/css2?family=${encodeURIComponent(name).replace(/%20/g, '+')}:wght@400;500;600;700;800&display=swap`;
  document.head.appendChild(link);
};

RTSInformationPanelPresets.stopCutBlocks = panel => {
  panel?.querySelector('.panel-cut-bar-track')?.remove();
};

RTSInformationPanelPresets.startCutBlocks = (panel, command) => {
  if (!panel?.classList.contains('panel-cut')) return;
  const header = panel.querySelector('.rts-panel-header');
  if (!header) return;
  RTSInformationPanelPresets.stopCutBlocks(panel);

  const colour = value => {
    const raw = String(value || '').trim();
    const match = raw.match(/^#([0-9a-f]{6}|[0-9a-f]{8})$/i);
    return match ? `#${match[1].slice(0, 6)}` : raw;
  };
  const number = (value, min, max, fallback) => Math.max(min, Math.min(max, Number(value) || fallback));
  const randomValue = max => 1 + Math.random() * Math.max(0, max - 1);
  const primary = colour(getComputedStyle(panel).getPropertyValue('--panel-primary')) || '#0384CB';
  const secondary = colour(getComputedStyle(panel).getPropertyValue('--panel-secondary')) || '#101416';
  const blockWidth = number(command?.replayCutBlockWidth, 1, 1000, 170);
  const randomWidth = command?.replayCutRandomWidth === undefined ? true : command.replayCutRandomWidth === true;
  const barHeight = number(command?.replayCutBarHeight, 1, 50, 5);
  const speed = 90;
  const barWidth = panel.clientWidth;
  if (barWidth <= 0) return;

  panel.style.setProperty('--panel-cut-bar-height', `${barHeight}px`);
  const track = document.createElement('div');
  track.className = 'panel-cut-bar-track';
  header.appendChild(track);

  const getWidth = () => randomWidth ? randomValue(blockWidth) : blockWidth;
  const createBlock = (left, width, colourValue) => {
    const block = document.createElement('span');
    block.className = 'panel-cut-bar-block';
    block.style.width = `${(width + 2).toFixed(1)}px`;
    block.style.backgroundColor = colourValue;
    block.style.left = `${left.toFixed(1)}px`;
    track.appendChild(block);
    return block;
  };

  let seedLeft = -40;
  while (seedLeft < barWidth) {
    const width = Math.min(getWidth(), barWidth - seedLeft);
    createBlock(seedLeft, width, Math.random() < 0.5 ? primary : secondary);
    seedLeft += width;
  }

  [...track.children].forEach(block => {
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
    if (!panel.classList.contains('show') || !panel.classList.contains('panel-cut') || track !== panel.querySelector('.panel-cut-bar-track')) return;
    const width = getWidth();
    const left = barWidth;
    const distance = barWidth + width;
    const block = createBlock(left, width, Math.random() < 0.5 ? primary : secondary);
    const animation = block.animate(
      [{ transform: 'translate3d(0,0,0)' }, { transform: `translate3d(-${distance}px,0,0)` }],
      { duration: (distance / speed) * 1000, easing: 'linear', fill: 'forwards' }
    );
    animation.onfinish = () => block.remove();
    panel._rtsPanelCutTimer = setTimeout(spawn, (width / speed) * 1000);
  };
  panel._rtsPanelCutTimer = setTimeout(spawn, 0);
};

RTSInformationPanelPresets.apply = (panel, command) => {
  if (!panel) return;
  const preset = String(command?.replayPanelPreset || 'Broadcast').toLowerCase();
  const primary = RTSInformationPanelPresets.toCssColor(command?.replayPanelPrimaryColor || '#0384CBFF');
  const secondary = RTSInformationPanelPresets.toCssColor(command?.replayPanelSecondaryColor || '#101416FF');
  const titleFont = String(command?.replayPanelTitleFont || 'Inter').trim();
  const titleSize = Math.max(12, Number(command?.replayPanelTitleSize) || 24);
  const titleColor = RTSInformationPanelPresets.toCssColor(command?.replayPanelTitleColor || '#FFFFFFFF');
  const listSize = Math.max(8, Number(command?.replayPanelListSize) || 15);
  const listColor = RTSInformationPanelPresets.toCssColor(command?.replayPanelListColor || '#FFFFFFFF');
  const className = ['broadcast', 'cinematic', 'cut', 'minimal'].includes(preset) ? preset : 'broadcast';
  panel.classList.remove('panel-broadcast', 'panel-cinematic', 'panel-cut', 'panel-minimal');
  panel.classList.add(`panel-${className}`);
  panel.style.setProperty('--panel-primary', primary);
  panel.style.setProperty('--panel-secondary', secondary);
  panel.style.setProperty('--panel-title-font', `'${titleFont.replace(/'/g, '')}', system-ui, sans-serif`);
  panel.style.setProperty('--panel-title-size', `${titleSize}px`);
  panel.style.setProperty('--panel-title-color', titleColor);
  panel.style.setProperty('--panel-list-size', `${listSize}px`);
  panel.style.setProperty('--panel-list-color', listColor);
  RTSInformationPanelPresets.loadFont(titleFont);
};

const originalShow = window.RTSInformationPanelAnimation?.show;
const originalHide = window.RTSInformationPanelAnimation?.hide;
if (originalShow) {
  window.RTSInformationPanelAnimation.show = (panel, command, name) => {
    RTSInformationPanelPresets.apply(panel, command);
    const result = originalShow(panel, command, name);
    requestAnimationFrame(() => RTSInformationPanelPresets.startCutBlocks(panel, command));
    return result;
  };
}
if (originalHide) {
  window.RTSInformationPanelAnimation.hide = (panel, command) => {
    clearTimeout(panel?._rtsPanelCutTimer);
    RTSInformationPanelPresets.stopCutBlocks(panel);
    return originalHide(panel, command);
  };
}

window.RTSInformationPanelPresets = RTSInformationPanelPresets;
