const RTSReplay = window.RTSReplay;

RTSReplay.handleEvent = message => {
  if (message?.event?.source !== 'Custom' || message?.event?.type !== 'Event') return;
  const data = message.data;
  if (data?.eventName !== RTSReplay.config.eventName || !data.args) return;
  RTSReplay.handleReplayCommand(data.args);
};

window.rtsOverlay = RTSReplay.config;
window.testReplay = url => RTSReplay.loadReplay({ replayUrl: url, replayAutoplay: false });
window.testMessage = text => RTSReplay.showMessage({ replayMessage: text });
RTSReplay.connect();
