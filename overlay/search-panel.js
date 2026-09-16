const RTSSearchPanel = window.RTSSearchPanel || {};

RTSSearchPanel.panel = document.getElementById('search-panel');
RTSSearchPanel.timer = null;
RTSSearchPanel.endTimer = null;
RTSSearchPanel.scrollTimer = null;
RTSSearchPanel.scrollInterval = null;
RTSSearchPanel.avatarRequests = new Map();
RTSSearchPanel.avatarCache = new Map();
RTSSearchPanel.avatarPending = new Map();

RTSSearchPanel.notifyEnded = requestId => {
  if (!RTSReplay.socket || RTSReplay.socket.readyState !== WebSocket.OPEN) return;
  RTSReplay.socket.send(JSON.stringify({
    request: 'DoAction', id: `rts-search-ended-${Date.now()}`,
    action: { name: RTSReplay.config.searchEndedAction },
    args: { replaySearchRequestId: requestId || '' }
  }));
};

RTSSearchPanel.avatarKey = (userId, userName, platform) => {
  const identity = userId || userName || '';
  return `${RTSSearchPanel.normalizePlatform(platform).toLowerCase()}:${String(identity).toLowerCase()}`;
};

RTSSearchPanel.requestAvatar = (userId, userName, platform, apply) => {
  if (!RTSReplay.socket || RTSReplay.socket.readyState !== WebSocket.OPEN) return;
  const key = RTSSearchPanel.avatarKey(userId, userName, platform);
  const cached = RTSSearchPanel.avatarCache.get(key);
  if (cached) { apply(cached); return; }
  const pending = RTSSearchPanel.avatarPending.get(key);
  if (pending) { pending.push(apply); return; }
  RTSSearchPanel.avatarPending.set(key, [apply]);
  const requestId = `rts-avatar-${Date.now()}-${Math.random().toString(36).slice(2)}`;
  RTSSearchPanel.avatarRequests.set(requestId, key);
  RTSReplay.socket.send(JSON.stringify({
    request: 'DoAction', id: requestId,
    action: { name: 'RTS - Action Replay - Core - Catalog' },
    args: {
      replayAvatarRequestId: requestId,
      replayAvatarUserId: userId || '',
      replayAvatarUserName: userName || '',
      replayAvatarPlatform: platform || ''
    }
  }));
};

RTSSearchPanel.handleAvatar = command => {
  const requestId = String(command.replayAvatarRequestId || '');
  const key = RTSSearchPanel.avatarRequests.get(requestId);
  if (!key) return;
  RTSSearchPanel.avatarRequests.delete(requestId);
  const callbacks = RTSSearchPanel.avatarPending.get(key) || [];
  RTSSearchPanel.avatarPending.delete(key);
  const url = String(command.replayAvatarUrl || '').trim();
  if (!url) return;
  RTSSearchPanel.avatarCache.set(key, url);
  callbacks.forEach(apply => apply(url));
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

RTSSearchPanel.describeSearch = parameters => {
  const value = String(parameters || '').trim();
  const separator = value.indexOf(':');
  if (separator < 0) {
    const labels = { CATALOG: 'Full Catalog', RECENT: 'Recent Replays', 'LAST PLAYED': 'Last Played', 'MOST VIEWS': 'Most Views', 'TOP RATED': 'Top Rated' };
    return labels[value.toUpperCase()] || value;
  }
  const type = value.slice(0, separator).trim().toLowerCase();
  const parameter = value.slice(separator + 1).trim();
  if (type === 'search') return `Search Term: ${parameter}`;
  const labels = { date: 'By Date', creator: 'By Creator' };
  return `${labels[type] || type} ${parameter}`.trim();
};

RTSSearchPanel.normalizePlatform = platform => {
  const value = String(platform || '').trim().toLowerCase();
  if (value === 'youtube') return 'YouTube';
  if (value === 'kick') return 'Kick';
  if (value === 'twitch') return 'Twitch';
  return String(platform || '').trim();
};

RTSSearchPanel.parsePageInfo = header => {
  const parts = String(header || '').split('•').map(x => x.trim());
  const page = parts.length > 1 ? parts[1].split('/').map(x => x.trim()) : [];
  return {
    page: page[0] || '1',
    pages: page[1] || '1',
    total: parts.length > 2 ? parts[2] : '0'
  };
};

RTSSearchPanel.show = command => {
  const panel = RTSSearchPanel.panel;
  if (!panel) return;
  let entries = [];
  try { entries = JSON.parse(String(command.replaySearchEntries || '[]')); } catch (_) {}
  const isLastPlayed = String(command.replaySearchMode || '') === 'lastPlayed';
  panel.dataset.rtsInformationPanel = 'search';
  panel.innerHTML = '<div class="rts-panel-header"><span class="rts-panel-kicker">CATALOG SEARCH</span><strong class="rts-search-type"></strong><span class="rts-search-summary"></span><span class="rts-search-requester"></span></div><div class="rts-panel-list"></div>';
  panel.querySelector('.rts-search-type').textContent = RTSSearchPanel.describeSearch(command.replaySearchParameters);
  const pageInfo = RTSSearchPanel.parsePageInfo(command.replaySearchHeader);
  panel.querySelector('.rts-search-summary').textContent = `Page ${pageInfo.page} of ${pageInfo.pages} | ${pageInfo.total} Clips`;

  const requester = panel.querySelector('.rts-search-requester');
  const requesterName = String(command.replaySearchRequester || 'Unknown');
  const requesterPlatform = RTSSearchPanel.normalizePlatform(command.replaySearchRequesterPlatform || command.commandSource);
  requester.textContent = '';
  const label = document.createElement('span'); label.className = 'rts-search-requester-label'; label.textContent = 'Requested By ';
  requester.appendChild(label);
  if (requesterPlatform) {
    const platform = document.createElement('span');
    platform.className = `rts-search-requester-platform rts-search-requester-platform--${requesterPlatform.toLowerCase()}`;
    platform.textContent = requesterPlatform;
    requester.append(platform, document.createTextNode(':'));
  }
  const headerAvatar = document.createElement('img');
  headerAvatar.className = 'rts-search-requester-avatar';
  headerAvatar.alt = '';
  headerAvatar.hidden = true;
  const suppliedHeaderAvatar = String(command.replaySearchRequesterAvatar || command.userAvatar || '').trim();
  if (suppliedHeaderAvatar) { headerAvatar.src = suppliedHeaderAvatar; headerAvatar.hidden = false; }
  else if (command.replaySearchRequesterUserId) RTSSearchPanel.requestAvatar(String(command.replaySearchRequesterUserId), requesterName, requesterPlatform, url => { headerAvatar.src = url; headerAvatar.hidden = false; });
  const name = document.createElement('span'); name.className = 'rts-search-requester-name'; name.textContent = requesterName;
  requester.append(headerAvatar, name);

  const list = panel.querySelector('.rts-panel-list');
  entries.forEach(entry => {
    const row = document.createElement('div'); row.className = 'rts-panel-entry';
    const number = document.createElement('span'); number.className = 'rts-panel-number'; number.textContent = entry.number ?? '';
    const content = document.createElement('div'); content.className = 'rts-search-result-content';
    const title = document.createElement('span'); title.className = 'rts-panel-title'; title.textContent = String(entry.title || 'Untitled replay');
    content.appendChild(title);
    if (entry.creator) {
      const creatorPlatform = RTSSearchPanel.normalizePlatform(entry.creatorPlatform);
      const creator = document.createElement('span'); creator.className = `rts-search-result-creator rts-search-requester-platform--${creatorPlatform.toLowerCase()}`;
      const name = document.createElement('span'); name.className = 'rts-search-result-creator-name'; name.textContent = String(entry.creator);
      creator.appendChild(name);
      content.appendChild(creator);
    }
    const stats = document.createElement('span'); stats.className = 'rts-search-stats';
    if (isLastPlayed || entry.historyCount != null) {
      const playedBy = String(entry.lastPlayedBy || 'Unknown');
      const playedPlatform = RTSSearchPanel.normalizePlatform(entry.lastPlayedPlatform);
      const player = document.createElement('span'); player.className = `rts-search-history-player rts-search-requester-platform--${playedPlatform.toLowerCase()}`;
      const playerLabel = document.createElement('span'); playerLabel.className = 'rts-search-history-label'; playerLabel.textContent = 'Played By';
      player.appendChild(playerLabel);
      const avatar = document.createElement('img'); avatar.className = 'rts-search-history-avatar'; avatar.alt = ''; avatar.hidden = true;
      player.appendChild(avatar);
      const name = document.createElement('span'); name.className = 'rts-search-history-name'; name.textContent = playedBy;
      player.appendChild(name);
      stats.appendChild(player);
      if (entry.lastPlayedUserId) RTSSearchPanel.requestAvatar(String(entry.lastPlayedUserId), playedBy, playedPlatform, url => { avatar.src = url; avatar.hidden = false; });
      const count = Number(entry.historyCount || 1);
      if (count > 1) {
        const repeat = document.createElement('span'); repeat.textContent = String(count); repeat.className = 'rts-search-history-count';
        stats.append(' • ', repeat);
      }
    } else {
      const rating = Number(entry.rating || 0);
      stats.textContent = `${Number(entry.plays || 0)} views${rating ? ` • ★ ${rating.toFixed(1)} (${Number(entry.ratingCount || 0)})` : ''}`;
    }
    row.append(number, content, stats); list.appendChild(row);
  });
  if (!entries.length) {
    const empty = document.createElement('div'); empty.className = 'rts-search-empty'; empty.textContent = isLastPlayed ? 'No replays have been played recently.' : 'No matching Catalog entries.'; list.appendChild(empty);
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
