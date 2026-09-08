const RTSReplay = window.RTSReplay;

function clamp(value, min, max, fallback) {
  const number = Number(value);
  if (!Number.isFinite(number)) return fallback;
  return Math.min(max, Math.max(min, number));
}

RTSReplay.applyMessageStyle = command => {
  const style = RTSReplay.messageCard.style;
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

RTSReplay.showMessage = command => {
  const text = command.replayMessage || '';
  if (!text) return;

  RTSReplay.applyMessageStyle(command);
  RTSReplay.messageText.textContent = text;
  const logoUrl = command.replayLogoUrl || '';
  if (logoUrl) {
    RTSReplay.brandLogo.onload = () => {
      RTSReplay.brandLogo.style.display = 'block';
      RTSReplay.brandFallback.style.display = 'none';
    };
    RTSReplay.brandLogo.onerror = () => {
      RTSReplay.brandLogo.style.display = 'none';
      RTSReplay.brandFallback.style.display = 'block';
    };
    RTSReplay.brandLogo.src = logoUrl;
  } else {
    RTSReplay.brandLogo.removeAttribute('src');
    RTSReplay.brandLogo.style.display = 'none';
    RTSReplay.brandFallback.style.display = 'block';
  }

  clearTimeout(RTSReplay.messageTimer);
  RTSReplay.messageCard.classList.remove('show');
  void RTSReplay.messageCard.offsetWidth;
  RTSReplay.messageCard.classList.add('show');
  RTSReplay.messageCard.setAttribute('aria-hidden', 'false');
  RTSReplay.messageTimer = setTimeout(() => {
    RTSReplay.messageCard.classList.remove('show');
    RTSReplay.messageCard.setAttribute('aria-hidden', 'true');
  }, RTSReplay.config.messageDuration);
};
