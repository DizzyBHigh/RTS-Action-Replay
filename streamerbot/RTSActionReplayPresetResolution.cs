using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string ClapperKey = "rts.actionreplay.config.clapper";
    private const string PresetsKey = RTSActionReplayPresetStore.PresetsKey;

    public bool Execute() => EnsureEntryPoints();

    public bool EnsureEntryPoints()
    {
        var store = new RTSActionReplayPresetStore();
        store.EnsureDefaults();
        var player = store.Read(PlayerKey, new JObject());
        var panel = store.Read(PanelKey, new JObject());
        var clapper = store.Read(ClapperKey, new JObject());
        MigratePlayer(player);
        MigratePanel(panel);
        MigrateClapper(clapper);
        store.Write(PlayerKey, player);
        store.Write(PanelKey, panel);
        store.Write(ClapperKey, clapper);
        return true;
    }

    public JObject ResolvePlayerEntry(string entryPoint) => Resolve(PlayerKey, entryPoint,
        new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" });

    public JObject ResolvePanelEntry(string entryPoint) => Resolve(PanelKey, entryPoint,
        new[] { "recent", "playlist", "creatorLeaderboard" });

    public JObject ResolveClapper() =>
        new RTSActionReplayPresetStore().Read(ClapperKey, new JObject())["entryPoint"] as JObject ?? new JObject();

    private JObject Resolve(string configKey, string entryPoint, string[] validEntries)
    {
        var store = new RTSActionReplayPresetStore();
        var config = store.Read(configKey, new JObject());
        var entries = config["entryPoints"] as JObject ?? new JObject();
        var name = string.IsNullOrWhiteSpace(entryPoint) ? validEntries[0] : entryPoint.Trim().ToLowerInvariant();
        if (Array.IndexOf(validEntries, name) < 0) name = validEntries[0];
        var entry = entries[name] as JObject ?? new JObject();
        return new JObject
        {
            ["animationProfile"] = ResolveAnimationProfile(config, entry),
            ["visualPreset"] = RTSActionReplayPresetStore.ResolveId(store.Visuals(), (string)entry["visualPreset"]) ?? "broadcast",
            ["brandingPreset"] = RTSActionReplayPresetStore.ResolveId(store.Branding(), (string)entry["brandingPreset"]) ?? "default"
        };
    }

    private static string ResolveAnimationProfile(JObject config, JObject entry)
    {
        var explicitId = (string)entry["animationProfile"];
        if (!string.IsNullOrWhiteSpace(explicitId)) return explicitId;
        return (string)(config["animation"] as JObject)?["selectedProfile"] ?? "default";
    }

    private static void MigratePlayer(JObject player)
    {
        var animationEntries = (player["animation"] as JObject)?["entryPoints"] as JObject ?? new JObject();
        var title = player["title"] as JObject ?? new JObject();
        var visual = NormalizeStyle((string)title["selectedProfile"] ??
            CPH.GetGlobalVar<string>("rts.actionreplay.titleBarStyle", true));
        foreach (var point in new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" })
        {
            var entry = player["entryPoints"]?[point] as JObject ?? new JObject();
            entry["animationProfile"] = (string)entry["animationProfile"] ?? (string)animationEntries[point] ?? "default";
            entry["visualPreset"] = (string)entry["visualPreset"] ?? visual;
            entry["brandingPreset"] = (string)entry["brandingPreset"] ?? "default";
            EnsureEntry(player, point, entry);
        }
    }

    private static void MigratePanel(JObject panel)
    {
        var preset = panel["preset"] as JObject ?? new JObject();
        var legacyFallback = (string)preset["fallback"] ?? "Broadcast";
        var legacyEntries = preset["entryPoints"] as JObject ?? new JObject();
        var animationEntries = (panel["animation"] as JObject)?["entryPoints"] as JObject ?? new JObject();
        foreach (var point in new[] { "recent", "playlist", "creatorLeaderboard" })
        {
            var entry = panel["entryPoints"]?[point] as JObject ?? new JObject();
            entry["animationProfile"] = (string)entry["animationProfile"] ?? (string)animationEntries[point] ?? "default";
            entry["visualPreset"] = (string)entry["visualPreset"] ?? NormalizeStyle((string)legacyEntries[point] ?? legacyFallback);
            entry["brandingPreset"] = (string)entry["brandingPreset"] ?? "default";
            EnsureEntry(panel, point, entry);
        }
    }

    private static void MigrateClapper(JObject clapper)
    {
        var entry = clapper["entryPoint"] as JObject ?? new JObject();
        entry["animationProfile"] = (string)entry["animationProfile"] ??
            (string)(clapper["animation"] as JObject)?["selectedProfile"] ?? "default";
        entry["brandingPreset"] = (string)entry["brandingPreset"] ?? "default";
        clapper["entryPoint"] = entry;
    }

    private static void EnsureEntry(JObject config, string name, JObject entry)
    {
        var entries = config["entryPoints"] as JObject ?? new JObject();
        entries[name] = entry;
        config["entryPoints"] = entries;
    }

    private static string NormalizeStyle(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "broadcast";
        var normalized = value.Trim().ToLowerInvariant();
        return normalized == "broadcast" || normalized == "cinematic" || normalized == "cut" || normalized == "minimal"
            ? normalized : "broadcast";
    }
}
