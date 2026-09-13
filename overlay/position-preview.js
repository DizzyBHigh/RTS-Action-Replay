const RTSPositionPreview = window.RTSReplay;

RTSPositionPreview.previewVideoPosition = command => {
  const player = RTSPositionPreview.player;
  if (!player) return;
  RTSPositionPreview.currentCommand = { ...(RTSPositionPreview.currentCommand || {}), ...command };
  RTSPositionPreview.video.style.display = 'none';
  player.classList.add('position-preview', 'show');
  const position = RTSPositionPreview.getPosition(command.replayPosition || 'Full Screen');
  RTSPositionPreview.cancelPendingTransition?.();
  RTSPositionPreview.applyPosition(position, true);
  RTSPositionPreview.activePosition = position;
};

RTSPositionPreview.previewPanelPosition = command => {
  let panel = document.getElementById('rts-position-preview-panel');
  if (!panel) {
    panel = document.createElement('div');
    panel.id = 'rts-position-preview-panel';
    panel.setAttribute('data-rts-information-panel', 'preview');
    document.getElementById('rts-overlay')?.appendChild(panel);
  }

  const width = Math.max(1, Number(command.replayPanelWidth) || 500);
  const height = Math.max(1, Number(command.replayPanelHeight) || 700);
  panel.style.width = `${width}px`;
  panel.style.height = `${height}px`;
  panel.innerHTML = '<div class="rts-position-preview-kicker">INFORMATION PANEL</div><strong>POSITION PREVIEW</strong><span>Panel position</span>';

  const positionName = command.replayPanelPosition || 'Center';
  const position = RTSInformationPanels.normalise(
    RTSInformationPanels.getPosition(command, positionName)
  );
  panel.style.left = `calc(50% + ${position.x}vw)`;
  panel.style.top = `calc(50% - ${position.y}vh)`;
  panel.style.transform = `translate(-50%, -50%) scaleX(${position.scaleX / 100}) scaleY(${position.scaleY / 100}) rotateZ(${position.rotateZ}deg)`;
  panel.classList.add('show');
  panel.setAttribute('aria-hidden', 'false');
};

RTSPositionPreview.hidePositionPreview = () => {
  RTSPositionPreview.player?.classList.remove('position-preview');
  if (RTSPositionPreview.video) RTSPositionPreview.video.style.display = 'block';
  RTSPositionPreview.cancelPendingTransition?.();
  const panel = document.getElementById('rts-position-preview-panel');
  if (panel) {
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
