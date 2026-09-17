using System;
using Newtonsoft.Json.Linq;

// Shared Visual and Branding presets plus entry-point migration/resolution.
public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string ClapperKey = "rts.actionreplay.config.clapper";
    public const string PresetsKey = "rts.actionreplay.config.presets";

    public bool Execute() => EnsureEntryPoints();

    public bool EnsureEntryPoints()
    {
        EnsureDefaults();
        var player = Read(PlayerKey, new JObject());
        var panel = Read(PanelKey, new JObject());
        var clapper = Read(ClapperKey, new JObject());
        MigratePlayer(player); MigratePanel(panel); MigrateClapper(clapper);
        Write(PlayerKey, player); Write(PanelKey, panel); Write(ClapperKey, clapper);
        return true;
    }

    public bool EnsureDefaults()
    {
        var presets = Read(PresetsKey, CreateDefaults());
        var defaults = CreateDefaults();
        if (!(presets["branding"] is JArray)) presets["branding"] = defaults["branding"];
        if (!(presets["visual"] is JArray)) presets["visual"] = defaults["visual"];
        presets["version"] = 1;
        Write(PresetsKey, presets);
        return true;
    }

    public JObject ResolvePlayerEntry(string entryPoint) => Resolve(PlayerKey, entryPoint,
        new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" });

    public JObject ResolvePanelEntry(string entryPoint) => Resolve(PanelKey, entryPoint,
        new[] { "recent", "playlist", "creatorLeaderboard" });

    public JObject ResolveClapper() => Read(ClapperKey, new JObject())["entryPoint"] as JObject ?? new JObject();

    private JObject Resolve(string key, string entryPoint, string[] validEntries)
    {
        var config = Read(key, new JObject());
        var entries = config["entryPoints"] as JObject ?? new JObject();
        var name = string.IsNullOrWhiteSpace(entryPoint) ? validEntries[0] : entryPoint.Trim().ToLowerInvariant();
        if (Array.IndexOf(validEntries, name) < 0) name = validEntries[0];
        var entry = entries[name] as JObject ?? new JObject();
        return new JObject
        {
            ["animationProfile"] = ResolveAnimationProfile(config, entry),
            ["visualPreset"] = ResolveId(Visuals(), (string)entry["visualPreset"]) ?? "broadcast",
            ["brandingPreset"] = ResolveId(Branding(), (string)entry["brandingPreset"]) ?? "default"
        };
    }

    private static string ResolveAnimationProfile(JObject config, JObject entry)
    {
        var explicitId = (string)entry["animationProfile"];
        return string.IsNullOrWhiteSpace(explicitId)
            ? (string)(config["animation"] as JObject)?["selectedProfile"] ?? "default" : explicitId;
    }

    private void MigratePlayer(JObject player)
    {
        var animation = player["animation"] as JObject ?? new JObject();
        var animationEntries = animation["entryPoints"] as JObject ?? new JObject();
        var title = player["title"] as JObject ?? new JObject();
        var visual = NormalizeStyle((string)title["selectedProfile"] ?? CPH.GetGlobalVar<string>("rts.actionreplay.titleBarStyle", true));
        foreach (var point in new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" })
        {
            var entry = player["entryPoints"]?[point] as JObject ?? new JObject();
            entry["animationProfile"] = (string)entry["animationProfile"] ?? (string)animationEntries[point] ?? "default";
            entry["visualPreset"] = (string)entry["visualPreset"] ?? visual;
            entry["brandingPreset"] = (string)entry["brandingPreset"] ?? "default";
            EnsureEntry(player, point, entry);
        }
    }

    private void MigratePanel(JObject panel)
    {
        var preset = panel["preset"] as JObject ?? new JObject();
        var fallback = (string)preset["fallback"] ?? "Broadcast";
        var legacyEntries = preset["entryPoints"] as JObject ?? new JObject();
        var animationEntries = (panel["animation"] as JObject)?["entryPoints"] as JObject ?? new JObject();
        foreach (var point in new[] { "recent", "playlist", "creatorLeaderboard" })
        {
            var entry = panel["entryPoints"]?[point] as JObject ?? new JObject();
            entry["animationProfile"] = (string)entry["animationProfile"] ?? (string)animationEntries[point] ?? "default";
            entry["visualPreset"] = (string)entry["visualPreset"] ?? NormalizeStyle((string)legacyEntries[point] ?? fallback);
            entry["brandingPreset"] = (string)entry["brandingPreset"] ?? "default";
            EnsureEntry(panel, point, entry);
        }
    }

    private void MigrateClapper(JObject clapper)
    {
        var entry = clapper["entryPoint"] as JObject ?? new JObject();
        entry["animationProfile"] = (string)entry["animationProfile"] ?? (string)(clapper["animation"] as JObject)?["selectedProfile"] ?? "default";
        entry["brandingPreset"] = (string)entry["brandingPreset"] ?? "default";
        clapper["entryPoint"] = entry;
    }

    private static void EnsureEntry(JObject config, string name, JObject entry)
    {
        var entries = config["entryPoints"] as JObject ?? new JObject();
        entries[name] = entry; config["entryPoints"] = entries;
    }

    public JArray Branding() => Read(PresetsKey, CreateDefaults())["branding"] as JArray ?? new JArray();
    public JArray Visuals() => Read(PresetsKey, CreateDefaults())["visual"] as JArray ?? new JArray();

    public static string ResolveId(JArray values, string value)
    {
        foreach (var item in values ?? new JArray())
        {
            if (string.Equals((string)item["id"], value, StringComparison.OrdinalIgnoreCase)) return (string)item["id"];
            if (string.Equals((string)item["name"], value, StringComparison.OrdinalIgnoreCase)) return (string)item["id"];
        }
        return null;
    }

    private static string NormalizeStyle(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "broadcast";
        var normalized = value.Trim().ToLowerInvariant();
        return normalized == "broadcast" || normalized == "cinematic" || normalized == "cut" || normalized == "minimal" ? normalized : "broadcast";
    }

    public static JObject CreateDefaults() => new JObject
    {
        ["version"] = 1,
        ["branding"] = new JArray(new JObject
        {
            ["id"] = "default", ["name"] = "Default", ["primaryColor"] = "#0384CBFF", ["secondaryColor"] = "#101416FF",
            ["titleColor"] = "#FFFFFFFF", ["textColor"] = "#FFFFFFFF", ["shadowColor"] = "#000000FF", ["font"] = "Inter",
            ["logo"] = "", ["fallbackText"] = "RTS", ["brandLabel"] = "ACTION REPLAY"
        }),
        ["visual"] = new JArray(
            new JObject { ["id"] = "broadcast", ["name"] = "Broadcast" }, new JObject { ["id"] = "cinematic", ["name"] = "Cinematic" },
            new JObject { ["id"] = "cut", ["name"] = "Cut" }, new JObject { ["id"] = "minimal", ["name"] = "Minimal" })
    };

    public JObject Read(string key, JObject fallback)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        try { return string.IsNullOrWhiteSpace(raw) ? fallback : JObject.Parse(raw); } catch { return fallback; }
    }

    public void Write(string key, JObject value) => CPH.SetGlobalVar(key, value.ToString(Newtonsoft.Json.Formatting.None), true);
}
