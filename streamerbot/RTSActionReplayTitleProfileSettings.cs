using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string UiPrefix = "rts.actionreplay.ui.title.";
    private const string TitleAction = "RTS - Action Replay - Core - Title";

    public bool Execute()
    {
        CPH.ExecuteMethod(TitleAction, "EnsureProfiles");
        var ui = new RtsUI("RTS Action Replay — Title Profiles", "1.0.0",
            (key, persisted) => CPH.GetGlobalVar<bool?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<int?>(key, persisted),
            (key, persisted) => ReadUiValue(key),
            (key, persisted) => (object)CPH.GetGlobalVar<string>(key, persisted),
            (key, value, persisted) => SaveUiValue(key, value, persisted),
            message => CPH.LogInfo(message));
        BuildSettings(ui); ui.ShowUI(); return true;
    }

    private void BuildSettings(RtsUI ui)
    {
        var config = ReadConfig();
        ui.BeginSection("Title Profile Selection", "Replay Title");
        ui.AddDropdown("Default Title Profile", "Fallback title profile used when no entry point has a specific profile configured.", "Replay Title", UiPrefix + "selectedProfile", BuildProfileOptions(config), "Default");
        ui.BeginSection("Entry Point Profiles");
        ui.AddTitle("Choose which title profile is applied to each replay entry point.", "Replay Title");
        ui.BeginRow(); AddEntry(ui, "Create — OBS", "obs", config); AddEntry(ui, "Create — Twitch", "twitch", config); ui.EndRow();
        ui.BeginRow(); AddEntry(ui, "Create — YouTube", "youtube", config); AddEntry(ui, "Create — Kick", "kick", config); ui.EndRow();
        ui.BeginRow(); AddEntry(ui, "Play — Recent", "recent", config); AddEntry(ui, "Play — Catalog", "catalog", config); ui.EndRow();
        AddEntry(ui, "Play — Playlist", "playlist", config);
        ui.EndSection(); ui.EndSection();
        ui.AddClickableButton("Add Title Profile", "Create a new replay title profile copied from Default.", "Add Title Profile", "blue", "Replay Title", () => { CPH.SetArgument("titleProfileName", "New Title Profile"); if (CPH.ExecuteMethod(TitleAction, "AddProfile")) ui.RebuildUI(rebuilt => BuildSettings(rebuilt)); });
        foreach (var item in Profiles(config)) AddProfile(ui, item as JObject);
    }

    private void AddEntry(RtsUI ui, string title, string point, JObject config) => ui.AddDropdown(title, "Title profile used for this replay entry point.", "Replay Title", UiPrefix + "entry." + point, BuildProfileOptions(config), "Default");

    private void AddProfile(RtsUI ui, JObject profile)
    {
        if (profile == null) return;
        var id = (string)profile["id"] ?? "default"; var title = (string)profile["name"] ?? "Default";
        ui.BeginSection(title, "Replay Title");
        if (id == "default") ui.AddTitle("Default profile is permanent. Its title settings can be edited.", "Replay Title");
        else
        {
            ui.AddTextbox("Profile Name", "Display name for this replay title profile.", "Replay Title", UiPrefix + "profile." + id + ".name", title, false);
            ui.AddClickableButton("Remove Profile", "Delete this replay title profile.", "Delete Profile", "red", "Replay Title", () => { CPH.SetArgument("titleProfileId", id); if (CPH.ExecuteMethod(TitleAction, "RemoveProfile")) ui.RebuildUI(rebuilt => BuildSettings(rebuilt)); });
        }
        AddCoreFields(ui, id); AddTypography(ui, id); AddBroadcast(ui, id); AddCut(ui, id); ui.EndSection();
    }

    private void AddCoreFields(RtsUI ui, string id)
    {
        ui.AddToggleSwitch("Show Replay Title", "Display the replay title on the video.", "Replay Title", Key(id, "showTitle"), true);
        ui.AddDropdown("Title Decoration Position", "Choose whether the title decoration appears before or after the replay title.", "Replay Title", Key(id, "decorationPosition"), new[] { "Prefix", "Suffix" }, "Suffix");
        ui.AddTextbox("Title Decoration", "Optional text added to the replay title on the player only.", "Replay Title", Key(id, "decoration"), " - Replay Capture", false);
        ui.BeginRow(); ui.AddDropdown("Title Bar Style", "Choose the visual design used for the replay title.", "Replay Title", Key(id, "style"), new[] { "Broadcast", "Cinematic", "Cut", "Minimal" }, "Broadcast"); ui.AddDropdown("Title Position", "Place the title bar at the top or bottom of the video.", "Replay Title", Key(id, "position"), new[] { "Top", "Bottom" }, "Bottom"); ui.EndRow();
        ui.AddDropdown("Title Appearance", "Choose how the title enters and exits.", "Replay Title", Key(id, "animation"), new[] { "Fade", "Left to right", "Right to left", "Slide up/down" }, "Slide up/down");
        ui.BeginRow(); ui.AddNumericTextbox("Title Show Delay", "Delay before the replay title appears, in milliseconds.", "Replay Title", Key(id, "delay"), 0, 0, 10000); ui.AddNumericTextbox("Title Display Duration", "How long the replay title remains visible, in milliseconds. Zero keeps it visible.", "Replay Title", Key(id, "duration"), 5000, 0, 60000); ui.AddNumericTextbox("Title Animation Duration", "Duration of the title entrance and exit animation, in milliseconds.", "Replay Title", Key(id, "animationDuration"), 450, 0, 5000); ui.EndRow();
        ui.BeginRow(); ui.AddColorPicker("Primary Colour", "Primary title accent colour.", "Replay Title", Key(id, "primaryColor"), "#0384CBFF"); ui.AddColorPicker("Secondary Colour", "Secondary title accent colour.", "Replay Title", Key(id, "secondaryColor"), "#101416FF"); ui.EndRow();
    }

    private void AddTypography(RtsUI ui, string id)
    {
        ui.BeginSection("Typography"); ui.BeginRow(); ui.AddGoogleFontSelector("Title Font", "Choose the Google Font used by this title profile.", "Replay Title", Key(id, "font"), "Inter"); ui.AddNumericTextbox("Title Font Size", "Replay title font size in pixels.", "Replay Title", Key(id, "fontSize"), 34, 12, 96); ui.EndRow(); ui.BeginRow(); ui.AddColorPicker("Title Font Color", "Replay title text colour.", "Replay Title", Key(id, "textColor"), "#FFFFFFFF"); ui.AddColorPicker("Title Shadow Color", "Replay title shadow colour.", "Replay Title", Key(id, "shadowColor"), "#000000FF"); ui.EndRow(); ui.EndSection();
    }

    private void AddBroadcast(RtsUI ui, string id)
    {
        ui.BeginSection("Broadcast"); ui.BeginRow(); ui.AddColorPicker("Primary Chevron Colour", "Broadcast primary colour.", "Replay Title", Key(id, "broadcast.primaryColor"), "#0384CBFF"); ui.AddColorPicker("Secondary Chevron Colour", "Broadcast secondary colour.", "Replay Title", Key(id, "broadcast.secondaryColor"), "#FFD400FF"); ui.EndRow();
        ui.BeginRow(); ui.AddNumericTextbox("Chevron Height", "Height of each Broadcast chevron in pixels.", "Replay Title", Key(id, "broadcast.chevronHeight"), 42, 1, 89); ui.AddToggleSwitch("Random Height", "Randomize each Broadcast chevron height.", "Replay Title", Key(id, "broadcast.randomHeight"), false); ui.AddNumericTextbox("Chevron Width", "Width of each Broadcast chevron in pixels.", "Replay Title", Key(id, "broadcast.chevronWidth"), 42, 1, 300); ui.AddToggleSwitch("Random Width", "Randomize each Broadcast chevron width.", "Replay Title", Key(id, "broadcast.randomWidth"), false); ui.EndRow();
        ui.BeginRow(); ui.AddNumericTextbox("Chevron Spacing", "Visible gap between Broadcast chevrons in pixels.", "Replay Title", Key(id, "broadcast.chevronSpacing"), 0, 0, 200); ui.AddToggleSwitch("Random Spacing", "Randomize each Broadcast chevron gap.", "Replay Title", Key(id, "broadcast.randomSpacing"), false); ui.EndRow();
        ui.AddNumericTextbox("Chevron Speed", "Broadcast chevron movement speed in pixels per second.", "Replay Title", Key(id, "broadcast.chevronSpeed"), 95, 10, 500); ui.BeginRow(); ui.AddColorPicker("Title Prefix / Suffix Colour", "Colour of the Broadcast title decoration.", "Replay Title", Key(id, "broadcast.decorationColor"), "#0384CBFF"); ui.AddColorPicker("Title Colour", "Colour of the Broadcast title text.", "Replay Title", Key(id, "broadcast.titleColor"), "#FFFFFFFF"); ui.EndRow(); ui.EndSection();
    }

    private void AddCut(RtsUI ui, string id)
    {
        ui.BeginSection("Cut"); ui.BeginRow(); ui.AddColorPicker("Primary Colour", "Cut primary colour.", "Replay Title", Key(id, "cut.primaryColor"), "#0384CBFF"); ui.AddColorPicker("Secondary Colour", "Cut secondary colour.", "Replay Title", Key(id, "cut.secondaryColor"), "#FFD400FF"); ui.EndRow(); ui.BeginRow(); ui.AddNumericTextbox("Block Width", "Cut block width in pixels.", "Replay Title", Key(id, "cut.blockWidth"), 170, 1, 1000); ui.AddToggleSwitch("Random Width", "Randomize each Cut block width.", "Replay Title", Key(id, "cut.randomWidth"), true); ui.AddNumericTextbox("Bar Height", "Cut accent bar height in pixels.", "Replay Title", Key(id, "cut.barHeight"), 5, 1, 50); ui.EndRow(); ui.BeginRow(); ui.AddColorPicker("Title Prefix / Suffix Colour", "Colour of the Cut title decoration.", "Replay Title", Key(id, "cut.decorationColor"), "#0384CBFF"); ui.AddColorPicker("Title Colour", "Colour of the Cut title text.", "Replay Title", Key(id, "cut.titleColor"), "#FFFFFFFF"); ui.EndRow(); ui.EndSection();
    }

    private string ReadUiValue(string key)
    {
        if (!key.StartsWith(UiPrefix, StringComparison.Ordinal)) return CPH.GetGlobalVar<string>(key, true);
        try
        {
            var parts = key.Substring(UiPrefix.Length).Split('.'); var config = ReadConfig(); var title = config["title"] as JObject ?? new JObject();
            if (parts[0] == "selectedProfile") return ProfileName(config, (string)title["selectedProfile"]);
            if (parts[0] == "entry") { var entries = title["entryPoints"] as JObject ?? new JObject(); return ProfileName(config, (string)entries[parts[1]]); }
            if (parts[0] == "profile") { var profile = FindProfile(Profiles(config), parts[1]); if (parts.Length == 3 && parts[2] == "name") return (string)profile?["name"] ?? "New Title Profile"; return ReadProfileValue(profile, string.Join(".", parts, 2, parts.Length - 2)); }
        }
        catch { }
        return "";
    }

    private void SaveUiValue(string key, object value, bool persisted)
    {
        if (!key.StartsWith(UiPrefix, StringComparison.Ordinal)) { CPH.SetGlobalVar(key, value, persisted); return; }
        try
        {
            var parts = key.Substring(UiPrefix.Length).Split('.'); var config = ReadConfig(); var title = config["title"] as JObject ?? new JObject(); config["title"] = title; var text = value == null ? "" : value.ToString();
            if (parts[0] == "selectedProfile") title["selectedProfile"] = ResolveProfileId(config, text);
            else if (parts[0] == "entry") { var entries = title["entryPoints"] as JObject ?? new JObject(); entries[parts[1]] = ResolveProfileId(config, text); title["entryPoints"] = entries; }
            else if (parts[0] == "profile") { var profile = FindProfile(Profiles(config), parts[1]); if (profile != null) SetProfileValue(profile, string.Join(".", parts, 2, parts.Length - 2), value); }
            SaveConfig(config);
        }
        catch (Exception ex) { CPH.LogWarn("RTS Action Replay: title profile UI save failed: " + ex.Message); }
    }

    private JObject ReadConfig() { var raw = CPH.GetGlobalVar<string>(PlayerKey, true); try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { return new JObject(); } }
    private void SaveConfig(JObject config) => CPH.SetGlobalVar(PlayerKey, config.ToString(Newtonsoft.Json.Formatting.None), true);
    private JArray Profiles(JObject config) => config["titleProfiles"] as JArray ?? new JArray();
    private string[] BuildProfileOptions(JObject config) { var list = new List<string>(); foreach (var item in Profiles(config)) { var name = (string)item["name"]; if (!string.IsNullOrWhiteSpace(name)) list.Add(name); } return list.Count == 0 ? new[] { "Default" } : list.ToArray(); }
    private JObject FindProfile(JArray profiles, string id) { foreach (var item in profiles ?? new JArray()) if (string.Equals((string)item["id"], id, StringComparison.Ordinal)) return item as JObject; return null; }
    private string ProfileName(JObject config, string id) => (string)FindProfile(Profiles(config), id)?["name"] ?? "Default";
    private string ResolveProfileId(JObject config, string value) { foreach (var item in Profiles(config)) if (string.Equals((string)item["id"], value, StringComparison.Ordinal) || string.Equals((string)item["name"], value, StringComparison.OrdinalIgnoreCase)) return (string)item["id"]; return "default"; }
    private string Key(string id, string field) => UiPrefix + "profile." + id + "." + field;
    private string ReadProfileValue(JObject profile, string field) { if (profile == null) return ""; var parts = field.Split('.'); JToken token = profile; foreach (var part in parts) token = token?[part]; return token == null ? "" : token.Type == JTokenType.Boolean ? token.ToString().ToLowerInvariant() : token.ToString(); }
    private void SetProfileValue(JObject profile, string field, object value) { var parts = field.Split('.'); JObject target = profile; for (var i = 0; i < parts.Length - 1; i++) { target[parts[i]] = target[parts[i]] as JObject ?? new JObject(); target = (JObject)target[parts[i]]; } var name = parts[parts.Length - 1]; var text = value == null ? "" : value.ToString(); var old = target[name]; if (old != null && old.Type == JTokenType.Boolean && bool.TryParse(text, out var b)) target[name] = b; else if (old != null && old.Type == JTokenType.Integer && int.TryParse(text, out var n)) target[name] = n; else target[name] = text; }
}
