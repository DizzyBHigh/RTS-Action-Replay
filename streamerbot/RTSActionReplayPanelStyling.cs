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
        ui.BeginSection("Colours", "Information Panels");
        ui.BeginRow(); ui.AddColorPicker("Primary Colour", "RTS accent colour used throughout the panel.", "Information Panels", StylePrefix + "primary", "#0384CBFF"); ui.AddColorPicker("Background Colour", "Main panel and row background colour.", "Information Panels", StylePrefix + "background", "#101416FF"); ui.EndRow();
        ui.BeginRow(); ui.AddColorPicker("Text Colour", "Main panel text colour.", "Information Panels", StylePrefix + "text", "#FFFFFFFF"); ui.AddColorPicker("Alternate Row Colour", "Background colour for alternating list rows.", "Information Panels", StylePrefix + "alternateRow", "#182127FF"); ui.EndRow(); ui.EndSection();

        ui.BeginSection("Panel", "Information Panels");
        ui.BeginRow(); ui.AddToggleSwitch("Enable Background", "Display the panel background.", "Information Panels", StylePrefix + "panel.backgroundEnabled", true); ui.AddToggleSwitch("Enable Border", "Display the panel border.", "Information Panels", StylePrefix + "panel.borderEnabled", true); ui.EndRow();
        ui.BeginRow(); ui.AddSlider("Border Width", "Border width in pixels.", "Information Panels", StylePrefix + "panel.borderWidth", 0, 12, 3); ui.AddSlider("Corner Radius", "Panel corner radius in pixels.", "Information Panels", StylePrefix + "panel.radius", 0, 48, 0); ui.EndRow();
        ui.BeginRow(); ui.AddToggleSwitch("Enable Glow", "Display the panel glow.", "Information Panels", StylePrefix + "panel.glowEnabled", true); ui.AddSlider("Glow Strength", "Panel glow strength.", "Information Panels", StylePrefix + "panel.glowStrength", 0, 60, 24); ui.EndRow(); ui.EndSection();

        ui.BeginSection("Header", "Information Panels");
        ui.BeginRow(); ui.AddGoogleFontSelector("Font", "Google Font used by the panel title.", "Information Panels", StylePrefix + "header.font", "Inter"); ui.AddNumericTextbox("Size", "Panel title font size in pixels.", "Information Panels", StylePrefix + "header.size", 24, 8, 72); ui.AddDropdown("Weight", "Panel title font weight.", "Information Panels", StylePrefix + "header.weight", new[] { "400", "500", "600", "700", "800", "900" }, "800"); ui.EndRow();
        ui.AddNumericTextbox("Header Height", "Header height in pixels.", "Information Panels", StylePrefix + "header.height", 88, 40, 240); ui.EndSection();

        ui.BeginSection("List", "Information Panels");
        ui.BeginRow(); ui.AddGoogleFontSelector("Font", "Google Font used by list entries.", "Information Panels", StylePrefix + "list.font", "Inter", "Inter"); ui.AddNumericTextbox("Size", "List entry font size in pixels.", "Information Panels", StylePrefix + "list.size", 15, 8, 48); ui.AddDropdown("Weight", "List entry font weight.", "Information Panels", StylePrefix + "list.weight", new[] { "400", "500", "600", "700", "800" }, "600"); ui.EndRow();
        ui.BeginRow(); ui.AddSlider("Row Spacing", "Space between list entries in pixels.", "Information Panels", StylePrefix + "list.spacing", 0, 30, 0); ui.AddSlider("Row Radius", "List row corner radius in pixels.", "Information Panels", StylePrefix + "list.radius", 0, 30, 0); ui.EndRow(); ui.EndSection();

        ui.BeginSection("RTS Accent", "Information Panels");
        ui.BeginRow(); ui.AddToggleSwitch("Show Accent", "Display the RTS chevron/route accent.", "Information Panels", StylePrefix + "accent.enabled", true); ui.AddToggleSwitch("Accent Glow", "Display a glow on RTS accent elements.", "Information Panels", StylePrefix + "accent.glow", true); ui.EndRow(); ui.EndSection();
        ui.EndSection();
    }

    public bool Apply() { EnsureStyle(); var panel = ReadPanel(); CPH.SetArgument("replayPanelStyle", (panel["style"] as JObject ?? CreateStyle()).ToString(Newtonsoft.Json.Formatting.None)); return true; }

    public void Preview()
    {
        EnsureStyle(); var panel = ReadPanel();
        var positions = (panel["positions"] as JObject ?? new JObject()).ToString(Newtonsoft.Json.Formatting.None);
        var style = (panel["style"] as JObject ?? CreateStyle()).ToString(Newtonsoft.Json.Formatting.None);
        var width = (int?)panel["width"] ?? 500;
        var height = (int?)panel["height"] ?? 700;

        CPH.SetArgument("replayCommand", "panel-position-preview"); CPH.SetArgument("replayPanelPosition", "Centered");
        CPH.SetArgument("replayPanelPositions", positions);
        CPH.SetArgument("replayPanelWidth", width); CPH.SetArgument("replayPanelHeight", height);
        CPH.SetArgument("replayPanelStyle", style);

        var previewArguments = new JObject
        {
            ["replayCommand"] = "panel-position-preview",
            ["replayPanelPosition"] = "Centered",
            ["replayPanelPositions"] = positions,
            ["replayPanelWidth"] = width,
            ["replayPanelHeight"] = height,
            ["replayPanelStyle"] = style
        };
        CPH.LogInfo("RTS Action Replay - Preview Panel arguments: " + previewArguments.ToString(Newtonsoft.Json.Formatting.None));
        CPH.TriggerEvent("RTS-Action Replay", true);
    }

    private string ReadValue(string key)
    {
        var style = ReadPanel()["style"] as JObject ?? CreateStyle(); var path = key.Substring(StylePrefix.Length).Split('.'); JToken value = style;
        foreach (var part in path) value = value?[part]; return value?.ToString() ?? "";
    }

    private void SaveValue(string key, object value)
    {
        var panel = ReadPanel(); var style = panel["style"] as JObject ?? CreateStyle(); panel["style"] = style; var path = key.Substring(StylePrefix.Length).Split('.'); var target = style;
        for (var i = 0; i < path.Length - 1; i++) target = target[path[i]] as JObject ?? CreateChild(target, path[i]);
        target[path[path.Length - 1]] = JToken.FromObject(value ?? ""); SavePanel(panel);
    }

    private JObject CreateChild(JObject parent, string key) { var child = new JObject(); parent[key] = child; return child; }
    private JObject ReadPanel() { var raw = CPH.GetGlobalVar<string>(PanelKey, true); try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { return new JObject(); } }
    private void SavePanel(JObject panel) => CPH.SetGlobalVar(PanelKey, panel.ToString(Newtonsoft.Json.Formatting.None), true);
    private void EnsureStyle() { var panel = ReadPanel(); if (panel.Count == 0) panel = new JObject { ["version"] = 1, ["width"] = 500, ["height"] = 700 }; if (panel["style"] == null) { panel["style"] = CreateStyle(); SavePanel(panel); } }

    private JObject CreateStyle() => new JObject
    {
        ["primary"] = "#0384CBFF", ["background"] = "#101416FF", ["text"] = "#FFFFFFFF", ["alternateRow"] = "#182127FF",
        ["panel"] = new JObject { ["backgroundEnabled"] = true, ["borderEnabled"] = true, ["borderWidth"] = 3, ["radius"] = 0, ["glowEnabled"] = true, ["glowStrength"] = 24 },
        ["header"] = new JObject { ["font"] = "Inter", ["size"] = 24, ["weight"] = "800", ["height"] = 88 },
        ["list"] = new JObject { ["font"] = "Inter", ["size"] = 15, ["weight"] = "600", ["spacing"] = 0, ["radius"] = 0 },
        ["accent"] = new JObject { ["enabled"] = true, ["glow"] = true }
    };
}
