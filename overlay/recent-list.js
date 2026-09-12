const RTSRecentList = window.RTSReplay;

RTSRecentList.showRecentList = command => {
  const text = String(command?.replayRecent || '').trim();
  if (!text) return;
  const panel = RTSRecentList.recentList;
  if (!panel) return;

  const entries = text.split(' | ').filter(Boolean);
  panel.innerHTML = '';
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
  RTSRecentList.recentListTimer = setTimeout(() => {
    panel.classList.remove('show');
    panel.setAttribute('aria-hidden', 'true');
  }, 10000);
};
