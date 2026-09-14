const RTSLeaderboardList = {};

RTSLeaderboardList.panel = document.getElementById('leaderboard-list');
RTSLeaderboardList.timer = null;

RTSLeaderboardList.clearTimer = () => {
  if (RTSLeaderboardList.timer) clearTimeout(RTSLeaderboardList.timer);
  RTSLeaderboardList.timer = null;
};

RTSLeaderboardList.show = command => {
  const panel = RTSLeaderboardList.panel;
  if (!panel) return;
  let entries;
  try {
    const raw = command?.replayLeaderboardEntries || '[]';
    entries = typeof raw === 'string' ? JSON.parse(raw) : raw;
  } catch (_) { entries = []; }
  if (!Array.isArray(entries)) entries = [];

  panel.innerHTML = '<div class="rts-panel-header"><span class="rts-panel-kicker">ACTION REPLAY</span><strong>CLIP CREATORS</strong><small class="rts-leaderboard-period"></small></div><div class="rts-panel-list"></div>';
  const period = panel.querySelector('.rts-leaderboard-period');
  if (period) period.textContent = String(command?.replayLeaderboardPeriod || 'ALL TIME');
  const list = panel.querySelector('.rts-panel-list');

  if (!entries.length) {
    const empty = document.createElement('div');
    empty.className = 'rts-leaderboard-empty';
    empty.textContent = 'No clips found for this period.';
    list.appendChild(empty);
  } else {
    entries.forEach(entry => {
      const row = document.createElement('div');
      row.className = 'rts-panel-entry';
      const rank = document.createElement('span');
      rank.className = 'rts-panel-number';
      rank.textContent = `#${entry.rank ?? ''}`;
      const creator = document.createElement('strong');
      creator.className = 'rts-panel-title';
      creator.textContent = entry.creator || 'Unknown creator';
      const count = document.createElement('span');
      count.className = 'rts-leaderboard-count';
      count.textContent = `${entry.count ?? 0} clip${Number(entry.count) === 1 ? '' : 's'}`;
      row.append(rank, creator, count);
      list.appendChild(row);
    });
  }

  RTSLeaderboardList.clearTimer();
  const messageCard = document.getElementById('message-card');
  if (messageCard) {
    messageCard.classList.remove('show');
    messageCard.setAttribute('aria-hidden', 'true');
  }
  panel.classList.remove('show');
  panel.setAttribute('aria-hidden', 'true');
  void panel.offsetWidth;

  const panelPosition = command?.replayPanelPosition || 'Centered';
  const animationCommand = { ...command, replayPanelPreset: command?.replayPanelPreset || 'Broadcast' };
  RTSInformationPanels.show(panel, animationCommand, panelPosition);
  RTSLeaderboardList.timer = setTimeout(() => RTSInformationPanels.hide(panel, animationCommand), 10000);
};

RTSLeaderboardList.handle = command => {
  if (command?.replayCommand !== 'leaderboard-panel') return false;
  RTSLeaderboardList.show(command);
  return true;
};

window.RTSLeaderboardList = RTSLeaderboardList;
