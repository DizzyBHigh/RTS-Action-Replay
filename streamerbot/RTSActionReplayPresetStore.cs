using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    public const string PresetsKey = "rts.actionreplay.config.presets";

    public bool Execute() => EnsureDefaults();

    public bool EnsureDefaults()
    {
        var presets = Read(PresetsKey, CreateDefaults());
        if (!(presets["branding"] is JArray)) presets["branding"] = CreateDefaults()["branding"];
        if (!(presets["visual"] is JArray)) presets["visual"] = CreateDefaults()["visual"];
        presets["version"] = 1;
        Write(PresetsKey, presets);
        return true;
    }

    public JArray Branding() => Read(PresetsKey, CreateDefaults())["branding"] as JArray ?? new JArray();
    public JArray Visuals() => Read(PresetsKey, CreateDefaults())["visual"] as JArray ?? new JArray();

    public JObject Read(string key, JObject fallback)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        try { return string.IsNullOrWhiteSpace(raw) ? fallback : JObject.Parse(raw); }
        catch { return fallback; }
    }

    public void Write(string key, JObject value) =>
        CPH.SetGlobalVar(key, value.ToString(Newtonsoft.Json.Formatting.None), true);

    public static string ResolveId(JArray values, string value)
    {
        foreach (var item in values ?? new JArray())
        {
            if (string.Equals((string)item["id"], value, StringComparison.OrdinalIgnoreCase)) return (string)item["id"];
            if (string.Equals((string)item["name"], value, StringComparison.OrdinalIgnoreCase)) return (string)item["id"];
        }
        return null;
    }

    public static JObject CreateDefaults() => new JObject
    {
        ["version"] = 1,
        ["branding"] = new JArray(
            new JObject
            {
                ["id"] = "default", ["name"] = "Default",
                ["primaryColor"] = "#0384CBFF", ["secondaryColor"] = "#101416FF",
                ["titleColor"] = "#FFFFFFFF", ["textColor"] = "#FFFFFFFF",
                ["shadowColor"] = "#000000FF", ["font"] = "Inter", ["logo"] = "",
                ["fallbackText"] = "RTS", ["brandLabel"] = "ACTION REPLAY"
            }),
        ["visual"] = new JArray(
            new JObject { ["id"] = "broadcast", ["name"] = "Broadcast" },
            new JObject { ["id"] = "cinematic", ["name"] = "Cinematic" },
            new JObject { ["id"] = "cut", ["name"] = "Cut" },
            new JObject { ["id"] = "minimal", ["name"] = "Minimal" })
    };
}
