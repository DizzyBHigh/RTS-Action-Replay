using System;
using Newtonsoft.Json.Linq;

// User-managed animation profiles for replay video and information panels.
public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";
    private const string PlaybackProfileHandoffKey = "rts.actionreplay.handoff.playbackProfile";
    private const string AnimationProfileHandoffKey = "rts.actionreplay.handoff.animationProfile";

    public bool Execute() => EnsureProfiles();

    public bool EnsureProfiles()
    {
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        player["animationProfiles"] = NormalizeProfiles(player["animationProfiles"] as JArray);
        var animation = player["animation"] as JObject ?? new JObject();
        var profiles = (JArray)player["animationProfiles"];
        animation["selectedProfile"] = ResolveProfileId(profiles, (string)animation["selectedProfile"]) ?? "default";
        animation["entryPoints"] = NormalizeEntryPoints(animation["entryPoints"] as JObject, profiles,
            new[] { "obs", "twitch", "recent", "catalog", "playlist" });
        player["animation"] = animation;
        SaveConfig(PlayerKey, player);

        var panel = ReadConfig(PanelKey, CreatePanelDefaults());
        panel["animationProfiles"] = NormalizeProfiles(panel["animationProfiles"] as JArray);
        var panelAnimation = panel["animation"] as JObject ?? new JObject();
        panelAnimation["entryPoints"] = NormalizeEntryPoints(panelAnimation["entryPoints"] as JObject,
            (JArray)panel["animationProfiles"],
            new[] { "recent", "playlist", "creatorLeaderboard", "playbackLeaderboard" });
        panel["animation"] = panelAnimation;
        SaveConfig(PanelKey, panel);
        return true;
    }

    public bool AddProfile()
    {
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        var profiles = NormalizeProfiles(player["animationProfiles"] as JArray);
        var name = CPH.TryGetArg("profileName", out string requested) && !string.IsNullOrWhiteSpace(requested)
            ? requested.Trim() : "New Profile";
        profiles.Add(CreateProfile(Guid.NewGuid().ToString("N"), name));
        player["animationProfiles"] = profiles;
        SaveConfig(PlayerKey, player);
        return true;
    }

    public bool RemoveProfile()
    {
        if (!CPH.TryGetArg("profileId", out string id) || string.IsNullOrWhiteSpace(id) || id.Trim() == "default") return false;
        id = id.Trim();
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        var profiles = NormalizeProfiles(player["animationProfiles"] as JArray);
        for (var i = profiles.Count - 1; i >= 0; i--)
            if (string.Equals((string)profiles[i]["id"], id, StringComparison.Ordinal)) profiles.RemoveAt(i);
        var animation = player["animation"] as JObject ?? new JObject();
        animation["selectedProfile"] = ResolveProfileId(profiles, (string)animation["selectedProfile"]) ?? "default";
        animation["entryPoints"] = ResetRemovedEntries(animation["entryPoints"] as JObject, id);
        player["animationProfiles"] = profiles;
        player["animation"] = animation;
        SaveConfig(PlayerKey, player);
        return true;
    }

    public bool ResolveEntryPointProfile()
    {
        var entryPoint = CPH.TryGetArg("animationEntryPoint", out string requested) && !string.IsNullOrWhiteSpace(requested)
            ? requested.Trim() : CPH.GetGlobalVar<string>(EntryPointHandoffKey, false);
        CPH.UnsetGlobalVar(EntryPointHandoffKey, false);
        if (string.IsNullOrWhiteSpace(entryPoint)) return false;
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        var profiles = NormalizeProfiles(player["animationProfiles"] as JArray);
        var animation = player["animation"] as JObject ?? new JObject();
        var entries = animation["entryPoints"] as JObject ?? new JObject();
        var profile = ResolveProfileId(profiles, (string)entries[entryPoint.ToLowerInvariant()]) ?? "default";
        CPH.SetGlobalVar(ResolvedProfileHandoffKey, profile, false);
        CPH.SetArgument("replayAnimationProfileId", profile);
        return true;
    }

    public bool ApplyProfile()
    {
        var handoffRequested = !string.IsNullOrWhiteSpace(CPH.GetGlobalVar<string>(PlaybackProfileHandoffKey, false));
        string profile = null;
        if (CPH.TryGetArg("replayAnimationProfileId", out string explicitProfile) && !string.IsNullOrWhiteSpace(explicitProfile))
            profile = explicitProfile.Trim();
        if (string.IsNullOrWhiteSpace(profile)) profile = CPH.GetGlobalVar<string>(PlaybackProfileHandoffKey, false);
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        var profiles = NormalizeProfiles(player["animationProfiles"] as JArray);
        if (string.IsNullOrWhiteSpace(profile)) profile = (string)((JObject)player["animation"])?["selectedProfile"];
        profile = ResolveProfileId(profiles, profile) ?? "default";
        CPH.UnsetGlobalVar(PlaybackProfileHandoffKey, false);
        var item = FindProfile(profiles, profile) ?? CreateProfile("default", "Default");
        var start = item["startSequence"] as JArray ?? new JArray();
        var end = item["endSequence"] as JArray ?? new JArray();
        if (start.Count == 0) start = DefaultPlayerStart();
        if (end.Count == 0) end = DefaultPlayerEnd();
        var profileJson = new JObject { ["id"] = profile, ["name"] = (string)item["name"] ?? "Default", ["start"] = start, ["end"] = end }
            .ToString(Newtonsoft.Json.Formatting.None);
        CPH.SetArgument("profileId", profile);
        CPH.SetArgument("replayAnimationProfile", profileJson);
        CPH.SetArgument("replayStartPosition", (string)start[0]["position"] ?? "Full Screen");
        CPH.SetArgument("replayEndPosition", (string)end[end.Count - 1]["position"] ?? "Full Screen");
        CPH.SetArgument("replayAnimationDuration", 0.5);
        CPH.SetArgument("replayAnimationEasing", (string)start[0]["easing"] ?? "ease-in-out");
        if (handoffRequested) CPH.SetGlobalVar(AnimationProfileHandoffKey, profileJson, false);
        return true;
    }

    public bool GetProfile() => ApplyProfile();

    public bool AddPanelProfile()
    {
        var panel = ReadConfig(PanelKey, CreatePanelDefaults());
        var profiles = NormalizeProfiles(panel["animationProfiles"] as JArray);
        var name = CPH.TryGetArg("panelProfileName", out string requested) && !string.IsNullOrWhiteSpace(requested)
            ? requested.Trim() : "New Panel Profile";
        profiles.Add(CreatePanelProfile(Guid.NewGuid().ToString("N"), name));
        panel["animationProfiles"] = profiles;
        SaveConfig(PanelKey, panel);
        return true;
    }

    public bool RemovePanelProfile()
    {
        if (!CPH.TryGetArg("panelProfileId", out string id) || string.IsNullOrWhiteSpace(id) || id.Trim() == "default") return false;
        id = id.Trim();
        var panel = ReadConfig(PanelKey, CreatePanelDefaults());
        var profiles = NormalizeProfiles(panel["animationProfiles"] as JArray);
        for (var i = profiles.Count - 1; i >= 0; i--)
            if (string.Equals((string)profiles[i]["id"], id, StringComparison.Ordinal)) profiles.RemoveAt(i);
        var animation = panel["animation"] as JObject ?? new JObject();
        animation["entryPoints"] = ResetRemovedEntries(animation["entryPoints"] as JObject, id);
        panel["animationProfiles"] = profiles;
        panel["animation"] = animation;
        SaveConfig(PanelKey, panel);
        return true;
    }

    public bool ResolvePanelAnimation()
    {
        var panelType = CPH.TryGetArg("panelType", out string requested) && !string.IsNullOrWhiteSpace(requested)
            ? requested.Trim() : "recent";
        var panel = ReadConfig(PanelKey, CreatePanelDefaults());
        var profiles = NormalizeProfiles(panel["animationProfiles"] as JArray);
        var animation = panel["animation"] as JObject ?? new JObject();
        var entries = animation["entryPoints"] as JObject ?? new JObject();
        var profile = ResolveProfileId(profiles, (string)entries[panelType.ToLowerInvariant()]) ?? "default";
        var item = FindProfile(profiles, profile) ?? CreatePanelProfile("default", "Default");
        var start = item["startSequence"] as JArray ?? DefaultPanelStart();
        var end = item["endSequence"] as JArray ?? DefaultPanelEnd();
        if (start.Count == 0) start = DefaultPanelStart();
        if (end.Count == 0) end = DefaultPanelEnd();
        CPH.SetArgument("replayPanelAnimation", new JObject
        {
            ["id"] = profile, ["name"] = (string)item["name"] ?? "Default", ["start"] = start, ["end"] = end
        }.ToString(Newtonsoft.Json.Formatting.None));
        return true;
    }

    private JArray NormalizeProfiles(JArray source)
    {
        var result = new JArray();
        var defaultProfile = (JObject)null;
        foreach (var token in source ?? new JArray())
        {
            var id = (string)token["id"];
            if (string.IsNullOrWhiteSpace(id) || FindProfile(result, id) != null) continue;
            var item = new JObject
            {
                ["id"] = id,
                ["name"] = id == "default" ? "Default" : (string)token["name"] ?? "New Profile",
                ["startSequence"] = token["startSequence"] as JArray ?? new JArray(),
                ["endSequence"] = token["endSequence"] as JArray ?? new JArray()
            };
            if (id == "default") defaultProfile = item;
            else result.Add(item);
        }
        result.Insert(0, defaultProfile ?? CreateProfile("default", "Default"));
        return result;
    }

    private JObject NormalizeEntryPoints(JObject source, JArray profiles, string[] names)
    {
        var result = new JObject();
        foreach (var name in names) result[name] = ResolveProfileId(profiles, (string)source?[name]) ?? "default";
        return result;
    }

    private JObject ResetRemovedEntries(JObject source, string id)
    {
        var result = source ?? new JObject();
        foreach (var property in result.Properties())
            if (string.Equals((string)property.Value, id, StringComparison.Ordinal)) property.Value = "default";
        return result;
    }

    private string ResolveProfileId(JArray profiles, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        foreach (var item in profiles ?? new JArray())
        {
            if (string.Equals((string)item["id"], value, StringComparison.Ordinal)) return value;
            if (string.Equals((string)item["name"], value, StringComparison.OrdinalIgnoreCase)) return (string)item["id"];
        }
        return null;
    }

    private JObject FindProfile(JArray profiles, string id)
    {
        foreach (var item in profiles ?? new JArray())
            if (string.Equals((string)item["id"], id, StringComparison.Ordinal)) return (JObject)item;
        return null;
    }

    private JObject CreateProfile(string id, string name) => new JObject
    {
        ["id"] = id, ["name"] = name, ["startSequence"] = DefaultPlayerStart(), ["endSequence"] = DefaultPlayerEnd()
    };

    private JObject CreatePanelProfile(string id, string name) => new JObject
    {
        ["id"] = id, ["name"] = name, ["startSequence"] = DefaultPanelStart(), ["endSequence"] = DefaultPanelEnd()
    };

    private JObject CreatePlayerDefaults() => new JObject
    {
        ["version"] = 1, ["positions"] = new JObject(),
        ["animationProfiles"] = new JArray(CreateProfile("default", "Default")),
        ["animation"] = new JObject { ["selectedProfile"] = "default", ["entryPoints"] = new JObject
        {
            ["obs"] = "default", ["twitch"] = "default", ["recent"] = "default", ["catalog"] = "default", ["playlist"] = "default"
        }}
    };

    private JObject CreatePanelDefaults() => new JObject
    {
        ["version"] = 1, ["width"] = 500, ["height"] = 700, ["positions"] = new JObject(),
        ["animationProfiles"] = new JArray(CreatePanelProfile("default", "Default")),
        ["animation"] = new JObject { ["entryPoints"] = new JObject
        {
            ["recent"] = "default", ["playlist"] = "default", ["creatorLeaderboard"] = "default", ["playbackLeaderboard"] = "default"
        }}
    };

    private JArray DefaultPlayerStart() => new JArray(new JObject
    {
        ["position"] = "Full Screen", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out"
    });

    private JArray DefaultPlayerEnd() => new JArray(new JObject
    {
        ["position"] = "Full Screen", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out"
    });

    private JArray DefaultPanelStart() => new JArray(new JObject
    {
        ["position"] = "Centered", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out"
    });

    private JArray DefaultPanelEnd() => new JArray(new JObject
    {
        ["position"] = "Centered", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out"
    });

    private JObject ReadConfig(string key, JObject defaults)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(raw)) return defaults;
        try { return JObject.Parse(raw); }
        catch { return defaults; }
    }

    private void SaveConfig(string key, JObject value) => CPH.SetGlobalVar(key, value.ToString(Newtonsoft.Json.Formatting.None), true);
}