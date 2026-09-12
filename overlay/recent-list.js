const RTSRecentList = window.RTSReplay;

const recentListLog = (message, details) => window.RTSDevToolbar?.log?.(message, details);

RTSRecentList.showRecentList = command => {
  const text = String(command?.replayRecent || '').trim();
  const panel = RTSRecentList.recentList;
  if (!panel || !text) return;

  let entries = [];
  try { entries = JSON.parse(String(command?.replayRecentData || '[]')); } catch { entries = []; }
  if (!Array.isArray(entries) || entries.length === 0) {
    entries = text.split(' | ').filter(Boolean).map(entry => {
      const match = entry.match(/^#(\d+)\s+(.*?)\s+—\s+(.*)$/);
      return match ? { number: match[1], title: match[2], requester: match[3], avatarUrl: '' } : { number: '', title: entry, requester: '', avatarUrl: '' };
    });
  }

  panel.dataset.rtsInformationPanel = 'recent';
  panel.innerHTML = '<div class="rts-panel-header"><span class="rts-panel-kicker">ACTION REPLAY</span><strong>RECENT REPLAYS</strong></div><div class="rts-panel-list"></div>';
  const list = panel.querySelector('.rts-panel-list');

  entries.forEach(entry => {
    const row = document.createElement('div');
    row.className = 'rts-panel-entry';
    const number = document.createElement('span');
    number.className = 'rts-panel-number';
    number.textContent = entry.number ?? '';
    const avatar = document.createElement('img');
    avatar.className = 'rts-panel-avatar';
    avatar.alt = '';
    avatar.loading = 'lazy';
    avatar.src = String(entry.avatarUrl || '');
    avatar.onerror = () => avatar.classList.add('missing');
    const title = document.createElement('span');
    title.className = 'rts-panel-title';
    title.textContent = String(entry.title || 'Untitled replay');
    row.append(number, avatar, title);
    list.appendChild(row);
  });

  clearTimeout(RTSRecentList.recentListTimer);
  clearTimeout(RTSRecentList.recentListScrollTimer);
  clearInterval(RTSRecentList.recentListScrollInterval);
  panel.classList.remove('show');
  panel.setAttribute('aria-hidden', 'true');
  list.scrollTop = 0;
  void panel.offsetWidth;

  const panelPosition = command?.replayPanelPosition || command?.replayRecentPosition || 'Center';
  const animationCommand = { ...command, replayPanelAnimation: command?.replayPanelAnimation || JSON.stringify({
    id: 'default-panel',
    name: 'Default',
    start: [
      { position: 'Hidden Left', duration: 0, delay: 0, easing: 'ease-in-out' },
      { position: panelPosition, duration: 600, delay: 0, easing: 'ease-out' }
    ],
    end: [
      { position: panelPosition, duration: 0, delay: 0, easing: 'ease-in-out' },
      { position: 'Hidden Left', duration: 600, delay: 0, easing: 'ease-in' }
    ]
  }) };
  panel._rtsPanelAnimationCommand = animationCommand;
  RTSInformationPanels.show(panel, animationCommand, panelPosition);

  const startedAt = Date.now();
  const scrollable = list.scrollHeight > list.clientHeight;
  let scrollFinished = !scrollable;

  const scheduleHide = delay => {
    clearTimeout(RTSRecentList.recentListTimer);
    RTSRecentList.recentListTimer = setTimeout(() => {
      RTSInformationPanels.hide(panel, panel._rtsPanelAnimationCommand || animationCommand);
    }, delay);
  };

  const finishScrolling = () => {
    if (scrollFinished) return;
    scrollFinished = true;
    clearInterval(RTSRecentList.recentListScrollInterval);
    RTSRecentList.recentListScrollInterval = null;
    const elapsed = Date.now() - startedAt;
    if (elapsed >= 10000) scheduleHide(3000);
  };

  const startAutoScroll = () => {
    if (!scrollable) return;
    RTSRecentList.recentListScrollInterval = setInterval(() => {
      if (list.scrollTop + list.clientHeight >= list.scrollHeight - 1) {
        finishScrolling();
        return;
      }
      list.scrollTop += 1;
    }, 35);
  };

  RTSRecentList.recentListScrollTimer = setTimeout(startAutoScroll, 3000);
  recentListLog('recent list rendered', { entries: entries.length, scrollable, panelPosition });

  RTSRecentList.recentListTimer = setTimeout(() => {
    if (scrollFinished) RTSInformationPanels.hide(panel, panel._rtsPanelAnimationCommand || animationCommand);
  }, 10000);
};
