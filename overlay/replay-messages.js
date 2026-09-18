const RTSReplayMessages = window.RTSReplay;
let clapperRunner = null;

const getClapperRunner = command => {
  if (!clapperRunner) clapperRunner = RTSAnimationEngine.createRunner({
    target: RTSReplayMessages.messageCard,
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
  const runner = getClapperRunner(command);
  runner.apply(runner.resolve(command.replayClapperPosition || 'Centered'));
  loadGoogleFont(font);
};

RTSReplayMessages.showMessage = command => {
  const text = command.replayMessage || '';
  if (!text) return;

  RTSReplayMessages.applyMessageStyle(command);
  RTSReplayMessages.messageText.textContent = text;
  const showBranding = command.replayShowClapperBranding !== false;
  RTSReplayMessages.messageCard.querySelector('.brand').style.display = showBranding ? '' : 'none';
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
  RTSReplayMessages.messageCard.classList.remove('show');
  void RTSReplayMessages.messageCard.offsetWidth;
  RTSReplayMessages.messageCard.classList.add('show');
  RTSReplayMessages.messageCard.setAttribute('aria-hidden', 'false');
  if (Array.isArray(start) && start.length) {
    runClapper(command, start, () => {
      const stick = RTSReplayMessages.messageCard.querySelector('.clapstick');
      if (stick) { stick.classList.remove('clap'); void stick.offsetWidth; stick.classList.add('clap'); }
    });
  } else {
    const stick = RTSReplayMessages.messageCard.querySelector('.clapstick');
    if (stick) { stick.classList.remove('clap'); void stick.offsetWidth; stick.classList.add('clap'); }
  }
  RTSReplayMessages.messageTimer = setTimeout(() => {
    const profile = RTSAnimationEngine.readProfile(command.replayClapperAnimation);
    const end = profile?.end;
    if (Array.isArray(end) && end.length) {
      runClapper(command, end, () => {
        RTSReplayMessages.messageCard.classList.remove('show');
        RTSReplayMessages.messageCard.setAttribute('aria-hidden', 'true');
      }, true);
    } else {
      getClapperRunner(command).cancel();
      RTSReplayMessages.messageCard.classList.remove('show');
      RTSReplayMessages.messageCard.setAttribute('aria-hidden', 'true');
    }
  }, RTSReplayMessages.config.messageDuration);
};
