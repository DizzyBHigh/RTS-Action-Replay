const RTSReplayMessages = window.RTSReplay;

function clamp(value, min, max, fallback) {
  const number = Number(value);
  if (!Number.isFinite(number)) return fallback;
  return Math.min(max, Math.max(min, number));
}

function cssColor(value, fallback) {
  const color = String(value || fallback).trim();
  if (/^#[0-9a-fA-F]{8}$/.test(color)) {
    // RtsUI stores colours as #AARRGGBB; CSS uses #RRGGBBAA.
    return `#${color.slice(3)}${color.slice(1, 3)}`;
  }
  return color;
}

RTSReplayMessages.applyMessageStyle = command => {
  const style = RTSReplayMessages.messageCard.style;
  style.setProperty('--board-color', cssColor(command.replayMessageBoardColor, '#101416'));
  style.setProperty('--stripe-light', cssColor(command.replayMessageStripeLight, '#EEEEEE'));
  style.setProperty('--stripe-dark', cssColor(command.replayMessageStripeDark, '#111111'));
  style.setProperty('--accent-color', cssColor(command.replayMessageAccent, '#0384CB'));
  style.setProperty('--message-color', cssColor(command.replayMessageTextColor, '#0384CB'));
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
