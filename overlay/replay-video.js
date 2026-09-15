const RTSReplayVideo = window.RTSReplay;

const replayDevLog = (message, details) => window.RTSDevToolbar?.log?.(message, details);

let youtubePlayer = null;
let youtubeReady = false;
let youtubeReadyWaiters = [];
let youtubeBoundaryTimer = null;
let youtubeEndedNotified = false;
let youtubeReplayToken = 0;
let hlsPlayer = null;

window.onYouTubeIframeAPIReady = () => {
  youtubeReady = true;
  youtubeReadyWaiters.splice(0).forEach(resolve => resolve());
};

const waitForYouTube = () => {
  if (youtubeReady || window.YT?.Player) return Promise.resolve();
  return new Promise(resolve => youtubeReadyWaiters.push(resolve));
};

const stopYouTubeBoundaryTimer = () => {
  if (youtubeBoundaryTimer) clearInterval(youtubeBoundaryTimer);
  youtubeBoundaryTimer = null;
};

const updateYouTubeControls = command => {
  if (!youtubePlayer || !command) return;
  const start = Number(command.replayStartTime || 0);
  const duration = Number(command.replayDuration || 0);
  const current = Number(youtubePlayer.getCurrentTime?.() || 0);
  RTSReplayControls.updateYouTubeControls?.(Math.max(0, current - start), duration);
};

const notifyEndedOnce = (command, token) => {
  if (token !== youtubeReplayToken || youtubeEndedNotified) return;
  youtubeEndedNotified = true;
  RTSReplayVideo.notifyPlaybackEnded(command);
};

const startYouTubeBoundaryTimer = (command, token) => {
  stopYouTubeBoundaryTimer();
  youtubeBoundaryTimer = setInterval(() => {
    if (token !== youtubeReplayToken) { stopYouTubeBoundaryTimer(); return; }
    if (!youtubePlayer || !command) return;
    updateYouTubeControls(command);
    const current = Number(youtubePlayer.getCurrentTime?.() || 0);
    const end = Number(command.replayStartTime || 0) + Number(command.replayDuration || 0);
    if (end > 0 && current >= end - 0.05) {
      stopYouTubeBoundaryTimer();
      youtubePlayer.pauseVideo();
      replayDevLog('YouTube replay boundary reached', { replayId: command.replayId, currentTime: current, endTime: end });
      notifyEndedOnce(command, token);
    }
  }, 100);
};

const destroyHls = () => {
  if (hlsPlayer) hlsPlayer.destroy();
  hlsPlayer = null;
};

const isHlsUrl = url => /\.m3u8(?:\?|$)/i.test(url || '');

const loadHlsReplay = (url, command) => {
  destroyHls();
  if (window.Hls?.isSupported?.()) {
    hlsPlayer = new Hls({ enableWorker: true });
    hlsPlayer.loadSource(url);
    hlsPlayer.attachMedia(RTSReplayVideo.video);
    hlsPlayer.on(Hls.Events.MANIFEST_PARSED, () => {
      replayDevLog('Kick HLS manifest loaded', { replayId: command.replayId });
      if (command.replayAutoplay) RTSReplayVideo.playReplay(command);
    });
    hlsPlayer.on(Hls.Events.ERROR, (_, data) => replayDevLog('Kick HLS playback error', data));
    return;
  }
  if (RTSReplayVideo.video.canPlayType('application/vnd.apple.mpegurl')) {
    RTSReplayVideo.video.src = url;
    RTSReplayVideo.video.load();
    if (command.replayAutoplay) RTSReplayVideo.video.addEventListener('canplay', () => RTSReplayVideo.playReplay(command), { once: true });
    return;
  }
  replayDevLog('Kick HLS playback unsupported by browser', { replayId: command.replayId });
};

const loadNativeReplay = (url, command) => {
  destroyHls();
  RTSReplayVideo.video.src = url;
  RTSReplayVideo.video.load();
  if (command.replayAutoplay) RTSReplayVideo.video.addEventListener('canplay', () => RTSReplayVideo.playReplay(command), { once: true });
};

const loadYouTubePlayer = async command => {
  const token = ++youtubeReplayToken;
  stopYouTubeBoundaryTimer();
  youtubeEndedNotified = false;
  await waitForYouTube();
  if (token !== youtubeReplayToken) return;
  const videoId = command.replaySourceId;
  if (!videoId) return;
  const host = document.getElementById('youtube-player-host');
  if (!host) return;
  host.classList.add('show');
  host.setAttribute('aria-hidden', 'false');
  RTSReplayVideo.video.style.display = 'none';

  const start = Number(command.replayStartTime || 0);
  const end = start + Number(command.replayDuration || 0);
  const speed = Number(command.replayPlaybackSpeed) || 1;
  const configureQueuedVideo = player => {
    if (token !== youtubeReplayToken) return;
    player.cueVideoById({ videoId, startSeconds: start, endSeconds: end });
  };

  const create = () => {
    youtubePlayer = new YT.Player(host, {
      width: '100%', height: '100%', videoId,
      playerVars: { autoplay: 0, controls: command.replayShowControls !== false ? 1 : 0, playsinline: 1, rel: 0 },
      events: {
        onReady: event => {
          if (token !== youtubeReplayToken) return;
          configureQueuedVideo(event.target);
          updateYouTubeControls(command);
          replayDevLog('YouTube player ready', { replayId: command.replayId, videoId, startTime: start, duration: command.replayDuration });
          RTSReplayVideo.confirmPlayback(command.replayId, command.replayUserId, command.replayUserName);
        },
        onStateChange: event => {
          if (token !== youtubeReplayToken) return;
          if (event.data === YT.PlayerState.CUED) {
            event.target.setPlaybackRate(speed);
            updateYouTubeControls(command);
            if (command.replayAutoplay) { event.target.playVideo(); startYouTubeBoundaryTimer(command, token); }
          }
          if (event.data === YT.PlayerState.PLAYING) startYouTubeBoundaryTimer(command, token);
          if (event.data === YT.PlayerState.ENDED) { stopYouTubeBoundaryTimer(); updateYouTubeControls(command); notifyEndedOnce(command, token); }
        },
        onError: event => {
          if (token === youtubeReplayToken) replayDevLog('YouTube player error', { replayId: command.replayId, error: event.data });
        }
      }
    });
  };

  if (youtubePlayer?.cueVideoById) {
    configureQueuedVideo(youtubePlayer);
  } else create();
};

RTSReplayVideo.confirmPlayback = (replayId, userId, userName) => {
  if (!replayId || !RTSReplayVideo.socket || RTSReplayVideo.socket.readyState !== WebSocket.OPEN) return;
  RTSReplayVideo.socket.send(JSON.stringify({ request: 'DoAction', id: `rts-replay-confirm-${Date.now()}`, action: { name: RTSReplayVideo.config.confirmAction }, args: { replayId, userId: userId || '', userName: userName || '' } }));
};

RTSReplayVideo.notifyPlaybackEnded = command => {
  if (!command?.replayId || !RTSReplayVideo.socket || RTSReplayVideo.socket.readyState !== WebSocket.OPEN) return;
  replayDevLog('sending playback ended action', { action: RTSReplayVideo.config.endedAction, replayId: command.replayId, replayQueueEntryId: command.replayQueueEntryId || '' });
  RTSReplayVideo.socket.send(JSON.stringify({ request: 'DoAction', id: `rts-replay-ended-${Date.now()}`, action: { name: RTSReplayVideo.config.endedAction }, args: { replayId: command.replayId, replayQueueEntryId: command.replayQueueEntryId || '' } }));
};

RTSReplayVideo.playReplay = command => {
  if (command?.replaySource?.toLowerCase() === 'youtube') {
    if (youtubePlayer?.playVideo) { youtubePlayer.playVideo(); startYouTubeBoundaryTimer(command, youtubeReplayToken); }
    return;
  }
  RTSReplayVideo.video.play().then(() => RTSReplayVideo.confirmPlayback(command.replayId, command.replayUserId, command.replayUserName)).catch(error => console.warn('Replay play failed', error));
};

RTSReplayVideo.loadReplay = command => {
  const isYouTube = command?.replaySource?.toLowerCase() === 'youtube';
  if (!isYouTube && !command.replayUrl) return;
  RTSReplayVideo.currentCommand = command;
  window.RTSDevToolbar?.updateClapper?.(command);
  RTSReplayControls.configure(command);
  RTSReplayElements.configure(command);

  const profile = RTSReplayAnimation.readProfile(command);
  const startSequence = Array.isArray(profile?.start) ? profile.start : [];
  const startName = command.replayStartPosition || command.replayPosition || 'Full Screen';
  const endName = command.replayEndPosition || startName;
  const startPosition = startSequence.length ? RTSReplayAnimation.getPosition(startSequence[0].position) : RTSReplayVideo.getPosition(startName);
  const endPosition = startSequence.length ? RTSReplayAnimation.getPosition(startSequence[startSequence.length - 1].position) : RTSReplayVideo.getPosition(endName);

  if (isYouTube) {
    destroyHls();
    RTSReplayVideo.video.style.display = 'none';
    RTSReplayVideo.visiblePosition = endPosition;
    RTSReplayVideo.player.classList.add('show');
    if (startSequence.length) RTSReplayAnimation.runSequence(startSequence); else RTSReplayVideo.applyPosition(startPosition, true);
    loadYouTubePlayer(command);
    return;
  }

  ++youtubeReplayToken;
  stopYouTubeBoundaryTimer();
  const host = document.getElementById('youtube-player-host');
  host?.classList.remove('show');
  host?.setAttribute('aria-hidden', 'true');
  RTSReplayVideo.video.style.display = 'block';
  RTSReplayVideo.visiblePosition = endPosition;
  const alreadyVisible = RTSReplayVideo.player.classList.contains('show');
  if (alreadyVisible) { RTSReplayAnimation.cancelSequence(); RTSReplayVideo.applyPosition(endPosition, true); RTSReplayVideo.activePosition = endPosition; }
  else if (startSequence.length) { RTSReplayAnimation.runSequence(startSequence); RTSReplayVideo.player.classList.add('show'); }
  else RTSReplayVideo.animateIn(startPosition, endPosition);
  if (isHlsUrl(command.replayUrl)) loadHlsReplay(command.replayUrl, command); else loadNativeReplay(command.replayUrl, command);
};

RTSReplayVideo.testTitle = command => {
  const params = new URLSearchParams(window.location.search); if (params.get('dev') !== 'true') return;
  RTSReplayVideo.currentCommand = command; RTSReplayControls.configure(command); RTSReplayElements.configure(command); RTSReplayVideo.player.classList.add('dev-player', 'show'); RTSReplayVideo.player.style.opacity = '1'; RTSReplayVideo.player.style.visibility = 'visible'; RTSReplayVideo.frame?.classList.add('dev-frame'); window.RTSDevToolbar?.refreshPositions?.();
};

RTSReplayVideo.moveReplay = command => {
  RTSReplayVideo.currentCommand = { ...(RTSReplayVideo.currentCommand || {}), ...command };
  const position = RTSReplayVideo.getPosition(command.replayPosition || 'Full Screen'); const current = RTSReplayVideo.activePosition;
  if (current && RTSReplayVideo.positionsEqual?.(current, position)) return;
  RTSReplayVideo.player.classList.add('show');
  if (!current) { RTSReplayVideo.applyPosition(position, true); RTSReplayVideo.activePosition = position; return; }
  RTSReplayVideo.animatePosition(current, position, () => { RTSReplayVideo.activePosition = position; });
};

RTSReplayVideo.showPlayer = () => {
  const command = RTSReplayVideo.currentCommand || {};
  const target = RTSReplayVideo.visiblePosition || RTSReplayVideo.activePosition || RTSReplayVideo.getPosition('Full Screen');
  RTSReplayVideo.player.classList.add('show'); RTSReplayVideo.applyPosition(target, true); RTSReplayVideo.activePosition = target; RTSReplayVideo.playReplay(command);
};

RTSReplayVideo.hideReplay = () => {
  const command = RTSReplayVideo.currentCommand || {};
  RTSReplayAnimation.cancelSequence(); stopYouTubeBoundaryTimer();
  if (command.replaySource?.toLowerCase() === 'youtube') { youtubePlayer?.pauseVideo?.(); } else RTSReplayVideo.video.pause();
  RTSReplayVideo.visiblePosition = RTSReplayVideo.activePosition;
  const profile = RTSReplayAnimation.readProfile(command);
  if (Array.isArray(profile?.end) && profile.end.length) { RTSReplayAnimation.runEndSequence(profile.end, () => RTSReplayVideo.player.classList.remove('show')); return; }
  RTSReplayVideo.animateOut();
};

RTSReplayVideo.handleReplayCommand = command => {
  if (command.replayCommand === 'message') RTSReplayVideo.showMessage(command);
  if (command.replayCommand === 'load') RTSReplayVideo.loadReplay(command);
  if (command.replayCommand === 'title-test') RTSReplayVideo.testTitle(command);
  if (command.replayCommand === 'play') RTSReplayVideo.playReplay(command);
  if (command.replayCommand === 'pause') command.replaySource?.toLowerCase() === 'youtube' ? youtubePlayer?.pauseVideo?.() : RTSReplayVideo.video.pause();
  if (command.replayCommand === 'speed') {
    const speed = Math.max(0.25, Math.min(4, Number(command.replayPlaybackSpeed) || 1));
    if (command.replaySource?.toLowerCase() === 'youtube') youtubePlayer?.setPlaybackRate?.(speed); else RTSReplayVideo.video.playbackRate = speed;
  }
  if (command.replayCommand === 'move') RTSReplayVideo.moveReplay(command);
  if (command.replayCommand === 'hide') RTSReplayVideo.hideReplay(command);
  if (command.replayCommand === 'show') RTSReplayVideo.showPlayer(command);
  if (command.replayCommand === 'stop') { if (command.replaySource?.toLowerCase() === 'youtube') youtubePlayer?.stopVideo?.(); else { RTSReplayVideo.video.pause(); RTSReplayVideo.video.currentTime = 0; } }
  if (command.replayCommand === 'replay') { if (command.replaySource?.toLowerCase() === 'youtube') { youtubeEndedNotified = false; youtubePlayer?.seekTo?.(Number(command.replayStartTime || 0), true); RTSReplayVideo.playReplay(command); } else { RTSReplayVideo.video.currentTime = 0; RTSReplayVideo.playReplay(command); } }
};

RTSReplayVideo.video.addEventListener('ended', () => {
  const command = RTSReplayVideo.currentCommand;
  if (command?.replaySource?.toLowerCase() === 'youtube') return;
  if (command) RTSReplayVideo.notifyPlaybackEnded(command);
});
