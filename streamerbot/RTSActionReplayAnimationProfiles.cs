using System;
using Newtonsoft.Json.Linq;

// Animation-profile defaults and sequence helpers.
// Profiles are presentation presets and are intentionally source-independent.
public static class RTSActionReplayAnimationProfiles
{
    private const string DefaultEasingKey = "rts.actionreplay.animation.default.easing";

    public static void EnsureProfiles()
    {
        EnsureProfile("default", "Mini Player", true);
        EnsureProfile("fullScreen", "Full Screen", false);
        EnsureProfile("halfScreen", "Half Screen", false);
        EnsureProfile("twitchClip", "Mini Player", false);
        EnsureProfile("obsClip", "Mini Player", false);
        EnsureProfile("playlist", "Mini Player", false);
        EnsureProfile("recent", "Mini Player", false);
    }

    public static string BuildProfile(string profile)
    {
        var key = "rts.actionreplay.animation." + profile + ".";
        var start = ReadSequence(key + "startSequence");
        var end = ReadSequence(key + "endSequence");
        if (start.Count == 0)
            start.Add(new JObject { ["position"] = GetProfileString(profile, "startPosition", "Full Screen"), ["duration"] = 0, ["delay"] = 0, ["easing"] = GetProfileString(profile, "easing", "ease-in-out") });
        if (end.Count == 0)
            end.Add(new JObject { ["position"] = GetProfileString(profile, "endPosition", "Full Screen"), ["duration"] = (int)Math.Round(GetProfileDouble(profile, "duration", .5) * 1000), ["delay"] = 0, ["easing"] = GetProfileString(profile, "easing", "ease-in-out") });
        return new JObject
        {
            ["name"] = CPH.GetGlobalVar<string>(key + "name", true) ?? profile,
            ["start"] = start,
            ["end"] = end
        }.ToString(Newtonsoft.Json.Formatting.None);
    }

    public static string GetProfileString(string profile, string field, string fallback)
    {
        var value = CPH.GetGlobalVar<string>("rts.actionreplay.animation." + profile + "." + field, true);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    public static double GetProfileDouble(string profile, string field, double fallback)
    {
        var value = CPH.GetGlobalVar<double?>("rts.actionreplay.animation." + profile + "." + field, true);
        return value ?? fallback;
    }

    public static string GetEasing()
    {
        return CPH.GetGlobalVar<string>(DefaultEasingKey, true) ?? "ease-in-out";
    }

    private static void EnsureProfile(string profile, string name, bool miniStart)
    {
        SetDefault("rts.actionreplay.animation." + profile + ".name", name);
        var start = miniStart
            ? "[{\"position\":\"Mini Hidden\",\"duration\":0,\"delay\":0,\"easing\":\"ease-in-out\"},{\"position\":\"Mini Angled\",\"duration\":1000,\"delay\":3000,\"easing\":\"ease-in-out\"},{\"position\":\"Mini\",\"duration\":1000,\"delay\":0,\"easing\":\"ease-in-out\"}]"
            : "[{\"position\":\"Full Screen\",\"duration\":0,\"delay\":0,\"easing\":\"ease-in-out\"}]";
        SetDefault("rts.actionreplay.animation." + profile + ".startSequence", start);
        SetDefault("rts.actionreplay.animation." + profile + ".endSequence", "[{\"position\":\"Mini Hidden\",\"duration\":1000,\"delay\":0,\"easing\":\"ease-in-out\"}]");
    }

    private static void SetDefault(string key, object value)
    {
        if (value is string)
        {
            if (CPH.GetGlobalVar<string>(key, true) == null) CPH.SetGlobalVar(key, value, true);
            return;
        }
        CPH.SetGlobalVar(key, value, true);
    }

    private static JArray ReadSequence(string key)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(raw)) return new JArray();
        try { return JArray.Parse(raw); }
        catch { return new JArray(); }
    }
}
