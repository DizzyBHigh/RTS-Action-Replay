const RTSReplayOverlay = window.RTSReplay;

RTSReplayOverlay.handleEvent = message => {
  if (message?.event?.source !== 'Custom' || message?.event?.type !== 'Event') return;
  const data = message.data;
  if (data?.eventName !== RTSReplayOverlay.config.eventName || !data.args) return;
  window.RTSDevToolbar?.log?.('Custom replay event accepted', {
    eventName: data.eventName,
    replayCommand: data.args?.replayCommand,
    replayId: data.args?.replayId,
    queueEntryId: data.args?.replayQueueEntryId
  });
  RTSReplayOverlay.handleReplayCommand(data.args);
  if (data.args.replayCommand === 'recent-list') RTSReplayOverlay.showRecentList?.(data.args);
};

window.rtsOverlay = RTSReplayOverlay.config;
window.testReplay = url => RTSReplayOverlay.loadReplay({ replayUrl: url, replayAutoplay: false });
window.testMessage = text => RTSReplayOverlay.showMessage({ replayMessage: text });
RTSReplayOverlay.connect();
