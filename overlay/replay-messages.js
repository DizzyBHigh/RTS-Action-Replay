const RTSReplayMessages = window.RTSReplay;

function clamp(value, min, max, fallback) {
  const number = Number(value);
  if (!Number.isFinite(number)) return fallback;
  return Math.min(max, Math.max(min, number));
}

function cssColor(value, fallback) {
  const text = String(value || '').trim();
  if (/^#[0-9a-fA-F]{8}$/.test(text)) return `#${text.slice(3)}${text.slice(1, 3)}`;
  if (/^#[0-9a-fA-F]{6}$/.test(text)) return text;
  return fallback;
}

function loadGoogleFont(family) {
  const name = String(family || '').trim();
  if (!name || name === 'Arial, sans-serif') return;
  const id = `rts-google-font-${name.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`;
  if (document.getElementById(id)) return;
  const link = document.createElement('link');
  link.id = id;
  link.rel = 'stylesheet';
  link.href = `https://fonts.googleapis.com/css2?family=${encodeURIComponent(name).replace(/%20/g, '+')}&display=swap`;
  document.head.appendChild(link);
}

RTSReplayMessages.applyMessageStyle = command => {
  const style = RTSReplayMessages.messageCard.style;
  const font = command.replayMessageFont || 'Inter';
  style.setProperty('--board-color', cssColor(command.replayMessageBoardColor, '#101416'));
  style.setProperty('--stripe-light', cssColor(command.replayMessageStripeLight, '#EEEEEE'));
  style.setProperty('--stripe-dark', cssColor(command.replayMessageStripeDark, '#111111'));
  style.setProperty('--accent-color', cssColor(command.replayMessageAccent, '#0384CB'));
  style.setProperty('--message-color', cssColor(command.replayMessageTextColor, '#0384CB'));
  style.setProperty('--message-font', `'${font.replace(/'/g, "\\'")}', Arial, sans-serif`);
  style.setProperty('--message-scale', clamp(command.replayMessageSize, 0, 100, 50) / 100);
  style.left = `${clamp(command.replayMessagePositionX, 0, 100, 50)}%`;
  style.top = `${clamp(command.replayMessagePositionY, 0, 100, 50)}%`;
  loadGoogleFont(font);
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
