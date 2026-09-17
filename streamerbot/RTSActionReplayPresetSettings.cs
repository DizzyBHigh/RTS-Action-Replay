using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

// Dedicated settings UI for reusable Branding and Title Presets plus entry points.
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
            (key, persisted) => ReadUi(key),
            (key, persisted) => (object)CPH.GetGlobalVar<string>(key, persisted),
            (key, value, persisted) => SaveUi(key, value, persisted),
            message => CPH.LogInfo(message));
        Build(ui);
        ui.ShowUI();
        return true;
    }

    private void Build(RtsUI ui)
    {
        AddBrandingPresets(ui);
        AddTitlePresets(ui);
        AddEntryPoints(ui, PlayerKey, "Player Entry Points",
            new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" },
            new[] { "Create — OBS", "Create — Twitch", "Create — YouTube", "Create — Kick", "Play — Recent", "Play — Catalog", "Play — Playlist" });
        AddEntryPoints(ui, PanelKey, "Panel Entry Points",
            new[] { "recent", "playlist", "creatorLeaderboard" },
            new[] { "Recent / Search", "Playlist", "Leaderboards" });
        AddClapperboard(ui);
    }

    private void AddBrandingPresets(RtsUI ui)
    {
        ui.AddTitle("Reusable identity, colours and typography.", "Branding Presets");
        foreach (var token in Presets("branding")) AddBrandingPreset(ui, token as JObject);
    }

    private void AddBrandingPreset(RtsUI ui, JObject preset)
    {
        var id = (string)preset?["id"];
        if (string.IsNullOrWhiteSpace(id)) return;
        ui.BeginSection((string)preset["name"] ?? id, "Branding Presets");
        ui.BeginRow();
        AddColour(ui, "Primary Colour", "primaryColor", preset, "#0384CBFF");
        AddColour(ui, "Secondary Colour", "secondaryColor", preset, "#101416FF");
        ui.EndRow();
        ui.BeginRow();
        AddColour(ui, "Title Colour", "titleColor", preset, "#FFFFFFFF");
        AddColour(ui, "Title Prefix / Suffix Colour", "titlePrefixSuffixColor", preset, "#0384CBFF");
        ui.EndRow();
        ui.BeginRow();
        AddColour(ui, "Text Colour", "textColor", preset, "#FFFFFFFF");
        AddColour(ui, "Shadow Colour", "shadowColor", preset, "#000000FF");
        ui.EndRow();
        ui.BeginRow();
        ui.AddGoogleFontSelector("Font", "Google Font used by branded text.", "Branding Presets", Key("branding", id, "font"), (string)preset["font"] ?? "Inter");
        ui.AddNumericTextbox("Font Size", "Default branded font size in pixels.", "Branding Presets", Key("branding", id, "fontSize"), (int?)preset["fontSize"] ?? 34, 12, 96);
        ui.EndRow();
        ui.AddTextbox("Logo URL", "HTTPS URL of the branding logo.", "Branding Presets", Key("branding", id, "logo"), (string)preset["logo"] ?? "", false);
        ui.BeginRow();
        ui.AddTextbox("Fallback Text", "Text used when no logo is defined.", "Branding Presets", Key("branding", id, "fallbackText"), (string)preset["fallbackText"] ?? "RTS", false);
        ui.AddTextbox("Brand Label", "Label displayed beside the logo or fallback text.", "Branding Presets", Key("branding", id, "brandLabel"), (string)preset["brandLabel"] ?? "ACTION REPLAY", false);
        ui.EndRow();
        ui.EndSection();
    }

    private void AddTitlePresets(RtsUI ui)
    {
        ui.AddTitle("Title Presets configure the Broadcast and Cut Title Designs.", "Title Presets");
        foreach (var token in Presets("visual"))
        {
            var preset = token as JObject;
            var design = (string)preset?["design"] ?? (string)preset?["id"];
            if (design == "broadcast" || design == "cut") AddTitlePreset(ui, preset, design);
        }
    }

    private void AddTitlePreset(RtsUI ui, JObject preset, string design)
    {
        var id = (string)preset["id"];
        ui.BeginSection((string)preset["name"] ?? id, "Title Presets");
        if (design == "broadcast")
        {
            ui.BeginRow();
            AddInt(ui, "Chevron Height", "chevronHeight", preset, 42, 1, 89);
            AddBool(ui, "Randomize Height", "randomHeight", preset, false);
            AddInt(ui, "Chevron Width", "chevronWidth", preset, 42, 1, 300);
            AddBool(ui, "Randomize Width", "randomWidth", preset, false);
            ui.EndRow();
            ui.BeginRow();
            AddInt(ui, "Chevron Spacing", "chevronSpacing", preset, 0, 0, 200);
            AddBool(ui, "Randomize Spacing", "randomSpacing", preset, false);
            AddInt(ui, "Chevron Speed", "chevronSpeed", preset, 95, 10, 500);
            ui.EndRow();
        }
        else
        {
            ui.BeginRow();
            AddInt(ui, "Block Width", "blockWidth", preset, 170, 1, 600);
            AddBool(ui, "Randomize Width", "randomWidth", preset, true);
            AddInt(ui, "Bar Height", "barHeight", preset, 5, 1, 50);
            ui.EndRow();
        }
        ui.EndSection();
    }

    private void AddEntryPoints(RtsUI ui, string configKey, string category, string[] ids, string[] labels)
    {
        var config = Read(configKey);
        var entries = config["entryPoints"] as JObject ?? new JObject();
        for (var i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            var entry = entries[id] as JObject ?? new JObject();
            ui.BeginSection(labels[i], category);
            AddAnimationSelector(ui, configKey, id, category, entry);
            AddDropdown(ui, "Title Preset", "Title preset used by this entry point.", category,
                EntryKey(configKey, id, "titlePreset"), Names("visual"), Name("visual", (string)entry["titlePreset"] ?? (string)entry["visualPreset"] ?? "broadcast"));
            AddDropdown(ui, "Branding Preset", "Branding preset used by this entry point.", category,
                EntryKey(configKey, id, "brandingPreset"), Names("branding"), Name("branding", (string)entry["brandingPreset"] ?? "default"));
            ui.EndSection();
        }
    }

    private void AddClapperboard(RtsUI ui)
    {
        var config = Read(ClapperKey);
        var entry = config["entryPoint"] as JObject ?? new JObject();
        ui.AddDropdown("Animation Profile", "Animation profile used by the Clapperboard.", "Clapperboard",
            UiPrefix + "clapper.animation", AnimationNames(ClapperKey), AnimationName(ClapperKey, (string)entry["animationProfile"] ?? "default"));
        ui.AddDropdown("Branding Preset", "Branding preset used by the Clapperboard.", "Clapperboard",
            UiPrefix + "clapper.branding", Names("branding"), Name("branding", (string)entry["brandingPreset"] ?? "default"));
    }

    private void AddAnimationSelector(RtsUI ui, string configKey, string entryId, string category, JObject entry)
    {
        ui.AddDropdown("Animation Profile", "Animation profile used by this entry point.", category,
            EntryKey(configKey, entryId, "animationProfile"), AnimationNames(configKey), AnimationName(configKey, (string)entry["animationProfile"] ?? "default"));
    }

    private void AddDropdown(RtsUI ui, string label, string help, string category, string key, string[] options, string value)
        => ui.AddDropdown(label, help, category, key, options, value);

    private void AddColour(RtsUI ui, string label, string field, JObject preset, string fallback)
    {
        var id = (string)preset["id"];
        ui.AddColorPicker(label, "Branding colour.", "Branding Presets", Key("branding", id, field), (string)preset[field] ?? fallback);
    }

    private void AddInt(RtsUI ui, string label, string field, JObject preset, int fallback, int min, int max)
    {
        var id = (string)preset["id"];
        ui.AddNumericTextbox(label, "Title preset setting.", "Title Presets", Key("visual", id, field), (int?)preset[field] ?? fallback, min, max);
    }

    private void AddBool(RtsUI ui, string label, string field, JObject preset, bool fallback)
    {
        var id = (string)preset["id"];
        ui.AddToggleSwitch(label, "Title preset setting.", "Title Presets", Key("visual", id, field), (bool?)preset[field] ?? fallback);
    }

    private string Key(string type, string id, string field) => UiPrefix + type + "." + id + "." + field;
    private string EntryKey(string config, string id, string field) => UiPrefix + "entry." + config + "." + id + "." + field;
    private JArray Presets(string type) => Read(PresetsKey)[type] as JArray ?? new JArray();

    private JObject Find(string type, string id)
    {
        foreach (var token in Presets(type))
            if (string.Equals((string)token["id"], id, StringComparison.OrdinalIgnoreCase)) return token as JObject;
        return null;
    }

    private string[] Names(string type)
    {
        var names = new List<string>();
        foreach (var token in Presets(type))
        {
            var design = (string)token["design"];
            if (type == "visual" && design != "broadcast" && design != "cut") continue;
            var name = (string)token["name"];
            if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
        }
        return names.Count > 0 ? names.ToArray() : new[] { type == "visual" ? "Broadcast" : "Default" };
    }

    private string Name(string type, string id)
    {
        var preset = Find(type, id);
        if (type == "visual" && preset != null)
        {
            var design = (string)preset["design"];
            if (design != "broadcast" && design != "cut") preset = null;
        }
        return preset == null ? (type == "visual" ? "Broadcast" : "Default") : (string)preset["name"] ?? id;
    }

    private JArray AnimationProfiles(string configKey) => Read(configKey)["animationProfiles"] as JArray ?? new JArray();

    private string[] AnimationNames(string configKey)
    {
        var names = new List<string>();
        foreach (var token in AnimationProfiles(configKey))
        {
            var name = (string)token["name"];
            if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
        }
        return names.Count > 0 ? names.ToArray() : new[] { "Default" };
    }

    private string AnimationName(string configKey, string id)
    {
        foreach (var token in AnimationProfiles(configKey))
            if (string.Equals((string)token["id"], id, StringComparison.OrdinalIgnoreCase)) return (string)token["name"] ?? id;
        return id == "default" ? "Default" : id;
    }

    private string ReadUi(string key)
    {
        if (!key.StartsWith(UiPrefix, StringComparison.Ordinal)) return CPH.GetGlobalVar<string>(key, true);
        var parts = key.Substring(UiPrefix.Length).Split('.');
        try
        {
            if (parts[0] == "branding" || parts[0] == "visual") return Find(parts[0], parts[1])?[parts[2]]?.ToString() ?? "";
            if (parts[0] == "entry") return EntryValue(parts[1], parts[2], parts[3]);
            if (parts[0] == "clapper") return ClapperValue(parts[1]);
        }
        catch { }
        return "";
    }

    private string EntryValue(string configKey, string id, string field)
    {
        var entry = (Read(configKey)["entryPoints"] as JObject)?[id] as JObject ?? new JObject();
        if (field == "animationProfile") return AnimationName(configKey, (string)entry["animationProfile"] ?? "default");
        return Name("visual", (string)entry["titlePreset"] ?? (string)entry["visualPreset"] ?? "broadcast");
    }

    private string ClapperValue(string field)
    {
        var entry = Read(ClapperKey)["entryPoint"] as JObject ?? new JObject();
        return field == "animation" ? AnimationName(ClapperKey, (string)entry["animationProfile"] ?? "default") : Name("branding", (string)entry["brandingPreset"] ?? "default");
    }

    private void SaveUi(string key, object value, bool persisted)
    {
        if (!key.StartsWith(UiPrefix, StringComparison.Ordinal)) { CPH.SetGlobalVar(key, value, persisted); return; }
        var parts = key.Substring(UiPrefix.Length).Split('.');
        var text = value?.ToString() ?? "";
        try
        {
            if (parts[0] == "branding" || parts[0] == "visual") SavePreset(parts[0], parts[1], parts[2], text);
            else if (parts[0] == "entry") SaveEntry(parts[1], parts[2], parts[3], text);
            else if (parts[0] == "clapper") SaveClapper(parts[1], text);
        }
        catch (Exception ex) { CPH.LogWarn("RTS Action Replay preset settings save failed: " + ex.Message); }
    }

    private void SavePreset(string type, string id, string field, string value)
    {
        var preset = Find(type, id);
        if (preset == null) return;
        preset[field] = Parse(value);
        Save(PresetsKey, Read(PresetsKey));
    }

    private void SaveEntry(string configKey, string id, string field, string value)
    {
        var config = Read(configKey);
        var entries = config["entryPoints"] as JObject ?? new JObject();
        var entry = entries[id] as JObject ?? new JObject();
        if (field == "animationProfile") entry["animationProfile"] = ResolveAnimation(configKey, value);
        else if (field == "titlePreset") entry["titlePreset"] = ResolveId("visual", value);
        else entry["brandingPreset"] = ResolveId("branding", value);
        entries[id] = entry;
        config["entryPoints"] = entries;
        Save(configKey, config);
    }

    private void SaveClapper(string field, string value)
    {
        var config = Read(ClapperKey);
        var entry = config["entryPoint"] as JObject ?? new JObject();
        if (field == "animation") entry["animationProfile"] = ResolveAnimation(ClapperKey, value);
        else entry["brandingPreset"] = ResolveId("branding", value);
        config["entryPoint"] = entry;
        Save(ClapperKey, config);
    }

    private string ResolveAnimation(string configKey, string name)
    {
        foreach (var token in AnimationProfiles(configKey))
            if (string.Equals((string)token["name"], name, StringComparison.OrdinalIgnoreCase)) return (string)token["id"] ?? "default";
        return "default";
    }

    private string ResolveId(string type, string name)
    {
        var preset = Find(type, name);
        if (preset != null && (type != "visual" || (string)preset["design"] == "broadcast" || (string)preset["design"] == "cut")) return (string)preset["id"];
        foreach (var token in Presets(type))
            if (string.Equals((string)token["name"], name, StringComparison.OrdinalIgnoreCase) && (type != "visual" || (string)token["design"] == "broadcast" || (string)token["design"] == "cut")) return (string)token["id"];
        return type == "visual" ? "broadcast" : "default";
    }

    private JObject Read(string key)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); }
        catch { return new JObject(); }
    }

    private void Save(string key, JObject value) => CPH.SetGlobalVar(key, value.ToString(Newtonsoft.Json.Formatting.None), true);

    private JToken Parse(string value)
    {
        if (bool.TryParse(value, out var boolean)) return boolean;
        if (int.TryParse(value, out var integer)) return integer;
        return value;
    }
}
