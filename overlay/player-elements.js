const RTSReplayElements = window.RTSReplay;

RTSReplayElements.clearTitleTimer = () => {
  if (RTSReplayElements.titleTimer) clearTimeout(RTSReplayElements.titleTimer);
  RTSReplayElements.titleTimer = null;
};

RTSReplayElements.hideTitle = () => {
  RTSReplayElements.clearTitleTimer();
  const title = RTSReplayElements.title;
  if (!title || !title.classList.contains('visible')) return;
  title.classList.remove('title-enter');
  title.classList.add('title-exit');
  title.addEventListener('animationend', () => {
    title.classList.remove('visible', 'title-exit');
  }, { once:true });
};

RTSReplayElements.showTitle = command => {
  const title = RTSReplayElements.title;
  if (!title || command.replayShowTitle === false) return;
  const text = String(command.replayTitle || '').trim();
  if (!text) return;

  RTSReplayElements.hideTitle();
  const style = String(command.replayTitleStyle || 'Broadcast').toLowerCase();
  const position = String(command.replayTitlePosition || 'Bottom').toLowerCase();
  const className = ['broadcast','cinematic','cut','minimal'].includes(style) ? style : 'broadcast';

  title.className = `title-${className} title-${position === 'top' ? 'top' : 'bottom'}`;
  title.textContent = text;
  title.classList.add('visible', 'title-enter');

  const displayDuration = Math.max(0, Number(command.replayTitleDuration) || 0);
  if (displayDuration > 0) {
    RTSReplayElements.titleTimer = setTimeout(() => RTSReplayElements.hideTitle(), displayDuration * 1000);
  }
};

RTSReplayElements.configure = command => {
  RTSReplayElements.command = command;
  if (RTSReplayElements.speed) RTSReplayElements.speed.textContent = `${Number(command.replayPlaybackSpeed || 1).toFixed(2).replace(/\.00$/, '')}×`;
  if (command.replayTitle) RTSReplayElements.showTitle(command);
};

RTSReplayElements.title = document.getElementById('player-title');
RTSReplayElements.speed = document.getElementById('player-speed');
