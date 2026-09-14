using Newtonsoft.Json.Linq;

public class CPHInline
{
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
        CPH.SetArgument("replayPanelPreset", GetString("preset", "Broadcast"));
        CPH.SetArgument("replayPanelPrimaryColor", GetString("primaryColor", "#0384CBFF"));
        CPH.SetArgument("replayPanelSecondaryColor", GetString("secondaryColor", "#101416FF"));
        CPH.SetArgument("replayPanelTitleFont", GetString("titleFont", "Inter"));
        CPH.SetArgument("replayPanelTitleSize", CPH.GetGlobalVar<int?>(Prefix + "titleSize", true) ?? 24);
        CPH.SetArgument("replayPanelTitleColor", GetString("titleColor", "#FFFFFFFF"));
        CPH.SetArgument("replayPanelListSize", CPH.GetGlobalVar<int?>(Prefix + "listSize", true) ?? 15);
        CPH.SetArgument("replayPanelListColor", GetString("listColor", "#FFFFFFFF"));
        return true;
    }

    private string GetString(string name, string fallback) => CPH.GetGlobalVar<string>(Prefix + name, true) ?? fallback;
}
