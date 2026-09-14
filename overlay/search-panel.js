const RTSSearchPanel = window.RTSSearchPanel || {};

RTSSearchPanel.panel = document.getElementById('search-panel');
RTSSearchPanel.timer = null;
RTSSearchPanel.endTimer = null;

RTSSearchPanel.notifyEnded = requestId => {
  if (!RTSReplay.socket || RTSReplay.socket.readyState !== WebSocket.OPEN) return;
  RTSReplay.socket.send(JSON.stringify({
    request: 'DoAction', id: `rts-search-ended-${Date.now()}`,
    action: { name: RTSReplay.config.searchEndedAction },
    args: { replaySearchRequestId: requestId || '' }
  }));
};

RTSSearchPanel.show = command => {
  const panel = RTSSearchPanel.panel;
  if (!panel) return;
  let entries = [];
  try { entries = JSON.parse(String(command.replaySearchEntries || '[]')); } catch (_) {}
  panel.dataset.rtsInformationPanel = 'search';
  panel.innerHTML = '<div class="rts-panel-header"><span class="rts-panel-kicker">CATALOG SEARCH</span><strong></strong><span class="rts-search-requester"></span></div><div class="rts-panel-list"></div>';
  panel.querySelector('strong').textContent = String(command.replaySearchHeader || 'CATALOG');
  panel.querySelector('.rts-search-requester').textContent = `REQUESTED BY: ${String(command.replaySearchRequester || 'Unknown')}`;
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
  clearTimeout(RTSSearchPanel.timer); clearTimeout(RTSSearchPanel.endTimer);
  panel.classList.remove('show'); panel.setAttribute('aria-hidden', 'true'); void panel.offsetWidth;
  RTSInformationPanels.show(panel, command, command.replayPanelPosition || 'Centered');
  const duration = Math.max(1000, Number(command.replaySearchDuration) || 10000);
  const requestId = String(command.replaySearchRequestId || '');
  RTSSearchPanel.timer = setTimeout(() => {
    RTSInformationPanels.hide(panel, command);
    RTSSearchPanel.endTimer = setTimeout(() => RTSSearchPanel.notifyEnded(requestId), 700);
  }, duration);
};

RTSSearchPanel.handle = command => {
  if (command?.replayCommand === 'search-panel') RTSSearchPanel.show(command);
};

window.RTSSearchPanel = RTSSearchPanel;
