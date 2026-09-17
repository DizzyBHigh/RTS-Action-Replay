using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

// Dedicated settings UI for reusable Visual and Branding presets.
public class CPHInline
{
    private const string PresetsKey = "rts.actionreplay.config.presets";
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string ClapperKey = "rts.actionreplay.config.clapper";
    private const string UiPrefix = "rts.actionreplay.ui.presetSettings.";

    public bool Execute()
    {
        CPH.ExecuteMethod("RTS - Action Replay - Core - Presets Store", "EnsureDefaults");
        CPH.ExecuteMethod("RTS - Action Replay - Core - Presets Store", "EnsureEntryPoints");
        var ui = new RtsUI("RTS Action Replay Presets", "1.0.0",
            (key, persisted) => CPH.GetGlobalVar<bool?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<int?>(key, persisted),
            (key, persisted) => ReadUiValue(key),
            (key, persisted) => (object)CPH.GetGlobalVar<string>(key, persisted),
            (key, value, persisted) => SaveUiValue(key, value, persisted),
            message => CPH.LogInfo(message));
        Build(ui); ui.ShowUI(); return true;
    }

    private void Build(RtsUI ui) { AddBranding(ui); AddVisual(ui); AddPlayerEntries(ui); AddPanelEntries(ui); AddClapper(ui); }

    private void AddBranding(RtsUI ui)
    {
        ui.AddTitle("Reusable identity and typography. Branding presets contain no layout or geometry.", "Branding Presets");
        foreach (var p in Presets("branding")) AddBrandingPreset(ui, p as JObject);
        ui.AddClickableButton("Add Branding Preset", "Create a new reusable Branding Preset.", "Add Branding Preset", "blue", "Branding Presets", () => { AddPreset("branding"); ui.RebuildUI(Build); });
    }

    private void AddBrandingPreset(RtsUI ui, JObject p)
    {
        var id = (string)p["id"]; if (string.IsNullOrWhiteSpace(id)) return; ui.BeginSection((string)p["name"] ?? id, "Branding Presets");
        ui.AddTextbox("Preset Name", "Display name for this Branding Preset.", "Branding Presets", Key("branding", id, "name"), (string)p["name"] ?? "Branding", false);
        ui.BeginRow(); ui.AddColorPicker("Primary Colour", "Primary brand colour.", "Branding Presets", Key("branding", id, "primaryColor"), (string)p["primaryColor"] ?? "#0384CBFF"); ui.AddColorPicker("Secondary Colour", "Secondary brand colour.", "Branding Presets", Key("branding", id, "secondaryColor"), (string)p["secondaryColor"] ?? "#101416FF"); ui.EndRow();
        ui.BeginRow(); ui.AddColorPicker("Title Colour", "Replay title colour.", "Branding Presets", Key("branding", id, "titleColor"), (string)p["titleColor"] ?? "#FFFFFFFF"); ui.AddColorPicker("Title Prefix / Suffix Colour", "Colour used by title prefix or suffix decoration.", "Branding Presets", Key("branding", id, "titlePrefixSuffixColor"), (string)p["titlePrefixSuffixColor"] ?? "#0384CBFF"); ui.EndRow();
        ui.BeginRow(); ui.AddColorPicker("Text Colour", "General branded text colour.", "Branding Presets", Key("branding", id, "textColor"), (string)p["textColor"] ?? "#FFFFFFFF"); ui.AddColorPicker("Shadow Colour", "Text shadow colour.", "Branding Presets", Key("branding", id, "shadowColor"), (string)p["shadowColor"] ?? "#000000FF"); ui.EndRow();
        ui.BeginRow(); ui.AddGoogleFontSelector("Font", "Google Font used by the branded title and text.", "Branding Presets", Key("branding", id, "font"), (string)p["font"] ?? "Inter"); ui.AddNumericTextbox("Font Size", "Default branded font size in pixels.", "Branding Presets", Key("branding", id, "fontSize"), (int?)p["fontSize"] ?? 34, 12, 96); ui.EndRow();
        ui.AddTextbox("Logo URL", "HTTPS URL of the branding logo.", "Branding Presets", Key("branding", id, "logo"), (string)p["logo"] ?? "", false);
        ui.BeginRow(); ui.AddTextbox("Fallback Text", "Text used when no logo is defined.", "Branding Presets", Key("branding", id, "fallbackText"), (string)p["fallbackText"] ?? "RTS", false); ui.AddTextbox("Brand Label", "Label displayed beside the logo or fallback text.", "Branding Presets", Key("branding", id, "brandLabel"), (string)p["brandLabel"] ?? "ACTION REPLAY", false); ui.EndRow();
        if (id != "default") ui.AddClickableButton("Remove Preset", "Delete this Branding Preset.", "Remove Preset", "red", "Branding Presets", () => { RemovePreset("branding", id); ui.RebuildUI(Build); }); ui.EndSection();
    }

    private void AddVisual(RtsUI ui)
    {
        ui.AddTitle("Configure the reusable visual treatments that can be changed. Cinematic and Minimal are fixed designs.", "Visual Presets");
        foreach (var p in Presets("visual")) { var id = (string)p["id"]; if (id == "broadcast" || id == "cut") AddVisualPreset(ui, p as JObject); }
    }

    private void AddVisualPreset(RtsUI ui, JObject p)
    {
        var id = (string)p["id"]; if (string.IsNullOrWhiteSpace(id)) return; ui.BeginSection((string)p["name"] ?? id, "Visual Presets");
        AddTitleFields(ui, p);
        if (id == "broadcast") AddBroadcastFields(ui, p); else AddCutFields(ui, p);
        ui.EndSection();
    }

    private void AddTitleFields(RtsUI ui, JObject p)
    {
        ui.BeginRow(); AddBool(ui, "Show Title", "showTitle", p, true); ui.AddDropdown("Decoration Position", "Position of the title prefix or suffix decoration.", "Visual Presets", Key("visual", (string)p["id"], "decorationPosition"), new[] { "Prefix", "Suffix" }, (string)p["decorationPosition"] ?? "Suffix"); ui.EndRow();
        ui.BeginRow(); ui.AddTextbox("Decoration", "Text added before or after the replay title.", "Visual Presets", Key("visual", (string)p["id"], "decoration"), (string)p["decoration"] ?? " - Replay Capture", false); ui.AddDropdown("Position", "Place the replay title at the top or bottom of the video.", "Visual Presets", Key("visual", (string)p["id"], "position"), new[] { "Top", "Bottom" }, (string)p["position"] ?? "Bottom"); ui.EndRow();
        ui.BeginRow(); ui.AddDropdown("Animation", "Animation used when the replay title enters and leaves the player.", "Visual Presets", Key("visual", (string)p["id"], "animation"), new[] { "Fade", "Left to right", "Right to left", "Slide up/down" }, (string)p["animation"] ?? "Slide up/down"); AddInt(ui, "Show Delay", "delay", p, 0, 0, 10000); AddInt(ui, "Display Duration", "duration", p, 5000, 0, 60000); AddInt(ui, "Animation Duration", "animationDuration", p, 450, 0, 5000); ui.EndRow();
    }

    private void AddBroadcastFields(RtsUI ui, JObject p)
    { ui.BeginRow(); AddInt(ui, "Chevron Height", "chevronHeight", p, 42, 1, 89); AddBool(ui, "Randomize Height", "randomHeight", p, false); AddInt(ui, "Chevron Width", "chevronWidth", p, 42, 1, 300); AddBool(ui, "Randomize Width", "randomWidth", p, false); ui.EndRow(); ui.BeginRow(); AddInt(ui, "Chevron Spacing", "chevronSpacing", p, 0, 0, 200); AddBool(ui, "Randomize Spacing", "randomSpacing", p, false); AddInt(ui, "Chevron Speed", "chevronSpeed", p, 95, 10, 500); ui.EndRow(); }
    private void AddCutFields(RtsUI ui, JObject p)
    { ui.BeginRow(); AddInt(ui, "Block Width", "blockWidth", p, 170, 1, 600); AddBool(ui, "Randomize Width", "randomWidth", p, true); AddInt(ui, "Bar Height", "barHeight", p, 5, 1, 50); ui.EndRow(); }

    private void AddPlayerEntries(RtsUI ui) => AddEntries(ui, PlayerKey, new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" }, new[] { "Create — OBS", "Create — Twitch", "Create — YouTube", "Create — Kick", "Play — Recent", "Play — Catalog", "Play — Playlist" }, "Player Entry Points");
    private void AddPanelEntries(RtsUI ui) => AddEntries(ui, PanelKey, new[] { "recent", "playlist", "creatorLeaderboard" }, new[] { "Recent / Search", "Playlist", "Creator Leaderboard" }, "Panel Entry Points");
    private void AddClapper(RtsUI ui)
    { var c = Read(ClapperKey); var e = c["entryPoint"] as JObject ?? new JObject(); ui.AddDropdown("Branding Preset", "Branding preset used by the Clapperboard.", "Clapperboard", UiPrefix + "clapper.branding", Names("branding"), Name("branding", (string)e["brandingPreset"] ?? "default")); }

    private void AddEntries(RtsUI ui, string configKey, string[] ids, string[] labels, string category)
    { var c = Read(configKey); var entries = c["entryPoints"] as JObject ?? new JObject(); for (var i = 0; i < ids.Length; i++) { var e = entries[ids[i]] as JObject ?? new JObject(); ui.BeginSection(labels[i], category); ui.AddDropdown("Visual Preset", "Visual preset used by this entry point.", category, UiPrefix + "entry." + configKey + "." + ids[i] + ".visual", Names("visual"), Name("visual", (string)e["visualPreset"] ?? "broadcast")); ui.AddDropdown("Branding Preset", "Branding preset used by this entry point.", category, UiPrefix + "entry." + configKey + "." + ids[i] + ".branding", Names("branding"), Name("branding", (string)e["brandingPreset"] ?? "default")); ui.EndSection(); } }

    private string Key(string type, string id, string field) => UiPrefix + type + "." + id + "." + field;
    private void AddInt(RtsUI ui, string label, string field, JObject p, int fallback, int min, int max) => ui.AddNumericTextbox(label, "Visual preset setting.", "Visual Presets", Key("visual", (string)p["id"], field), (int?)p[field] ?? fallback, min, max);
    private void AddBool(RtsUI ui, string label, string field, JObject p, bool fallback) => ui.AddToggleSwitch(label, "Visual preset setting.", "Visual Presets", Key("visual", (string)p["id"], field), (bool?)p[field] ?? fallback);
    private JArray Presets(string type) => Read(PresetsKey)[type] as JArray ?? new JArray();
    private string[] Names(string type) { var list = new List<string>(); foreach (var p in Presets(type)) { var n = (string)p["name"]; if (!string.IsNullOrWhiteSpace(n)) list.Add(n); } return list.Count == 0 ? new[] { type == "visual" ? "Broadcast" : "Default" } : list.ToArray(); }
    private string Name(string type, string id) { foreach (var p in Presets(type)) if (string.Equals((string)p["id"], id, StringComparison.OrdinalIgnoreCase)) return (string)p["name"] ?? id; return type == "visual" ? "Broadcast" : "Default"; }

    private string ReadUiValue(string key)
    { if (!key.StartsWith(UiPrefix, StringComparison.Ordinal)) return CPH.GetGlobalVar<string>(key, true); var p = key.Substring(UiPrefix.Length).Split('.'); try { if (p[0] == "branding" || p[0] == "visual") return PresetValue(p[0], p[1], p[2]); if (p[0] == "entry") return EntryValue(p[1], p[2], p[3]); if (p[0] == "clapper") return ClapperValue(p[1]); } catch { } return ""; }
    private string PresetValue(string type, string id, string field) { var p = Find(type, id); return p == null ? "" : p[field]?.ToString() ?? ""; }
    private string EntryValue(string configKey, string id, string field) { var c = Read(configKey); var e = (c["entryPoints"] as JObject)?[id] as JObject ?? new JObject(); return Name(field, (string)e[field + "Preset"] ?? (field == "visual" ? "broadcast" : "default")); }
    private string ClapperValue(string field) { var c = Read(ClapperKey); var e = c["entryPoint"] as JObject ?? new JObject(); return field == "branding" ? Name("branding", (string)e["brandingPreset"] ?? "default") : ""; }

    private void SaveUiValue(string key, object value, bool persisted)
    { if (!key.StartsWith(UiPrefix, StringComparison.Ordinal)) { CPH.SetGlobalVar(key, value, persisted); return; } var p = key.Substring(UiPrefix.Length).Split('.'); var text = value == null ? "" : value.ToString(); try { if (p[0] == "branding" || p[0] == "visual") SavePresetField(p[0], p[1], p[2], text); else if (p[0] == "entry") SaveEntry(p[1], p[2], p[3], text); else if (p[0] == "clapper" && p[1] == "branding") SaveClapperBranding(text); } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: preset settings save failed: " + ex.Message); } }
    private void SavePresetField(string type, string id, string field, string value) { var p = Find(type, id); if (p == null) return; p[field] = JToken.FromObject(Parse(field, value)); Save(PresetsKey, Read(PresetsKey)); }
    private void SaveEntry(string configKey, string id, string field, string value) { var c = Read(configKey); var entries = c["entryPoints"] as JObject ?? new JObject(); var e = entries[id] as JObject ?? new JObject(); e[field + "Preset"] = ResolveId(field, value); entries[id] = e; c["entryPoints"] = entries; Save(configKey, c); }
    private void SaveClapperBranding(string value) { var c = Read(ClapperKey); var e = c["entryPoint"] as JObject ?? new JObject(); e["brandingPreset"] = ResolveId("branding", value); c["entryPoint"] = e; Save(ClapperKey, c); }

    private void AddPreset(string type)
    { var a = Presets(type); var id = "branding-custom-" + DateTime.Now.Ticks.ToString(); var p = new JObject { ["id"] = id, ["name"] = "New Branding Preset", ["primaryColor"] = "#0384CBFF", ["secondaryColor"] = "#101416FF", ["titleColor"] = "#FFFFFFFF", ["titlePrefixSuffixColor"] = "#0384CBFF", ["textColor"] = "#FFFFFFFF", ["shadowColor"] = "#000000FF", ["font"] = "Inter", ["fontSize"] = 34, ["logo"] = "", ["fallbackText"] = "RTS", ["brandLabel"] = "ACTION REPLAY" }; a.Add(p); Save(PresetsKey, Read(PresetsKey)); }
    private void RemovePreset(string type, string id) { var a = Presets(type); for (var i = a.Count - 1; i >= 0; i--) if (string.Equals((string)a[i]["id"], id, StringComparison.OrdinalIgnoreCase)) a.RemoveAt(i); Save(PresetsKey, Read(PresetsKey)); }
    private JObject Find(string type, string id) { foreach (var p in Presets(type)) if (string.Equals((string)p["id"], id, StringComparison.OrdinalIgnoreCase)) return p as JObject; return null; }
    private string ResolveId(string type, string name) { foreach (var p in Presets(type)) if (string.Equals((string)p["name"], name, StringComparison.OrdinalIgnoreCase)) return (string)p["id"] ?? name; return name; }
    private object Parse(string field, string value) { if (field == "fontSize" || field.Contains("Width") || field.Contains("Height") || field.Contains("Spacing") || field == "chevronSpeed" || field == "barHeight" || field == "delay" || field == "duration" || field == "animationDuration") { int n; return int.TryParse(value, out n) ? n : 0; } if (field.StartsWith("random", StringComparison.OrdinalIgnoreCase) || field == "showTitle") { bool b; return bool.TryParse(value, out b) && b; } return value; }
    private JObject Read(string key) { var raw = CPH.GetGlobalVar<string>(key, true); if (string.IsNullOrWhiteSpace(raw)) return new JObject(); try { return JObject.Parse(raw); } catch { return new JObject(); } }
    private void Save(string key, JObject value) { CPH.SetGlobalVar(key, value.ToString(Newtonsoft.Json.Formatting.None), true); }
}