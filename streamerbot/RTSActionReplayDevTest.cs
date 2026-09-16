// Streamer.bot C# action: send the current Action Replay settings to the dev overlay.
// This action is for testing only. Normal replay actions continue to send their
// own command settings and do not read anything from the dev toolbar.
using System;

public class CPHInline
{
    private const string EventName = "RTS-Action Replay";

    public bool Execute()
    {
        CPH.SetArgument("replayCommand", "dev-test");
        CPH.SetArgument("replayId", "dev-test");
        CPH.SetArgument("replayNumber", 1);
        CPH.SetArgument("replayTitle", "ACTION REPLAY DEV TEST");
        CPH.SetArgument("replayMessage", "CLAPPERBOARD ANIMATION TEST");
        ApplySettings();
        ApplyPlayerAnimation();
        ApplyPanelAnimation();
        ApplyClapperboard();
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
        SetJson("replayPositions", "rts.actionreplay.positions", null);
        SetString("replayBrandLogoUrl", "rts.actionreplay.brandLogoUrl", "");
        SetString("replayBrandFallbackText", "rts.actionreplay.brandFallbackText", "RTS");
        SetString("replayBrandFallbackTextColor", "rts.actionreplay.brandFallbackTextColor", "#0384CBFF");
        SetString("replayBrandLabel", "rts.actionreplay.brandLabel", "ACTION REPLAY");
        SetString("replayBrandLabelColor", "rts.actionreplay.brandLabelColor", "#FFFFFFFF");
        SetBool("replayShowBranding", "rts.actionreplay.showBranding", true);
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
        SetBool("replayBroadcastOverrideColours", "rts.actionreplay.broadcast.overrideColours", false);
        SetString("replayBroadcastPrimaryColor", "rts.actionreplay.broadcast.primaryColor", "#0384CBFF");
        SetString("replayBroadcastSecondaryColor", "rts.actionreplay.broadcast.secondaryColor", "#FFD400FF");
        SetInt("replayBroadcastChevronHeight", "rts.actionreplay.broadcast.chevronHeight", 42);
        SetBool("replayBroadcastRandomHeight", "rts.actionreplay.broadcast.randomHeight", false);
        SetInt("replayBroadcastChevronWidth", "rts.actionreplay.broadcast.chevronWidth", 42);
        SetBool("replayBroadcastRandomWidth", "rts.actionreplay.broadcast.randomWidth", false);
        SetInt("replayBroadcastChevronSpacing", "rts.actionreplay.broadcast.chevronSpacing", 0);
        SetBool("replayBroadcastRandomSpacing", "rts.actionreplay.broadcast.randomSpacing", false);
        SetInt("replayBroadcastChevronSpeed", "rts.actionreplay.broadcast.chevronSpeed", 95);
        SetString("replayBroadcastDecorationColor", "rts.actionreplay.broadcast.decorationColor", "#0384CBFF");
        SetString("replayBroadcastTitleColor", "rts.actionreplay.broadcast.titleColor", "#FFFFFFFF");
        SetBool("replayCutOverrideColours", "rts.actionreplay.cut.overrideColours", false);
        SetString("replayCutPrimaryColor", "rts.actionreplay.cut.primaryColor", "#0384CBFF");
        SetString("replayCutSecondaryColor", "rts.actionreplay.cut.secondaryColor", "#FFD400FF");
        SetInt("replayCutBlockWidth", "rts.actionreplay.cut.blockWidth", 170);
        SetBool("replayCutRandomWidth", "rts.actionreplay.cut.randomWidth", true);
        SetInt("replayCutBarHeight", "rts.actionreplay.cut.barHeight", 5);
        SetString("replayCutDecorationColor", "rts.actionreplay.cut.decorationColor", "#0384CBFF");
        SetString("replayCutTitleColor", "rts.actionreplay.cut.titleColor", "#FFFFFFFF");
    }

    private void ApplyPlayerAnimation()
    {
        SetJson("replayPlayerConfig", "rts.actionreplay.config.player", null);
        SetJson("replayPlayerPositions", "rts.actionreplay.config.player", "positions");
        SetJson("replayAnimationProfiles", "rts.actionreplay.config.player", "animationProfiles");
        SetJson("replayPlayerAnimationConfig", "rts.actionreplay.config.player", "animation");
        SetSelectedAnimation("replayAnimationProfile", "rts.actionreplay.config.player");
        SetString("replayStartPosition", "rts.actionreplay.animation.default.startPosition", "Full Screen");
        SetString("replayEndPosition", "rts.actionreplay.animation.default.endPosition", "Full Screen");
        SetDouble("replayAnimationDuration", "rts.actionreplay.animation.default.duration", .5);
        SetString("replayAnimationEasing", "rts.actionreplay.animation.default.easing", "ease-in-out");
    }

    private void ApplyPanelAnimation()
    {
        SetJson("replayPanelConfig", "rts.actionreplay.config.panel", null);
        SetJson("replayPanelPositions", "rts.actionreplay.config.panel", "positions");
        SetJson("replayPanelAnimationProfiles", "rts.actionreplay.config.panel", "animationProfiles");
        SetJson("replayPanelAnimationConfig", "rts.actionreplay.config.panel", "animation");
        SetSelectedAnimation("replayPanelAnimation", "rts.actionreplay.config.panel");
    }

    private void ApplyClapperboard()
    {
        SetString("replayLogoUrl", "rts.actionreplay.brandLogoUrl", "");
        SetString("replayMessageBoardColor", "rts.actionreplay.clapper.boardColor", "#101416");
        SetString("replayMessageStripeLight", "rts.actionreplay.clapper.stripeLight", "#EEEEEE");
        SetString("replayMessageStripeDark", "rts.actionreplay.clapper.stripeDark", "#111111");
        SetString("replayMessageAccent", "rts.actionreplay.clapper.accent", "#0384CB");
        SetString("replayMessageTextColor", "rts.actionreplay.clapper.textColor", "#0384CB");
        SetString("replayMessageFont", "rts.actionreplay.clapper.font", "Arial, sans-serif");
        SetString("replayClapperPosition", "rts.actionreplay.clapper.position", "Centered");
        SetJson("replayClapperPositions", "rts.actionreplay.clapper.positions", null);
        SetJson("replayClapperAnimationProfiles", "rts.actionreplay.config.clapper", "animationProfiles");
        SetJson("replayClapperAnimationConfig", "rts.actionreplay.config.clapper", "animation");
        SetSelectedAnimation("replayClapperAnimation", "rts.actionreplay.config.clapper");
        SetInt("replayClapperWidth", "rts.actionreplay.clapper.width", 680);
        SetInt("replayClapperHeight", "rts.actionreplay.clapper.height", 372);
        SetInt("replayMessageDuration", "rts.actionreplay.clapper.duration", 5000);
    }

    private void SetSelectedAnimation(string arg, string key)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        try
        {
            var config = Newtonsoft.Json.Linq.JObject.Parse(raw ?? "{}");
            var selected = (string)config["animation"]?["selectedProfile"] ?? "default";
            var profiles = config["animationProfiles"] as Newtonsoft.Json.Linq.JArray;
            foreach (var token in profiles ?? new Newtonsoft.Json.Linq.JArray())
            {
                if (!string.Equals((string)token["id"], selected, StringComparison.Ordinal)) continue;
                var output = new Newtonsoft.Json.Linq.JObject {
                    ["id"] = (string)token["id"] ?? "default",
                    ["name"] = (string)token["name"] ?? "Default",
                    ["start"] = token["startSequence"] ?? new Newtonsoft.Json.Linq.JArray(),
                    ["end"] = token["endSequence"] ?? new Newtonsoft.Json.Linq.JArray()
                };
                CPH.SetArgument(arg, output.ToString(Newtonsoft.Json.Formatting.None));
                return;
            }
        }
        catch { }
        CPH.SetArgument(arg, "{}");
    }

    private void SetJson(string arg, string key, string child)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        try
        {
            var value = Newtonsoft.Json.Linq.JObject.Parse(raw ?? "{}");
            Newtonsoft.Json.Linq.JToken selected = string.IsNullOrWhiteSpace(child) ? value : value[child];
            CPH.SetArgument(arg, (selected ?? new Newtonsoft.Json.Linq.JObject()).ToString(Newtonsoft.Json.Formatting.None));
        }
        catch { CPH.SetArgument(arg, "{}"); }
    }

    private void SetBool(string arg, string key, bool fallback) { CPH.SetArgument(arg, CPH.GetGlobalVar<bool?>(key, true) ?? fallback); }
    private void SetInt(string arg, string key, int fallback) { CPH.SetArgument(arg, CPH.GetGlobalVar<int?>(key, true) ?? fallback); }
    private void SetDouble(string arg, string key, double fallback) { CPH.SetArgument(arg, CPH.GetGlobalVar<double?>(key, true) ?? fallback); }
    private void SetString(string arg, string key, string fallback) { CPH.SetArgument(arg, CPH.GetGlobalVar<string>(key, true) ?? fallback); }
}
