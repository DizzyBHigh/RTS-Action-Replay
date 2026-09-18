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
    private const string PlaybackProfileHandoffKey = "rts.actionreplay.handoff.playbackProfile";
    private const string AnimationProfileHandoffKey = "rts.actionreplay.handoff.animationProfile";
    private const string PlayerPositionsHandoffKey = "rts.actionreplay.handoff.playerPositions";
    private const string PositionStoreAction = "RTS - Action Replay - Core - Position Store";

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

    public bool ApplyProfile()
    {
        var handoffRequested = !string.IsNullOrWhiteSpace(CPH.GetGlobalVar<string>(PlaybackProfileHandoffKey, false));
        string profile = null;
        if (CPH.TryGetArg("replayAnimationProfileId", out string explicitProfile) && !string.IsNullOrWhiteSpace(explicitProfile)) profile = explicitProfile.Trim();
        if (string.IsNullOrWhiteSpace(profile)) profile = CPH.GetGlobalVar<string>(PlaybackProfileHandoffKey, false);
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        NormalizePositionConfig(player, false);
        var legacyProfiles = player["animationProfiles"] as JArray;
        var profiles = NormalizeProfiles(legacyProfiles);
        EnsureSequenceStore("player", profiles, legacyProfiles);
        if (string.IsNullOrWhiteSpace(profile)) profile = (string)((JObject)player["animation"])?["selectedProfile"];
        profile = ResolveProfileId(profiles, profile) ?? "default";
        CPH.UnsetGlobalVar(PlaybackProfileHandoffKey, false);
        var item = FindProfile(profiles, profile) ?? CreateProfile("default", "Default");
        var sequences = GetProfileSequences("player", profile);
        var start = ToStoredSequence("player", sequences["startSequence"] as JArray ?? new JArray());
        var end = ToStoredSequence("player", sequences["endSequence"] as JArray ?? new JArray());
        if (start.Count == 0) start = DefaultPlayerStart();
        if (end.Count == 0) end = DefaultPlayerEnd();
        var profileJson = new JObject { ["id"] = profile, ["name"] = (string)item["name"] ?? "Default", ["start"] = start, ["end"] = end }.ToString(Newtonsoft.Json.Formatting.None);
        var positions = GetUnifiedPlayerPositions();
        var playerPositions = positions.ToString(Newtonsoft.Json.Formatting.None);
        CPH.SetArgument("profileId", profile);
        CPH.SetArgument("replayAnimationProfile", profileJson);
        CPH.SetArgument("replayPlayerPositions", playerPositions);
        CPH.SetArgument("replayStartPosition", (string)start[0]["position"] ?? "Full Screen");
        CPH.SetArgument("replayEndPosition", (string)end[end.Count - 1]["position"] ?? "Full Screen");
        CPH.SetArgument("replayAnimationDuration", 0.5);
        CPH.SetArgument("replayAnimationEasing", (string)start[0]["easing"] ?? "ease-in-out");
        if (handoffRequested)
        {
            CPH.SetGlobalVar(AnimationProfileHandoffKey, profileJson, false);
            CPH.SetGlobalVar(PlayerPositionsHandoffKey, playerPositions, false);
        }
        return true;
    }

    public bool GetProfile() => ApplyProfile();

    public bool ResolvePanelAnimation()
    {
        var panelType = CPH.TryGetArg("panelType", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "recent";
        var panel = ReadConfig(PanelKey, CreatePanelDefaults());
        NormalizePositionConfig(panel, true);
        var legacyProfiles = panel["animationProfiles"] as JArray;
        var profiles = NormalizeProfiles(legacyProfiles);
        EnsureSequenceStore("panel", profiles, legacyProfiles);
        var animation = panel["animation"] as JObject ?? new JObject();
        var entries = animation["entryPoints"] as JObject ?? new JObject();
        var profile = ResolveProfileId(profiles, (string)entries[panelType.ToLowerInvariant()]) ?? "default";
        var item = FindProfile(profiles, profile) ?? CreatePanelProfile("default", "Default");
        var sequences = GetProfileSequences("panel", profile);
        var start = sequences["startSequence"] as JArray ?? DefaultPanelStart();
        var end = sequences["endSequence"] as JArray ?? DefaultPanelEnd();
        if (start.Count == 0) start = DefaultPanelStart();
        if (end.Count == 0) end = DefaultPanelEnd();
        CPH.SetArgument("replayPanelPositions", (panel["positions"] as JObject ?? new JObject()).ToString(Newtonsoft.Json.Formatting.None));
        CPH.SetArgument("replayPanelAnimation", new JObject { ["id"] = profile, ["name"] = (string)item["name"] ?? "Default", ["start"] = start, ["end"] = end }.ToString(Newtonsoft.Json.Formatting.None));
        CPH.SetArgument("presetComponent", "panel");
        CPH.SetArgument("entryPoint", panelType.ToLowerInvariant());
        CPH.ExecuteMethod("RTS - Action Replay - Core - Preset Store", "ResolveEntryPoint");
        CPH.ExecuteMethod("RTS - Action Replay - Core - Preset Store", "ApplyVisualAndBranding");
        ApplyVisualBrandingHandoff();
        return true;
    }


    private void ApplyVisualBrandingHandoff()
    {
        var key = "rts.actionreplay.handoff.visualBranding";
        CPH.TryGetArg("replayQueueEntryId", out string entryId);
        if (!string.IsNullOrWhiteSpace(entryId)) key += "." + entryId;
        var raw = CPH.GetGlobalVar<string>(key, false);
        if (string.IsNullOrWhiteSpace(raw)) return;
        try
        {
            ApplyArguments(JObject.Parse(raw));
        }
        catch { }
    }

    private string ResolvePanelPreset(JObject panel, string panelType)
    {
        var preset = panel["preset"] as JObject;
        var entries = preset?["entryPoints"] as JObject;
        var value = (string)entries?[panelType.ToLowerInvariant()];
        return string.IsNullOrWhiteSpace(value) ? (string)preset?["fallback"] ?? "Broadcast" : value;
    }

    private JArray ToStoredSequence(string target, JArray input)
    {
        var positions = (ReadConfig("rts.actionreplay.config.presets", new JObject())["positions"] as JObject)?[target] as JObject ?? new JObject();
        var rows = new JArray();
        foreach (var token in input ?? new JArray())
        {
            var row = token as JObject ?? new JObject();
            var value = (string)row["position"] ?? "";
            var position = positions[value] as JObject;
            var tag = (string)position?["tag"] ?? value;
            rows.Add(new JObject { ["position"] = tag, ["duration"] = (int?)row["duration"] ?? 0, ["delay"] = (int?)row["delay"] ?? 0, ["easing"] = (string)row["easing"] ?? "ease-in-out" });
        }
        return rows;
    }

    private JObject GetUnifiedPlayerPositions()
    {
        CPH.ExecuteMethod(PositionStoreAction, "GetPlayerPositions");
        var raw = CPH.GetGlobalVar<string>(PlayerPositionsHandoffKey, false);
        try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); }
        catch { return new JObject(); }
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