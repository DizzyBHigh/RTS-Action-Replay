// Streamer.bot C# action: send the current Action Replay settings to the dev overlay.
// This action is for testing only. Normal replay actions continue to send their
// own command settings and do not read anything from the dev toolbar.
using System;

public class CPHInline
{
    private const string EventName = "RTS-Action Replay";

    public bool Execute()
    {
        CPH.SetArgument("replayCommand", "title-test");
        CPH.SetArgument("replayId", "dev-title-test");
        CPH.SetArgument("replayTitle", "TITLE SETTINGS TEST");
        ApplySettings();
        CPH.TriggerEvent(EventName, true);
        return true;
    }

    private void ApplySettings()
    {
        SetBool("replayShowControls", "rts.actionreplay.showControls", false);
        SetBool("replayShowProgress", "rts.actionreplay.showProgress", true);
        SetDouble("replayPlaybackSpeed", "rts.actionreplay.playbackSpeed", 1.0);
        SetString("replayPlaybackSpeedVisibility", "rts.actionreplay.playbackSpeedVisibility", "Only when greater or less than 1");
        SetString("replayFrameColor", "rts.actionreplay.frameColor", "#0384CBFF");
        SetInt("replayBorderWidth", "rts.actionreplay.borderWidth", 4);
        SetInt("replayCornerRadius", "rts.actionreplay.cornerRadius", 0);
        SetBool("replayBorderGlow", "rts.actionreplay.borderGlow", true);

        SetString("replayPositions", "rts.actionreplay.positions", "{\"Full Screen\":{\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}");
        SetProfile("default");

        SetBool("replayShowBranding", "rts.actionreplay.showBranding", true);
        SetString("replayBrandLogoUrl", "rts.actionreplay.brandLogoUrl", "");
        SetString("replayBrandFallbackText", "rts.actionreplay.brandFallbackText", "RTS");
        SetString("replayBrandFallbackTextColor", "rts.actionreplay.brandFallbackTextColor", "#0384CBFF");
        SetString("replayBrandLabel", "rts.actionreplay.brandLabel", "ACTION REPLAY");
        SetString("replayBrandLabelColor", "rts.actionreplay.brandLabelColor", "#FFFFFFFF");

        SetBool("replayShowTitle", "rts.actionreplay.showTitle", true);
        SetString("replayTitleDecorationPosition", "rts.actionreplay.titleDecorationPosition", "Suffix");
        SetString("replayTitleDecoration", "rts.actionreplay.titleDecoration", " - Replay Capture");
        SetString("replayTitleStyle", "rts.actionreplay.titleBarStyle", "Broadcast");
        SetString("replayTitlePosition", "rts.actionreplay.titlePosition", "Bottom");
        SetString("replayTitleAnimation", "rts.actionreplay.titleAnimation", "Slide up/down");
        SetInt("replayTitleDelay", "rts.actionreplay.titleDelay", 0);
        SetInt("replayTitleDuration", "rts.actionreplay.titleDuration", 5000);
        SetInt("replayTitleAnimationDuration", "rts.actionreplay.titleAnimationDuration", 450);
        SetString("replayTitleFont", "rts.actionreplay.titleFont", "Inter");
        SetInt("replayTitleFontSize", "rts.actionreplay.titleFontSize", 34);
        SetString("replayTitleTextColor", "rts.actionreplay.titleTextColor", "#FFFFFFFF");
        SetString("replayTitleShadowColor", "rts.actionreplay.titleShadowColor", "#000000FF");
        SetString("replayTitlePrimaryColor", "rts.actionreplay.titlePrimaryColor", "#0384CBFF");
        SetString("replayTitleSecondaryColor", "rts.actionreplay.titleSecondaryColor", "#101416FF");

        SetString("replayBroadcastPrimaryColor", "rts.actionreplay.broadcast.primaryColor", "#0384CBFF");
        SetString("replayBroadcastSecondaryColor", "rts.actionreplay.broadcast.secondaryColor", "#FFD400FF");
        SetInt("replayBroadcastChevronWidth", "rts.actionreplay.broadcast.chevronWidth", 42);
        SetBool("replayBroadcastRandomWidth", "rts.actionreplay.broadcast.randomWidth", false);
        SetInt("replayBroadcastChevronSpacing", "rts.actionreplay.broadcast.chevronSpacing", 13);
        SetBool("replayBroadcastRandomSpacing", "rts.actionreplay.broadcast.randomSpacing", false);
        SetString("replayBroadcastDecorationColor", "rts.actionreplay.broadcast.decorationColor", "#0384CBFF");
        SetString("replayBroadcastTitleColor", "rts.actionreplay.broadcast.titleColor", "#FFFFFFFF");

        SetString("replayCutPrimaryColor", "rts.actionreplay.cut.primaryColor", "#0384CBFF");
        SetString("replayCutSecondaryColor", "rts.actionreplay.cut.secondaryColor", "#FFD400FF");
        SetInt("replayCutBlockWidth", "rts.actionreplay.cut.blockWidth", 170);
        SetBool("replayCutRandomWidth", "rts.actionreplay.cut.randomWidth", true);
        SetInt("replayCutBarHeight", "rts.actionreplay.cut.barHeight", 5);
        SetString("replayCutDecorationColor", "rts.actionreplay.cut.decorationColor", "#0384CBFF");
        SetString("replayCutTitleColor", "rts.actionreplay.cut.titleColor", "#FFFFFFFF");
    }

    private void SetProfile(string profile)
    {
        SetString("replayStartPosition", "rts.actionreplay.animation." + profile + ".startPosition", "Full Screen");
        SetString("replayEndPosition", "rts.actionreplay.animation." + profile + ".endPosition", "Full Screen");
        SetDouble("replayAnimationDuration", "rts.actionreplay.animation." + profile + ".duration", .5);
        SetString("replayAnimationEasing", "rts.actionreplay.animation." + profile + ".easing", "ease-in-out");
    }

    private void SetBool(string arg, string key, bool fallback) { CPH.SetArgument(arg, CPH.GetGlobalVar<bool?>(key, true) ?? fallback); }
    private void SetInt(string arg, string key, int fallback) { CPH.SetArgument(arg, CPH.GetGlobalVar<int?>(key, true) ?? fallback); }
    private void SetDouble(string arg, string key, double fallback) { CPH.SetArgument(arg, CPH.GetGlobalVar<double?>(key, true) ?? fallback); }
    private void SetString(string arg, string key, string fallback) { CPH.SetArgument(arg, CPH.GetGlobalVar<string>(key, true) ?? fallback); }
}
