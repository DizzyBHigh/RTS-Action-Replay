using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedTitleProfile";
    private const string PlaybackProfileHandoffKey = "rts.actionreplay.handoff.playbackTitleProfile";

    public bool Execute() => EnsureProfiles();

    public bool EnsureProfiles()
    {
        var player = ReadConfig();
        var profiles = NormalizeProfiles(player["titleProfiles"] as JArray);
        var title = player["title"] as JObject ?? new JObject();
        title["selectedProfile"] = ResolveProfileId(profiles, (string)title["selectedProfile"]) ?? "default";
        title["entryPoints"] = NormalizeEntryPoints(title["entryPoints"] as JObject, profiles);
        player["titleProfiles"] = profiles;
        player["title"] = title;
        SaveConfig(player);
        return true;
    }

    public bool AddProfile()
    {
        var player = ReadConfig();
        var profiles = NormalizeProfiles(player["titleProfiles"] as JArray);
        var name = Arg("titleProfileName", "New Title Profile");
        var source = FindProfile(profiles, "default") ?? CreateProfile("default", "Default");
        profiles.Add(CloneProfile(Guid.NewGuid().ToString("N"), name, source));
        player["titleProfiles"] = profiles;
        SaveConfig(player);
        return true;
    }

    public bool RemoveProfile()
    {
        var id = Arg("titleProfileId", "");
        if (string.IsNullOrWhiteSpace(id) || id == "default") return false;
        var player = ReadConfig();
        var profiles = NormalizeProfiles(player["titleProfiles"] as JArray);
        for (var i = profiles.Count - 1; i >= 0; i--)
            if (string.Equals((string)profiles[i]["id"], id, StringComparison.Ordinal)) profiles.RemoveAt(i);
        var title = player["title"] as JObject ?? new JObject();
        title["selectedProfile"] = ResolveProfileId(profiles, (string)title["selectedProfile"]) ?? "default";
        title["entryPoints"] = ResetRemovedEntries(title["entryPoints"] as JObject, id);
        player["titleProfiles"] = profiles;
        player["title"] = title;
        SaveConfig(player);
        return true;
    }

    public bool ResolveEntryPointProfile()
    {
        var entryPoint = Arg("titleEntryPoint", "");
        if (string.IsNullOrWhiteSpace(entryPoint)) entryPoint = CPH.GetGlobalVar<string>(EntryPointHandoffKey, false);
        CPH.UnsetGlobalVar(EntryPointHandoffKey, false);
        if (string.IsNullOrWhiteSpace(entryPoint)) return false;
        var player = ReadConfig();
        var profiles = NormalizeProfiles(player["titleProfiles"] as JArray);
        var title = player["title"] as JObject ?? new JObject();
        var entries = title["entryPoints"] as JObject ?? new JObject();
        var profile = ResolveProfileId(profiles, (string)entries[entryPoint.ToLowerInvariant()]) ?? "default";
        CPH.SetGlobalVar(ResolvedProfileHandoffKey, profile, false);
        CPH.SetArgument("replayTitleProfileId", profile);
        return true;
    }

    public bool ApplyProfile()
    {
        var profile = Arg("replayTitleProfileId", "");
        if (string.IsNullOrWhiteSpace(profile)) profile = CPH.GetGlobalVar<string>(PlaybackProfileHandoffKey, false);
        var player = ReadConfig();
        var profiles = NormalizeProfiles(player["titleProfiles"] as JArray);
        var title = player["title"] as JObject ?? new JObject();
        if (string.IsNullOrWhiteSpace(profile)) profile = (string)title["selectedProfile"];
        profile = ResolveProfileId(profiles, profile) ?? "default";
        CPH.UnsetGlobalVar(PlaybackProfileHandoffKey, false);
        var item = FindProfile(profiles, profile) ?? CreateProfile("default", "Default");
        CPH.SetArgument("replayTitleProfileId", profile);
        CPH.SetArgument("replayTitleProfile", item.ToString(Newtonsoft.Json.Formatting.None));
        ApplyArguments(item);
        return true;
    }

    private void ApplyArguments(JObject p)
    {
        CPH.SetArgument("replayShowTitle", Bool(p, "showTitle", true));
        CPH.SetArgument("replayTitleDecorationPosition", String(p, "decorationPosition", "Suffix"));
        CPH.SetArgument("replayTitleDecoration", String(p, "decoration", " - Replay Capture"));
        CPH.SetArgument("replayTitleStyle", String(p, "style", "Broadcast"));
        CPH.SetArgument("replayTitlePosition", String(p, "position", "Bottom"));
        CPH.SetArgument("replayTitleAnimation", String(p, "animation", "Slide up/down"));
        CPH.SetArgument("replayTitleDelay", Number(p, "delay", 0));
        CPH.SetArgument("replayTitleDuration", Number(p, "duration", 5000));
        CPH.SetArgument("replayTitleAnimationDuration", Number(p, "animationDuration", 450));
        CPH.SetArgument("replayTitleFont", String(p, "font", "Inter"));
        CPH.SetArgument("replayTitleFontSize", Number(p, "fontSize", 34));
        CPH.SetArgument("replayTitleTextColor", String(p, "textColor", "#FFFFFFFF"));
        CPH.SetArgument("replayTitleShadowColor", String(p, "shadowColor", "#000000FF"));
        CPH.SetArgument("replayTitlePrimaryColor", String(p, "primaryColor", "#0384CBFF"));
        CPH.SetArgument("replayTitleSecondaryColor", String(p, "secondaryColor", "#101416FF"));
        var broadcast = p["broadcast"] as JObject ?? new JObject();
        CPH.SetArgument("replayBroadcastPrimaryColor", String(broadcast, "primaryColor", "#0384CBFF"));
        CPH.SetArgument("replayBroadcastSecondaryColor", String(broadcast, "secondaryColor", "#FFD400FF"));
        CPH.SetArgument("replayBroadcastChevronHeight", Number(broadcast, "chevronHeight", 42));
        CPH.SetArgument("replayBroadcastRandomHeight", Bool(broadcast, "randomHeight", false));
        CPH.SetArgument("replayBroadcastChevronWidth", Number(broadcast, "chevronWidth", 42));
        CPH.SetArgument("replayBroadcastRandomWidth", Bool(broadcast, "randomWidth", false));
        CPH.SetArgument("replayBroadcastChevronSpacing", Number(broadcast, "chevronSpacing", 0));
        CPH.SetArgument("replayBroadcastRandomSpacing", Bool(broadcast, "randomSpacing", false));
        CPH.SetArgument("replayBroadcastChevronSpeed", Number(broadcast, "chevronSpeed", 95));
        CPH.SetArgument("replayBroadcastDecorationColor", String(broadcast, "decorationColor", "#0384CBFF"));
        CPH.SetArgument("replayBroadcastTitleColor", String(broadcast, "titleColor", "#FFFFFFFF"));
        var cut = p["cut"] as JObject ?? new JObject();
        CPH.SetArgument("replayCutPrimaryColor", String(cut, "primaryColor", "#0384CBFF"));
        CPH.SetArgument("replayCutSecondaryColor", String(cut, "secondaryColor", "#FFD400FF"));
        CPH.SetArgument("replayCutBlockWidth", Number(cut, "blockWidth", 170));
        CPH.SetArgument("replayCutRandomWidth", Bool(cut, "randomWidth", true));
        CPH.SetArgument("replayCutBarHeight", Number(cut, "barHeight", 5));
        CPH.SetArgument("replayCutDecorationColor", String(cut, "decorationColor", "#0384CBFF"));
        CPH.SetArgument("replayCutTitleColor", String(cut, "titleColor", "#FFFFFFFF"));
    }

    private JArray NormalizeProfiles(JArray source)
    {
        var result = new JArray();
        foreach (var token in source ?? new JArray())
        {
            var item = token as JObject;
            var id = (string)item?["id"];
            if (item == null || string.IsNullOrWhiteSpace(id) || FindProfile(result, id) != null) continue;
            result.Add(item.DeepClone());
        }
        var legacy = CreateProfile("default", "Default");
        var existing = FindProfile(result, "default");
        if (existing == null) result.Insert(0, legacy); else result.Remove(existing);
        result.Insert(0, existing ?? legacy);
        return result;
    }

    private JObject CreateProfile(string id, string name)
    {
        var p = new JObject { ["id"] = id, ["name"] = name, ["showTitle"] = LegacyBool("rts.actionreplay.showTitle", true), ["decorationPosition"] = LegacyString("rts.actionreplay.titleDecorationPosition", "Suffix"), ["decoration"] = LegacyString("rts.actionreplay.titleDecoration", " - Replay Capture"), ["style"] = LegacyString("rts.actionreplay.titleBarStyle", "Broadcast"), ["position"] = LegacyString("rts.actionreplay.titlePosition", "Bottom"), ["animation"] = LegacyString("rts.actionreplay.titleAnimation", "Slide up/down"), ["delay"] = LegacyInt("rts.actionreplay.titleDelay", 0), ["duration"] = LegacyInt("rts.actionreplay.titleDuration", 5000), ["animationDuration"] = LegacyInt("rts.actionreplay.titleAnimationDuration", 450), ["font"] = LegacyString("rts.actionreplay.titleFont", "Inter"), ["fontSize"] = LegacyInt("rts.actionreplay.titleFontSize", 34), ["textColor"] = LegacyString("rts.actionreplay.titleTextColor", "#FFFFFFFF"), ["shadowColor"] = LegacyString("rts.actionreplay.titleShadowColor", "#000000FF"), ["primaryColor"] = LegacyString("rts.actionreplay.titlePrimaryColor", "#0384CBFF"), ["secondaryColor"] = LegacyString("rts.actionreplay.titleSecondaryColor", "#101416FF") };
        p["broadcast"] = new JObject { ["primaryColor"] = LegacyString("rts.actionreplay.broadcast.primaryColor", "#0384CBFF"), ["secondaryColor"] = LegacyString("rts.actionreplay.broadcast.secondaryColor", "#FFD400FF"), ["chevronHeight"] = LegacyInt("rts.actionreplay.broadcast.chevronHeight", 42), ["randomHeight"] = LegacyBool("rts.actionreplay.broadcast.randomHeight", false), ["chevronWidth"] = LegacyInt("rts.actionreplay.broadcast.chevronWidth", 42), ["randomWidth"] = LegacyBool("rts.actionreplay.broadcast.randomWidth", false), ["chevronSpacing"] = LegacyInt("rts.actionreplay.broadcast.chevronSpacing", 0), ["randomSpacing"] = LegacyBool("rts.actionreplay.broadcast.randomSpacing", false), ["chevronSpeed"] = LegacyInt("rts.actionreplay.broadcast.chevronSpeed", 95), ["decorationColor"] = LegacyString("rts.actionreplay.broadcast.decorationColor", "#0384CBFF"), ["titleColor"] = LegacyString("rts.actionreplay.broadcast.titleColor", "#FFFFFFFF") };
        p["cut"] = new JObject { ["primaryColor"] = LegacyString("rts.actionreplay.cut.primaryColor", "#0384CBFF"), ["secondaryColor"] = LegacyString("rts.actionreplay.cut.secondaryColor", "#FFD400FF"), ["blockWidth"] = LegacyInt("rts.actionreplay.cut.blockWidth", 170), ["randomWidth"] = LegacyBool("rts.actionreplay.cut.randomWidth", true), ["barHeight"] = LegacyInt("rts.actionreplay.cut.barHeight", 5), ["decorationColor"] = LegacyString("rts.actionreplay.cut.decorationColor", "#0384CBFF"), ["titleColor"] = LegacyString("rts.actionreplay.cut.titleColor", "#FFFFFFFF") };
        return p;
    }

    private JObject CloneProfile(string id, string name, JObject source) { var copy = (JObject)source.DeepClone(); copy["id"] = id; copy["name"] = name; return copy; }
    private JObject ReadConfig() { var raw = CPH.GetGlobalVar<string>(PlayerKey, true); try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { return new JObject(); } }
    private void SaveConfig(JObject player) => CPH.SetGlobalVar(PlayerKey, player.ToString(Newtonsoft.Json.Formatting.None), true);
    private JObject FindProfile(JArray profiles, string id) { foreach (var item in profiles ?? new JArray()) if (string.Equals((string)item["id"], id, StringComparison.Ordinal)) return item as JObject; return null; }
    private string ResolveProfileId(JArray profiles, string value) { foreach (var item in profiles ?? new JArray()) if (string.Equals((string)item["id"], value, StringComparison.Ordinal) || string.Equals((string)item["name"], value, StringComparison.OrdinalIgnoreCase)) return (string)item["id"]; return null; }
    private JObject ResetRemovedEntries(JObject source, string id) { var result = source ?? new JObject(); foreach (var property in result.Properties()) if (string.Equals((string)property.Value, id, StringComparison.Ordinal)) property.Value = "default"; return result; }
    private JObject NormalizeEntryPoints(JObject source, JArray profiles) { var result = new JObject(); foreach (var point in new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" }) result[point] = ResolveProfileId(profiles, (string)source?[point]) ?? "default"; return result; }
    private string Arg(string name, string fallback) => CPH.TryGetArg(name, out string value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
    private static string String(JObject o, string key, string fallback) => (string)o[key] ?? fallback;
    private static int Number(JObject o, string key, int fallback) => (int?)o[key] ?? fallback;
    private static bool Bool(JObject o, string key, bool fallback) => (bool?)o[key] ?? fallback;
    private string LegacyString(string key, string fallback) => CPH.GetGlobalVar<string>(key, true) ?? fallback;
    private int LegacyInt(string key, int fallback) => CPH.GetGlobalVar<int?>(key, true) ?? fallback;
    private bool LegacyBool(string key, bool fallback) => CPH.GetGlobalVar<bool?>(key, true) ?? fallback;
}
