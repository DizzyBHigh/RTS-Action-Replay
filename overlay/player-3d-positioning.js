const RTSReplayPlayer3D = window.RTSReplay;

// Keep X/Y in the same 3D transform as Z so perspective affects all three
// coordinates together, matching the RtsUI position preview.
RTSReplayPlayer3D.transformFor = (p, scaleFactor = 1) => {
  const legacyScale = RTSReplayPlayer3D.numberOr(p?.scale, 100) / 100;
  const scaleX = RTSReplayPlayer3D.numberOr(p?.scaleX, legacyScale * 100) / 100 * scaleFactor;
  const scaleY = RTSReplayPlayer3D.numberOr(p?.scaleY, legacyScale * 100) / 100 * scaleFactor;
  const x = RTSReplayPlayer3D.numberOr(p?.x, 0);
  const y = RTSReplayPlayer3D.numberOr(p?.y, 0);
  const z = RTSReplayPlayer3D.numberOr(p?.z, 0);
  const rotateX = -RTSReplayPlayer3D.numberOr(p?.rotateX, 0);
  const rotateY = RTSReplayPlayer3D.numberOr(p?.rotateY, 0);
  const rotateZ = -RTSReplayPlayer3D.numberOr(p?.rotateZ, 0);
  const fov = Math.max(30, Math.min(120, RTSReplayPlayer3D.numberOr(p?.fov, 90)));

  const viewportWidth = Math.max(1, window.innerWidth || 1920);
  const perspective = Math.max(1, (viewportWidth / 2) / Math.tan((fov * Math.PI / 180) / 2));
  RTSReplayPlayer3D.stage.style.perspective = `${perspective}px`;
  RTSReplayPlayer3D.stage.style.perspectiveOrigin = 'center center';
  RTSReplayPlayer3D.stage.style.left = '50%';
  RTSReplayPlayer3D.stage.style.top = '50%';

  // RtsUI coordinates use +Y as up; CSS screen coordinates use +Y down.
  // Keep X/Y/Z as one world-space translation so Z changes the projected
  // X/Y position naturally through the perspective camera.
  return `translate(-50%, -50%) translate3d(${x}vw, ${-y}vh, ${z}px) rotateZ(${rotateZ}deg) rotateY(${rotateY}deg) rotateX(${rotateX}deg) scale3d(${scaleX}, ${scaleY}, 1)`;
};
