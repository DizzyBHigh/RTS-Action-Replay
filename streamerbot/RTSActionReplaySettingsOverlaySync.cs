using Newtonsoft.Json.Linq;

public partial class CPHInline
{
    private void SendAllSettingsToOverlay()
    {
        var command = BuildOverlayCommand();
        var payload = new JObject
        {
            ["version"] = 1,
            ["player"] = ReadConfig(PlayerKey),
            ["panel"] = ReadConfig(PanelKey),
            ["command"] = command
        };
        CPH.SetArgument("replayCommand", "settings-sync");
        CPH.SetArgument("replaySettings", payload.ToString(Newtonsoft.Json.Formatting.None));
        CPH.TriggerEvent("RTS-Action Replay", true);
        CPH.LogInfo("RTS Action Replay: sent complete player/panel settings payload to overlay.");
    }

    private JObject BuildOverlayCommand()
    {
        var command = new JObject();
        AddBool(command, "replayShowControls", "rts.actionreplay.showControls", false);
        AddBool(command, "replayShowProgress", "rts.actionreplay.showProgress", true);
        AddNumber(command, "replayPlaybackSpeed", "rts.actionreplay.playbackSpeed", 1.0);
        AddString(command, "replayPlaybackSpeedVisibility", "rts.actionreplay.playbackSpeedVisibility", "Only when greater or less than 1");
        AddString(command, "replayFrameColor", "rts.actionreplay.frameColor", "#0384CBFF");
        AddBool(command, "replayBorderGlow", "rts.actionreplay.borderGlow", true);
        AddInt(command, "replayBorderWidth", "rts.actionreplay.borderWidth", 4);
        AddInt(command, "replayCornerRadius", "rts.actionreplay.cornerRadius", 0);
        AddBool(command, "replayShowBranding", "rts.actionreplay.showBranding", true);
        AddString(command, "replayBrandLogoUrl", "rts.actionreplay.brandLogoUrl", "");
        AddString(command, "replayBrandFallbackText", "rts.actionreplay.brandFallbackText", "RTS");
        AddString(command, "replayBrandFallbackTextColor", "rts.actionreplay.brandFallbackTextColor", "#0384CBFF");
        AddString(command, "replayBrandLabel", "rts.actionreplay.brandLabel", "ACTION REPLAY");
        AddString(command, "replayBrandLabelColor", "rts.actionreplay.brandLabelColor", "#FFFFFFFF");
        AddBool(command, "replayShowTitle", "rts.actionreplay.showTitle", true);
        AddString(command, "replayTitleDecorationPosition", "rts.actionreplay.titleDecorationPosition", "Suffix");
        AddString(command, "replayTitleDecoration", "rts.actionreplay.titleDecoration", " - Replay Capture");
        AddString(command, "replayTitleStyle", "rts.actionreplay.titleBarStyle", "Broadcast");
        AddString(command, "replayTitlePosition", "rts.actionreplay.titlePosition", "Bottom");
        AddString(command, "replayTitleAnimation", "rts.actionreplay.titleAnimation", "Slide up/down");
        AddInt(command, "replayTitleDelay", "rts.actionreplay.titleDelay", 0);
        AddInt(command, "replayTitleDuration", "rts.actionreplay.titleDuration", 5000);
        AddInt(command, "replayTitleAnimationDuration", "rts.actionreplay.titleAnimationDuration", 450);
        AddString(command, "replayTitleFont", "rts.actionreplay.titleFont", "Inter");
        AddInt(command, "replayTitleFontSize", "rts.actionreplay.titleFontSize", 34);
        AddString(command, "replayTitleTextColor", "rts.actionreplay.titleTextColor", "#FFFFFFFF");
        AddString(command, "replayTitleShadowColor", "rts.actionreplay.titleShadowColor", "#000000FF");
        AddString(command, "replayTitlePrimaryColor", "rts.actionreplay.titlePrimaryColor", "#0384CBFF");
        AddString(command, "replayTitleSecondaryColor", "rts.actionreplay.titleSecondaryColor", "#101416FF");
        AddBroadcast(command); AddCut(command); AddPanel(command); AddMessage(command);
        return command;
    }

    private void AddBroadcast(JObject command)
    {
        AddString(command, "replayBroadcastPrimaryColor", "rts.actionreplay.broadcast.primaryColor", "#0384CBFF");
        AddString(command, "replayBroadcastSecondaryColor", "rts.actionreplay.broadcast.secondaryColor", "#FFD400FF");
        AddInt(command, "replayBroadcastChevronHeight", "rts.actionreplay.broadcast.chevronHeight", 42);
        AddBool(command, "replayBroadcastRandomHeight", "rts.actionreplay.broadcast.randomHeight", false);
        AddInt(command, "replayBroadcastChevronWidth", "rts.actionreplay.broadcast.chevronWidth", 42);
        AddBool(command, "replayBroadcastRandomWidth", "rts.actionreplay.broadcast.randomWidth", false);
        AddInt(command, "replayBroadcastChevronSpacing", "rts.actionreplay.broadcast.chevronSpacing", 0);
        AddBool(command, "replayBroadcastRandomSpacing", "rts.actionreplay.broadcast.randomSpacing", false);
        AddInt(command, "replayBroadcastChevronSpeed", "rts.actionreplay.broadcast.chevronSpeed", 95);
        AddString(command, "replayBroadcastDecorationColor", "rts.actionreplay.broadcast.decorationColor", "#0384CBFF");
        AddString(command, "replayBroadcastTitleColor", "rts.actionreplay.broadcast.titleColor", "#FFFFFFFF");
    }

    private void AddCut(JObject command)
    {
        AddString(command, "replayCutPrimaryColor", "rts.actionreplay.cut.primaryColor", "#0384CBFF");
        AddString(command, "replayCutSecondaryColor", "rts.actionreplay.cut.secondaryColor", "#FFD400FF");
        AddInt(command, "replayCutBlockWidth", "rts.actionreplay.cut.blockWidth", 170);
        AddBool(command, "replayCutRandomWidth", "rts.actionreplay.cut.randomWidth", true);
        AddInt(command, "replayCutBarHeight", "rts.actionreplay.cut.barHeight", 5);
        AddString(command, "replayCutDecorationColor", "rts.actionreplay.cut.decorationColor", "#0384CBFF");
        AddString(command, "replayCutTitleColor", "rts.actionreplay.cut.titleColor", "#FFFFFFFF");
    }

    private void AddPanel(JObject command)
    {
        var panel = ReadConfig(PanelKey);
        command["replayPanelWidth"] = (int?)panel["width"] ?? CPH.GetGlobalVar<int?>("rts.actionreplay.panel.width", true) ?? 500;
        command["replayPanelHeight"] = (int?)panel["height"] ?? CPH.GetGlobalVar<int?>("rts.actionreplay.panel.height", true) ?? 700;
        AddString(command, "replayPanelPreset", "rts.actionreplay.panel.preset", "Broadcast");
        AddString(command, "replayPanelPrimaryColor", "rts.actionreplay.panel.primaryColor", "#0384CBFF");
        AddString(command, "replayPanelSecondaryColor", "rts.actionreplay.panel.secondaryColor", "#101416FF");
        AddString(command, "replayPanelTitleFont", "rts.actionreplay.panel.titleFont", "Inter");
        AddInt(command, "replayPanelTitleSize", "rts.actionreplay.panel.titleSize", 24);
        AddString(command, "replayPanelTitleColor", "rts.actionreplay.panel.titleColor", "#FFFFFFFF");
        AddInt(command, "replayPanelListSize", "rts.actionreplay.panel.listSize", 15);
        AddString(command, "replayPanelListColor", "rts.actionreplay.panel.listColor", "#FFFFFFFF");
    }

    private void AddMessage(JObject command)
    {
        AddString(command, "replayMessageBoardColor", "rts.actionreplay.clapper.boardColor", "#101416");
        AddString(command, "replayMessageStripeLight", "rts.actionreplay.clapper.stripeLight", "#EEEEEE");
        AddString(command, "replayMessageStripeDark", "rts.actionreplay.clapper.stripeDark", "#111111");
        AddString(command, "replayMessageAccent", "rts.actionreplay.clapper.accent", "#0384CB");
        AddString(command, "replayMessageTextColor", "rts.actionreplay.clapper.textColor", "#0384CB");
        AddString(command, "replayMessageFont", "rts.actionreplay.clapper.font", "Inter");
        AddInt(command, "replayMessageSize", "rts.actionreplay.clapper.size", 50);
        AddInt(command, "replayMessagePositionX", "rts.actionreplay.clapper.positionX", 50);
        AddInt(command, "replayMessagePositionY", "rts.actionreplay.clapper.positionY", 50);
    }

    private void AddString(JObject target, string name, string key, string fallback) => target[name] = CPH.GetGlobalVar<string>(key, true) ?? fallback;
    private void AddBool(JObject target, string name, string key, bool fallback) => target[name] = CPH.GetGlobalVar<bool?>(key, true) ?? fallback;
    private void AddInt(JObject target, string name, string key, int fallback) => target[name] = CPH.GetGlobalVar<int?>(key, true) ?? fallback;
    private void AddNumber(JObject target, string name, string key, double fallback) => target[name] = CPH.GetGlobalVar<double?>(key, true) ?? fallback;
}
