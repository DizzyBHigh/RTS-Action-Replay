using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string StylePrefix = "rts.actionreplay.panel.style.";

    public bool Execute()
    {
        var ui = new RtsUI("RTS Action Replay - Panel Styling", "1.0.0",
            (key, persisted) => CPH.GetGlobalVar<bool?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<int?>(key, persisted),
            (key, persisted) => ReadValue(key),
            (key, persisted) => (object)CPH.GetGlobalVar<string>(key, persisted),
            (key, value, persisted) => SaveValue(key, value),
            message => CPH.LogInfo(message));
        Build(ui); ui.ShowUI(); return true;
    }

    private void Build(RtsUI ui)
    {
        EnsureStyle();
        ui.BeginSection("Panel", "Information Panels");
        ui.AddClickableButton("Preview Panel", "Show the current information-panel styling on the Action Replay overlay.", "Preview Panel", "blue", "Information Panels", Preview);
        ui.BeginSection("Background", "Information Panels");
        ui.BeginRow(); ui.AddToggleSwitch("Enable Background", "Display the panel background.", "Information Panels", StylePrefix + "background.enabled", true); ui.EndRow();
        ui.BeginRow(); ui.AddColorPicker("Primary Colour", "Primary background colour.", "Information Panels", StylePrefix + "background.primary", "#101416FF"); ui.AddColorPicker("Secondary Colour", "Secondary background colour.", "Information Panels", StylePrefix + "background.secondary", "#0384CBFF"); ui.EndRow(); ui.EndSection();
        ui.BeginSection("Border", "Information Panels");
        ui.BeginRow(); ui.AddToggleSwitch("Enable Border", "Display the panel border.", "Information Panels", StylePrefix + "border.enabled", true); ui.AddSlider("Width", "Border width in pixels.", "Information Panels", StylePrefix + "border.width", 0, 12, 3); ui.EndRow();
        ui.BeginRow(); ui.AddColorPicker("Colour", "Border colour.", "Information Panels", StylePrefix + "border.color", "#0384CBFF");  ui.AddSlider("Radius", "Panel corner radius in pixels.", "Information Panels", StylePrefix + "border.radius", 0, 48, 0); ui.EndRow(); ui.EndSection();
        ui.BeginSection("Glow", "Information Panels");
        ui.BeginRow(); ui.AddToggleSwitch("Enable Glow", "Display the panel glow.", "Information Panels", StylePrefix + "glow.enabled", true); ui.AddColorPicker("Colour", "Glow colour.", "Information Panels", StylePrefix + "glow.color", "#0384CBFF"); ui.EndRow();
        ui.BeginRow(); ui.AddSlider("Blur", "Glow blur in pixels.", "Information Panels", StylePrefix + "glow.blur", 0, 60, 24); ui.AddSlider("Spread", "Glow spread in pixels.", "Information Panels", StylePrefix + "glow.spread", 0, 20, 0); ui.EndRow(); ui.EndSection();
        ui.EndSection();

        ui.BeginSection("Header / Title", "Information Panels");
        ui.BeginRow(); ui.AddColorPicker("Title Colour", "Panel title text colour.", "Information Panels", StylePrefix + "header.titleColor", "#FFFFFFFF"); ui.AddColorPicker("Title Shadow Colour", "Panel title shadow colour.", "Information Panels", StylePrefix + "header.shadowColor", "#000000FF"); ui.EndRow();
        ui.BeginRow(); ui.AddGoogleFontSelector("Title Font", "Google Font used by the panel title.", "Information Panels", StylePrefix + "header.font", "Inter"); ui.AddNumericTextbox("Title Size", "Panel title font size in pixels.", "Information Panels", StylePrefix + "header.size", 24, 8, 72); ui.AddDropdown("Title Weight", "Panel title font weight.", "Information Panels", StylePrefix + "header.weight", new[] { "400", "500", "600", "700", "800", "900" }, "800"); ui.EndRow();
        ui.BeginRow();  ui.AddColorPicker("Primary Colour", "Header gradient primary colour.", "Information Panels", StylePrefix + "header.primary", "#0384CBFF"); ui.AddColorPicker("Secondary Colour", "Header gradient secondary colour.", "Information Panels", StylePrefix + "header.secondary", "#101416FF"); ui.EndRow();
        ui.BeginRow(); ui.AddNumericTextbox("Gradient Angle", "Header gradient angle in degrees.", "Information Panels", StylePrefix + "header.angle", 135, 0, 360); ui.AddNumericTextbox("Header Height", "Header height in pixels.", "Information Panels", StylePrefix + "header.height", 88, 40, 240); ui.EndRow(); ui.EndSection();

        ui.BeginSection("List Entries", "Information Panels");
        ui.BeginRow(); ui.AddColorPicker("Text Colour", "Primary list text colour.", "Information Panels", StylePrefix + "list.textColor", "#FFFFFFFF"); ui.AddColorPicker("Secondary Text Colour", "Secondary list text colour.", "Information Panels", StylePrefix + "list.secondaryTextColor", "#AAB4BAFF"); ui.EndRow();
        ui.BeginRow(); ui.AddGoogleFontSelector("Font", "Google Font used by list entries.", "Information Panels", StylePrefix + "list.font", "Inter"); ui.AddNumericTextbox("Font Size", "List entry font size in pixels.", "Information Panels", StylePrefix + "list.size", 15, 8, 48); ui.EndRow();
        ui.BeginRow(); ui.AddDropdown("Font Weight", "List entry font weight.", "Information Panels", StylePrefix + "list.weight", new[] { "400", "500", "600", "700", "800" }, "600"); ui.AddSlider("Row Spacing", "Space between list entries in pixels.", "Information Panels", StylePrefix + "list.spacing", 0, 30, 0); ui.EndRow();
        ui.BeginRow(); ui.AddColorPicker("Row Primary Colour", "Primary row background colour.", "Information Panels", StylePrefix + "list.primary", "#101416FF"); ui.AddColorPicker("Row Secondary Colour", "Secondary row background colour.", "Information Panels", StylePrefix + "list.secondary", "#0384CBFF"); ui.EndRow();
        ui.BeginRow(); ui.AddColorPicker("Row Border Colour", "Row border colour.", "Information Panels", StylePrefix + "list.border", "#FFFFFF14"); ui.AddSlider("Row Radius", "Row corner radius in pixels.", "Information Panels", StylePrefix + "list.radius", 0, 30, 0); ui.EndRow();
        ui.BeginRow(); ui.AddToggleSwitch("Row Glow", "Display a glow on list rows.", "Information Panels", StylePrefix + "list.glowEnabled", false); ui.AddColorPicker("Row Glow Colour", "List row glow colour.", "Information Panels", StylePrefix + "list.glowColor", "#0384CBFF"); ui.EndRow(); ui.EndSection();

        ui.BeginSection("Accent / RTS Elements", "Information Panels");
        ui.BeginRow(); ui.AddColorPicker("Accent Colour", "RTS panel accent colour.", "Information Panels", StylePrefix + "accent.color", "#0384CBFF"); ui.EndRow();
        ui.BeginRow(); ui.AddToggleSwitch("Chevron / Route Accent", "Display the RTS chevron/route accent.", "Information Panels", StylePrefix + "accent.chevron", true); ui.AddToggleSwitch("Accent Glow", "Display a glow on RTS accent elements.", "Information Panels", StylePrefix + "accent.glow", true); ui.EndRow(); ui.EndSection();
    }

    public bool Apply()
    {
        EnsureStyle();
        var panel = ReadPanel();
        CPH.SetArgument("replayPanelStyle", (panel["style"] as JObject ?? CreateStyle()).ToString(Newtonsoft.Json.Formatting.None));
        return true;
    }

    public void Preview()
    {
        EnsureStyle();
        var panel = ReadPanel();
        CPH.SetArgument("replayCommand", "panel-position-preview");
        CPH.SetArgument("replayPanelPosition", "Centered");
        CPH.SetArgument("replayPanelPositions", (panel["positions"] as JObject ?? new JObject()).ToString(Newtonsoft.Json.Formatting.None));
        CPH.SetArgument("replayPanelWidth", (int?)panel["width"] ?? 500);
        CPH.SetArgument("replayPanelHeight", (int?)panel["height"] ?? 700);
        CPH.SetArgument("replayPanelStyle", (panel["style"] as JObject ?? CreateStyle()).ToString(Newtonsoft.Json.Formatting.None));
        CPH.TriggerEvent("RTS-Action Replay", true);
    }

    private string ReadValue(string key)
    {
        var panel = ReadPanel(); var style = panel["style"] as JObject ?? CreateStyle();
        var path = key.Substring(StylePrefix.Length).Split('.'); JToken value = style;
        foreach (var part in path) value = value?[part];
        return value?.ToString() ?? "";
    }

    private void SaveValue(string key, object value)
    {
        var panel = ReadPanel(); var style = panel["style"] as JObject ?? CreateStyle(); panel["style"] = style;
        var path = key.Substring(StylePrefix.Length).Split('.'); var target = style;
        for (var i = 0; i < path.Length - 1; i++) target = target[path[i]] as JObject ?? CreateChild(target, path[i]);
        target[path[path.Length - 1]] = JToken.FromObject(value ?? "");
        SavePanel(panel);
    }

    private JObject CreateChild(JObject parent, string key) { var child = new JObject(); parent[key] = child; return child; }
    private JObject ReadPanel() { var raw = CPH.GetGlobalVar<string>(PanelKey, true); try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { return new JObject(); } }
    private void SavePanel(JObject panel) => CPH.SetGlobalVar(PanelKey, panel.ToString(Newtonsoft.Json.Formatting.None), true);
    private void EnsureStyle() { var panel = ReadPanel(); if (panel.Count == 0) panel = new JObject { ["version"] = 1, ["width"] = 500, ["height"] = 700 }; if (panel["style"] == null) { panel["style"] = CreateStyle(); SavePanel(panel); } }

    private JObject CreateStyle() => new JObject
    {
        ["background"] = new JObject { ["enabled"] = true, ["primary"] = "#101416FF", ["secondary"] = "#0384CBFF" },
        ["border"] = new JObject { ["enabled"] = true, ["color"] = "#0384CBFF", ["width"] = 3, ["radius"] = 0 },
        ["glow"] = new JObject { ["enabled"] = true, ["color"] = "#0384CBFF", ["blur"] = 24, ["spread"] = 0 },
        ["header"] = new JObject { ["titleColor"] = "#FFFFFFFF", ["font"] = "Inter", ["size"] = 24, ["weight"] = "800", ["shadowColor"] = "#000000FF", ["primary"] = "#0384CBFF", ["secondary"] = "#101416FF", ["angle"] = 135, ["height"] = 88 },
        ["list"] = new JObject { ["textColor"] = "#FFFFFFFF", ["secondaryTextColor"] = "#AAB4BAFF", ["font"] = "Inter", ["size"] = 15, ["weight"] = "600", ["spacing"] = 0, ["primary"] = "#101416FF", ["secondary"] = "#0384CBFF", ["border"] = "#FFFFFF14", ["radius"] = 0, ["glowEnabled"] = false, ["glowColor"] = "#0384CBFF" },
        ["accent"] = new JObject { ["color"] = "#0384CBFF", ["chevron"] = true, ["glow"] = true }
    };
}
