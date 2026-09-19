using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    const string PlayerKey = "rts.actionreplay.config.player";
    const string PanelKey = "rts.actionreplay.config.panel";
    const string PresetsKey = "rts.actionreplay.config.presets";
    const string AnimationKey = "rts.actionreplay.config.animation";
    const string PlayerOperationKey = "rts.actionreplay.operation.player";
    const string PanelOperationKey = "rts.actionreplay.operation.panel";
    const string EventName = "RTS-Action Replay";

    public bool Execute() => false;

    public bool ResolvePlayer()
    {
        var op = Read(PlayerOperationKey);
        if (op == null) return false;
        ApplyObject(op);
        var entry = Entry(Read(PlayerKey), "play");
        var animation = Animation(Read(PlayerKey), "player", (string)entry["animationProfile"]);
        var designId = (string)op["designPresetId"] ?? (string)entry["designPreset"] ?? (string)entry["visualPreset"] ?? "broadcast";
        var titleId = (string)op["titlePresetId"] ?? (string)entry["titlePreset"] ?? "default";
        var brandId = (string)op["brandingPresetId"] ?? (string)entry["brandingPreset"] ?? "default";
        if ((bool?)entry["useSourcePlatformBranding"] == true)
        {
            var source = (string)op["replaySource"] ?? "";
            var sourceBrand = PlatformBranding(source);
            if (sourceBrand != null) brandId = (string)sourceBrand["id"] ?? brandId;
        }
        ApplyPresentation(designId, titleId, brandId, animation, false);
        CPH.TriggerEvent(EventName, true);
        CPH.UnsetGlobalVar(PlayerOperationKey, false);
        return true;
    }

    public bool ResolvePanel()
    {
        var op = Read(PanelOperationKey);
        if (op == null) return false;
        ApplyObject(op);
        var panel = Read(PanelKey);
        var type = (string)op["panelType"] ?? "recent";
        var entry = Entry(panel, type);
        var animation = Animation(panel, "panel", (string)entry["animationProfile"]);
        var designId = (string)entry["designPreset"] ?? (string)entry["visualPreset"] ?? "broadcast";
        var titleId = (string)entry["titlePreset"] ?? "default";
        var brandId = (string)entry["brandingPreset"] ?? "default";
        ApplyPresentation(designId, titleId, brandId, animation, true);
        CPH.TriggerEvent(EventName, true);
        CPH.UnsetGlobalVar(PanelOperationKey, false);
        return true;
    }

    JObject Entry(JObject config, string id)
    {
        var entries = config["entryPoints"] as JObject ?? new JObject();
        var e = entries[id] as JObject;
        if (e != null) return e;
        return entries["recent"] as JObject ?? new JObject();
    }

    JObject Animation(JObject config, string target, string profileId)
    {
        profileId = string.IsNullOrWhiteSpace(profileId) ? "default" : profileId;
        var store = Read(AnimationKey);
        var item = (store[target] as JObject)?[profileId] as JObject ?? new JObject();
        var start = item["startSequence"] as JArray ?? new JArray();
        var end = item["endSequence"] as JArray ?? new JArray();
        var positions = ((Read(PresetsKey)["positions"] as JObject)?[target] as JObject) ?? new JObject();
        start = StoredSequence(start, positions, target == "panel" ? "Centered" : "Full Screen");
        end = StoredSequence(end, positions, target == "panel" ? "Centered" : "Full Screen");
        return new JObject {
            ["id"] = profileId, ["name"] = profileId,
            ["start"] = start.Count > 0 ? start : DefaultSequence(target),
            ["end"] = end.Count > 0 ? end : DefaultSequence(target),
            ["positions"] = positions
        };
    }

    JArray StoredSequence(JArray source, JObject positions, string fallback)
    {
        var result = new JArray();
        foreach (var token in source ?? new JArray())
        {
            var row = token as JObject ?? new JObject();
            var name = (string)row["position"] ?? fallback;
            var p = positions[name] as JObject;
            var tag = (string)p?["tag"] ?? name;
            result.Add(new JObject {
                ["position"] = tag,
                ["duration"] = (int?)row["duration"] ?? 0,
                ["delay"] = (int?)row["delay"] ?? 0,
                ["easing"] = (string)row["easing"] ?? "ease-in-out"
            });
        }
        return result;
    }

    JArray DefaultSequence(string target) => new JArray(new JObject {
        ["position"] = target == "panel" ? "centered" : "full-screen",
        ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out"
    });

    void ApplyPresentation(string designId, string titleId, string brandId, JObject animation, bool panel)
    {
        var designs = Read(PresetsKey)["visual"] as JArray ?? new JArray();
        var titles = Read(PresetsKey)["title"] as JArray ?? new JArray();
        var brands = Read(PresetsKey)["branding"] as JArray ?? new JArray();
        var d = Find(designs, designId) ?? Find(designs, "broadcast");
        var t = Find(titles, titleId) ?? Find(titles, "default");
        var b = Find(brands, brandId) ?? Find(brands, "default");
        if (d == null || t == null || b == null) return;
        var design = (string)d["design"] ?? (string)d["id"] ?? "broadcast";
        CPH.SetArgument("replayAnimationProfile", animation.ToString(Newtonsoft.Json.Formatting.None));
        CPH.SetArgument("replayAnimationProfileId", (string)animation["id"]);
        CPH.SetArgument("replayPlayerPositions", ((JObject)animation["positions"] ?? new JObject()).ToString(Newtonsoft.Json.Formatting.None));
        CPH.SetArgument("replayPositions", ((JObject)animation["positions"] ?? new JObject()).ToString(Newtonsoft.Json.Formatting.None));
        var start = animation["start"] as JArray ?? new JArray();
        var end = animation["end"] as JArray ?? new JArray();
        CPH.SetArgument("replayStartPosition", (string)start[0]?["position"] ?? (panel ? "centered" : "full-screen"));
        CPH.SetArgument("replayEndPosition", (string)end[end.Count - 1]?["position"] ?? (panel ? "centered" : "full-screen"));
        CPH.SetArgument("replayPanelPreset", design);
        CPH.SetArgument("replayPanelPrimaryColor", (string)b["primaryColor"] ?? "#0384CBFF");
        CPH.SetArgument("replayPanelSecondaryColor", (string)b["secondaryColor"] ?? "#101416FF");
        CPH.SetArgument("replayPanelTitleFont", (string)b["font"] ?? "Inter");
        CPH.SetArgument("replayPanelTitleSize", (int?)b["fontSize"] ?? 34);
        CPH.SetArgument("replayPanelTitleColor", (string)b["textColor"] ?? "#FFFFFFFF");
        CPH.SetArgument("replayPanelListColor", (string)b["textColor"] ?? "#FFFFFFFF");
        CPH.SetArgument("replayShowTitle", (bool?)t["showTitle"] ?? true);
        CPH.SetArgument("replayTitleDecorationPosition", (string)t["decorationPosition"] ?? "Prefix");
        CPH.SetArgument("replayTitleDecoration", (string)t["decoration"] ?? "Action Replay -");
        CPH.SetArgument("replayTitlePosition", (string)t["position"] ?? "Bottom");
        CPH.SetArgument("replayTitleAnimation", (string)t["animation"] ?? "Left to right");
        CPH.SetArgument("replayTitleDelay", (int?)t["delay"] ?? 2000);
        CPH.SetArgument("replayTitleDuration", (int?)t["duration"] ?? 10000);
        CPH.SetArgument("replayTitleAnimationDuration", (int?)t["animationDuration"] ?? 1000);
        CPH.SetArgument("replayTitleFont", (string)b["font"] ?? "Inter");
        CPH.SetArgument("replayTitleFontSize", (int?)b["fontSize"] ?? 34);
        CPH.SetArgument("replayTitleTextColor", (string)b["textColor"] ?? "#FFFFFFFF");
        CPH.SetArgument("replayTitleShadowColor", (string)b["shadowColor"] ?? "#000000FF");
        CPH.SetArgument("replayTitlePrimaryColor", (string)b["primaryColor"] ?? "#0384CBFF");
        CPH.SetArgument("replayTitleSecondaryColor", (string)b["secondaryColor"] ?? "#101416FF");
        CPH.SetArgument("replayBrandLogoUrl", (string)b["logo"] ?? "");
        CPH.SetArgument("replayBrandFallbackText", (string)b["fallbackText"] ?? "RTS");
        CPH.SetArgument("replayBrandLabel", (string)b["brandLabel"] ?? "ACTION REPLAY");
        CPH.SetArgument("replayBrandFallbackTextColor", (string)b["primaryColor"] ?? "#0384CBFF");
        CPH.SetArgument("replayBrandLabelColor", (string)b["textColor"] ?? "#FFFFFFFF");
        Props("replayBroadcast", Broadcast(d, b));
        Props("replayCut", Cut(d, b));
        CPH.SetArgument("replayDesignPresetId", (string)d["id"] ?? "broadcast");
        CPH.SetArgument("replayTitlePresetId", (string)t["id"] ?? "default");
        CPH.SetArgument("replayBrandingPresetId", (string)b["id"] ?? "default");
    }

    JObject Broadcast(JObject d, JObject b) => new JObject {
        ["primaryColor"]=(string)b["primaryColor"] ?? "#0384CBFF", ["secondaryColor"]=(string)b["secondaryColor"] ?? "#101416FF",
        ["chevronHeight"]=(int?)d["chevronHeight"] ?? 42, ["randomHeight"]=(bool?)d["randomHeight"] ?? false,
        ["chevronWidth"]=(int?)d["chevronWidth"] ?? 42, ["randomWidth"]=(bool?)d["randomWidth"] ?? false,
        ["chevronSpacing"]=(int?)d["chevronSpacing"] ?? 0, ["randomSpacing"]=(bool?)d["randomSpacing"] ?? false,
        ["chevronSpeed"]=(int?)d["chevronSpeed"] ?? 95, ["decorationColor"]=(string)b["titlePrefixSuffixColor"] ?? "#0384CBFF",
        ["titleColor"]=(string)b["titleColor"] ?? "#FFFFFFFF"
    };

    JObject Cut(JObject d, JObject b) => new JObject {
        ["primaryColor"]=(string)b["primaryColor"] ?? "#0384CBFF", ["secondaryColor"]=(string)b["secondaryColor"] ?? "#101416FF",
        ["blockWidth"]=(int?)d["blockWidth"] ?? 170, ["randomWidth"]=(bool?)d["randomWidth"] ?? true,
        ["barHeight"]=(int?)d["barHeight"] ?? 5, ["decorationColor"]=(string)b["titlePrefixSuffixColor"] ?? "#0384CBFF",
        ["titleColor"]=(string)b["titleColor"] ?? "#FFFFFFFF"
    };

    JObject PlatformBranding(string platform)
    {
        foreach (var token in Read(PresetsKey)["branding"] as JArray ?? new JArray())
        {
            var b = token as JObject;
            if (b != null && string.Equals((string)b["platform"], platform, StringComparison.OrdinalIgnoreCase)) return b;
        }
        return null;
    }

    void ApplyObject(JObject value)
    {
        foreach (var p in value.Properties())
        {
            var v = p.Value;
            object o = v.Type == JTokenType.Boolean ? (object)(bool)v : v.Type == JTokenType.Integer ? (object)(int)v : v.Type == JTokenType.Float ? (object)(double)v : v.ToString();
            CPH.SetArgument(p.Name, o);
        }
    }

    JObject Find(JArray values, string id)
    {
        foreach (var x in values ?? new JArray()) if (string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase)) return x as JObject;
        return null;
    }

    JObject Read(string key)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        if (key == PlayerOperationKey || key == PanelOperationKey) raw = CPH.GetGlobalVar<string>(key, false);
        try { return string.IsNullOrWhiteSpace(raw) ? null : JObject.Parse(raw); } catch { return null; }
    }
}
