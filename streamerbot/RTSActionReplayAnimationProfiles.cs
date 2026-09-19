using System;
using Newtonsoft.Json.Linq;

// User-managed animation profiles for replay video, information panels and Clapperboard messages.
public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string AnimationKey = "rts.actionreplay.config.animation";
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string ClapperKey = "rts.actionreplay.config.clapper";
    private const string ClapperPositionsKey = "rts.actionreplay.clapper.positions";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";

    public bool Execute() => EnsureProfiles();

    public bool EnsureProfiles()
    {
        EnsurePlayerProfiles();
        EnsurePanelProfiles();
        EnsureClapperProfiles();
        return true;
    }

    public bool EnsureClapperProfiles()
    {
        var clapper = ReadConfig(ClapperKey, CreateClapperDefaults());
        var legacyProfiles = clapper["animationProfiles"] as JArray;
        var profiles = NormalizeProfiles(legacyProfiles);
        EnsureSequenceStore("clapperboard", profiles, legacyProfiles);
        var animation = clapper["animation"] as JObject ?? new JObject();
        animation["selectedProfile"] = ResolveProfileId(profiles, (string)animation["selectedProfile"]) ?? "default";
        clapper["animationProfiles"] = profiles;
        clapper["animation"] = animation;
        SaveConfig(ClapperKey, clapper);
        return true;
    }

    private void EnsurePlayerProfiles()
    {
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        NormalizePositionConfig(player, false);
        var legacyProfiles = player["animationProfiles"] as JArray;
        var profiles = NormalizeProfiles(legacyProfiles);
        EnsureSequenceStore("player", profiles, legacyProfiles);
        player["animationProfiles"] = profiles;
        var animation = player["animation"] as JObject ?? new JObject();
        animation["selectedProfile"] = ResolveProfileId(profiles, (string)animation["selectedProfile"]) ?? "default";
        animation["entryPoints"] = NormalizeEntryPoints(animation["entryPoints"] as JObject, profiles, new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" });
        player["animation"] = animation;
        SaveConfig(PlayerKey, player);
    }

    private void EnsurePanelProfiles()
    {
        var panel = ReadConfig(PanelKey, CreatePanelDefaults());
        NormalizePositionConfig(panel, true);
        var legacyProfiles = panel["animationProfiles"] as JArray;
        var profiles = NormalizeProfiles(legacyProfiles);
        EnsureSequenceStore("panel", profiles, legacyProfiles);
        panel["animationProfiles"] = profiles;
        var animation = panel["animation"] as JObject ?? new JObject();
        animation["entryPoints"] = NormalizeEntryPoints(animation["entryPoints"] as JObject, profiles, new[] { "recent", "playlist", "creatorLeaderboard" });
        panel["animation"] = animation;
        panel["preset"] = NormalizePresetConfig(panel["preset"]);
        SaveConfig(PanelKey, panel);
    }

    public bool AddProfile()
    {
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        var profiles = NormalizeProfiles(player["animationProfiles"] as JArray);
        var name = CPH.TryGetArg("profileName", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "New Profile";
        var id = Guid.NewGuid().ToString("N");
        profiles.Add(CreateProfile(id, name));
        player["animationProfiles"] = profiles;
        SaveConfig(PlayerKey, player);
        EnsureSequenceStore("player", profiles, null);
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
        RemoveProfileSequences("player", id);
        return true;
    }

    public bool AddPanelProfile()
    {
        var panel = ReadConfig(PanelKey, CreatePanelDefaults());
        var profiles = NormalizeProfiles(panel["animationProfiles"] as JArray);
        var name = CPH.TryGetArg("panelProfileName", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "New Panel Profile";
        var id = Guid.NewGuid().ToString("N");
        profiles.Add(CreatePanelProfile(id, name));
        panel["animationProfiles"] = profiles;
        SaveConfig(PanelKey, panel);
        EnsureSequenceStore("panel", profiles, null);
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
        RemoveProfileSequences("panel", id);
        return true;
    }

    public bool AddClapperProfile()
    {
        var clapper = ReadConfig(ClapperKey, CreateClapperDefaults());
        var profiles = NormalizeProfiles(clapper["animationProfiles"] as JArray);
        var name = CPH.TryGetArg("clapperProfileName", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "New Clapperboard Profile";
        var id = Guid.NewGuid().ToString("N");
        profiles.Add(CreateClapperProfile(id, name));
        clapper["animationProfiles"] = profiles;
        SaveConfig(ClapperKey, clapper);
        EnsureSequenceStore("clapperboard", profiles, null);
        return true;
    }

    public bool RemoveClapperProfile()
    {
        if (!CPH.TryGetArg("clapperProfileId", out string id) || string.IsNullOrWhiteSpace(id) || id.Trim() == "default") return false;
        id = id.Trim();
        var clapper = ReadConfig(ClapperKey, CreateClapperDefaults());
        var profiles = NormalizeProfiles(clapper["animationProfiles"] as JArray);
        for (var i = profiles.Count - 1; i >= 0; i--)
            if (string.Equals((string)profiles[i]["id"], id, StringComparison.Ordinal)) profiles.RemoveAt(i);
        var animation = clapper["animation"] as JObject ?? new JObject();
        animation["selectedProfile"] = ResolveProfileId(profiles, (string)animation["selectedProfile"]) ?? "default";
        clapper["animationProfiles"] = profiles;
        clapper["animation"] = animation;
        SaveConfig(ClapperKey, clapper);
        RemoveProfileSequences("clapperboard", id);
        return true;
    }

    public bool ResolveEntryPointProfile()
    {
        var entryPoint = CPH.TryGetArg("animationEntryPoint", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : CPH.GetGlobalVar<string>(EntryPointHandoffKey, false);
        CPH.UnsetGlobalVar(EntryPointHandoffKey, false);
        if (string.IsNullOrWhiteSpace(entryPoint)) return false;
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        var profiles = NormalizeProfiles(player["animationProfiles"] as JArray);
        var animation = player["animation"] as JObject ?? new JObject();
        var entries = animation["entryPoints"] as JObject ?? new JObject();
        var profile = ResolveProfileId(profiles, (string)entries[entryPoint.ToLowerInvariant()]) ?? "default";
        CPH.SetArgument("replayTitleEntryPoint", entryPoint.ToLowerInvariant());
        CPH.SetGlobalVar(ResolvedProfileHandoffKey, profile, false);
        CPH.SetArgument("replayAnimationProfileId", profile);
        return true;
    }

    private string ResolvePanelPreset(JObject panel, string panelType)
    {
        var preset = panel["preset"] as JObject;
        var entries = preset?["entryPoints"] as JObject;
        var value = (string)entries?[panelType.ToLowerInvariant()];
        return string.IsNullOrWhiteSpace(value) ? (string)preset?["fallback"] ?? "Broadcast" : value;
    }

    private JObject ReadClapperPositions()
    {
        var raw = CPH.GetGlobalVar<string>(ClapperPositionsKey, true);
        try
        {
            var positions = string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw);
            if (!positions.ContainsKey("Centered")) positions["Centered"] = new JObject { ["name"] = "Centered", ["tag"] = "centered", ["scale"] = 50, ["scaleX"] = 50, ["scaleY"] = 50, ["x"] = 0, ["y"] = 0, ["z"] = 0, ["rotateX"] = 0, ["rotateY"] = 0, ["rotateZ"] = 0, ["fov"] = 90 };
            return positions;
        }
        catch { return new JObject { ["Centered"] = new JObject { ["name"] = "Centered", ["tag"] = "centered", ["scale"] = 50, ["scaleX"] = 50, ["scaleY"] = 50, ["x"] = 0, ["y"] = 0, ["z"] = 0, ["rotateX"] = 0, ["rotateY"] = 0, ["rotateZ"] = 0, ["fov"] = 90 } }; }
    }

    private JObject NormalizePresetConfig(JToken source)
    {
        var legacy = source as JValue;
        var legacyPreset = legacy?.Type == JTokenType.String ? legacy.ToString() : null;
        var objectSource = source as JObject;
        var entries = objectSource?["entryPoints"] as JObject;
        var fallback = (string)objectSource?["fallback"] ?? legacyPreset ?? "Broadcast";
        var result = new JObject { ["fallback"] = fallback, ["entryPoints"] = new JObject() };
        foreach (var name in new[] { "recent", "playlist", "creatorLeaderboard" }) result["entryPoints"][name] = (string)entries?[name] ?? fallback;
        return result;
    }

    private void NormalizePositionConfig(JObject config, bool panel)
    {
        var positions = config["positions"] as JObject ?? new JObject();
        var obsolete = new[] { "Hidden Left", "Hidden Right", "Hidden Top", "Hidden Bottom", "Center", "Top", "Bottom", "Top Left", "Top Right", "Bottom Left", "Bottom Right" };
        foreach (var name in obsolete) positions.Remove(name);
        if (panel) positions.Remove("Full Screen");
        var builtIn = panel
            ? new JObject { ["name"] = "Centered", ["tag"] = "centered", ["scale"] = 100, ["scaleX"] = 100, ["scaleY"] = 100, ["x"] = 0, ["y"] = 0, ["z"] = 0, ["rotateX"] = 0, ["rotateY"] = 0, ["rotateZ"] = 0, ["fov"] = 90 }
            : new JObject { ["name"] = "Full Screen", ["tag"] = "full-screen", ["scale"] = 100, ["scaleX"] = 100, ["scaleY"] = 100, ["x"] = 0, ["y"] = 0, ["z"] = 0, ["rotateX"] = 0, ["rotateY"] = 0, ["rotateZ"] = 0, ["fov"] = 90 };
        positions[panel ? "Centered" : "Full Screen"] = builtIn;
        config["positions"] = positions;
    }

    private JArray NormalizeProfiles(JArray source)
    {
        var result = new JArray();
        JObject defaultProfile = null;
        foreach (var token in source ?? new JArray())
        {
            var id = (string)token["id"];
            if (string.IsNullOrWhiteSpace(id) || FindProfile(result, id) != null) continue;
            var item = new JObject { ["id"] = id, ["name"] = id == "default" ? "Default" : (string)token["name"] ?? "New Profile" };
            if (id == "default") defaultProfile = item; else result.Add(item);
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
        foreach (var property in result.Properties()) if (string.Equals((string)property.Value, id, StringComparison.Ordinal)) property.Value = "default";
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
        foreach (var item in profiles ?? new JArray()) if (string.Equals((string)item["id"], id, StringComparison.Ordinal)) return (JObject)item;
        return null;
    }

    private JObject CreateProfile(string id, string name) => new JObject { ["id"] = id, ["name"] = name };
    private JObject CreatePanelProfile(string id, string name) => new JObject { ["id"] = id, ["name"] = name };
    private JObject CreateClapperProfile(string id, string name) => new JObject { ["id"] = id, ["name"] = name };

    private JObject CreatePlayerDefaults() => new JObject { ["version"] = 1, ["positions"] = new JObject(), ["animationProfiles"] = new JArray(CreateProfile("default", "Default")), ["animation"] = new JObject { ["selectedProfile"] = "default", ["entryPoints"] = new JObject { ["obs"] = "default", ["twitch"] = "default", ["youtube"] = "default", ["kick"] = "default", ["recent"] = "default", ["catalog"] = "default", ["playlist"] = "default" } } };
    private JObject CreatePanelDefaults() => new JObject { ["version"] = 1, ["width"] = 500, ["height"] = 700, ["positions"] = new JObject(), ["animationProfiles"] = new JArray(CreatePanelProfile("default", "Default")), ["animation"] = new JObject { ["entryPoints"] = new JObject { ["recent"] = "default", ["playlist"] = "default", ["creatorLeaderboard"] = "default" } }, ["preset"] = new JObject { ["fallback"] = "Broadcast", ["entryPoints"] = new JObject { ["recent"] = "Broadcast", ["playlist"] = "Broadcast", ["creatorLeaderboard"] = "Broadcast" } } };
    private JObject CreateClapperDefaults() => new JObject { ["version"] = 1, ["animationProfiles"] = new JArray(CreateClapperProfile("default", "Default")), ["animation"] = new JObject { ["selectedProfile"] = "default" } };

    private JObject GetProfileSequences(string target, string id)
    {
        var store = ReadConfig(AnimationKey, new JObject());
        var targetObject = store[target] as JObject;
        var profile = targetObject?[id] as JObject;
        return profile ?? new JObject();
    }

    private void EnsureSequenceStore(string target, JArray profiles, JArray legacyProfiles)
    {
        var store = ReadConfig(AnimationKey, new JObject());
        var targetObject = store[target] as JObject ?? new JObject();
        var changed = false;
        foreach (var token in profiles ?? new JArray())
        {
            var id = (string)token["id"];
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (targetObject[id] is JObject) continue;
            var legacy = FindProfile(legacyProfiles, id);
            targetObject[id] = new JObject
            {
                ["startSequence"] = legacy?["startSequence"] as JArray ?? DefaultSequence(target),
                ["endSequence"] = legacy?["endSequence"] as JArray ?? DefaultEndSequence(target)
            };
            changed = true;
        }
        store[target] = targetObject;
        if (changed) SaveConfig(AnimationKey, store);
    }

    private void RemoveProfileSequences(string target, string id)
    {
        var store = ReadConfig(AnimationKey, new JObject());
        var targetObject = store[target] as JObject;
        if (targetObject == null || targetObject[id] == null) return;
        targetObject.Remove(id);
        store[target] = targetObject;
        SaveConfig(AnimationKey, store);
    }

    private JArray DefaultSequence(string target)
    {
        return target == "player" ? DefaultPlayerStart() : target == "panel" ? DefaultPanelStart() : DefaultClapperStart();
    }

    private JArray DefaultEndSequence(string target)
    {
        return target == "player" ? DefaultPlayerEnd() : target == "panel" ? DefaultPanelEnd() : DefaultClapperEnd();
    }

    private JArray DefaultPlayerStart() => new JArray(new JObject { ["position"] = "Full Screen", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });
    private JArray DefaultPlayerEnd() => new JArray(new JObject { ["position"] = "Full Screen", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });
    private JArray DefaultPanelStart() => new JArray(new JObject { ["position"] = "Centered", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });
    private JArray DefaultPanelEnd() => new JArray(new JObject { ["position"] = "Centered", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });
    private JArray DefaultClapperStart() => new JArray(new JObject { ["position"] = "Centered", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });
    private JArray DefaultClapperEnd() => new JArray(new JObject { ["position"] = "Centered", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });

    private JObject ReadConfig(string key, JObject defaults)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(raw)) return defaults;
        try { return JObject.Parse(raw); } catch { return defaults; }
    }

    private void SaveConfig(string key, JObject value) => CPH.SetGlobalVar(key, value.ToString(Newtonsoft.Json.Formatting.None), true);
}