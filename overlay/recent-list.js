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

  recentListLog('recent list panel found', { textLength: text.length });
  panel.innerHTML = '<div id="recent-list-title">RECENT REPLAYS</div>';
  const entries = text.split(' | ').filter(Boolean);
  entries.forEach(entry => {
    const row = document.createElement('div');
    row.className = 'recent-list-entry';
    row.textContent = entry;
    panel.appendChild(row);
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
