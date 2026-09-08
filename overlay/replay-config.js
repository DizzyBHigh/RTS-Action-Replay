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
RTSReplay.player = document.getElementById('replay-player');
RTSReplay.frame = document.getElementById('player-frame');
RTSReplay.video = document.getElementById('video');
RTSReplay.controls = document.getElementById('player-controls');
RTSReplay.state = document.getElementById('player-state');
RTSReplay.progress = document.getElementById('player-progress');
RTSReplay.progressBar = document.getElementById('player-progress-bar');
RTSReplay.time = document.getElementById('player-time');
RTSReplay.speed = document.getElementById('player-speed');
RTSReplay.messageCard = document.getElementById('message-card');
RTSReplay.messageText = document.getElementById('message-text');
RTSReplay.brandLogo = document.getElementById('brand-logo');
RTSReplay.brandFallback = document.getElementById('brand-fallback');
RTSReplay.socket = null;
RTSReplay.reconnectTimer = null;
RTSReplay.messageTimer = null;
RTSReplay.currentCommand = null;
RTSReplay.activePosition = null;

window.RTSReplay = RTSReplay;
