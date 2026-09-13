const RTSPositionPreview = window.RTSReplay;

RTSPositionPreview.previewVideoPosition = command => {
  const player = RTSPositionPreview.player;
  if (!player) return;
  RTSPositionPreview.currentCommand = { ...(RTSPositionPreview.currentCommand || {}), ...command };
  const position = RTSPositionPreview.getPosition(command.replayPosition || 'Full Screen');
  const wasVisible = player.classList.contains('show');
  const start = RTSPositionPreview.activePosition;
  RTSPositionPreview.cancelPendingTransition?.();
  RTSPositionPreview.video.style.display = 'block';
  player.classList.add('position-preview', 'show');
  if (wasVisible && start) RTSPositionPreview.animatePosition?.(start, position);
  else RTSPositionPreview.applyPosition(position, true);
  RTSPositionPreview.activePosition = position;
};

RTSPositionPreview.panelPosition = null;

const originalPanelApplyPosition = window.RTSInformationPanels?.applyPosition;
if (originalPanelApplyPosition) {
  window.RTSInformationPanels.applyPosition = (panel, command, name) => {
    originalPanelApplyPosition(panel, command, name);
    RTSPositionPreview.panelPosition = window.RTSInformationPanels.normalise(
      window.RTSInformationPanels.getPosition(command, name)
    );
  };
}

const buildPanelPreview = panel => {
  if (!panel || panel.querySelector('.rts-panel-header')) return;
  panel.innerHTML = '<div class="rts-panel-header"><span class="rts-panel-kicker">ACTION REPLAY</span><strong>RECENT REPLAYS</strong></div><div class="rts-panel-list"></div>';
  const list = panel.querySelector('.rts-panel-list');
  [
    { number: '1', title: 'Example Replay' },
    { number: '2', title: 'Another Recent Replay' },
    { number: '3', title: 'Requested Replay' },
    { number: '4', title: 'Latest Replay' }
  ].forEach(entry => {
    const row = document.createElement('div');
    row.className = 'rts-panel-entry';
    const number = document.createElement('span');
    number.className = 'rts-panel-number';
    number.textContent = entry.number;
    const avatar = document.createElement('span');
    avatar.className = 'rts-panel-avatar';
    const title = document.createElement('span');
    title.className = 'rts-panel-title';
    title.textContent = entry.title;
    row.append(number, avatar, title);
    list.appendChild(row);
  });
};

const applyPanelPreviewSize = (panel, command) => {
  const width = Number(command?.replayPanelWidth);
  const height = Number(command?.replayPanelHeight);
  if (!panel || !Number.isFinite(width) || !Number.isFinite(height) || width <= 0 || height <= 0) return;

  const screen = document.getElementById('rts-dev-screen');
  if (screen) {
    const bounds = screen.getBoundingClientRect();
    panel.style.setProperty('--preview-panel-width', `${width * bounds.width / 1920}px`);
    panel.style.setProperty('--preview-panel-height', `${height * bounds.height / 1080}px`);
  } else {
    panel.style.setProperty('--preview-panel-width', `${width}px`);
    panel.style.setProperty('--preview-panel-height', `${height}px`);
  }
};

RTSPositionPreview.previewPanelPosition = command => {
  const panel = RTSPositionPreview.recentList;
  if (!panel || !window.RTSInformationPanels) return;

  RTSPositionPreview.currentCommand = { ...(RTSPositionPreview.currentCommand || {}), ...command };
  panel.dataset.rtsInformationPanel = 'recent';
  panel.classList.add('position-preview');
  applyPanelPreviewSize(panel, command);
  buildPanelPreview(panel);

  const positionName = command.replayPanelPosition || 'Center';
  const position = window.RTSInformationPanels.normalise(
    window.RTSInformationPanels.getPosition(command, positionName)
  );
  const wasVisible = panel.classList.contains('show');
  const start = RTSPositionPreview.panelPosition;
  window.RTSInformationPanelAnimation?.cancel?.();

  if (wasVisible && start) {
    const duration = Math.max(
      100,
      Number(RTSPositionPreview.currentCommand?.replayAnimationDuration) || 500
    );
    const started = performance.now();
    const easing = value => value < .5
      ? 4 * value * value * value
      : 1 - Math.pow(-2 * value + 2, 3) / 2;
    const frame = now => {
      const progress = Math.min(1, Math.max(0, (now - started) / duration));
      const amount = easing(progress);
      const current = {
        scaleX: start.scaleX + (position.scaleX - start.scaleX) * amount,
        scaleY: start.scaleY + (position.scaleY - start.scaleY) * amount,
        x: start.x + (position.x - start.x) * amount,
        y: start.y + (position.y - start.y) * amount,
        rotateZ: start.rotateZ + (position.rotateZ - start.rotateZ) * amount
      };
      window.RTSInformationPanelAnimation.apply(panel, current);
      if (progress < 1) {
        RTSPositionPreview.panelAnimationFrame = requestAnimationFrame(frame);
      } else {
        RTSPositionPreview.panelAnimationFrame = null;
        RTSPositionPreview.panelPosition = position;
      }
    };
    if (RTSPositionPreview.panelAnimationFrame) cancelAnimationFrame(RTSPositionPreview.panelAnimationFrame);
    RTSPositionPreview.panelAnimationFrame = requestAnimationFrame(frame);
  } else {
    window.RTSInformationPanels.applyPosition(panel, command, positionName);
    RTSPositionPreview.panelPosition = position;
  }

  panel.classList.add('show');
  panel.setAttribute('aria-hidden', 'false');
};

RTSPositionPreview.hidePositionPreview = () => {
  RTSPositionPreview.player?.classList.remove('position-preview');
  if (RTSPositionPreview.video) RTSPositionPreview.video.style.display = 'block';
  RTSPositionPreview.cancelPendingTransition?.();
  if (RTSPositionPreview.panelAnimationFrame) {
    cancelAnimationFrame(RTSPositionPreview.panelAnimationFrame);
    RTSPositionPreview.panelAnimationFrame = null;
  }

  const panel = RTSPositionPreview.recentList;
  if (panel) {
    window.RTSInformationPanelAnimation?.cancel?.();
    panel.classList.remove('position-preview');
    panel.style.removeProperty('--preview-panel-width');
    panel.style.removeProperty('--preview-panel-height');
    panel.classList.remove('show');
    panel.setAttribute('aria-hidden', 'true');
  }
};

const originalHandleReplayCommand = RTSPositionPreview.handleReplayCommand;
RTSPositionPreview.handleReplayCommand = command => {
  if (command?.replayCommand === 'position-preview') {
    RTSPositionPreview.previewVideoPosition(command);
    return;
  }
  if (command?.replayCommand === 'panel-position-preview') {
    RTSPositionPreview.previewPanelPosition(command);
    return;
  }
  if (command?.replayCommand === 'position-preview-hide') {
    RTSPositionPreview.hidePositionPreview();
    return;
  }
  originalHandleReplayCommand(command);
};
