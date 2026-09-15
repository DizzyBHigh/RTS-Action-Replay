using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string EventName = "RTS-Action Replay";
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string PanelKey = "rts.actionreplay.config.panel";

    public bool Execute()
    {
        var payload = new JObject
        {
            ["version"] = 1,
            ["player"] = ReadConfig(PlayerKey),
            ["panel"] = ReadConfig(PanelKey),
            ["command"] = BuildCommand()
        };

        CPH.SetArgument("replayCommand", "settings-sync");
        CPH.SetArgument("replaySettings", payload.ToString(Newtonsoft.Json.Formatting.None));
        CPH.TriggerEvent(EventName, true);
        CPH.LogInfo("RTS Action Replay: sent complete settings JSON to overlay.");
        return true;
    }

    private JObject BuildCommand()
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

    private void AddBroadcast(JObject c)
    {
        AddBool(c, "replayBroadcastOverrideColours", "rts.actionreplay.broadcast.overrideColours", false);
        AddString(c, "replayBroadcastPrimaryColor", "rts.actionreplay.broadcast.primaryColor", "#0384CBFF");
        AddString(c, "replayBroadcastSecondaryColor", "rts.actionreplay.broadcast.secondaryColor", "#FFD400FF");
        AddInt(c, "replayBroadcastChevronHeight", "rts.actionreplay.broadcast.chevronHeight", 42);
        AddBool(c, "replayBroadcastRandomHeight", "rts.actionreplay.broadcast.randomHeight", false);
        AddInt(c, "replayBroadcastChevronWidth", "rts.actionreplay.broadcast.chevronWidth", 42);
        AddBool(c, "replayBroadcastRandomWidth", "rts.actionreplay.broadcast.randomWidth", false);
        AddInt(c, "replayBroadcastChevronSpacing", "rts.actionreplay.broadcast.chevronSpacing", 0);
        AddBool(c, "replayBroadcastRandomSpacing", "rts.actionreplay.broadcast.randomSpacing", false);
        AddInt(c, "replayBroadcastChevronSpeed", "rts.actionreplay.broadcast.chevronSpeed", 95);
        AddString(c, "replayBroadcastDecorationColor", "rts.actionreplay.broadcast.decorationColor", "#0384CBFF");
        AddString(c, "replayBroadcastTitleColor", "rts.actionreplay.broadcast.titleColor", "#FFFFFFFF");
    }

    private void AddCut(JObject c)
    {
        AddBool(c, "replayCutOverrideColours", "rts.actionreplay.cut.overrideColours", false);
        AddString(c, "replayCutPrimaryColor", "rts.actionreplay.cut.primaryColor", "#0384CBFF");
        AddString(c, "replayCutSecondaryColor", "rts.actionreplay.cut.secondaryColor", "#FFD400FF");
        AddInt(c, "replayCutBlockWidth", "rts.actionreplay.cut.blockWidth", 170);
        AddBool(c, "replayCutRandomWidth", "rts.actionreplay.cut.randomWidth", true);
        AddInt(c, "replayCutBarHeight", "rts.actionreplay.cut.barHeight", 5);
        AddString(c, "replayCutDecorationColor", "rts.actionreplay.cut.decorationColor", "#0384CBFF");
        AddString(c, "replayCutTitleColor", "rts.actionreplay.cut.titleColor", "#FFFFFFFF");
    }

    private void AddPanel(JObject c)
    {
        var panel = ReadConfig(PanelKey);
        c["replayPanelWidth"] = (int?)panel["width"] ?? 500;
        c["replayPanelHeight"] = (int?)panel["height"] ?? 700;
        AddString(c, "replayPanelPreset", "rts.actionreplay.panel.preset", "Broadcast");
        AddString(c, "replayPanelPrimaryColor", "rts.actionreplay.panel.primaryColor", "#0384CBFF");
        AddString(c, "replayPanelSecondaryColor", "rts.actionreplay.panel.secondaryColor", "#101416FF");
        AddString(c, "replayPanelTitleFont", "rts.actionreplay.panel.titleFont", "Inter");
        AddInt(c, "replayPanelTitleSize", "rts.actionreplay.panel.titleSize", 24);
        AddString(c, "replayPanelTitleColor", "rts.actionreplay.panel.titleColor", "#FFFFFFFF");
        AddInt(c, "replayPanelListSize", "rts.actionreplay.panel.listSize", 15);
        AddString(c, "replayPanelListColor", "rts.actionreplay.panel.listColor", "#FFFFFFFF");
    }

    private void AddMessage(JObject c)
    {
        AddString(c, "replayMessageBoardColor", "rts.actionreplay.clapper.boardColor", "#101416");
        AddString(c, "replayMessageStripeLight", "rts.actionreplay.clapper.stripeLight", "#EEEEEE");
        AddString(c, "replayMessageStripeDark", "rts.actionreplay.clapper.stripeDark", "#111111");
        AddString(c, "replayMessageAccent", "rts.actionreplay.clapper.accent", "#0384CB");
        AddString(c, "replayMessageTextColor", "rts.actionreplay.clapper.textColor", "#0384CB");
        AddString(c, "replayMessageFont", "rts.actionreplay.clapper.font", "Inter");
        AddInt(c, "replayMessageSize", "rts.actionreplay.clapper.size", 50);
        AddInt(c, "replayMessagePositionX", "rts.actionreplay.clapper.positionX", 50);
        AddInt(c, "replayMessagePositionY", "rts.actionreplay.clapper.positionY", 50);
    }

    private JObject ReadConfig(string key)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); }
        catch { return new JObject(); }
    }

    private void AddString(JObject c, string name, string key, string fallback) => c[name] = CPH.GetGlobalVar<string>(key, true) ?? fallback;
    private void AddBool(JObject c, string name, string key, bool fallback) => c[name] = CPH.GetGlobalVar<bool?>(key, true) ?? fallback;
    private void AddInt(JObject c, string name, string key, int fallback) => c[name] = CPH.GetGlobalVar<int?>(key, true) ?? fallback;
    private void AddNumber(JObject c, string name, string key, double fallback) => c[name] = CPH.GetGlobalVar<double?>(key, true) ?? fallback;
}