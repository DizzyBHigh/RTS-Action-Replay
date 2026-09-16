using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string UiPrefix = "rts.actionreplay.ui.animation.";
    private const string PanelPresetUiPrefix = "rts.actionreplay.ui.panelPreset.";
    private const string TitleUiPrefix = "rts.actionreplay.ui.title.";
    private const string ClapperPositionsUiKey = "rts.actionreplay.ui.clapperPositions";
    private const string ClapperPositionsKey = "rts.actionreplay.clapper.positions";

    public bool Execute()
    {
        var ui = new RtsUI("RTS Action Replay", "1.0.0",
            (key, persisted) => CPH.GetGlobalVar<bool?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<int?>(key, persisted),
            (key, persisted) => ReadUiValue(key),
            (key, persisted) => (object)CPH.GetGlobalVar<string>(key, persisted),
            (key, value, persisted) => SaveUiValue(key, value, persisted),
            message => CPH.LogInfo(message));
        BuildSettings(ui); ui.ShowUI(); return true;
    }

    private void AddAnimationProfile(RtsUI ui, string title, string id)
    {
        AddAnimationRows(ui, title, id, PlayerKey, "startSequence", "Full Screen", "Positions");
        AddAnimationRows(ui, title, id, PlayerKey, "endSequence", "Full Screen", "Positions");
    }

    private void AddAnimationRows(RtsUI ui, string title, string description, string configKey, string profile, string sequence, string defaultPosition, string category)
    {
        var key = UiPrefix + (configKey == PlayerKey ? "player" : "panel") + ".profile." + profile + "." + sequence;
        ui.AddDynamicRows(title, description, category, key, rows => { rows.AddDropdown("Position", "Saved position used by this animation step.", configKey == PlayerKey ? BuildAnimationPositionOptions() : BuildPanelAnimationPositionOptions(), defaultPosition); rows.AddNumericTextbox("Duration", "Duration of the movement to this position, in milliseconds.", 600, 0, 60000); rows.AddDropdown("Easing", "Timing curve used for the movement to this position.", new[] { "linear", "ease", "ease-in", "ease-out", "ease-in-out" }, "ease-in-out"); rows.AddNumericTextbox("Delay", "Delay before the next animation step starts, in milliseconds.", 0, 0, 60000); }, json => SaveSequence(configKey, profile, sequence, json));
    }

    private string[] BuildProfileOptions(string key) { var list = new List<string>(); foreach (var item in ReadProfiles(key)) { var name = (string)item["name"]; if (!string.IsNullOrWhiteSpace(name)) list.Add(name); } if (list.Count == 0) list.Add("Default"); return list.ToArray(); }
    private JArray ReadProfiles(string key) { var config = ReadConfig(key); var profiles = config["animationProfiles"] as JArray; if (profiles == null || profiles.Count == 0) return new JArray(new JObject { ["id"] = "default", ["name"] = "Default", ["startSequence"] = new JArray(), ["endSequence"] = new JArray() }); return profiles; }
    private string[] BuildAnimationPositionOptions() => BuildPositionOptions(CPH.GetGlobalVar<string>("rts.actionreplay.ui.playerPositions", true), "Full Screen");
    private string[] BuildPanelAnimationPositionOptions() => BuildPositionOptions(CPH.GetGlobalVar<string>("rts.actionreplay.ui.panelPositions", true), "Centered");
    private string[] BuildPositionOptions(string json, string builtIn) { var options = new List<string> { builtIn }; foreach (var item in ParsePositions(json)) { var name = (string)item.Value["name"]; if (!string.IsNullOrWhiteSpace(name) && !options.Contains(name)) options.Add(name); } return options.ToArray(); }
    private string[] BuildClapperPositionOptions() { var json = CPH.GetGlobalVar<string>(ClapperPositionsUiKey, true); var options = new List<string> { "Centered" }; foreach (var item in ParsePositions(json)) { var name = (string)item.Value["name"]; if (!string.IsNullOrWhiteSpace(name) && !options.Contains(name)) options.Add(name); } return options.ToArray(); }

    private string ReadUiValue(string key)
    {
        if (key.StartsWith(PanelPresetUiPrefix, StringComparison.Ordinal)) { var point = key.Substring(PanelPresetUiPrefix.Length); var config = ReadConfig(PanelKey); var preset = config["preset"] as JObject; var entries = preset?["entryPoints"] as JObject; return (string)entries?[point] ?? (string)preset?["fallback"] ?? "Broadcast"; }
        if (key.StartsWith(TitleUiPrefix, StringComparison.Ordinal)) { try { var parts = key.Substring(TitleUiPrefix.Length).Split('.'); var config = ReadConfig(PlayerKey); var title = config["title"] as JObject ?? new JObject(); if (parts[0] == "defaultProfile") return NormalizeTitleProfile((string)title["selectedProfile"] ?? CPH.GetGlobalVar<string>("rts.actionreplay.titleBarStyle", true)); if (parts[0] == "entry") return NormalizeTitleProfile((string)(title["entryPoints"] as JObject)?[parts[1]] ?? (string)title["selectedProfile"]); } catch { } return "Broadcast"; }
        if (!key.StartsWith(UiPrefix, StringComparison.Ordinal)) return CPH.GetGlobalVar<string>(key, true);
        try { var parts = key.Substring(UiPrefix.Length).Split('.'); var configKey = parts[0] == "player" ? PlayerKey : PanelKey; var config = ReadConfig(configKey); if (parts[1] == "selectedProfile") return ProfileName(config, (string)(config["animation"] as JObject)?["selectedProfile"]); if (parts[1] == "entry") { var entries = (config["animation"] as JObject)?["entryPoints"] as JObject; return ProfileName(config, (string)entries?[parts[2]]); } if (parts[1] == "profile") { var profile = FindProfile(config["animationProfiles"] as JArray, parts[2]); if (parts.Length == 4 && parts[3] == "name") return (string)profile?["name"] ?? "New Profile"; if (parts.Length == 4) return SequenceJson(profile, parts[3]); } } catch { } return "";
    }

    private void SaveUiValue(string key, object value, bool persisted)
    {
        if (key.StartsWith(PanelPresetUiPrefix, StringComparison.Ordinal)) { try { var point = key.Substring(PanelPresetUiPrefix.Length); var config = ReadConfig(PanelKey); var preset = config["preset"] as JObject ?? new JObject(); var entries = preset["entryPoints"] as JObject ?? new JObject(); var selected = NormalizeTitleProfile(value == null ? "" : value.ToString()); entries[point] = selected; preset["entryPoints"] = entries; if (string.IsNullOrWhiteSpace((string)preset["fallback"])) preset["fallback"] = "Broadcast"; config["preset"] = preset; SaveConfig(PanelKey, config); } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: panel preset UI save failed: " + ex.Message); } return; }
        if (key.StartsWith(TitleUiPrefix, StringComparison.Ordinal)) { try { var parts = key.Substring(TitleUiPrefix.Length).Split('.'); var config = ReadConfig(PlayerKey); var title = config["title"] as JObject ?? new JObject(); config["title"] = title; var profile = NormalizeTitleProfile(value == null ? "" : value.ToString()); if (parts[0] == "defaultProfile") { title["selectedProfile"] = profile; CPH.SetGlobalVar("rts.actionreplay.titleBarStyle", profile, true); } else if (parts[0] == "entry") { var entries = title["entryPoints"] as JObject ?? new JObject(); entries[parts[1]] = profile; title["entryPoints"] = entries; } SaveConfig(PlayerKey, config); } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: title profile UI save failed: " + ex.Message); } return; }
        if (!key.StartsWith(UiPrefix, StringComparison.Ordinal)) { CPH.SetGlobalVar(key, value, persisted); return; }
    }
}
