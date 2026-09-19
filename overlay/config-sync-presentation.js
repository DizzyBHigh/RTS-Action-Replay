(() => {
  const list = value => Array.isArray(value) ? value : [];
  const find = (items, id) => list(items).find(item =>
    String(item?.id || '').toLowerCase() === String(id || '').toLowerCase()
  ) || null;

  const branding = (config, id) => find(config?.presets?.branding, id) || find(config?.presets?.branding, 'default') || {};
  const visual = (config, id) => find(config?.presets?.visual, id) || find(config?.presets?.visual, 'broadcast') || {};
  const title = (config, id) => find(config?.presets?.title, id) || find(config?.presets?.title, 'default') || {};

  const apply = (command, config, entry) => {
    const d = visual(config, entry?.designPreset || entry?.visualPreset || 'broadcast');
    const t = title(config, entry?.titlePreset || 'default');
    const b = branding(config, entry?.brandingPreset || 'default');

    Object.assign(command, {
      replayPanelPreset: d.design || d.id || 'broadcast',
      replayPanelPrimaryColor: b.primaryColor || '#0384CBFF',
      replayPanelSecondaryColor: b.secondaryColor || '#101416FF',
      replayPanelTitleFont: b.font || 'Inter',
      replayPanelTitleSize: b.fontSize || 34,
      replayPanelTitleColor: b.textColor || '#FFFFFFFF',
      replayPanelListColor: b.textColor || '#FFFFFFFF',
      replayPanelListShadowColor: b.shadowColor || '#000000FF',
      replayPanelBackgroundColor: d.backgroundColor || b.secondaryColor || '#101416FF',
      replayShowTitle: t.showTitle !== false,
      replayTitleDecorationPosition: t.decorationPosition || 'Prefix',
      replayTitleDecoration: t.decoration || 'Action Replay -',
      replayTitlePosition: t.position || 'Bottom',
      replayTitleAnimation: t.animation || 'Left to right',
      replayTitleDelay: Number(t.delay) || 0,
      replayTitleDuration: Number(t.duration) || 0,
      replayTitleAnimationDuration: Number(t.animationDuration) || 1000,
      replayTitleFont: b.font || 'Inter',
      replayTitleFontSize: b.fontSize || 34,
      replayTitleTextColor: b.textColor || '#FFFFFFFF',
      replayTitleShadowColor: b.shadowColor || '#000000FF',
      replayTitlePrimaryColor: b.primaryColor || '#0384CBFF',
      replayTitleSecondaryColor: b.secondaryColor || '#101416FF',
      replayBrandLogoUrl: b.logo || '',
      replayBrandFallbackText: b.fallbackText || 'RTS',
      replayBrandLabel: b.brandLabel || 'ACTION REPLAY',
      replayBrandFallbackTextColor: b.primaryColor || '#0384CBFF',
      replayBrandLabelColor: b.textColor || '#FFFFFFFF',
      replayBroadcastPrimaryColor: b.primaryColor || '#0384CBFF',
      replayBroadcastSecondaryColor: b.secondaryColor || '#101416FF',
      replayBroadcastChevronHeight: d.chevronHeight ?? 42,
      replayBroadcastChevronWidth: d.chevronWidth ?? 42,
      replayBroadcastChevronSpacing: d.chevronSpacing ?? 0,
      replayBroadcastChevronSpeed: d.chevronSpeed ?? 95,
      replayCutPrimaryColor: b.primaryColor || '#0384CBFF',
      replayCutSecondaryColor: b.secondaryColor || '#101416FF',
      replayCutBackgroundColor: d.backgroundColor || '#101416FF',
      replayCutBlockWidth: d.blockWidth ?? 170,
      replayCutRandomWidth: d.randomWidth !== false,
      replayCutBarHeight: d.barHeight ?? 5
    });
  };

  window.RTSOverlayConfigPresentation = { apply };
})();
