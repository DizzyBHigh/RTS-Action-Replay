const RTSReplay = window.RTSReplay || {};

RTSReplay.config = {
  host: '127.0.0.1',
  port: 8080,
  eventName: 'RTS-Action Replay',
  confirmAction: 'RTS Action Replay - Playback Confirm',
  reconnectDelay: 3000,
  messageDuration: 5000
};

RTSReplay.status = document.getElementById('status');
RTSReplay.messageCard = document.getElementById('message-card');
RTSReplay.messageText = document.getElementById('message-text');
RTSReplay.brandLogo = document.getElementById('brand-logo');
RTSReplay.brandFallback = document.getElementById('brand-fallback');
RTSReplay.video = document.getElementById('video');
RTSReplay.socket = null;
RTSReplay.reconnectTimer = null;
RTSReplay.messageTimer = null;

window.RTSReplay = RTSReplay;
