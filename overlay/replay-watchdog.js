const RTSReplayWatchdog = window.RTSReplayWatchdog || {};

const CHECK_INTERVAL = 1000;
const STALL_AFTER = 5000;
const YOUTUBE_STALL_AFTER = 8000;
const STARTUP_GRACE = 10000;
const RECOVERY_COOLDOWN = 3000;
const MAX_RECOVERY_ATTEMPTS = 2;

let timer = null;
let command = null;
let lastPosition = 0;
let lastProgressAt = 0;
let startedAt = 0;
let recoveringUntil = 0;
let recoveryAttempts = 0;

const reset = active => {
  if (command === active) return;
  command = active || null;
  lastPosition = 0;
  lastProgressAt = Date.now();
  startedAt = Date.now();
  recoveringUntil = 0;
  recoveryAttempts = 0;
};

const fail = active => {
  window.RTSDevToolbar?.log?.('Playback watchdog exhausted recovery attempts', {
    replayId: active?.replayId,
    replayQueueEntryId: active?.replayQueueEntryId || '',
    attempts: recoveryAttempts
  });
  RTSReplayVideo.notifyPlaybackEnded(active);
};

const recover = active => {
  if (recoveryAttempts >= MAX_RECOVERY_ATTEMPTS) {
    fail(active);
    return;
  }
  recoveryAttempts += 1;
  recoveringUntil = Date.now() + RECOVERY_COOLDOWN;
  window.RTSDevToolbar?.log?.('Playback watchdog recovering stalled replay', {
    replayId: active?.replayId,
    replayQueueEntryId: active?.replayQueueEntryId || '',
    attempt: recoveryAttempts,
    maxAttempts: MAX_RECOVERY_ATTEMPTS
  });
  RTSReplayVideo.recoverPlayback?.();
  lastProgressAt = Date.now();
};

const sample = () => {
  const active = RTSReplayVideo.currentCommand;
  const visible = RTSReplayVideo.player?.classList.contains('show');
  if (!active || !visible) { reset(null); return; }
  if (active !== command) reset(active);
  if (Date.now() < recoveringUntil) return;

  const source = String(active.replaySource || '').toLowerCase();
  let position = 0;
  let playing = false;
  let stalled = false;
  let threshold = STALL_AFTER;

  if (source === 'youtube') {
    const player = RTSReplayVideo.youtubePlayer;
    const state = player?.getPlayerState?.();
    position = Number(player?.getCurrentTime?.() || 0);
    playing = !!active.replayAutoplay && RTSReplayVideo.expectedPlaying === true && state === 1;
    stalled = RTSReplayVideo.expectedPlaying === true && state !== 1;
    threshold = YOUTUBE_STALL_AFTER;
  } else {
    const video = RTSReplayVideo.video;
    position = Number(video?.currentTime || 0);
    playing = !!video && RTSReplayVideo.expectedPlaying === true && !video.paused && !video.ended;
    stalled = !!video && RTSReplayVideo.expectedPlaying === true && (video.paused || video.ended || video.readyState < 3 || video.networkState === 2 || video.seeking);
  }

  if (position > lastPosition + 0.05) {
    lastPosition = position;
    lastProgressAt = Date.now();
    recoveryAttempts = 0;
    return;
  }

  if (!playing) return;
  if (Date.now() - startedAt < STARTUP_GRACE) return;
  if (!stalled && Date.now() - lastProgressAt < threshold) return;
  recover(active);
};

RTSReplayWatchdog.start = () => {
  if (timer) return;
  timer = setInterval(sample, CHECK_INTERVAL);
};

RTSReplayWatchdog.stop = () => {
  if (timer) clearInterval(timer);
  timer = null;
  reset(null);
};

window.RTSReplayWatchdog = RTSReplayWatchdog;
RTSReplayWatchdog.start();
