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
        var panelType = CPH.TryGetArg("panelType", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "recent";
        CPH.SetGlobalVar("rts.actionreplay.operation.panel", new JObject { ["panelType"] = panelType, ["triggerEvent"] = false }.ToString(Newtonsoft.Json.Formatting.None), false);
        return CPH.ExecuteMethod("RTS - Action Replay - Core - Resolver", "ResolvePanel");
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
