const RTSPlaylistList = {};

RTSPlaylistList.panel = document.getElementById('playlist-list');
RTSPlaylistList.timer = null;
RTSPlaylistList.scrollTimer = null;
RTSPlaylistList.scrollInterval = null;

RTSPlaylistList.clearTimers = () => {
  clearTimeout(RTSPlaylistList.timer);
  clearTimeout(RTSPlaylistList.scrollTimer);
  clearInterval(RTSPlaylistList.scrollInterval);
  RTSPlaylistList.timer = null;
  RTSPlaylistList.scrollTimer = null;
  RTSPlaylistList.scrollInterval = null;
};

RTSPlaylistList.show = command => {
  const panel = RTSPlaylistList.panel;
  const text = String(command?.replayPlaylist || '').trim();
  if (!panel || !text) return;
  const entries = text.split(' | ').filter(Boolean).map(entry => {
    const match = entry.match(/^#(\d+)\s+(.*?)\s+—\s+(.*)$/);
    return match ? { number: match[1], title: match[2], requester: match[3] } : { number: '', title: entry, requester: '' };
  });

  panel.dataset.rtsInformationPanel = 'playlist';
  panel.innerHTML = '<div class="rts-panel-header"><span class="rts-panel-kicker">ACTION REPLAY</span><strong>PLAYLIST</strong></div><div class="rts-panel-list"></div>';
  const list = panel.querySelector('.rts-panel-list');
  entries.forEach(entry => {
    const row = document.createElement('div');
    row.className = 'rts-panel-entry';
    const number = document.createElement('span'); number.className = 'rts-panel-number'; number.textContent = entry.number;
    const content = document.createElement('span'); content.className = 'rts-playlist-content';
    const title = document.createElement('strong'); title.className = 'rts-panel-title'; title.textContent = entry.title || 'Untitled replay';
    const requester = document.createElement('small'); requester.className = 'rts-playlist-requester'; requester.textContent = entry.requester || 'Created automatically';
    content.append(title, requester); row.append(number, content); list.appendChild(row);
  });

  RTSPlaylistList.clearTimers();
  panel.classList.remove('show'); panel.setAttribute('aria-hidden', 'true');
  list.scrollTop = 0; void panel.offsetWidth;
  const panelPosition = command?.replayPanelPosition || 'Centered';
  const animationCommand = { ...command, replayPanelPreset: command?.replayPanelPreset || 'Broadcast', replayPanelAnimation: command?.replayPanelAnimation || JSON.stringify({ id: 'playlist', name: 'Playlist', start: [{ position: 'Hidden Left', duration: 0, delay: 0, easing: 'ease-in-out' }, { position: panelPosition, duration: 600, delay: 0, easing: 'ease-out' }], end: [{ position: panelPosition, duration: 0, delay: 0, easing: 'ease-in-out' }, { position: 'Hidden Left', duration: 600, delay: 0, easing: 'ease-in' }] }) };
  RTSInformationPanels.show(panel, animationCommand, panelPosition);

  const scrollable = list.scrollHeight > list.clientHeight;
  const hide = () => RTSInformationPanels.hide(panel, animationCommand);
  if (!scrollable) { RTSPlaylistList.timer = setTimeout(hide, 10000); return; }
  RTSPlaylistList.scrollTimer = setTimeout(() => {
    RTSPlaylistList.scrollInterval = setInterval(() => {
      if (list.scrollTop + list.clientHeight >= list.scrollHeight - 1) {
        RTSPlaylistList.clearTimers(); RTSPlaylistList.timer = setTimeout(hide, 3000); return;
      }
      list.scrollTop += 1;
    }, 35);
  }, 3000);
};

RTSPlaylistList.handle = command => {
  if (command?.replayCommand === 'playlist-panel' || (command?.replayCommand === 'message' && command?.replayPlaylist)) { RTSPlaylistList.show(command); return true; }
  return false;
};

window.RTSPlaylistList = RTSPlaylistList;
