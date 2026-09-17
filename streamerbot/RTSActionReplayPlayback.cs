using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string LegacyCatalogKey = "rts.actionreplay.catalog";
    private const string PendingKey = "rts.actionreplay.pendingSaves";
    private const string EventName = "RTS-Action Replay";
    private const string TwitchFolderKey = "rts.actionreplay.twitch.folder";
    private const string TwitchMappingKey = "rts.actionreplay.twitch.httpMapping";
    private const string TwitchModeKey = "rts.actionreplay.twitch.playbackMode";
    private const string KickFolderKey = "rts.actionreplay.kick.folder";
    private const string KickMappingKey = "rts.actionreplay.kick.httpMapping";
    private const string KickModeKey = "rts.actionreplay.kick.playbackMode";
    private const string AnimationAction = "RTS - Action Replay - Core - Animation";
    private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";
    private const string CatalogAction = "RTS - Action Replay - Core - Catalog";
    private const string PresetStoreAction = "RTS - Action Replay - Core - Presets Store";
    private const string PositionStoreAction = "RTS - Action Replay - Core - Positions";
    private const string ReplayIdHandoffKey = "rts.actionreplay.handoff.replayId";
    private const string PlaybackProfileHandoffKey = "rts.actionreplay.handoff.playbackProfile";
    private const string PlaybackQueueEntryHandoffKey = "rts.actionreplay.handoff.playbackQueueEntryId";
    private const string PlayerPositionsHandoffKey = "rts.actionreplay.handoff.playerPositions";
    private const string AnimationProfileHandoffKey = "rts.actionreplay.handoff.animationProfile";
    private const string VisualHandoffKey = "rts.actionreplay.handoff.visualBranding";

    public bool Execute() => PlayReplay();

    public bool SaveReplay()
    {
        CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName);
        var pending = CPH.GetGlobalVar<string>(PendingKey, false); var queue = string.IsNullOrWhiteSpace(pending) ? new JArray() : JArray.Parse(pending);
        if (!string.IsNullOrWhiteSpace(userId)) queue.Add(new JObject { ["id"] = userId, ["name"] = userName ?? "", ["queued"] = DateTime.UtcNow.ToString("o") });
        CPH.SetGlobalVar(PendingKey, queue.ToString(Newtonsoft.Json.Formatting.None), false); CPH.ObsReplayBufferSave(); CPH.LogInfo("RTS Action Replay: requested OBS Replay Buffer save."); return true;
    }

    public bool PlayReplay()
    {
        var data = Load(); var list = GetCatalog(data); JObject replay = null;
        var handoffQueueEntryId = CPH.GetGlobalVar<string>(PlaybackQueueEntryHandoffKey, false);
        var handoffReplayId = CPH.GetGlobalVar<string>(ReplayIdHandoffKey, false);
        string selector = null; CPH.TryGetArg("rawInput", out selector);
        CPH.LogInfo($"RTS Action Replay TRACE: PlayReplay entered; handoffReplayId={handoffReplayId ?? "<none>"}; handoffQueueEntryId={handoffQueueEntryId ?? "<none>"}; rawInput={selector ?? "<none>"}; catalogCount={list.Count}.");
        if (!string.IsNullOrWhiteSpace(handoffQueueEntryId) && !string.IsNullOrWhiteSpace(handoffReplayId))
        {
            replay = list.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], handoffReplayId, StringComparison.OrdinalIgnoreCase));
            if (replay == null) { CPH.LogWarn($"RTS Action Replay TRACE: PlayReplay failed - handoff replay {handoffReplayId} not found in catalog."); SendMessage("Replay not found."); return false; }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(selector)) { CPH.LogWarn("RTS Action Replay TRACE: PlayReplay failed - no queue handoff and rawInput is empty."); return false; }
            selector = selector.Trim();
            if (TryParseUserSelection(selector, out var targetPlatform, out var targetUser, out var index))
            {
                CPH.SetArgument("catalogSelectionReplayId", ""); CPH.SetArgument("catalogSelectionUser", targetUser); CPH.SetArgument("catalogSelectionPlatform", targetPlatform);
                if (CPH.ExecuteMethod(CatalogAction, "ResolveSelectionForUser"))
                {
                    var replayId = Arg("catalogSelectionReplayId");
                    replay = list.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], replayId, StringComparison.OrdinalIgnoreCase));
                    CPH.LogInfo($"RTS Action Replay TRACE: PlayReplay user catalog selection {targetPlatform}:{targetUser} #{index} resolved to replayId={replayId ?? "<none>"}.");
                }
            }
            else if (int.TryParse(selector, out var numericIndex) && numericIndex > 0)
            {
                CPH.SetArgument("catalogSelectionReplayId", "");
                if (CPH.ExecuteMethod(CatalogAction, "ResolveSelection"))
                {
                    var replayId = Arg("catalogSelectionReplayId");
                    replay = list.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], replayId, StringComparison.OrdinalIgnoreCase));
                    CPH.LogInfo($"RTS Action Replay TRACE: PlayReplay catalog selection {numericIndex} resolved to replayId={replayId ?? "<none>"}.");
                }
            }
            else
            {
                replay = list.OfType<JObject>().FirstOrDefault(x => ((bool?)x["customTitle"] ?? false) && string.Equals((string)x["title"], selector, StringComparison.OrdinalIgnoreCase));
            }
            if (replay == null) { CPH.LogWarn($"RTS Action Replay TRACE: PlayReplay failed - selector '{selector}' did not resolve to a replay."); SendMessage("Replay not found."); return false; }
        }

        var queueEntryId = handoffQueueEntryId;
        if (string.IsNullOrWhiteSpace(queueEntryId)) CPH.TryGetArg("replayQueueEntryId", out queueEntryId);
        CPH.LogInfo($"RTS Action Replay TRACE: PlayReplay resolved replayId={(string)replay["id"]}; title={(string)replay["title"]}; queueEntryId={queueEntryId ?? "<none>"}.");
        if (string.IsNullOrWhiteSpace(queueEntryId))
        {
            CPH.SetGlobalVar(ReplayIdHandoffKey, (string)replay["id"] ?? "", false); CPH.SetArgument("presetComponent", "player"); CPH.SetArgument("entryPoint", "play");
            CPH.LogInfo("RTS Action Replay TRACE: PlayReplay selection path; resolving Play — Replay presets before enqueue.");
            if (!CPH.ExecuteMethod(PresetStoreAction, "ResolveEntryPoint")) { CPH.LogWarn("RTS Action Replay TRACE: PlayReplay failed - Play — Replay preset resolution returned false."); return false; }
            return CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay");
        }

        var source = (string)replay["sourceType"] ?? "OBS"; var url = ResolveReplayUrl(replay);
        if (string.Equals(source, "Kick", StringComparison.OrdinalIgnoreCase))
        {
            url = ResolveKickUrl(replay);
            if (string.IsNullOrWhiteSpace(url)) { CPH.LogWarn($"RTS Action Replay TRACE: Kick media resolution failed for replay {(string)replay["id"]}."); SendMessage("Unable to resolve Kick media file."); return false; }
            CPH.LogInfo($"RTS Action Replay TRACE: Kick media resolved for playback; replayId={(string)replay["id"]}; url={url}.");
        }
        CPH.LogInfo($"RTS Action Replay TRACE: PlayReplay media resolution returned {(string.IsNullOrWhiteSpace(url) ? "<null>" : url)}.");
        if (string.IsNullOrWhiteSpace(url)) { CPH.LogWarn($"RTS Action Replay TRACE: PlayReplay failed - media URL unavailable for replay {(string)replay["id"]}; file={(string)replay["file"]}; filePath={(string)replay["filePath"]}."); SendMessage($"Replay media is unavailable: {(string)replay["title"]}"); return false; }
        CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName); var creator = replay["creator"] as JObject; var creatorName = (string)creator?["name"] ?? "";
        CPH.SetArgument("replayCommand", "load"); CPH.SetArgument("replayId", (string)replay["id"]); CPH.SetArgument("replayUrl", url); CPH.SetArgument("replayAutoplay", true); CPH.SetArgument("replayQueueEntryId", queueEntryId); CPH.SetArgument("replayUserId", userId ?? ""); CPH.SetArgument("replayUserName", userName ?? ""); CPH.SetArgument("replayDirector", creatorName);
        CPH.SetArgument("replayNumber", Array.IndexOf(list.ToArray(), replay) + 1); CPH.SetArgument("replayTitle", (string)replay["title"] ?? ""); CPH.SetArgument("replayPlayedCount", ((int?)replay["plays"] ?? 0) + 1); CPH.SetArgument("replaySource", source); CPH.SetArgument("replaySourceId", (string)replay["sourceId"] ?? "");
        if (string.Equals(source, "YouTube", StringComparison.OrdinalIgnoreCase)) { CPH.SetArgument("replayStartTime", (long?)replay["startTime"] ?? 0); CPH.SetArgument("replayDuration", (int?)replay["duration"] ?? 0); }
        var profile = CPH.TryGetArg("replayAnimationProfileId", out string requestedProfile) && !string.IsNullOrWhiteSpace(requestedProfile) ? requestedProfile.Trim() : CPH.GetGlobalVar<string>(PlaybackProfileHandoffKey, false); if (string.IsNullOrWhiteSpace(profile)) profile = "default";
        var designProfile = CPH.TryGetArg("designPreset", out string requestedDesign) && !string.IsNullOrWhiteSpace(requestedDesign) ? requestedDesign.Trim() : "broadcast";
        var titleProfile = CPH.TryGetArg("titlePreset", out string requestedTitle) && !string.IsNullOrWhiteSpace(requestedTitle) ? requestedTitle.Trim() : "default";
        var brandingProfile = CPH.TryGetArg("brandingPreset", out string requestedBranding) && !string.IsNullOrWhiteSpace(requestedBranding) ? requestedBranding.Trim() : "default";
        CPH.SetArgument("animationProfile", profile); CPH.SetArgument("designPreset", designProfile); CPH.SetArgument("titlePreset", titleProfile); CPH.SetArgument("brandingPreset", brandingProfile);
        CPH.LogInfo($"RTS Action Replay TRACE: PlayReplay dispatching load; replayId={(string)replay["id"]}; url={url}; animationProfile={profile}; designPreset={designProfile}; titlePreset={titleProfile}; brandingPreset={brandingProfile}; queueEntryId={queueEntryId}; source={source}.");
        if (!CPH.ExecuteMethod(PresetStoreAction, "ApplyVisualAndBranding")) { CPH.LogWarn("RTS Action Replay TRACE: PlayReplay failed - queued visual/title/branding presets could not be applied."); return false; }
        ApplyVisualHandoff(queueEntryId);
        CPH.SetArgument("replayAnimationProfileId", profile);
        CPH.SetGlobalVar(PlaybackProfileHandoffKey, profile, false);
        if (!CPH.ExecuteMethod(AnimationAction, "ApplyProfile")) { CPH.LogWarn($"RTS Action Replay TRACE: PlayReplay failed - animation profile '{profile}' could not be applied."); return false; }
        ApplyAnimationHandoff();
        CPH.ExecuteMethod(PositionStoreAction, "GetPlayerPositions");
        CPH.SetArgument("replayPositions", CPH.GetGlobalVar<string>(PlayerPositionsHandoffKey, false) ?? "");
        CPH.TriggerEvent(EventName, true); SendMessage("play"); CPH.LogInfo($"RTS Action Replay TRACE: PlayReplay completed dispatch for replay {(string)replay["id"]}."); return true;
    }

    private void ApplyVisualHandoff(string queueEntryId)
    {
        var key = VisualHandoffKey + "." + (queueEntryId ?? "");
        var raw = CPH.GetGlobalVar<string>(key, false); if (string.IsNullOrWhiteSpace(raw)) return;
        try
        {
            var p = JObject.Parse(raw);
            CPH.SetArgument("replayShowTitle", (bool?)p["showTitle"] ?? true);
            CPH.SetArgument("replayTitleDecorationPosition", (string)p["decorationPosition"] ?? "Prefix");
            CPH.SetArgument("replayTitleDecoration", (string)p["decoration"] ?? "Action Replay -");
            CPH.SetArgument("replayTitlePosition", (string)p["position"] ?? "Bottom");
            CPH.SetArgument("replayTitleAnimation", (string)p["animation"] ?? "Left to right");
            CPH.SetArgument("replayTitleDelay", (int?)p["delay"] ?? 2000);
            CPH.SetArgument("replayTitleDuration", (int?)p["duration"] ?? 10000);
            CPH.SetArgument("replayTitleAnimationDuration", (int?)p["animationDuration"] ?? 1000);
            CPH.SetArgument("replayTitleFont", (string)p["font"] ?? "Inter");
            CPH.SetArgument("replayTitleFontSize", (int?)p["fontSize"] ?? 34);
            CPH.SetArgument("replayTitleTextColor", (string)p["textColor"] ?? "#FFFFFFFF");
            CPH.SetArgument("replayTitleShadowColor", (string)p["shadowColor"] ?? "#000000FF");
            CPH.SetArgument("replayTitlePrimaryColor", (string)p["primaryColor"] ?? "#0384CBFF");
            CPH.SetArgument("replayTitleSecondaryColor", (string)p["secondaryColor"] ?? "#101416FF");
            CPH.SetArgument("replayBrandLogoUrl", (string)p["brandLogoUrl"] ?? "");
            CPH.SetArgument("replayBrandFallbackText", (string)p["brandFallbackText"] ?? "RTS");
            CPH.SetArgument("replayBrandLabel", (string)p["brandLabel"] ?? "ACTION REPLAY");
            CPH.SetArgument("replayBrandFallbackTextColor", (string)p["brandFallbackTextColor"] ?? "#0384CBFF");
            CPH.SetArgument("replayBrandLabelColor", (string)p["brandLabelColor"] ?? "#FFFFFFFF");
            ApplyProperties("replayBroadcast", p["broadcast"] as JObject); ApplyProperties("replayCut", p["cut"] as JObject);
            CPH.UnsetGlobalVar(key, false);
        }
        catch { CPH.LogWarn("RTS Action Replay: visual/branding preset handoff could not be parsed."); }
    }

    private void ApplyAnimationHandoff()
    {
        var raw = CPH.GetGlobalVar<string>(AnimationProfileHandoffKey, false); if (string.IsNullOrWhiteSpace(raw)) return;
        try
        {
            var p = JObject.Parse(raw); CPH.SetArgument("replayAnimationProfile", raw); CPH.SetArgument("replayAnimationProfileId", (string)p["id"] ?? "default");
            CPH.SetArgument("replayStartPosition", (string)p["start"]?[0]?["position"] ?? "Full Screen");
            var end = p["end"] as JArray; CPH.SetArgument("replayEndPosition", (string)end?[end.Count - 1]?["position"] ?? "Full Screen");
            CPH.UnsetGlobalVar(AnimationProfileHandoffKey, false);
        }
        catch { CPH.LogWarn("RTS Action Replay: animation profile handoff could not be parsed."); }
    }

    private void ApplyProperties(string prefix, JObject value)
    {
        foreach (var property in value?.Properties() ?? new JProperty[0]) CPH.SetArgument(prefix + char.ToUpperInvariant(property.Name[0]) + property.Name.Substring(1), property.Value.Type == JTokenType.Boolean ? (object)(bool)property.Value : property.Value.Type == JTokenType.Integer ? (object)(int)property.Value : property.Value.ToString());
    }

    private bool TryParseUserSelection(string selector, out string platform, out string userName, out int index)
    {
        platform = ""; userName = ""; index = 0;
        var parts = (selector ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out index) || index < 1) return false;
        var target = parts[0].Trim(); if (string.IsNullOrWhiteSpace(target)) return false;
        if (target.Equals("twitch", StringComparison.OrdinalIgnoreCase)) platform = "Twitch";
        else if (target.Equals("youtube", StringComparison.OrdinalIgnoreCase) || target.Equals("yt", StringComparison.OrdinalIgnoreCase)) platform = "YouTube";
        else if (target.Equals("kick", StringComparison.OrdinalIgnoreCase)) platform = "Kick";
        else return false;
        CPH.TryGetArg("userName", out userName); if (string.IsNullOrWhiteSpace(userName)) userName = target;
        return true;
    }

    private string ResolveReplayUrl(JObject replay)
    {
        var file = (string)replay["filePath"] ?? (string)replay["file"];
        if (!string.IsNullOrWhiteSpace(file) && File.Exists(file)) return BuildHttpUrl(file);
        var url = (string)replay["mediaUrl"] ?? (string)replay["url"];
        return url;
    }

    private string ResolveKickUrl(JObject replay)
    {
        var direct = (string)replay["mediaUrl"] ?? (string)replay["url"];
        if (!string.IsNullOrWhiteSpace(direct)) return direct;
        var sourceId = (string)replay["sourceId"];
        if (string.IsNullOrWhiteSpace(sourceId)) return "";
        var folder = CPH.GetGlobalVar<string>(KickFolderKey, true);
        var mapping = CPH.GetGlobalVar<string>(KickMappingKey, true);
        var file = FindMediaFile(folder, sourceId);
        return string.IsNullOrWhiteSpace(file) ? "" : BuildHttpUrl(file, mapping, CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474);
    }

    private string FindMediaFile(string folder, string sourceId)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return "";
        var files = Directory.GetFiles(folder); return files.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(sourceId, StringComparison.OrdinalIgnoreCase)) ?? "";
    }

    private string BuildHttpUrl(string file, string mapping = null, int port = 7474)
    {
        var root = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true); mapping = string.IsNullOrWhiteSpace(mapping) ? CPH.GetGlobalVar<string>("rts.actionreplay.httpMapping", true) : mapping;
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(mapping)) return "";
        var relative = file.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : Path.GetFileName(file);
        return "http://127.0.0.1:" + port + "/" + mapping.Trim('/') + "/" + Uri.EscapeDataString(relative.Replace('\\', '/'));
    }

    private JObject Load(){var raw=CPH.GetGlobalVar<string>(DataKey,true);if(string.IsNullOrWhiteSpace(raw))raw=CPH.GetGlobalVar<string>(LegacyCatalogKey,true);try{return string.IsNullOrWhiteSpace(raw)?new JObject():JObject.Parse(raw);}catch{return new JObject();}}
    private JArray GetCatalog(JObject data)=>data["catalog"] as JArray??data["replays"] as JArray??new JArray();
    private string Arg(string key){try{CPH.TryGetArg(key,out string value);return value;}catch{return "";}}
    private void SendMessage(string message){CPH.LogInfo("RTS Action Replay: "+message);}
}
