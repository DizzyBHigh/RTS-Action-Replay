using System;
using Newtonsoft.Json.Linq;

// Shared reusable Visual and Branding presets. Component animation profiles remain elsewhere.
public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string ClapperKey = "rts.actionreplay.config.clapper";
    private const string PresetsKey = "rts.actionreplay.config.presets";

    public bool Execute() => EnsurePresets();

    public bool EnsurePresets()
    {
        var player = Read(PlayerKey, new JObject());
        var panel = Read(PanelKey, new JObject());
        var clapper = Read(ClapperKey, new JObject());
        var presets = Read(PresetsKey, CreateDefaults());

        MigrateLegacyPlayer(player);
        MigrateLegacyPanel(panel);
        EnsureClapperDefaults(clapper);

        Write(PresetsKey, presets);
        Write(PlayerKey, player);
        Write(PanelKey, panel);
        Write(ClapperKey, clapper);
        return true;
    }

    public JObject ResolvePlayerEntry(string entryPoint) =>
        Resolve(PlayerKey, entryPoint, new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" });

    public JObject ResolvePanelEntry(string entryPoint) =>
        Resolve(PanelKey, entryPoint, new[] { "recent", "playlist", "creatorLeaderboard" });

    public JObject ResolveClapper() =>
        Read(ClapperKey, new JObject())["entryPoint"] as JObject ?? new JObject();

    private JObject Resolve(string configKey, string entryPoint, string[] validEntries)
    {
        var config = Read(configKey, new JObject());
        var entries = (config["entryPoints"] as JObject) ??
            (config["animation"] as JObject)?["entryPoints"] as JObject ?? new JObject();
        var name = string.IsNullOrWhiteSpace(entryPoint) ? null : entryPoint.Trim().ToLowerInvariant();
        if (Array.IndexOf(validEntries, name) < 0) name = validEntries[0];

        var value = entries[name] as JObject ?? new JObject();
        return new JObject
        {
            ["animationProfile"] = ResolveAnimationProfile(config, value),
            ["visualPreset"] = ResolveId(Visuals(), (string)value["visualPreset"]) ?? "broadcast",
            ["brandingPreset"] = ResolveId(Branding(), (string)value["brandingPreset"]) ?? "default"
        };
    }

    private static string ResolveAnimationProfile(JObject config, JObject entry)
    {
        var explicitId = (string)entry["animationProfile"];
        if (!string.IsNullOrWhiteSpace(explicitId)) return explicitId;
        return (string)(config["animation"] as JObject)?["selectedProfile"] ?? "default";
    }

    private void MigrateLegacyPlayer(JObject player)
    {
        var animation = player["animation"] as JObject ?? new JObject();
        var entries = animation["entryPoints"] as JObject ?? new JObject();
        var title = player["title"] as JObject ?? new JObject();
        var visual = NormalizeLegacyStyle((string)title["selectedProfile"] ??
            CPH.GetGlobalVar<string>("rts.actionreplay.titleBarStyle", true));

        foreach (var point in new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" })
        {
            var entry = player["entryPoints"]?[point] as JObject ?? new JObject();
            if (string.IsNullOrWhiteSpace((string)entry["visualPreset"])) entry["visualPreset"] = visual;
            if (string.IsNullOrWhiteSpace((string)entry["brandingPreset"])) entry["brandingPreset"] = "default";
            if (string.IsNullOrWhiteSpace((string)entry["animationProfile"]))
                entry["animationProfile"] = (string)entries[point] ?? "default";
            EnsureEntry(player, point, entry);
        }
    }

    private static void MigrateLegacyPanel(JObject panel)
    {
        var preset = panel["preset"] as JObject ?? new JObject();
        var legacyFallback = (string)preset["fallback"] ?? "Broadcast";
        var legacyEntries = preset["entryPoints"] as JObject ?? new JObject();
        var animationEntries = (panel["animation"] as JObject)?["entryPoints"] as JObject ?? new JObject();

        foreach (var point in new[] { "recent", "playlist", "creatorLeaderboard" })
        {
            var entry = panel["entryPoints"]?[point] as JObject ?? new JObject();
            var legacyVisual = (string)legacyEntries[point] ?? legacyFallback;
            if (string.IsNullOrWhiteSpace((string)entry["visualPreset"])) entry["visualPreset"] = NormalizeLegacyStyle(legacyVisual);
            if (string.IsNullOrWhiteSpace((string)entry["brandingPreset"])) entry["brandingPreset"] = "default";
            if (string.IsNullOrWhiteSpace((string)entry["animationProfile"]))
                entry["animationProfile"] = (string)animationEntries[point] ?? "default";
            EnsureEntry(panel, point, entry);
        }
    }

    private static void EnsureClapperDefaults(JObject clapper)
    {
        var entry = clapper["entryPoint"] as JObject ?? new JObject();
        if (string.IsNullOrWhiteSpace((string)entry["animationProfile"]))
            entry["animationProfile"] = (string)(clapper["animation"] as JObject)?["selectedProfile"] ?? "default";
        if (string.IsNullOrWhiteSpace((string)entry["brandingPreset"])) entry["brandingPreset"] = "default";
        clapper["entryPoint"] = entry;
    }

    private static void EnsureEntry(JObject config, string name, JObject value)
    {
        var entries = config["entryPoints"] as JObject ?? new JObject();
        entries[name] = value;
        config["entryPoints"] = entries;
    }

    private static JObject CreateDefaults() => new JObject
    {
        ["version"] = 1,
        ["branding"] = new JArray(
            new JObject
            {
                ["id"] = "default",
                ["name"] = "Default",
                ["primaryColor"] = "#0384CBFF",
                ["secondaryColor"] = "#101416FF",
                ["titleColor"] = "#FFFFFFFF",
                ["textColor"] = "#FFFFFFFF",
                ["shadowColor"] = "#000000FF",
                ["font"] = "Inter",
                ["logo"] = "",
                ["fallbackText"] = "RTS",
                ["brandLabel"] = "ACTION REPLAY"
            }),
        ["visual"] = new JArray(
            new JObject { ["id"] = "broadcast", ["name"] = "Broadcast" },
            new JObject { ["id"] = "cinematic", ["name"] = "Cinematic" },
            new JObject { ["id"] = "cut", ["name"] = "Cut" },
            new JObject { ["id"] = "minimal", ["name"] = "Minimal" })
    };

    private JArray Branding() => Read(PresetsKey, CreateDefaults())["branding"] as JArray ?? new JArray();
    private JArray Visuals() => Read(PresetsKey, CreateDefaults())["visual"] as JArray ?? new JArray();

    private static string ResolveId(JArray values, string value)
    {
        foreach (var item in values ?? new JArray())
        {
            if (string.Equals((string)item["id"], value, StringComparison.OrdinalIgnoreCase)) return (string)item["id"];
            if (string.Equals((string)item["name"], value, StringComparison.OrdinalIgnoreCase)) return (string)item["id"];
        }
        return null;
    }

    private static string NormalizeLegacyStyle(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "broadcast";
        var normalized = value.Trim().ToLowerInvariant();
        return normalized == "cinematic" || normalized == "cut" || normalized == "minimal" || normalized == "broadcast"
            ? normalized : "broadcast";
    }

    private JObject Read(string key, JObject fallback)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        try { return string.IsNullOrWhiteSpace(raw) ? fallback : JObject.Parse(raw); }
        catch { return fallback; }
    }

    private void Write(string key, JObject value) =>
        CPH.SetGlobalVar(key, value.ToString(Newtonsoft.Json.Formatting.None), true);
}
