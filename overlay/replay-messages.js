const RTSReplayMessages = window.RTSReplay;

let clapperRunner = null;

const getClapperRunner = command => {
  if (!clapperRunner) clapperRunner = RTSAnimationEngine.createRunner({
    target: RTSReplayMessages.clapperCard,
    defaultPosition: { scale: 50, x: 0, y: 0, z: 0, rotateX: 0, rotateY: 0, rotateZ: 0, fov: 90 }
  });
  clapperRunner.configure(command?.replayClapperPositions);
  return clapperRunner;
};

const runClapper = (command, sequence, complete, end = false) => {
  const runner = getClapperRunner(command);
  if (!Array.isArray(sequence) || !sequence.length) { complete?.(); return; }
  if (end) runner.runEnd(sequence, complete);
  else runner.run(sequence, complete);
};

const messagePanelCommand = command => ({
  ...command,
  replayInformationPanelType: 'message',
  replayPanelPositions: command?.replayMessagePositions || '{}',
  replayPanelAnimation: command?.replayMessageAnimation || '',
  replayPanelPosition: command?.replayMessagePosition || 'Centered'
});

RTSReplayMessages.showMessage = command => {
  const text = String(command?.replayMessage || '').trim();
  if (!text || !RTSReplayMessages.messageCard) return;

  RTSReplayMessages.messageText.textContent = text;
  const panelCommand = messagePanelCommand(command);
  clearTimeout(RTSReplayMessages.messageTimer);
  RTSReplayMessages.messageCard.classList.remove('show');
  void RTSReplayMessages.messageCard.offsetWidth;
  RTSInformationPanels.show(
    RTSReplayMessages.messageCard,
    panelCommand,
    panelCommand.replayPanelPosition
  );
  RTSReplayMessages.messageTimer = setTimeout(
    () => RTSReplayMessages.hideMessage(command),
    Number.isFinite(Number(command?.replayMessageDuration)) ? Number(command.replayMessageDuration) : RTSReplayMessages.config.messageDuration
  );
};

RTSReplayMessages.hideMessage = command => {
  clearTimeout(RTSReplayMessages.messageTimer);
  if (!RTSReplayMessages.messageCard) return;
  RTSInformationPanels.hide(RTSReplayMessages.messageCard, messagePanelCommand(command || {}), () => {
    RTSReplayWebSocket.acknowledgeMessage(command?.messageQueueId);
  });
};

RTSReplayMessages.showClapperboard = command => {
  if (!RTSReplayMessages.clapperCard) return;
  const text = command?.replayMessage || '';
  if (!text) return;

  const card = RTSReplayMessages.clapperCard;
  RTSReplayMessages.clapperMessageText.textContent = text;
  const showBranding = command.replayShowClapperBranding !== false;
  card.querySelector('.brand').style.display = showBranding ? '' : 'none';
  const logoUrl = command.replayLogoUrl || '';

  if (logoUrl) {
    RTSReplayMessages.brandLogo.onload = () => {
      RTSReplayMessages.brandLogo.style.display = 'block';
      RTSReplayMessages.brandFallback.style.display = 'none';
    };
    RTSReplayMessages.brandLogo.onerror = () => {
      RTSReplayMessages.brandLogo.style.display = 'none';
      RTSReplayMessages.brandFallback.style.display = 'block';
    };
    RTSReplayMessages.brandLogo.src = logoUrl;
  } else {
    RTSReplayMessages.brandLogo.removeAttribute('src');
    RTSReplayMessages.brandLogo.style.display = 'none';
    RTSReplayMessages.brandFallback.style.display = 'block';
  }

  clearTimeout(RTSReplayMessages.messageTimer);
  const profile = RTSAnimationEngine.readProfile(command.replayClapperAnimation);
  const start = profile?.start;
  card.classList.remove('show');
  card.style.opacity = '1';
  card.style.visibility = 'visible';
  card.style.zIndex = '55';
  void card.offsetWidth;
  card.classList.add('show');
  card.setAttribute('aria-hidden', 'false');

  if (Array.isArray(start) && start.length) {
    runClapper(command, start, () => {
      const stick = card.querySelector('.clapstick');
      if (stick) { stick.classList.remove('clap'); void stick.offsetWidth; stick.classList.add('clap'); }
    });
  } else {
    const stick = card.querySelector('.clapstick');
    if (stick) { stick.classList.remove('clap'); void stick.offsetWidth; stick.classList.add('clap'); }
  }

  RTSReplayMessages.messageTimer = setTimeout(() => {
    const end = RTSAnimationEngine.readProfile(command.replayClapperAnimation)?.end;
    if (Array.isArray(end) && end.length) {
      runClapper(command, end, () => {
        card.classList.remove('show');
        card.style.opacity = '';
        card.style.visibility = '';
        card.style.zIndex = '';
        card.setAttribute('aria-hidden', 'true');
        RTSReplayWebSocket.acknowledgeMessage(command?.messageQueueId);
      }, true);
    } else {
      getClapperRunner(command).cancel();
      card.classList.remove('show');
      card.style.opacity = '';
      card.style.visibility = '';
      card.style.zIndex = '';
      card.setAttribute('aria-hidden', 'true');
      RTSReplayWebSocket.acknowledgeMessage(command?.messageQueueId);
    }
  }, Number.isFinite(Number(command?.replayClapperDuration)) ? Number(command.replayClapperDuration) : RTSReplayMessages.config.messageDuration);
};

window.RTSReplayMessages = RTSReplayMessages;
