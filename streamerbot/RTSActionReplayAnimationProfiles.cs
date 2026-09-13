using System;
using Newtonsoft.Json.Linq;

// User-managed animation profiles for replay video and information panels.
public class CPHInline
{
    private const string DefaultEasingKey = "rts.actionreplay.animation.default.easing";
    private const string ProfilesKey = "rts.actionreplay.animation.profiles";
    private const string SelectedProfileKey = "rts.actionreplay.animation.selectedProfile";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";
    private const string PlaybackProfileHandoffKey = "rts.actionreplay.handoff.playbackProfile";
    private const string AnimationProfileHandoffKey = "rts.actionreplay.handoff.animationProfile";
    private const string PanelProfilesKey = "rts.actionreplay.panel.animation.profiles";

    public bool Execute() => EnsureProfiles();
    public bool EnsureProfiles()
    {
        EnsureDefaultProfile(); EnsureProfileRegistry(); EnsureEntryPointDefaults(); EnsurePanelProfiles();
        var selectedId = ResolveProfileId(CPH.GetGlobalVar<string>(SelectedProfileKey, true));
        CPH.SetGlobalVar(SelectedProfileKey, selectedId == null ? "Default" : GetProfileName(selectedId), true); return true;
    }
    public bool AddProfile()
    {
        var name = CPH.TryGetArg("profileName", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "New Profile";
        var profiles = ReadProfiles(); var id = Guid.NewGuid().ToString("N"); profiles.Add(new JObject { ["id"] = id, ["name"] = name }); WriteProfiles(profiles); EnsureProfile(id, name); return true;
    }
    public bool RemoveProfile()
    {
        if (!CPH.TryGetArg("profileId", out string id) || string.IsNullOrWhiteSpace(id)) return false; id = id.Trim(); if (id == "default") return false;
        var profiles = ReadProfiles(); for (var i = profiles.Count - 1; i >= 0; i--) if (string.Equals((string)profiles[i]["id"], id, StringComparison.Ordinal)) profiles.RemoveAt(i);
        ResetEntryPointProfiles(id); WriteProfiles(profiles);
        if (string.Equals(ResolveProfileId(CPH.GetGlobalVar<string>(SelectedProfileKey, true)), id, StringComparison.Ordinal)) CPH.SetGlobalVar(SelectedProfileKey, "Default", true);
        RemoveProfileData(id); return true;
    }
    public bool ResolveEntryPointProfile()
    {
        var entryPoint = CPH.TryGetArg("animationEntryPoint", out string requestedEntryPoint) && !string.IsNullOrWhiteSpace(requestedEntryPoint) ? requestedEntryPoint.Trim() : CPH.GetGlobalVar<string>(EntryPointHandoffKey, false);
        CPH.UnsetGlobalVar(EntryPointHandoffKey, false);
        if (string.IsNullOrWhiteSpace(entryPoint)) return false;
        var configured = CPH.GetGlobalVar<string>("rts.actionreplay.animation.entry." + entryPoint.ToLowerInvariant(), true);
        var profile = ResolveProfileId(configured) ?? "default";
        CPH.SetGlobalVar(ResolvedProfileHandoffKey, profile, false); CPH.SetArgument("replayAnimationProfileId", profile); return true;
    }
    public bool ApplyProfile()
    {
        var handoffRequested = !string.IsNullOrWhiteSpace(CPH.GetGlobalVar<string>(PlaybackProfileHandoffKey, false));
        string profile = null;
        if (CPH.TryGetArg("replayAnimationProfileId", out string explicitProfile) && !string.IsNullOrWhiteSpace(explicitProfile)) profile = explicitProfile.Trim();
        if (string.IsNullOrWhiteSpace(profile)) profile = CPH.GetGlobalVar<string>(PlaybackProfileHandoffKey, false);
        if (string.IsNullOrWhiteSpace(profile)) profile = ResolveProfileId(CPH.GetGlobalVar<string>(SelectedProfileKey, true));
        CPH.UnsetGlobalVar(PlaybackProfileHandoffKey, false);
        if (string.IsNullOrWhiteSpace(profile) || !ProfileExists(profile)) profile = "default";
        var key = "rts.actionreplay.animation." + profile + "."; var start = ReadSequence(key + "startSequence"); var end = ReadSequence(key + "endSequence");
        if (start.Count == 0) start.Add(new JObject { ["position"] = GetProfileString(profile, "startPosition", "Full Screen"), ["duration"] = 0, ["delay"] = 0, ["easing"] = GetProfileString(profile, "easing", GetEasing()) });
        if (end.Count == 0) end.Add(new JObject { ["position"] = GetProfileString(profile, "endPosition", "Full Screen"), ["duration"] = (int)Math.Round(GetProfileDouble(profile, "duration", .5) * 1000), ["delay"] = 0, ["easing"] = GetProfileString(profile, "easing", GetEasing()) });
        CPH.SetArgument("profileId", profile);
        var profileJson = new JObject { ["id"] = profile, ["name"] = GetProfileName(profile), ["start"] = start, ["end"] = end }.ToString(Newtonsoft.Json.Formatting.None);
        CPH.SetArgument("replayAnimationProfile", profileJson);
        if (handoffRequested) CPH.SetGlobalVar(AnimationProfileHandoffKey, profileJson, false);
        CPH.SetArgument("replayStartPosition", GetProfileString(profile, "startPosition", "Full Screen")); CPH.SetArgument("replayEndPosition", GetProfileString(profile, "endPosition", "Full Screen")); CPH.SetArgument("replayAnimationDuration", GetProfileDouble(profile, "duration", .5)); CPH.SetArgument("replayAnimationEasing", GetProfileString(profile, "easing", GetEasing())); return true;
    }
    public bool GetProfile() => ApplyProfile();

    public bool EnsurePanelProfiles()
    {
        var profiles = ReadPanelProfiles();
        var cleaned = new JArray(); var hasDefault = false;
        foreach (var item in profiles)
        {
            var id = (string)item["id"]; if (string.IsNullOrWhiteSpace(id)) continue;
            if (id == "default") { if (hasDefault) continue; hasDefault = true; cleaned.Add(new JObject { ["id"] = "default", ["name"] = "Default" }); continue; }
            var name = CPH.GetGlobalVar<string>("rts.actionreplay.panel.animation." + id + ".name", true) ?? (string)item["name"] ?? "New Panel Profile";
            cleaned.Add(new JObject { ["id"] = id, ["name"] = name }); EnsurePanelProfile(id, name);
        }
        if (!hasDefault) cleaned.Insert(0, new JObject { ["id"] = "default", ["name"] = "Default" });
        WritePanelProfiles(cleaned);
        SetPanelDefault("rts.actionreplay.panel.animation.entry.recent", "Default"); SetPanelDefault("rts.actionreplay.panel.animation.entry.playlist", "Default"); SetPanelDefault("rts.actionreplay.panel.animation.entry.creatorLeaderboard", "Default"); SetPanelDefault("rts.actionreplay.panel.animation.entry.playbackLeaderboard", "Default");
        return true;
    }

    public bool AddPanelProfile()
    {
        var name = CPH.TryGetArg("panelProfileName", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "New Panel Profile";
        var profiles = ReadPanelProfiles(); var id = Guid.NewGuid().ToString("N"); profiles.Add(new JObject { ["id"] = id, ["name"] = name }); WritePanelProfiles(profiles); EnsurePanelProfile(id, name); return true;
    }

    public bool RemovePanelProfile()
    {
        if (!CPH.TryGetArg("panelProfileId", out string id) || string.IsNullOrWhiteSpace(id) || id.Trim() == "default") return false; id = id.Trim();
        var profiles = ReadPanelProfiles(); for (var i = profiles.Count - 1; i >= 0; i--) if (string.Equals((string)profiles[i]["id"], id, StringComparison.Ordinal)) profiles.RemoveAt(i);
        foreach (var panel in new[] { "recent", "playlist", "creatorLeaderboard", "playbackLeaderboard" }) SetPanelDefault("rts.actionreplay.panel.animation.entry." + panel, "Default");
        WritePanelProfiles(profiles); RemovePanelProfileData(id); return true;
    }

    public bool ResolvePanelAnimation()
    {
        var panel = CPH.TryGetArg("panelType", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "recent";
        var configured = CPH.GetGlobalVar<string>("rts.actionreplay.panel.animation.entry." + panel.ToLowerInvariant(), true);
        var profile = ResolvePanelProfileId(configured) ?? "default";
        var key = "rts.actionreplay.panel.animation." + profile + ".";
        var start = ReadSequence(key + "startSequence"); var end = ReadSequence(key + "endSequence");
        if (start.Count == 0) start = DefaultPanelStart(); if (end.Count == 0) end = DefaultPanelEnd();
        var json = new JObject { ["id"] = profile, ["name"] = GetPanelProfileName(profile), ["start"] = start, ["end"] = end }.ToString(Newtonsoft.Json.Formatting.None);
        CPH.SetArgument("replayPanelAnimation", json); return true;
    }

    private JArray DefaultPanelStart() => new JArray(new JObject { ["position"] = "Hidden Left", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" }, new JObject { ["position"] = "__PANEL_POSITION__", ["duration"] = 600, ["delay"] = 0, ["easing"] = "ease-out" });
    private JArray DefaultPanelEnd() => new JArray(new JObject { ["position"] = "__PANEL_POSITION__", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" }, new JObject { ["position"] = "Hidden Left", ["duration"] = 600, ["delay"] = 0, ["easing"] = "ease-in" });

    private void EnsureDefaultProfile()
    {
        SetDefault("rts.actionreplay.animation.default.name", "Default");
        SetDefault("rts.actionreplay.animation.default.startSequence", "[{\"position\":\"Mini Hidden\",\"duration\":0,\"delay\":0,\"easing\":\"ease-in-out\"},{\"position\":\"Mini Angled\",\"duration\":1000,\"delay\":3000,\"easing\":\"ease-in-out\"},{\"position\":\"Mini\",\"duration\":1000,\"delay\":0,\"easing\":\"ease-in-out\"}]");
        SetDefault("rts.actionreplay.animation.default.endSequence", "[{\"position\":\"Mini Hidden\",\"duration\":1000,\"delay\":0,\"easing\":\"ease-in-out\"}]");
    }
    private void EnsureProfile(string profile, string name)
    {
        SetDefault("rts.actionreplay.animation." + profile + ".name", name); SetDefault("rts.actionreplay.animation." + profile + ".startSequence", "[{\"position\":\"Full Screen\",\"duration\":0,\"delay\":0,\"easing\":\"ease-in-out\"}]"); SetDefault("rts.actionreplay.animation." + profile + ".endSequence", "[{\"position\":\"Mini Hidden\",\"duration\":1000,\"delay\":0,\"easing\":\"ease-in-out\"}]");
    }
    private void EnsureEntryPointDefaults() { foreach (var point in new[] { "obs", "twitch", "recent", "catalog", "playlist" }) SetDefault("rts.actionreplay.animation.entry." + point, "Default"); }
    private void ResetEntryPointProfiles(string removedId)
    {
        foreach (var point in new[] { "obs", "twitch", "recent", "catalog", "playlist" })
        {
            var key = "rts.actionreplay.animation.entry." + point; var raw = CPH.GetGlobalVar<string>(key, true);
            if (string.Equals(raw, removedId, StringComparison.Ordinal) || string.Equals(ResolveProfileId(raw), removedId, StringComparison.Ordinal)) CPH.SetGlobalVar(key, "Default", true);
        }
    }
    private void EnsureProfileRegistry()
    {
        var profiles = ReadProfiles(); var cleaned = new JArray(); var hasDefault = false;
        foreach (var item in profiles)
        {
            var id = (string)item["id"]; if (string.IsNullOrWhiteSpace(id)) continue;
            if (id == "default") { if (hasDefault) continue; hasDefault = true; cleaned.Add(new JObject { ["id"] = "default", ["name"] = "Default" }); continue; }
            if (id == "fullScreen" || id == "halfScreen" || id == "twitchClip" || id == "obsClip" || id == "playlist" || id == "recent") continue;
            var name = CPH.GetGlobalVar<string>("rts.actionreplay.animation." + id + ".name", true) ?? (string)item["name"]; if (string.IsNullOrWhiteSpace(name)) name = "New Profile"; cleaned.Add(new JObject { ["id"] = id, ["name"] = name }); EnsureProfile(id, name);
        }
        if (!hasDefault) cleaned.Insert(0, new JObject { ["id"] = "default", ["name"] = "Default" }); WriteProfiles(cleaned); RemoveLegacyProfileData();
    }
    private string ResolveProfileId(string value) { if (string.IsNullOrWhiteSpace(value)) return null; if (ProfileExists(value)) return value; foreach (var item in ReadProfiles()) if (string.Equals((string)item["name"], value, StringComparison.OrdinalIgnoreCase)) return (string)item["id"]; return null; }
    private string GetProfileName(string id)
    {
        if (id == "default") return "Default"; var value = CPH.GetGlobalVar<string>("rts.actionreplay.animation." + id + ".name", true); if (!string.IsNullOrWhiteSpace(value)) return value;
        foreach (var item in ReadProfiles()) if (string.Equals((string)item["id"], id, StringComparison.Ordinal)) return (string)item["name"] ?? "New Profile"; return "New Profile";
    }
    private bool ProfileExists(string id) { if (string.IsNullOrWhiteSpace(id)) return false; foreach (var item in ReadProfiles()) if (string.Equals((string)item["id"], id, StringComparison.Ordinal)) return true; return false; }
    private void RemoveLegacyProfileData() { foreach (var id in new[] { "fullScreen", "halfScreen", "twitchClip", "obsClip", "playlist", "recent" }) RemoveProfileData(id); }
    private void RemoveProfileData(string id) { if (string.IsNullOrWhiteSpace(id) || id == "default") return; CPH.UnsetGlobalVar("rts.actionreplay.animation." + id + ".name", true); CPH.UnsetGlobalVar("rts.actionreplay.animation." + id + ".startSequence", true); CPH.UnsetGlobalVar("rts.actionreplay.animation." + id + ".endSequence", true); }
    private JArray ReadProfiles() { var raw = CPH.GetGlobalVar<string>(ProfilesKey, true); if (string.IsNullOrWhiteSpace(raw)) return new JArray(); try { return JArray.Parse(raw); } catch { return new JArray(); } }
    private void WriteProfiles(JArray profiles) => CPH.SetGlobalVar(ProfilesKey, profiles.ToString(Newtonsoft.Json.Formatting.None), true);
    private string GetEasing() => CPH.GetGlobalVar<string>(DefaultEasingKey, true) ?? "ease-in-out";
    private string GetProfileString(string profile, string field, string fallback) { var value = CPH.GetGlobalVar<string>("rts.actionreplay.animation." + profile + "." + field, true); return string.IsNullOrWhiteSpace(value) ? fallback : value; }
    private double GetProfileDouble(string profile, string field, double fallback) { var value = CPH.GetGlobalVar<double?>("rts.actionreplay.animation." + profile + "." + field, true); return value ?? fallback; }
    private void SetDefault(string key, string value) { if (CPH.GetGlobalVar<string>(key, true) == null) CPH.SetGlobalVar(key, value, true); }
    private JArray ReadSequence(string key) { var raw = CPH.GetGlobalVar<string>(key, true); if (string.IsNullOrWhiteSpace(raw)) return new JArray(); try { return JArray.Parse(raw); } catch { return new JArray(); } }

    private JArray ReadPanelProfiles() { var raw = CPH.GetGlobalVar<string>(PanelProfilesKey, true); if (string.IsNullOrWhiteSpace(raw)) return new JArray(); try { return JArray.Parse(raw); } catch { return new JArray(); } }
    private void WritePanelProfiles(JArray profiles) => CPH.SetGlobalVar(PanelProfilesKey, profiles.ToString(Newtonsoft.Json.Formatting.None), true);
    private void EnsurePanelProfile(string id, string name)
    {
        SetPanelDefault("rts.actionreplay.panel.animation." + id + ".name", name);
        SetPanelDefault("rts.actionreplay.panel.animation." + id + ".startSequence", "[{\"position\":\"Hidden Left\",\"duration\":0,\"delay\":0,\"easing\":\"ease-in-out\"},{\"position\":\"__PANEL_POSITION__\",\"duration\":600,\"delay\":0,\"easing\":\"ease-out\"}]");
        SetPanelDefault("rts.actionreplay.panel.animation." + id + ".endSequence", "[{\"position\":\"__PANEL_POSITION__\",\"duration\":0,\"delay\":0,\"easing\":\"ease-in-out\"},{\"position\":\"Hidden Left\",\"duration\":600,\"delay\":0,\"easing\":\"ease-in\"}]");
    }
    private void SetPanelDefault(string key, string value) { if (CPH.GetGlobalVar<string>(key, true) == null) CPH.SetGlobalVar(key, value, true); }
    private string ResolvePanelProfileId(string value) { if (string.IsNullOrWhiteSpace(value)) return null; if (PanelProfileExists(value)) return value; foreach (var item in ReadPanelProfiles()) if (string.Equals((string)item["name"], value, StringComparison.OrdinalIgnoreCase)) return (string)item["id"]; return null; }
    private bool PanelProfileExists(string id) { if (string.IsNullOrWhiteSpace(id)) return false; foreach (var item in ReadPanelProfiles()) if (string.Equals((string)item["id"], id, StringComparison.Ordinal)) return true; return false; }
    private string GetPanelProfileName(string id)
    {
        if (id == "default") return "Default"; var value = CPH.GetGlobalVar<string>("rts.actionreplay.panel.animation." + id + ".name", true); if (!string.IsNullOrWhiteSpace(value)) return value;
        foreach (var item in ReadPanelProfiles()) if (string.Equals((string)item["id"], id, StringComparison.Ordinal)) return (string)item["name"] ?? "New Panel Profile"; return "New Panel Profile";
    }
    private void RemovePanelProfileData(string id) { CPH.UnsetGlobalVar("rts.actionreplay.panel.animation." + id + ".name", true); CPH.UnsetGlobalVar("rts.actionreplay.panel.animation." + id + ".startSequence", true); CPH.UnsetGlobalVar("rts.actionreplay.panel.animation." + id + ".endSequence", true); }
}