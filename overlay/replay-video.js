const RTSReplayVideo = window.RTSReplay;

const replayDevLog = (message, details) => window.RTSDevToolbar?.log?.(message, details);

let youtubePlayer = null;
let youtubeReady = false;
let youtubeReadyWaiters = [];
let youtubeBoundaryTimer = null;
let youtubeEndedNotified = false;
let youtubeReplayToken = 0;
let hlsPlayer = null;
let endedCommand = null;
const playerRunner = RTSAnimationEngine.createRunner({
  target: RTSReplayVideo.player,
  defaultPosition: { scale: 100, x: 0, y: 0, z: 0, rotateX: 0, rotateY: 0, rotateZ: 0, fov: 90 }
});

RTSReplayVideo.configurePositions = raw => playerRunner.configure(raw);
RTSReplayVideo.getPosition = name => playerRunner.resolve(name);
RTSReplayVideo.positionsEqual = (a, b) => RTSAnimationEngine.positionsEqual(a, b);
RTSReplayVideo.applyPosition = (position, immediate = false) => {
  if (immediate) playerRunner.cancel();
  playerRunner.apply(position);
  RTSReplayVideo.activePosition = position;
};
RTSReplayVideo.animatePosition = (start, end, complete) => {
  const rawDuration = Number(RTSReplayVideo.currentCommand?.replayAnimationDuration);
  const duration = Math.max(100, Number.isFinite(rawDuration) ? (rawDuration < 10 ? rawDuration * 1000 : rawDuration) : 500);
  playerRunner.transition(start, end, duration, RTSReplayVideo.currentCommand?.replayAnimationEasing, () => {
    RTSReplayVideo.activePosition = end;
    complete?.();
  });
};
RTSReplayVideo.animateIn = (start, end) => {
  RTSReplayVideo.player.classList.add('show');
  RTSReplayVideo.applyPosition(start || end, true);
  if (RTSReplayVideo.positionsEqual(start || end, end)) return;
  RTSReplayVideo.animatePosition(start || end, end);
};
RTSReplayVideo.runAnimationProfile = command => {
  const profile = RTSAnimationEngine.readProfile(command?.replayAnimationProfile);
  if (Array.isArray(profile?.start) && profile.start.length) playerRunner.run(profile.start);
};
RTSReplayVideo.runEndAnimationProfile = (command, complete) => {
  const profile = RTSAnimationEngine.readProfile(command?.replayAnimationProfile);
  if (Array.isArray(profile?.end) && profile.end.length) playerRunner.runEnd(profile.end, complete);
  else complete?.();
};
RTSReplayVideo.cancelAnimation = () => playerRunner.cancel();

RTSReplayVideo.animateOut = () => {
  const start = RTSReplayVideo.activePosition || RTSReplayVideo.getPosition('Full Screen');
  const end = RTSReplayVideo.getPosition(RTSReplayVideo.currentCommand?.replayStartPosition || 'Full Screen');
  if (RTSReplayVideo.positionsEqual(start, end)) {
    RTSReplayVideo.player.classList.remove('show');
    RTSReplayVideo.activePosition = end;
    return;
  }
  RTSReplayVideo.animatePosition(start, end, () => RTSReplayVideo.player.classList.remove('show'));
};

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
    replayDevLog('YouTube replay cued', { replayId: command.replayId, startTime: start, duration: command.replayDuration });
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
          const activeCommand = RTSReplayVideo.currentCommand || command;
          if (token !== youtubeReplayToken) return;
          if (event.data === YT.PlayerState.CUED) {
            event.target.setPlaybackRate(speed);
            updateYouTubeControls(activeCommand);
            if (activeCommand.replayAutoplay) { event.target.playVideo(); startYouTubeBoundaryTimer(activeCommand, token); }
          }
          if (event.data === YT.PlayerState.PLAYING) {
            replayDevLog('YouTube replay playing', { replayId: activeCommand.replayId, currentTime: Number(event.target.getCurrentTime?.() || 0) });
            startYouTubeBoundaryTimer(activeCommand, token);
          }
          if (event.data === YT.PlayerState.ENDED) {
            stopYouTubeBoundaryTimer();
            updateYouTubeControls(activeCommand);
            replayDevLog('YouTube replay ended', { replayId: activeCommand.replayId });
            notifyEndedOnce(activeCommand, token);
          }
        },
        onError: event => {
          if (token === youtubeReplayToken) replayDevLog('YouTube player error', { replayId: command.replayId, error: event.data });
        }
      }
    });
    RTSReplayVideo.youtubePlayer = youtubePlayer;
  };

  if (youtubePlayer?.cueVideoById) {
    replayDevLog('YouTube player reused', { replayId: command.replayId, videoId, startTime: start, duration: command.replayDuration });
    configureQueuedVideo(youtubePlayer);
  } else create();
};


RTSReplayVideo.notifyPlaybackEnded = command => {
  if (!command?.replayId || endedCommand === command) return;
  if (!RTSReplayVideo.socket || RTSReplayVideo.socket.readyState !== WebSocket.OPEN) return;
  endedCommand = command;
  replayDevLog('sending playback ended action', { action: RTSReplayVideo.config.endedAction, replayId: command.replayId, replayQueueEntryId: command.replayQueueEntryId || '' });
  RTSReplayVideo.socket.send(JSON.stringify({ request: 'DoAction', id: `rts-replay-ended-${Date.now()}`, action: { name: RTSReplayVideo.config.endedAction }, args: { replayId: command.replayId, replayQueueEntryId: command.replayQueueEntryId || '' } }));
};

RTSReplayVideo.recoverPlayback = () => {
  const command = RTSReplayVideo.currentCommand;
  if (!command) return false;
  if (command.replaySource?.toLowerCase() === 'youtube') {
    if (!youtubePlayer?.seekTo || !youtubePlayer?.playVideo) return false;
    const current = Number(youtubePlayer.getCurrentTime?.() || command.replayStartTime || 0);
    youtubePlayer.seekTo(Math.max(Number(command.replayStartTime || 0), current - 0.5), true);
    youtubePlayer.playVideo();
    return true;
  }
  const video = RTSReplayVideo.video;
  const current = Number(video.currentTime || 0);
  if (hlsPlayer) {
    hlsPlayer.stopLoad?.();
    hlsPlayer.startLoad?.(Math.max(0, current - 0.5));
    video.play().catch(() => {});
    return true;
  }
  const url = video.currentSrc || command.replayUrl;
  if (!url) return false;
  video.pause();
  video.src = url;
  video.load();
  const resume = () => {
    try { video.currentTime = Math.max(0, current - 0.5); } catch (_) {}
    video.play().catch(() => {});
  };
  if (video.readyState >= 1) resume();
  else video.addEventListener('loadedmetadata', resume, { once: true });
  return true;
};

RTSReplayVideo.playReplay = command => {
  if (command?.replaySource?.toLowerCase() === 'youtube') {
    if (youtubePlayer?.playVideo) { youtubePlayer.playVideo(); startYouTubeBoundaryTimer(command, youtubeReplayToken); }
    return;
  }
  RTSReplayVideo.video.play().catch(error => console.warn('Replay play failed', error));
};

RTSReplayVideo.loadReplay = command => {
  const isYouTube = command?.replaySource?.toLowerCase() === 'youtube';
  if (!isYouTube && !command.replayUrl) return;
  RTSReplayVideo.currentCommand = command;
  endedCommand = null;
  window.RTSDevToolbar?.updateClapper?.(command);
  RTSReplayControls.configure(command);
  RTSReplayElements.configure(command);

  playerRunner.configure(command.replayPlayerPositions);
  const profile = RTSAnimationEngine.readProfile(command.replayAnimationProfile);
  const startSequence = Array.isArray(profile?.start) ? profile.start : [];
  const startName = command.replayStartPosition || command.replayPosition || 'Full Screen';
  const endName = command.replayEndPosition || startName;
  const startPosition = startSequence.length ? playerRunner.resolve(startSequence[0].position) : RTSReplayVideo.getPosition(startName);
  const endPosition = startSequence.length ? playerRunner.resolve(startSequence[startSequence.length - 1].position) : RTSReplayVideo.getPosition(endName);
  const alreadyVisible = RTSReplayVideo.player.classList.contains('show');

  if (isYouTube) {
    destroyHls();
    RTSReplayVideo.video.style.display = 'none';
    RTSReplayVideo.visiblePosition = endPosition;
    RTSReplayVideo.player.classList.add('show');
    if (alreadyVisible) {
      playerRunner.cancel();
      RTSReplayVideo.applyPosition(endPosition, true);
      RTSReplayVideo.activePosition = endPosition;
      replayDevLog('YouTube replay loaded while player already visible', { replayId: command.replayId, position: endPosition.name || endName });
    } else if (startSequence.length) {
      playerRunner.run(startSequence);
    } else {
      RTSReplayVideo.applyPosition(startPosition, true);
      RTSReplayVideo.activePosition = startPosition;
    }
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
  if (alreadyVisible) { playerRunner.cancel(); RTSReplayVideo.applyPosition(endPosition, true); RTSReplayVideo.activePosition = endPosition; }
  else if (startSequence.length) { playerRunner.run(startSequence); RTSReplayVideo.player.classList.add('show'); }
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
  playerRunner.cancel(); stopYouTubeBoundaryTimer();
  if (command.replaySource?.toLowerCase() === 'youtube') { youtubePlayer?.pauseVideo?.(); } else RTSReplayVideo.video.pause();
  RTSReplayVideo.visiblePosition = RTSReplayVideo.activePosition;
  const profile = RTSAnimationEngine.readProfile(command.replayAnimationProfile);
  if (Array.isArray(profile?.end) && profile.end.length) { playerRunner.runEnd(profile.end, () => RTSReplayVideo.player.classList.remove('show')); return; }
  RTSReplayVideo.animateOut();
};

RTSReplayVideo.handleReplayCommand = command => {
  const activeCommand = { ...(RTSReplayVideo.currentCommand || {}), ...(command || {}) };
  if (command.replayCommand === 'message') RTSReplayVideo.showMessage(activeCommand);
  if (command.replayCommand === 'load') RTSReplayVideo.loadReplay(command);
  if (command.replayCommand === 'title-test') RTSReplayVideo.testTitle(command);
  if (command.replayCommand === 'play') RTSReplayVideo.playReplay(activeCommand);
  if (command.replayCommand === 'pause') activeCommand.replaySource?.toLowerCase() === 'youtube' ? youtubePlayer?.pauseVideo?.() : RTSReplayVideo.video.pause();
  if (command.replayCommand === 'speed') {
    const speed = Math.max(0.25, Math.min(4, Number(command.replayPlaybackSpeed ?? activeCommand.replayPlaybackSpeed) || 1));
    if (activeCommand.replaySource?.toLowerCase() === 'youtube') youtubePlayer?.setPlaybackRate?.(speed); else RTSReplayVideo.video.playbackRate = speed;
  }
  if (command.replayCommand === 'move') RTSReplayVideo.moveReplay(activeCommand);
  if (command.replayCommand === 'hide') RTSReplayVideo.hideReplay(activeCommand);
  if (command.replayCommand === 'show') RTSReplayVideo.showPlayer(activeCommand);
  if (command.replayCommand === 'stop') { if (activeCommand.replaySource?.toLowerCase() === 'youtube') youtubePlayer?.stopVideo?.(); else { RTSReplayVideo.video.pause(); RTSReplayVideo.video.currentTime = 0; } }
  if (command.replayCommand === 'replay') { if (activeCommand.replaySource?.toLowerCase() === 'youtube') { youtubeEndedNotified = false; youtubePlayer?.seekTo?.(Number(activeCommand.replayStartTime || 0), true); RTSReplayVideo.playReplay(activeCommand); } else { RTSReplayVideo.video.currentTime = 0; RTSReplayVideo.playReplay(activeCommand); } }
};

RTSReplayVideo.video.addEventListener('ended', () => {
  const command = RTSReplayVideo.currentCommand;
  if (command?.replaySource?.toLowerCase() === 'youtube') return;
  if (command) RTSReplayVideo.notifyPlaybackEnded(command);
});
