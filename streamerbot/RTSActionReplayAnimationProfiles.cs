using System;
using Newtonsoft.Json.Linq;

// Shared animation-profile storage and sequence helpers.
// Profiles are presentation presets and are intentionally source-independent.
public static class RTSActionReplayAnimationProfiles
{
    private const string ProfilesKey = "rts.actionreplay.animation.profiles";
    private const string DefaultEasingKey = "rts.actionreplay.animation.default.easing";

    public static void EnsureProfiles()
    {
        var profiles = LoadProfiles();
        EnsureProfile(profiles, "default", "Mini Player");
        EnsureProfile(profiles, "fullScreen", "Full Screen");
        EnsureProfile(profiles, "halfScreen", "Half Screen");
        EnsureProfile(profiles, "twitchClip", "Mini Player");
        EnsureProfile(profiles, "obsClip", "Mini Player");
        EnsureProfile(profiles, "playlist", "Mini Player");
        EnsureProfile(profiles, "recent", "Mini Player");
        SaveProfiles(profiles);
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

    private static JArray ReadSequence(string key)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(raw)) return new JArray();
        try { return JArray.Parse(raw); }
        catch { return new JArray(); }
    }

    private static void EnsureProfile(JObject profiles, string id, string name)
    {
        if (profiles[id] is JObject) return;
        profiles[id] = new JObject { ["name"] = name, ["start"] = new JArray(), ["end"] = new JArray() };
    }

    private static JObject LoadProfiles()
    {
        var raw = CPH.GetGlobalVar<string>(ProfilesKey, true);
        try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); }
        catch { return new JObject(); }
    }

    private static void SaveProfiles(JObject profiles)
    {
        CPH.SetGlobalVar(ProfilesKey, profiles.ToString(Newtonsoft.Json.Formatting.None), true);
    }
}
