const RTSRecentList = window.RTSReplay;

const recentListLog = (message, details) => window.RTSDevToolbar?.log?.(message, details);

RTSRecentList.showRecentList = command => {
  recentListLog('recent list handler entered', {
    hasCommand: !!command,
    hasText: !!String(command?.replayRecent || '').trim(),
    hasPanel: !!RTSRecentList.recentList
  });

  const text = String(command?.replayRecent || '').trim();
  const panel = RTSRecentList.recentList;
  if (!panel) {
    recentListLog('recent list render skipped: panel missing');
    return;
  }
  if (!text) {
    recentListLog('recent list render skipped: replayRecent empty');
    return;
  }

  const entries = text.split(' | ').filter(Boolean);
  recentListLog('recent list panel found', { textLength: text.length, entries: entries.length });
  panel.innerHTML = '<div class="rts-panel-header"><span class="rts-panel-kicker">ACTION REPLAY</span><strong>RECENT REPLAYS</strong></div><div class="rts-panel-list"></div>';
  const list = panel.querySelector('.rts-panel-list');

  entries.forEach(entry => {
    const match = entry.match(/^#(\d+)\s+(.*?)\s+—\s+(.*)$/);
    const row = document.createElement('div');
    row.className = 'rts-panel-entry';
    if (match) {
      row.innerHTML = `<span class="rts-panel-number">${match[1]}</span><span class="rts-panel-title"></span><span class="rts-panel-requester"></span>`;
      row.querySelector('.rts-panel-title').textContent = match[2];
      row.querySelector('.rts-panel-requester').textContent = match[3];
    } else {
      row.textContent = entry;
    }
    list.appendChild(row);
  });

  clearTimeout(RTSRecentList.recentListTimer);
  panel.classList.remove('show');
  panel.setAttribute('aria-hidden', 'true');
  void panel.offsetWidth;
  panel.classList.add('show');
  panel.setAttribute('aria-hidden', 'false');
  recentListLog('recent list rendered', {
    entries: entries.length,
    showClass: panel.classList.contains('show'),
    display: getComputedStyle(panel).display,
    visibility: getComputedStyle(panel).visibility,
    opacity: getComputedStyle(panel).opacity
  });

  RTSRecentList.recentListTimer = setTimeout(() => {
    panel.classList.remove('show');
    panel.setAttribute('aria-hidden', 'true');
  }, 10000);
};
