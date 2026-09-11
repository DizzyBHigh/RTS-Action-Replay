using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string ProfilesKey = "rts.actionreplay.animation.profiles";
    private const string EasingKey = "rts.actionreplay.animation.default.easing";

    public bool Execute() => EnsureProfiles();

    public bool EnsureProfiles()
    {
        var profiles = Load();
        Ensure(profiles, "default", "Mini Player");
        Ensure(profiles, "fullScreen", "Full Screen");
        Ensure(profiles, "halfScreen", "Half Screen");
        Ensure(profiles, "twitchClip", "Mini Player");
        Ensure(profiles, "obsClip", "Mini Player");
        Ensure(profiles, "playlist", "Mini Player");
        Ensure(profiles, "recent", "Mini Player");
        Save(profiles);
        return true;
    }

    public bool ApplyProfile()
    {
        CPH.TryGetArg("profileId", out string id);
        var profiles = Load();
        var profile = profiles[id] as JObject ?? profiles["default"] as JObject;
        if (profile == null) return false;
        CPH.SetArgument("replayAnimationProfile", profile.ToString(Newtonsoft.Json.Formatting.None));
        CPH.SetArgument("replayAnimationEasing", CPH.GetGlobalVar<string>(EasingKey, true) ?? "ease-in-out");
        return true;
    }

    public bool GetProfile()
    {
        return ApplyProfile();
    }

    private void Ensure(JObject profiles, string id, string name)
    {
        if (profiles[id] is JObject) return;
        profiles[id] = new JObject
        {
            ["name"] = name,
            ["start"] = new JArray(),
            ["end"] = new JArray()
        };
    }

    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(ProfilesKey, true);
        try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); }
        catch { return new JObject(); }
    }

    private void Save(JObject profiles)
    {
        CPH.SetGlobalVar(ProfilesKey, profiles.ToString(Newtonsoft.Json.Formatting.None), true);
    }
}
