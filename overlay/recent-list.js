const RTSRecentList = window.RTSReplay;

RTSRecentList.showRecentList = command => {
  const text = String(command?.replayRecent || '').trim();
  if (!text) return;

  const entries = text.split(' | ').filter(Boolean);
  RTSRecentList.recentList.innerHTML = '';
  entries.forEach(entry => {
    const row = document.createElement('div');
    row.className = 'recent-list-entry';
    row.textContent = entry;
    RTSRecentList.recentList.appendChild(row);
  });

  clearTimeout(RTSRecentList.recentListTimer);
  RTSRecentList.recentList.classList.remove('show');
  RTSRecentList.recentList.setAttribute('aria-hidden', 'true');
  void RTSRecentList.recentList.offsetWidth;
  RTSRecentList.recentList.classList.add('show');
  RTSRecentList.recentList.setAttribute('aria-hidden', 'false');
  RTSRecentList.recentListTimer = setTimeout(() => {
    RTSRecentList.recentList.classList.remove('show');
    RTSRecentList.recentList.setAttribute('aria-hidden', 'true');
  }, 10000);
};

RTSRecentList.handleRecentListCommand = command => {
  if (command?.replayCommand === 'recent-list') RTSRecentList.showRecentList(command);
};
