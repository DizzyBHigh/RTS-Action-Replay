using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedTitleProfile";
    private const string PlaybackProfileHandoffKey = "rts.actionreplay.handoff.playbackTitleProfile";

    private static readonly string[] Profiles = { "Broadcast", "Cinematic", "Cut", "Minimal" };

    public bool Execute() => EnsureProfiles();

    public bool EnsureProfiles()
    {
        var player = ReadConfig();
        var title = player["title"] as JObject ?? new JObject();
        var legacy = NormalizeProfile(CPH.GetGlobalVar<string>("rts.actionreplay.titleBarStyle", true));
        var selected = NormalizeProfile((string)title["selectedProfile"]);
        if (string.IsNullOrWhiteSpace((string)title["selectedProfile"])) selected = legacy;
        title["selectedProfile"] = selected;
        title["entryPoints"] = NormalizeEntryPoints(title["entryPoints"] as JObject, selected);
        player["title"] = title;
        SaveConfig(player);
        return true;
    }

    public bool ResolveEntryPointProfile()
    {
        var entryPoint = Arg("titleEntryPoint", "");
        if (string.IsNullOrWhiteSpace(entryPoint)) entryPoint = CPH.GetGlobalVar<string>(EntryPointHandoffKey, false);
        CPH.UnsetGlobalVar(EntryPointHandoffKey, false);
        if (string.IsNullOrWhiteSpace(entryPoint)) return false;
        var player = ReadConfig();
        var title = player["title"] as JObject ?? new JObject();
        var selected = NormalizeProfile((string)title["selectedProfile"]);
        var entries = title["entryPoints"] as JObject ?? new JObject();
        var profile = NormalizeProfile((string)entries[entryPoint.ToLowerInvariant()]);
        if (string.IsNullOrWhiteSpace((string)entries[entryPoint.ToLowerInvariant()])) profile = selected;
        CPH.SetGlobalVar(ResolvedProfileHandoffKey, profile, false);
        CPH.SetArgument("replayTitleProfileId", profile);
        return true;
    }

    public bool ApplyProfile()
    {
        var profile = Arg("replayTitleProfileId", "");
        if (string.IsNullOrWhiteSpace(profile)) profile = CPH.GetGlobalVar<string>(PlaybackProfileHandoffKey, false);
        var player = ReadConfig();
        var title = player["title"] as JObject ?? new JObject();
        if (string.IsNullOrWhiteSpace(profile)) profile = (string)title["selectedProfile"];
        profile = NormalizeProfile(profile);
        CPH.UnsetGlobalVar(PlaybackProfileHandoffKey, false);
        CPH.SetArgument("replayTitleProfileId", profile);
        CPH.SetArgument("replayTitleProfile", BuildPayload(profile).ToString(Newtonsoft.Json.Formatting.None));
        ApplyArguments(profile);
        return true;
    }

    private JObject BuildPayload(string profile)
    {
        var payload = new JObject
        {
            ["profile"] = profile,
            ["showTitle"] = LegacyBool("rts.actionreplay.showTitle", true),
            ["decorationPosition"] = LegacyString("rts.actionreplay.titleDecorationPosition", "Suffix"),
            ["decoration"] = LegacyString("rts.actionreplay.titleDecoration", " - Replay Capture"),
            ["style"] = profile,
            ["position"] = LegacyString("rts.actionreplay.titlePosition", "Bottom"),
            ["animation"] = LegacyString("rts.actionreplay.titleAnimation", "Slide up/down"),
            ["delay"] = LegacyInt("rts.actionreplay.titleDelay", 0),
            ["duration"] = LegacyInt("rts.actionreplay.titleDuration", 5000),
            ["animationDuration"] = LegacyInt("rts.actionreplay.titleAnimationDuration", 450),
            ["font"] = LegacyString("rts.actionreplay.titleFont", "Inter"),
            ["fontSize"] = LegacyInt("rts.actionreplay.titleFontSize", 34),
            ["textColor"] = LegacyString("rts.actionreplay.titleTextColor", "#FFFFFFFF"),
            ["shadowColor"] = LegacyString("rts.actionreplay.titleShadowColor", "#000000FF"),
            ["primaryColor"] = LegacyString("rts.actionreplay.titlePrimaryColor", "#0384CBFF"),
            ["secondaryColor"] = LegacyString("rts.actionreplay.titleSecondaryColor", "#101416FF")
        };
        payload["broadcast"] = BuildBroadcast();
        payload["cut"] = BuildCut();
        return payload;
    }

    private void ApplyArguments(string profile)
    {
        var payload = BuildPayload(profile);
        CPH.SetArgument("replayShowTitle", (bool)payload["showTitle"]);
        CPH.SetArgument("replayTitleDecorationPosition", (string)payload["decorationPosition"]);
        CPH.SetArgument("replayTitleDecoration", (string)payload["decoration"]);
        CPH.SetArgument("replayTitleStyle", profile);
        CPH.SetArgument("replayTitlePosition", (string)payload["position"]);
        CPH.SetArgument("replayTitleAnimation", (string)payload["animation"]);
        CPH.SetArgument("replayTitleDelay", (int)payload["delay"]);
        CPH.SetArgument("replayTitleDuration", (int)payload["duration"]);
        CPH.SetArgument("replayTitleAnimationDuration", (int)payload["animationDuration"]);
        CPH.SetArgument("replayTitleFont", (string)payload["font"]);
        CPH.SetArgument("replayTitleFontSize", (int)payload["fontSize"]);
        CPH.SetArgument("replayTitleTextColor", (string)payload["textColor"]);
        CPH.SetArgument("replayTitleShadowColor", (string)payload["shadowColor"]);
        CPH.SetArgument("replayTitlePrimaryColor", (string)payload["primaryColor"]);
        CPH.SetArgument("replayTitleSecondaryColor", (string)payload["secondaryColor"]);
        var broadcast = payload["broadcast"] as JObject; foreach (var p in broadcast.Properties()) CPH.SetArgument("replayBroadcast" + Name(p.Name), Value(p.Value));
        var cut = payload["cut"] as JObject; foreach (var p in cut.Properties()) CPH.SetArgument("replayCut" + Name(p.Name), Value(p.Value));
    }

    private JObject BuildBroadcast() => new JObject
    {
        ["primaryColor"] = LegacyString("rts.actionreplay.broadcast.primaryColor", "#0384CBFF"), ["secondaryColor"] = LegacyString("rts.actionreplay.broadcast.secondaryColor", "#FFD400FF"),
        ["chevronHeight"] = LegacyInt("rts.actionreplay.broadcast.chevronHeight", 42), ["randomHeight"] = LegacyBool("rts.actionreplay.broadcast.randomHeight", false),
        ["chevronWidth"] = LegacyInt("rts.actionreplay.broadcast.chevronWidth", 42), ["randomWidth"] = LegacyBool("rts.actionreplay.broadcast.randomWidth", false),
        ["chevronSpacing"] = LegacyInt("rts.actionreplay.broadcast.chevronSpacing", 0), ["randomSpacing"] = LegacyBool("rts.actionreplay.broadcast.randomSpacing", false),
        ["chevronSpeed"] = LegacyInt("rts.actionreplay.broadcast.chevronSpeed", 95), ["decorationColor"] = LegacyString("rts.actionreplay.broadcast.decorationColor", "#0384CBFF"), ["titleColor"] = LegacyString("rts.actionreplay.broadcast.titleColor", "#FFFFFFFF")
    };

    private JObject BuildCut() => new JObject
    {
        ["primaryColor"] = LegacyString("rts.actionreplay.cut.primaryColor", "#0384CBFF"), ["secondaryColor"] = LegacyString("rts.actionreplay.cut.secondaryColor", "#FFD400FF"),
        ["blockWidth"] = LegacyInt("rts.actionreplay.cut.blockWidth", 170), ["randomWidth"] = LegacyBool("rts.actionreplay.cut.randomWidth", true), ["barHeight"] = LegacyInt("rts.actionreplay.cut.barHeight", 5),
        ["decorationColor"] = LegacyString("rts.actionreplay.cut.decorationColor", "#0384CBFF"), ["titleColor"] = LegacyString("rts.actionreplay.cut.titleColor", "#FFFFFFFF")
    };

    private JObject NormalizeEntryPoints(JObject source, string selected)
    {
        var result = new JObject();
        foreach (var point in new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" }) result[point] = NormalizeProfile((string)source?[point] ?? selected);
        return result;
    }

    private string NormalizeProfile(string value) { foreach (var profile in Profiles) if (string.Equals(profile, value, StringComparison.OrdinalIgnoreCase)) return profile; return "Broadcast"; }
    private string Arg(string name, string fallback) => CPH.TryGetArg(name, out string value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
    private JObject ReadConfig() { var raw = CPH.GetGlobalVar<string>(PlayerKey, true); try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { return new JObject(); } }
    private void SaveConfig(JObject player) => CPH.SetGlobalVar(PlayerKey, player.ToString(Newtonsoft.Json.Formatting.None), true);
    private string LegacyString(string key, string fallback) => CPH.GetGlobalVar<string>(key, true) ?? fallback;
    private int LegacyInt(string key, int fallback) => CPH.GetGlobalVar<int?>(key, true) ?? fallback;
    private bool LegacyBool(string key, bool fallback) => CPH.GetGlobalVar<bool?>(key, true) ?? fallback;
    private static string Name(string key) { var result = char.ToUpperInvariant(key[0]) + key.Substring(1); return result; }
    private static object Value(JToken value) => value.Type == JTokenType.Boolean ? (object)(bool)value : value.Type == JTokenType.Integer ? (object)(int)value : value.ToString();
}
