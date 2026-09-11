using System;
using Newtonsoft.Json.Linq;

// Animation profile storage and sequence helpers.
// Profiles are presentation presets and are intentionally source-independent.
public static class RTSActionReplayAnimationProfiles
{
    private const string ProfilesKey = "rts.actionreplay.animation.profiles";
    private const string DefaultEasingKey = "rts.actionreplay.animation.default.easing";

    public static string GetProfile(string profileId)
    {
        var profiles = LoadProfiles();
        var profile = profiles[profileId] as JObject;
        if (profile == null)
        {
            profile = profiles["default"] as JObject;
        }
        return profile == null ? "{}" : profile.ToString(Newtonsoft.Json.Formatting.None);
    }

    public static JArray GetSequence(string profileId, string sequenceName)
    {
        var profiles = LoadProfiles();
        var profile = profiles[profileId] as JObject ?? profiles["default"] as JObject;
        if (profile == null) return new JArray();
        return profile[sequenceName] as JArray ?? new JArray();
    }

    public static string GetEasing()
    {
        return CPH.GetGlobalVar<string>(DefaultEasingKey, false) ?? "ease-in-out";
    }

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

    private static void EnsureProfile(JObject profiles, string id, string name)
    {
        if (profiles[id] is JObject) return;
        profiles[id] = new JObject
        {
            ["name"] = name,
            ["start"] = new JArray(),
            ["end"] = new JArray()
        };
    }

    private static JObject LoadProfiles()
    {
        var raw = CPH.GetGlobalVar<string>(ProfilesKey, true);
        try
        {
            return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw);
        }
        catch
        {
            return new JObject();
        }
    }

    private static void SaveProfiles(JObject profiles)
    {
        CPH.SetGlobalVar(ProfilesKey, profiles.ToString(Newtonsoft.Json.Formatting.None), true);
    }
}
