using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string Prefix = "rts.actionreplay.panel.";
    private const string BroadcastPrefix = "rts.actionreplay.broadcast.";
    private static readonly string[] PanelTypes = { "recent", "playlist", "creatorLeaderboard" };
    private static readonly string[] Presets = { "Broadcast", "Cinematic", "Cut", "Minimal" };

    public bool Execute()
    {
        var ui = new RtsUI("RTS Action Replay - Panel Presets", "1.0.0",
            (key, persisted) => CPH.GetGlobalVar<bool?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<int?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<string>(key, persisted),
            (key, persisted) => (object)CPH.GetGlobalVar<string>(key, persisted),
            (key, value, persisted) => CPH.SetGlobalVar(key, value, persisted),
            message => CPH.LogInfo(message));
        Build(ui); ui.ShowUI(); return true;
    }

    private void Build(RtsUI ui)
    {
        var panel = ReadConfig();
        var profiles = PanelProfiles(panel);
        ui.BeginSection("Panel Entry Points", "Information Panels");
        AddPanelEntryPoint(ui, "Recent / Search", "recent", profiles);
        AddPanelEntryPoint(ui, "Playlist", "playlist", profiles);
        AddPanelEntryPoint(ui, "Creator Leaderboard", "creatorLeaderboard", profiles);
        ui.EndSection();

        ui.BeginSection("Panel Design", "Information Panels");
        ui.AddClickableButton("Preview Panel", "Preview the selected panel preset and typography.", "Preview Panel", "blue", "Information Panels", Preview);
        ui.AddDropdown("Panel Preset", "Fallback visual design used when a panel entry point has no assigned preset.", "Information Panels", Prefix + "preset", Presets, "Broadcast");
        ui.BeginRow();
        ui.AddColorPicker("Primary Colour", "Primary accent colour used by the panel design.", "Information Panels", Prefix + "primaryColor", "#0384CBFF");
        ui.AddColorPicker("Secondary Colour", "Secondary accent colour used by the panel design.", "Information Panels", Prefix + "secondaryColor", "#101416FF");
        ui.EndRow(); ui.EndSection();

        ui.BeginSection("Title", "Information Panels"); ui.BeginRow();
        ui.AddGoogleFontSelector("Font", "Font used by the panel title.", "Information Panels", Prefix + "titleFont", "Inter");
        ui.AddNumericTextbox("Size", "Panel title font size in pixels.", "Information Panels", Prefix + "titleSize", 24, 12, 72);
        ui.AddColorPicker("Colour", "Panel title text colour.", "Information Panels", Prefix + "titleColor", "#FFFFFFFF");
        ui.EndRow(); ui.EndSection();

        ui.BeginSection("List", "Information Panels"); ui.BeginRow();
        ui.AddNumericTextbox("Font Size", "List entry font size in pixels.", "Information Panels", Prefix + "listSize", 15, 8, 48);
        ui.AddColorPicker("Colour", "List entry text colour.", "Information Panels", Prefix + "listColor", "#FFFFFFFF");
        ui.EndRow(); ui.EndSection();
    }

    private void AddPanelEntryPoint(RtsUI ui, string label, string key, string[] profiles)
    {
        ui.BeginRow();
        ui.AddDropdown(label + " Animation Profile", "Animation profile used by this panel entry point.", "Information Panels", Prefix + "entryPoints." + key + ".animationProfile", profiles, "Default");
        ui.AddDropdown(label + " Visual Preset", "Visual design used by this panel entry point.", "Information Panels", Prefix + "entryPoints." + key + ".preset", Presets, "Broadcast");
        ui.EndRow();
    }

    public bool Apply()
    {
        var panel = ReadConfig();
        var animation = panel["animation"] as JObject ?? new JObject();
        var animationEntries = animation["entryPoints"] as JObject ?? new JObject();
        var preset = panel["preset"] as JObject ?? new JObject();
        var presetEntries = preset["entryPoints"] as JObject ?? new JObject();
        var globalPreset = GetString("preset", (string)preset["fallback"] ?? "Broadcast");
        foreach (var key in PanelTypes)
        {
            animationEntries[key] = GetString("entryPoints." + key + ".animationProfile", (string)animationEntries[key] ?? "default");
            presetEntries[key] = GetString("entryPoints." + key + ".preset", (string)presetEntries[key] ?? globalPreset);
        }
        animation["entryPoints"] = animationEntries;
        preset["fallback"] = globalPreset;
        preset["entryPoints"] = presetEntries;
        panel["animation"] = animation;
        panel["preset"] = preset;
        SaveConfig(panel);
        var resolvedPreset = Arg("replayPanelPreset");
        if (string.IsNullOrWhiteSpace(resolvedPreset)) resolvedPreset = globalPreset;
        SetDesignArguments(resolvedPreset);
        return true;
    }

    public void Preview()
    {
        var raw = CPH.GetGlobalVar<string>(PanelKey, true);
        JObject panel;
        try { panel = string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); }
        catch { panel = new JObject(); }
        Apply();
        CPH.SetArgument("replayCommand", "panel-position-preview");
        CPH.SetArgument("replayPanelPosition", "Centered");
        CPH.SetArgument("replayPanelPositions", (panel["positions"] as JObject ?? new JObject()).ToString(Newtonsoft.Json.Formatting.None));
        CPH.SetArgument("replayPanelWidth", (int?)panel["width"] ?? 500);
        CPH.SetArgument("replayPanelHeight", (int?)panel["height"] ?? 700);
        CPH.TriggerEvent("RTS-Action Replay", true);
    }

    private void SetDesignArguments(string preset)
    {
        CPH.SetArgument("replayPanelPreset", preset);
        CPH.SetArgument("replayPanelPrimaryColor", GetString("primaryColor", "#0384CBFF"));
        CPH.SetArgument("replayPanelSecondaryColor", GetString("secondaryColor", "#101416FF"));
        CPH.SetArgument("replayPanelTitleFont", GetString("titleFont", "Inter"));
        CPH.SetArgument("replayPanelTitleSize", CPH.GetGlobalVar<int?>(Prefix + "titleSize", true) ?? 24);
        CPH.SetArgument("replayPanelTitleColor", GetString("titleColor", "#FFFFFFFF"));
        CPH.SetArgument("replayPanelListSize", CPH.GetGlobalVar<int?>(Prefix + "listSize", true) ?? 15);
        CPH.SetArgument("replayPanelListColor", GetString("listColor", "#FFFFFFFF"));
        CPH.SetArgument("replayBroadcastPrimaryColor", GetString(BroadcastPrefix + "primaryColor", "#0384CBFF"));
        CPH.SetArgument("replayBroadcastSecondaryColor", GetString(BroadcastPrefix + "secondaryColor", "#FFD400FF"));
        CPH.SetArgument("replayBroadcastChevronHeight", CPH.GetGlobalVar<int?>(BroadcastPrefix + "chevronHeight", true) ?? 42);
        CPH.SetArgument("replayBroadcastRandomHeight", CPH.GetGlobalVar<bool?>(BroadcastPrefix + "randomHeight", true) ?? false);
        CPH.SetArgument("replayBroadcastChevronWidth", CPH.GetGlobalVar<int?>(BroadcastPrefix + "chevronWidth", true) ?? 42);
        CPH.SetArgument("replayBroadcastRandomWidth", CPH.GetGlobalVar<bool?>(BroadcastPrefix + "randomWidth", true) ?? false);
        CPH.SetArgument("replayBroadcastChevronSpacing", CPH.GetGlobalVar<int?>(BroadcastPrefix + "chevronSpacing", true) ?? 0);
        CPH.SetArgument("replayBroadcastRandomSpacing", CPH.GetGlobalVar<bool?>(BroadcastPrefix + "randomSpacing", true) ?? false);
        CPH.SetArgument("replayBroadcastChevronSpeed", CPH.GetGlobalVar<int?>(BroadcastPrefix + "chevronSpeed", true) ?? 95);
    }

    private string[] PanelProfiles(JObject panel)
    {
        var profiles = panel["animationProfiles"] as JArray;
        var names = profiles?.OfType<JObject>().Select(x => (string)x["name"]).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        return names != null && names.Length > 0 ? names : new[] { "Default" };
    }

    private JObject ReadConfig()
    {
        var raw = CPH.GetGlobalVar<string>(PanelKey, true);
        try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); }
        catch { return new JObject(); }
    }

    private void SaveConfig(JObject panel) => CPH.SetGlobalVar(PanelKey, panel.ToString(Newtonsoft.Json.Formatting.None), true);
    private string Arg(string name) { CPH.TryGetArg(name, out string value); return value ?? ""; }
    private string GetString(string name, string fallback) => CPH.GetGlobalVar<string>(Prefix + name, true) ?? fallback;
}