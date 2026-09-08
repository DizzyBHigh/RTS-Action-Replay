const RTSReplayMessages = window.RTSReplay;

function clamp(value, min, max, fallback) {
  const number = Number(value);
  if (!Number.isFinite(number)) return fallback;
  return Math.min(max, Math.max(min, number));
}

RTSReplayMessages.applyMessageStyle = command => {
  const style = RTSReplayMessages.messageCard.style;
  style.setProperty('--board-color', command.replayMessageBoardColor || '#101416');
  style.setProperty('--stripe-light', command.replayMessageStripeLight || '#EEEEEE');
  style.setProperty('--stripe-dark', command.replayMessageStripeDark || '#111111');
  style.setProperty('--accent-color', command.replayMessageAccent || '#0384CB');
  style.setProperty('--message-color', command.replayMessageTextColor || '#0384CB');
  style.setProperty('--message-font', command.replayMessageFont || 'Arial, sans-serif');
  style.setProperty('--message-scale', clamp(command.replayMessageSize, 50, 150, 100) / 100);
  style.left = `${clamp(command.replayMessagePositionX, 0, 100, 50)}%`;
  style.top = `${clamp(command.replayMessagePositionY, 0, 100, 50)}%`;
};

RTSReplayMessages.showMessage = command => {
  const text = command.replayMessage || '';
  if (!text) return;

  RTSReplayMessages.applyMessageStyle(command);
  RTSReplayMessages.messageText.textContent = text;
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
  RTSReplayMessages.messageCard.classList.remove('show');
  void RTSReplayMessages.messageCard.offsetWidth;
  RTSReplayMessages.messageCard.classList.add('show');
  RTSReplayMessages.messageCard.setAttribute('aria-hidden', 'false');
  RTSReplayMessages.messageTimer = setTimeout(() => {
    RTSReplayMessages.messageCard.classList.remove('show');
    RTSReplayMessages.messageCard.setAttribute('aria-hidden', 'true');
  }, RTSReplayMessages.config.messageDuration);
};
