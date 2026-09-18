const RTSPositionPreview = window.RTSReplay;

RTSPositionPreview.previewVideoPosition = command => {
  const player = RTSPositionPreview.player;
  if (!player) return;
  RTSPositionPreview.currentCommand = { ...(RTSPositionPreview.currentCommand || {}), ...command };
  RTSPositionPreview.configurePositions?.(command.replayPlayerPositions);
  const position = RTSPositionPreview.getPosition(command.replayPosition || 'Full Screen');
  const wasVisible = player.classList.contains('show');
  const start = RTSPositionPreview.activePosition;
  RTSPositionPreview.cancelPendingTransition?.();
  panelPreviewRunner?.cancel(); clapperPreviewRunner?.cancel();
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
    RTSPositionPreview.panelPosition = window.RTSInformationPanels.normalise(window.RTSInformationPanels.getPosition(command, name));
  };
}

const buildPanelPreview = panel => {
  if (!panel || panel.querySelector('.rts-panel-header')) return;
  panel.innerHTML = '<div class="rts-panel-header"><span class="rts-panel-kicker">ACTION REPLAY</span><strong>RECENT REPLAYS</strong></div><div class="rts-panel-list"></div>';
  const list = panel.querySelector('.rts-panel-list');
  [{ number: '1', title: 'Example Replay' }, { number: '2', title: 'Another Recent Replay' }, { number: '3', title: 'Requested Replay' }, { number: '4', title: 'Latest Replay' }].forEach(entry => {
    const row = document.createElement('div'); row.className = 'rts-panel-entry';
    const number = document.createElement('span'); number.className = 'rts-panel-number'; number.textContent = entry.number;
    const avatar = document.createElement('span'); avatar.className = 'rts-panel-avatar';
    const title = document.createElement('span'); title.className = 'rts-panel-title'; title.textContent = entry.title;
    row.append(number, avatar, title); list.appendChild(row);
  });
};

const applyPanelPreviewSize = (panel, command) => {
  const width = Number(command?.replayPanelWidth); const height = Number(command?.replayPanelHeight);
  if (!panel || !Number.isFinite(width) || !Number.isFinite(height) || width <= 0 || height <= 0) return;
  const screen = document.getElementById('rts-dev-screen');
  if (screen) { const bounds = screen.getBoundingClientRect(); panel.style.setProperty('--preview-panel-width', `${width * bounds.width / 1920}px`); panel.style.setProperty('--preview-panel-height', `${height * bounds.height / 1080}px`); }
  else { panel.style.setProperty('--preview-panel-width', `${width}px`); panel.style.setProperty('--preview-panel-height', `${height}px`); }
};

RTSPositionPreview.previewPanelPosition = command => {
  const panel = RTSPositionPreview.recentList;
  if (!panel || !window.RTSInformationPanels) return;
  RTSPositionPreview.currentCommand = { ...(RTSPositionPreview.currentCommand || {}), ...command };
  panel.dataset.rtsInformationPanel = 'recent'; panel.classList.add('position-preview'); applyPanelPreviewSize(panel, command); buildPanelPreview(panel);
  const positionName = command.replayPanelPosition || 'Centered';
  const position = RTSAnimationEngine.normalisePosition(window.RTSAnimationEngine.resolvePosition(RTSAnimationEngine.getPositions(command.replayPanelPositions, {}), positionName, { scale: 100, x: 0, y: 0, z: 0, rotateX: 0, rotateY: 0, rotateZ: 0, fov: 90 }));
  const wasVisible = panel.classList.contains('show'); const start = RTSPositionPreview.panelPosition;
  window.RTSInformationPanelAnimation?.cancel?.();
  if (!panelPreviewRunner) panelPreviewRunner = RTSAnimationEngine.createRunner({ target: panel });
  panelPreviewRunner.setTarget(panel); panelPreviewRunner.configure(command.replayPanelPositions);
  if (wasVisible && start) {
    panelPreviewRunner.transition(start, position, Math.max(100, Number(RTSPositionPreview.currentCommand?.replayAnimationDuration) || 500), 'ease-in-out', () => { RTSPositionPreview.panelPosition = position; });
  } else { panelPreviewRunner.apply(position); RTSPositionPreview.panelPosition = position; }
  panel.classList.add('show'); panel.setAttribute('aria-hidden', 'false');
};

RTSPositionPreview.previewClapperPosition = command => {
  const card = RTSPositionPreview.messageCard;
  if (!card) return;
  RTSPositionPreview.currentCommand = { ...(RTSPositionPreview.currentCommand || {}), ...command };
  RTSReplayMessages.applyMessageStyle(command);
  RTSPositionPreview.messageText.textContent = command.replayMessage || 'CLAPPERBOARD PREVIEW';
  card.classList.add('position-preview', 'show');
  card.setAttribute('aria-hidden', 'false');
  const positionName = command.replayClapperPosition || 'Centered';
  if (!clapperPreviewRunner) clapperPreviewRunner = RTSAnimationEngine.createRunner({ target: card, defaultPosition: { scale: 50, x: 0, y: 0, z: 0, rotateX: 0, rotateY: 0, rotateZ: 0, fov: 90 } });
  clapperPreviewRunner.setTarget(card); clapperPreviewRunner.configure(command.replayClapperPositions); clapperPreviewRunner.apply(clapperPreviewRunner.resolve(positionName));
};

RTSPositionPreview.hidePositionPreview = () => {
  RTSPositionPreview.player?.classList.remove('position-preview');
  if (RTSPositionPreview.video) RTSPositionPreview.video.style.display = 'block';
  RTSPositionPreview.cancelPendingTransition?.();
  if (RTSPositionPreview.panelAnimationFrame) { cancelAnimationFrame(RTSPositionPreview.panelAnimationFrame); RTSPositionPreview.panelAnimationFrame = null; }
  const panel = RTSPositionPreview.recentList;
  if (panel) { window.RTSInformationPanelAnimation?.cancel?.(); panel.classList.remove('position-preview'); panel.style.removeProperty('--preview-panel-width'); panel.style.removeProperty('--preview-panel-height'); panel.classList.remove('show'); panel.setAttribute('aria-hidden', 'true'); }
  const clapper = RTSPositionPreview.messageCard;
  if (clapper) { clapper.classList.remove('position-preview'); clapper.classList.remove('show'); clapper.setAttribute('aria-hidden', 'true'); }
};

const originalHandleReplayCommand = RTSPositionPreview.handleReplayCommand;
RTSPositionPreview.handleReplayCommand = command => {
  if (command?.replayCommand === 'position-preview') { RTSPositionPreview.previewVideoPosition(command); return; }
  if (command?.replayCommand === 'panel-position-preview') { RTSPositionPreview.previewPanelPosition(command); return; }
  if (command?.replayCommand === 'clapper-position-preview') { RTSPositionPreview.previewClapperPosition(command); return; }
  if (command?.replayCommand === 'position-preview-hide' || command?.replayCommand === 'panel-position-preview-hide' || command?.replayCommand === 'clapper-position-preview-hide') { RTSPositionPreview.hidePositionPreview(); return; }
  originalHandleReplayCommand(command);
};
