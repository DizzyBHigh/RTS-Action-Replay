const RTSReplayOverlay = window.RTSReplay;

RTSReplayOverlay.handleEvent = message => {
  if (message?.event?.source !== 'Custom' || message?.event?.type !== 'Event') return;
  const data = message.data;
  if (data?.eventName !== RTSReplayOverlay.config.eventName || !data.args) return;
  RTSReplayOverlay.handleReplayCommand(data.args);
};

window.rtsOverlay = RTSReplayOverlay.config;
window.testReplay = url => RTSReplayOverlay.loadReplay({ replayUrl: url, replayAutoplay: false });
window.testMessage = text => RTSReplayOverlay.showMessage({ replayMessage: text });
RTSReplayOverlay.connect();
