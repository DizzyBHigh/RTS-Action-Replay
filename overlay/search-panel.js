const RTSSearchPanel = window.RTSSearchPanel || {};

RTSSearchPanel.panel = document.getElementById('search-panel');
RTSSearchPanel.timer = null;
RTSSearchPanel.endTimer = null;
RTSSearchPanel.scrollTimer = null;
RTSSearchPanel.scrollInterval = null;

RTSSearchPanel.notifyEnded = requestId => {
  if (!RTSReplay.socket || RTSReplay.socket.readyState !== WebSocket.OPEN) return;
  RTSReplay.socket.send(JSON.stringify({
    request: 'DoAction', id: `rts-search-ended-${Date.now()}`,
    action: { name: RTSReplay.config.searchEndedAction },
    args: { replaySearchRequestId: requestId || '' }
  }));
};

RTSSearchPanel.clearTimers = () => {
  clearTimeout(RTSSearchPanel.timer);
  clearTimeout(RTSSearchPanel.endTimer);
  clearTimeout(RTSSearchPanel.scrollTimer);
  clearInterval(RTSSearchPanel.scrollInterval);
  RTSSearchPanel.timer = null;
  RTSSearchPanel.endTimer = null;
  RTSSearchPanel.scrollTimer = null;
  RTSSearchPanel.scrollInterval = null;
};

RTSSearchPanel.show = command => {
  const panel = RTSSearchPanel.panel;
  if (!panel) return;
  let entries = [];
  try { entries = JSON.parse(String(command.replaySearchEntries || '[]')); } catch (_) {}
  panel.dataset.rtsInformationPanel = 'search';
  panel.innerHTML = '<div class="rts-panel-header"><span class="rts-panel-kicker">CATALOG SEARCH</span><strong></strong><span class="rts-search-requester"></span></div><div class="rts-panel-list"></div>';
  panel.querySelector('strong').textContent = String(command.replaySearchHeader || 'CATALOG');
  const requester = panel.querySelector('.rts-search-requester');
  const requesterName = String(command.replaySearchRequester || 'Unknown');
  const requesterPlatform = String(command.replaySearchRequesterPlatform || '').trim();
  requester.textContent = '';
  const label = document.createElement('span'); label.className = 'rts-search-requester-label'; label.textContent = 'REQUESTED BY: ';
  requester.appendChild(label);
  if (requesterPlatform) {
    const platform = document.createElement('span');
    platform.className = `rts-search-requester-platform rts-search-requester-platform--${requesterPlatform.toLowerCase()}`;
    platform.textContent = requesterPlatform;
    requester.append(platform, document.createTextNode(':'));
  }
  const name = document.createElement('span'); name.className = 'rts-search-requester-name'; name.textContent = requesterName;
  requester.appendChild(name);
  const list = panel.querySelector('.rts-panel-list');
  entries.forEach(entry => {
    const row = document.createElement('div'); row.className = 'rts-panel-entry';
    const number = document.createElement('span'); number.className = 'rts-panel-number'; number.textContent = entry.number ?? '';
    const title = document.createElement('span'); title.className = 'rts-panel-title'; title.textContent = String(entry.title || 'Untitled replay');
    const stats = document.createElement('span'); stats.className = 'rts-search-stats';
    const rating = Number(entry.rating || 0);
    stats.textContent = `${Number(entry.plays || 0)} views${rating ? ` • ★ ${rating.toFixed(1)} (${Number(entry.ratingCount || 0)})` : ''}`;
    row.append(number, title, stats); list.appendChild(row);
  });
  if (!entries.length) {
    const empty = document.createElement('div'); empty.className = 'rts-search-empty'; empty.textContent = 'No matching Catalog entries.'; list.appendChild(empty);
  }
  RTSSearchPanel.clearTimers();
  panel.classList.remove('show'); panel.setAttribute('aria-hidden', 'true');
  list.scrollTop = 0;
  void panel.offsetWidth;
  RTSInformationPanels.show(panel, command, command.replayPanelPosition || 'Centered');

  const requestId = String(command.replaySearchRequestId || '');
  const scrollable = list.scrollHeight > list.clientHeight;
  const duration = Math.max(1000, Number(command.replaySearchDuration) || 10000);

  const hidePanel = () => {
    RTSSearchPanel.clearTimers();
    RTSInformationPanels.hide(panel, command);
    RTSSearchPanel.endTimer = setTimeout(() => RTSSearchPanel.notifyEnded(requestId), 700);
  };

  if (!scrollable) {
    RTSSearchPanel.timer = setTimeout(hidePanel, duration);
    return;
  }

  RTSSearchPanel.scrollTimer = setTimeout(() => {
    RTSSearchPanel.scrollInterval = setInterval(() => {
      if (list.scrollTop + list.clientHeight >= list.scrollHeight - 1) {
        RTSSearchPanel.clearTimers();
        RTSSearchPanel.timer = setTimeout(hidePanel, 3000);
        return;
      }
      list.scrollTop += 1;
    }, 35);
  }, 3000);
};

RTSSearchPanel.handle = command => {
  if (command?.replayCommand === 'search-panel') RTSSearchPanel.show(command);
};

window.RTSSearchPanel = RTSSearchPanel;
