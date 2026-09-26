const RTSReplayMessages = window.RTSReplay;

let clapperRunner = null;

const getClapperRunner = command => {
  if (!clapperRunner) clapperRunner = RTSAnimationEngine.createRunner({
    target: RTSReplayMessages.clapperCard?.querySelector('.clapper-position'),
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

const applyMessageBranding = command => {
  const card = RTSReplayMessages.messageCard;
  if (!card) return;

  const presets = window.rtsOverlayConfig?.presets?.branding;
  const configuredId = command?.replayBrandingPresetId
    || window.rtsOverlayConfig?.message?.entryPoint?.brandingPreset
    || 'default';
  const brand = Array.isArray(presets)
    ? presets.find(item => String(item?.id || '') === String(configuredId))
      || presets.find(item => String(item?.id || '') === 'default')
    : null;

  const fallback = String(command?.replayBrandFallbackText || brand?.fallbackText || 'RTS').trim();
  const label = String(command?.replayBrandLabel || brand?.brandLabel || 'ACTION REPLAY').trim();
  const logoUrl = String(command?.replayBrandLogoUrl || brand?.logo || '').trim();
  const logo = card.querySelector('#message-brand-logo');
  const fallbackNode = card.querySelector('#message-brand-fallback');
  const labelNode = card.querySelector('#message-brand-label');
  if (!logo || !fallbackNode || !labelNode) return;

  fallbackNode.textContent = fallback;
  labelNode.textContent = label;
  card.style.setProperty('--message-brand-fallback', command?.replayBrandFallbackTextColor || brand?.primaryColor || '#0384CBFF');
  card.style.setProperty('--message-brand-label', command?.replayBrandLabelColor || brand?.textColor || '#FFFFFFFF');
  logo.classList.remove('loaded');
  logo.removeAttribute('src');
  fallbackNode.style.display = '';
  if (!logoUrl) return;

  fallbackNode.style.display = 'none';
  logo.onload = () => logo.classList.add('loaded');
  logo.onerror = () => {
    logo.classList.remove('loaded');
    logo.removeAttribute('src');
    fallbackNode.style.display = '';
  };
  logo.src = logoUrl;
};

const fitMessageText = () => {
  const text = RTSReplayMessages.messageText;
  if (!text) return;
  let size = Number.parseFloat(getComputedStyle(text).fontSize) || 14;
  const minimum = 8;
  text.style.fontSize = size + 'px';
  while (size > minimum && (text.scrollWidth > text.clientWidth || text.scrollHeight > text.clientHeight)) {
    size -= 0.5;
    text.style.fontSize = size + 'px';
  }
};

RTSReplayMessages.showMessage = command => {
  const text = String(command?.replayMessage || '').trim();
  if (!text || !RTSReplayMessages.messageCard) return;

  RTSReplayMessages.messageText.textContent = text;
  applyMessageBranding(command);
  const panelCommand = messagePanelCommand(command);
  clearTimeout(RTSReplayMessages.messageTimer);
  RTSReplayMessages.messageCard.classList.remove('show');
  void RTSReplayMessages.messageCard.offsetWidth;
  RTSInformationPanels.show(
    RTSReplayMessages.messageCard,
    panelCommand,
    panelCommand.replayPanelPosition
  );
  requestAnimationFrame(fitMessageText);
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
  const logo = card.querySelector('#brand-logo');
  const fallback = card.querySelector('#brand-fallback');
  RTSReplayMessages.clapperMessageText.textContent = text;
  const showBranding = command.replayShowClapperBranding !== false;
  card.querySelector('.brand').style.display = showBranding ? '' : 'none';
  const logoUrl = command.replayBrandLogoUrl || command.replayLogoUrl || '';
  card.style.setProperty('--stripe-light', command.replayBrandPrimaryColor || '#eeeeee');
  card.style.setProperty('--stripe-dark', command.replayBrandSecondaryColor || '#111111');
  card.style.setProperty('--accent-color', command.replayBrandPrimaryColor || '#0384cb');
  card.style.setProperty('--message-color', command.replayMessageTextColor || '#0384cb');
  card.style.setProperty('--message-font', command.replayMessageFont || 'Arial, sans-serif');
  const fallbackText = command.replayBrandFallbackText || 'RTS';
  const brandLabel = command.replayBrandLabel || 'ACTION REPLAY';
  fallback.textContent = fallbackText;
  const label = card.querySelector('.brand small');
  if (label) label.textContent = brandLabel;
  fallback.style.color = command.replayBrandFallbackTextColor || command.replayBrandPrimaryColor || '#0384cb';
  if (label) label.style.color = command.replayBrandLabelColor || '#ddd';

  if (logoUrl) {
    logo.onload = () => {
      logo.style.display = 'block';
      fallback.style.display = 'none';
    };
    logo.onerror = () => {
      logo.style.display = 'none';
      fallback.style.display = 'block';
    };
    logo.src = logoUrl;
  } else {
    logo.removeAttribute('src');
    logo.style.display = 'none';
    fallback.style.display = 'block';
  }

  clearTimeout(RTSReplayMessages.clapperboardTimer);
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

  RTSReplayMessages.clapperboardTimer = setTimeout(() => {
    const end = RTSAnimationEngine.readProfile(command.replayClapperAnimation)?.end;
    if (Array.isArray(end) && end.length) {
      runClapper(command, end, () => {
        card.classList.remove('show');
        card.style.opacity = '';
        card.style.visibility = '';
        card.style.zIndex = '';
        card.setAttribute('aria-hidden', 'true');
      }, true);
    } else {
      getClapperRunner(command).cancel();
      card.classList.remove('show');
      card.style.opacity = '';
      card.style.visibility = '';
      card.style.zIndex = '';
      card.setAttribute('aria-hidden', 'true');
    }
  }, Number.isFinite(Number(command?.replayClapperDuration)) ? Number(command.replayClapperDuration) : RTSReplayMessages.config.messageDuration);
};

window.RTSReplayMessages = RTSReplayMessages;
