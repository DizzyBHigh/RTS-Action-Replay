using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string Prefix = "rts.actionreplay.panel.";

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
        ui.BeginSection("Panel Design", "Information Panels");
        ui.AddClickableButton("Preview Panel", "Preview the selected panel preset and typography.", "Preview Panel", "blue", "Information Panels", Preview);
        ui.AddDropdown("Panel Preset", "Choose the visual design used by information panels.", "Information Panels", Prefix + "preset", new[] { "Broadcast", "Cinematic", "Cut", "Minimal" }, "Broadcast");
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

    public bool Apply()
    {
        CPH.ExecuteMethod("RTS - Action Replay - Core - Preset Store", "ResolveEntryPoint");
        CPH.ExecuteMethod("RTS - Action Replay - Core - Preset Store", "ApplyVisualAndBranding");
        ApplyHandoff();
        return true;
    }

    private void ApplyHandoff()
    {
        var key = "rts.actionreplay.handoff.visualBranding";
        CPH.TryGetArg("replayQueueEntryId", out string entryId);
        if (!string.IsNullOrWhiteSpace(entryId)) key += "." + entryId;
        var raw = CPH.GetGlobalVar<string>(key, false);
        if (string.IsNullOrWhiteSpace(raw) && key != "rts.actionreplay.handoff.visualBranding")
            raw = CPH.GetGlobalVar<string>("rts.actionreplay.handoff.visualBranding", false);
        if (string.IsNullOrWhiteSpace(raw))
{
    CPH.LogInfo("RTS Action Replay: panel visual handoff read (nonpersistent): missing");
    return;
}
CPH.LogInfo("RTS Action Replay: panel visual handoff read (nonpersistent): found");
        try
        {
            var p = JObject.Parse(raw);
            Set("replayPanelPreset", p["style"], "broadcast");
            Set("replayPanelPrimaryColor", p["primaryColor"], "#0384CBFF");
            Set("replayPanelSecondaryColor", p["secondaryColor"], "#101416FF");
            Set("replayPanelTitleFont", p["font"], "Inter");
            Set("replayPanelTitleSize", p["fontSize"], 34);
            Set("replayPanelTitleColor", p["textColor"], "#FFFFFFFF");
            Set("replayPanelListColor", p["textColor"], "#FFFFFFFF");
            Set("replayBrandingPresetId", p["brandingPresetId"], "default");
            CPH.SetArgument("replayShowTitle", true);
            ApplyVisualObject("replayBroadcast", p["broadcast"] as JObject);
            ApplyVisualObject("replayCut", p["cut"] as JObject);
        }
        catch { }
    }

    private void ApplyVisualObject(string prefix, JObject value)
    {
        foreach (var property in value?.Properties() ?? new JProperty[0])
        {
            var name = property.Name.Length == 0 ? "" : char.ToUpperInvariant(property.Name[0]) + property.Name.Substring(1);
            var argument = property.Value.Type == JTokenType.Boolean
                ? (object)(bool)property.Value
                : property.Value.Type == JTokenType.Integer
                    ? (object)(int)property.Value
                    : property.Value.ToString();
            CPH.SetArgument(prefix + name, argument);
        }
    }

    private void Set(string name, JToken value, object fallback)
    {
        var argument = value == null ? fallback : value.Type == JTokenType.Integer ? (object)(int)value : value.ToString();
        CPH.SetArgument(name, argument);
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
}
