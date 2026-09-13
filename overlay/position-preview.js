const RTSPositionPreview = window.RTSReplay;

RTSPositionPreview.previewVideoPosition = command => {
  const player = RTSPositionPreview.player;
  if (!player) return;
  RTSPositionPreview.currentCommand = { ...(RTSPositionPreview.currentCommand || {}), ...command };
  const position = RTSPositionPreview.getPosition(command.replayPosition || 'Full Screen');
  RTSPositionPreview.cancelPendingTransition?.();
  RTSPositionPreview.video.style.display = 'block';
  player.classList.add('position-preview', 'show');
  RTSPositionPreview.applyPosition(position, true);
  RTSPositionPreview.activePosition = position;
};

RTSPositionPreview.previewPanelPosition = command => {
  const panel = document.querySelector('[data-rts-information-panel]');
  if (!panel || !window.RTSInformationPanels) return;

  RTSPositionPreview.currentCommand = { ...(RTSPositionPreview.currentCommand || {}), ...command };
  window.RTSInformationPanelAnimation?.cancel?.();
  window.RTSInformationPanels.applyPosition(
    panel,
    command,
    command.replayPanelPosition || 'Center'
  );
  panel.classList.add('show');
  panel.setAttribute('aria-hidden', 'false');
};

RTSPositionPreview.hidePositionPreview = () => {
  RTSPositionPreview.player?.classList.remove('position-preview');
  if (RTSPositionPreview.video) RTSPositionPreview.video.style.display = 'block';
  RTSPositionPreview.cancelPendingTransition?.();

  const panel = document.querySelector('[data-rts-information-panel]');
  if (panel) {
    window.RTSInformationPanelAnimation?.cancel?.();
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
