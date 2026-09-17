using System;
using Newtonsoft.Json.Linq;

// Compatibility/resolution layer for the existing Title action.
// Visual and Branding preset data are now the source of truth.
public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string PresetsKey = "rts.actionreplay.config.presets";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedTitleProfile";
    private const string PlaybackProfileHandoffKey = "rts.actionreplay.handoff.playbackTitleProfile";

    public bool Execute() => EnsureProfiles();

    public bool EnsureProfiles()
    {
        // Keep the legacy title object readable while entry-point preset references are created by Preset Store.
        var player = Read(PlayerKey, new JObject());
        var title = player["title"] as JObject ?? new JObject();
        title["selectedProfile"] = NormalizeVisual((string)title["selectedProfile"]);
        player["title"] = title;
        Save(PlayerKey, player);
        CPH.SetArgument("presetComponent", "player");
        CPH.SetArgument("entryPoint", "obs");
        CPH.ExecuteMethod("RTS - Action Replay - Core - Preset Store", "EnsureEntryPoints");
        return true;
    }

    public bool ResolveEntryPointProfile()
    {
        var entryPoint = Arg("replayTitleEntryPoint", "");
        if (string.IsNullOrWhiteSpace(entryPoint)) entryPoint = Arg("titleEntryPoint", "");
        if (string.IsNullOrWhiteSpace(entryPoint)) entryPoint = CPH.GetGlobalVar<string>(EntryPointHandoffKey, false);
        CPH.UnsetGlobalVar(EntryPointHandoffKey, false);
        if (string.IsNullOrWhiteSpace(entryPoint)) return false;

        CPH.SetArgument("presetComponent", "player");
        CPH.SetArgument("entryPoint", entryPoint);
        if (!CPH.ExecuteMethod("RTS - Action Replay - Core - Preset Store", "ResolveEntryPoint")) return false;
        var visual = Arg("visualPreset", "broadcast");
        var branding = Arg("brandingPreset", "default");
        CPH.SetGlobalVar(ResolvedProfileHandoffKey, visual, false);
        CPH.SetArgument("replayTitleProfileId", visual);
        CPH.SetArgument("replayVisualPresetId", visual);
        CPH.SetArgument("replayBrandingPresetId", branding);
        return true;
    }

    public bool ApplyProfile()
    {
        var visual = Arg("replayVisualPresetId", "");
        if (string.IsNullOrWhiteSpace(visual)) visual = Arg("replayTitleProfileId", "");
        if (string.IsNullOrWhiteSpace(visual)) visual = CPH.GetGlobalVar<string>(PlaybackProfileHandoffKey, false);
        visual = NormalizeVisual(visual);

        var branding = Arg("replayBrandingPresetId", "default");
        var payload = BuildPayload(visual, branding);
        CPH.UnsetGlobalVar(PlaybackProfileHandoffKey, false);
        CPH.SetArgument("replayTitleProfileId", visual);
        CPH.SetArgument("replayVisualPresetId", visual);
        CPH.SetArgument("replayBrandingPresetId", branding);
        CPH.SetArgument("replayTitleProfile", payload.ToString(Newtonsoft.Json.Formatting.None));
        ApplyArguments(visual, branding, payload);
        return true;
    }

    private JObject BuildPayload(string visualId, string brandingId)
    {
        var visual = Find(Visuals(), visualId) ?? Find(Visuals(), "broadcast") ?? new JObject();
        var brand = Find(Branding(), brandingId) ?? Find(Branding(), "default") ?? new JObject();
        var payload = new JObject
        {
            ["profile"] = visualId,
            ["showTitle"] = LegacyBool("rts.actionreplay.showTitle", true),
            ["decorationPosition"] = LegacyString("rts.actionreplay.titleDecorationPosition", "Suffix"),
            ["decoration"] = LegacyString("rts.actionreplay.titleDecoration", " - Replay Capture"),
            ["style"] = visualId,
            ["position"] = LegacyString("rts.actionreplay.titlePosition", "Bottom"),
            ["animation"] = LegacyString("rts.actionreplay.titleAnimation", "Slide up/down"),
            ["delay"] = LegacyInt("rts.actionreplay.titleDelay", 0), ["duration"] = LegacyInt("rts.actionreplay.titleDuration", 5000),
            ["animationDuration"] = LegacyInt("rts.actionreplay.titleAnimationDuration", 450),
            ["font"] = (string)brand["font"] ?? "Inter", ["fontSize"] = (int?)brand["fontSize"] ?? 34,
            ["textColor"] = (string)brand["textColor"] ?? "#FFFFFFFF", ["shadowColor"] = (string)brand["shadowColor"] ?? "#000000FF",
            ["primaryColor"] = (string)brand["primaryColor"] ?? "#0384CBFF", ["secondaryColor"] = (string)brand["secondaryColor"] ?? "#101416FF"
        };
        payload["broadcast"] = BuildBroadcast(visual, brand);
        payload["cut"] = BuildCut(visual, brand);
        return payload;
    }

    private void ApplyArguments(string visual, string branding, JObject payload)
    {
        CPH.SetArgument("replayShowTitle", (bool)payload["showTitle"]); CPH.SetArgument("replayTitleDecorationPosition", (string)payload["decorationPosition"]);
        CPH.SetArgument("replayTitleDecoration", (string)payload["decoration"]); CPH.SetArgument("replayTitleStyle", visual);
        CPH.SetArgument("replayTitlePosition", (string)payload["position"]); CPH.SetArgument("replayTitleAnimation", (string)payload["animation"]);
        CPH.SetArgument("replayTitleDelay", (int)payload["delay"]); CPH.SetArgument("replayTitleDuration", (int)payload["duration"]);
        CPH.SetArgument("replayTitleAnimationDuration", (int)payload["animationDuration"]); CPH.SetArgument("replayTitleFont", (string)payload["font"]);
        CPH.SetArgument("replayTitleFontSize", (int)payload["fontSize"]); CPH.SetArgument("replayTitleTextColor", (string)payload["textColor"]);
        CPH.SetArgument("replayTitleShadowColor", (string)payload["shadowColor"]); CPH.SetArgument("replayTitlePrimaryColor", (string)payload["primaryColor"]);
        CPH.SetArgument("replayTitleSecondaryColor", (string)payload["secondaryColor"]); CPH.SetArgument("replayVisualPresetId", visual); CPH.SetArgument("replayBrandingPresetId", branding);
        SetProperties("replayBroadcast", payload["broadcast"] as JObject); SetProperties("replayCut", payload["cut"] as JObject);
    }

    private JObject BuildBroadcast(JObject visual, JObject brand) => new JObject
    {
        ["primaryColor"] = (string)brand["primaryColor"] ?? "#0384CBFF", ["secondaryColor"] = (string)brand["secondaryColor"] ?? "#101416FF",
        ["chevronHeight"] = (int?)visual["chevronHeight"] ?? 42, ["randomHeight"] = (bool?)visual["randomHeight"] ?? false,
        ["chevronWidth"] = (int?)visual["chevronWidth"] ?? 42, ["randomWidth"] = (bool?)visual["randomWidth"] ?? false,
        ["chevronSpacing"] = (int?)visual["chevronSpacing"] ?? 0, ["randomSpacing"] = (bool?)visual["randomSpacing"] ?? false,
        ["chevronSpeed"] = (int?)visual["chevronSpeed"] ?? 95, ["decorationColor"] = (string)brand["titlePrefixSuffixColor"] ?? "#0384CBFF",
        ["titleColor"] = (string)brand["titleColor"] ?? "#FFFFFFFF"
    };

    private JObject BuildCut(JObject visual, JObject brand) => new JObject
    {
        ["primaryColor"] = (string)brand["primaryColor"] ?? "#0384CBFF", ["secondaryColor"] = (string)brand["secondaryColor"] ?? "#101416FF",
        ["blockWidth"] = (int?)visual["blockWidth"] ?? 170, ["randomWidth"] = (bool?)visual["randomWidth"] ?? true, ["barHeight"] = (int?)visual["barHeight"] ?? 5,
        ["decorationColor"] = (string)brand["titlePrefixSuffixColor"] ?? "#0384CBFF", ["titleColor"] = (string)brand["titleColor"] ?? "#FFFFFFFF"
    };

    private void SetProperties(string prefix, JObject values) { foreach (var p in values?.Properties() ?? new JProperty[0]) CPH.SetArgument(prefix + Name(p.Name), Value(p.Value)); }
    private static JObject Find(JArray values, string id) { foreach (var item in values ?? new JArray()) if (string.Equals((string)item["id"], id, StringComparison.OrdinalIgnoreCase)) return item as JObject; return null; }
    private JArray Branding() => Read(PresetsKey, new JObject())["branding"] as JArray ?? new JArray();
    private JArray Visuals() => Read(PresetsKey, new JObject())["visual"] as JArray ?? new JArray();
    private static string NormalizeVisual(string value) { var v = (value ?? "").Trim().ToLowerInvariant(); return v == "cinematic" || v == "cut" || v == "minimal" ? v : "broadcast"; }
    private string Arg(string name, string fallback) => CPH.TryGetArg(name, out string value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
    private JObject Read(string key, JObject fallback) { var raw = CPH.GetGlobalVar<string>(key, true); try { return string.IsNullOrWhiteSpace(raw) ? fallback : JObject.Parse(raw); } catch { return fallback; } }
    private void Save(string key, JObject value) => CPH.SetGlobalVar(key, value.ToString(Newtonsoft.Json.Formatting.None), true);
    private string LegacyString(string key, string fallback) => CPH.GetGlobalVar<string>(key, true) ?? fallback;
    private int LegacyInt(string key, int fallback) => CPH.GetGlobalVar<int?>(key, true) ?? fallback;
    private bool LegacyBool(string key, bool fallback) => CPH.GetGlobalVar<bool?>(key, true) ?? fallback;
    private static string Name(string key) => char.ToUpperInvariant(key[0]) + key.Substring(1);
    private static object Value(JToken value) => value.Type == JTokenType.Boolean ? (object)(bool)value : value.Type == JTokenType.Integer ? (object)(int)value : value.ToString();
}
