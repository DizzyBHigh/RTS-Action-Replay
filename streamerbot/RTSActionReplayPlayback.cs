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
        if (!CPH.ExecuteMethod(AnimationAction, "ApplyProfile")) { CPH.LogWarn($"RTS Action Replay TRACE: PlayReplay failed - animation profile '{profile}' could not be applied."); return false; }
        ApplyAnimationHandoff();
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
        platform = Arg("userType"); userName = target;
        var separator = target.IndexOf(':');
        if (separator > 0)
        {
            var prefix = target.Substring(0, separator); var name = target.Substring(separator + 1).Trim();
            if (!prefix.Equals("twitch", StringComparison.OrdinalIgnoreCase) && !prefix.Equals("kick", StringComparison.OrdinalIgnoreCase) && !prefix.Equals("youtube", StringComparison.OrdinalIgnoreCase)) return false;
            platform = prefix; userName = name;
        }
        return !string.IsNullOrWhiteSpace(userName);
    }

    private string ResolveReplayUrl(JObject replay)
    {
        var source = (string)replay["sourceType"];
        if (string.Equals(source, "Twitch", StringComparison.OrdinalIgnoreCase)) return ResolveTwitchUrl(replay);
        if (string.Equals(source, "YouTube", StringComparison.OrdinalIgnoreCase)) { var id = (string)replay["sourceId"]; return string.IsNullOrWhiteSpace(id) ? null : "https://www.youtube.com/embed/" + CPH.UrlEncode(id); }
        if (string.Equals(source, "Kick", StringComparison.OrdinalIgnoreCase)) return null;
        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true); var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.httpMapping", true) ?? "replays"; var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474; var file = (string)replay["file"]; var path = Path.Combine(folder ?? "", file ?? "");
        CPH.LogInfo($"RTS Action Replay TRACE: ResolveReplayUrl OBS; folder={folder ?? "<null>"}; file={file ?? "<null>"}; path={path}; exists={File.Exists(path)}; mapping={mapping}; port={port}."); if (!File.Exists(path)) return null;
        return "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode(file ?? "");
    }

    private string ResolveKickUrl(JObject replay)
    {
        var mode = GetKickPlaybackMode();
        if (ModeNeedsLocalCopy(mode))
        {
            var localPath = (string)replay["filePath"]; if (string.IsNullOrWhiteSpace(localPath)) localPath = (string)replay["file"];
            var folder = CPH.GetGlobalVar<string>(KickFolderKey, true);
            if (!string.IsNullOrWhiteSpace(localPath)) { var fullPath = Path.IsPathRooted(localPath) ? localPath : Path.Combine(folder ?? "", localPath); if (File.Exists(fullPath)) return BuildKickHttpUrl(Path.GetFileName(fullPath)); }
        }
        var mediaUrl = ResolveKickMediaUrl(replay);
        if (string.IsNullOrWhiteSpace(mediaUrl)) return null;
        if (!ModeNeedsLocalCopy(mode)) return mediaUrl;
        var downloaded = DownloadKickClip(replay, mediaUrl);
        if (!string.IsNullOrWhiteSpace(downloaded)) { replay["file"] = Path.GetFileName(downloaded); replay["filePath"] = downloaded; SaveReplayFile(replay); return BuildKickHttpUrl(Path.GetFileName(downloaded)); }
        CPH.LogWarn("RTS Action Replay: Kick local download failed; falling back to direct Kick media URL."); return mediaUrl;
    }

    private string ResolveKickMediaUrl(JObject replay)
    {
        var acquisition = (string)replay["acquisitionMethod"];
        if (string.Equals(acquisition, "KickBot", StringComparison.OrdinalIgnoreCase)) return ResolveKickBotUrl(replay);
        return ResolveNativeKickUrl(replay);
    }

    private string ResolveNativeKickUrl(JObject replay)
    {
        var clipId = ExtractKickClipId((string)replay["sourceId"]); if (string.IsNullOrWhiteSpace(clipId)) clipId = ExtractKickClipId((string)replay["sourceUrl"]); if (string.IsNullOrWhiteSpace(clipId)) return null;
        var json = DownloadString("https://kick.com/api/v2/clips/" + CPH.UrlEncode(clipId) + "/play"); if (string.IsNullOrWhiteSpace(json)) return null;
        try { var clip = JObject.Parse(json)["clip"] as JObject; var mediaUrl = (string)clip?["clip_url"]; CPH.LogInfo($"RTS Action Replay TRACE: native Kick media resolved; clipId={clipId}; mediaUrl={mediaUrl ?? "<null>"}."); return mediaUrl; }
        catch (Exception ex) { CPH.LogWarn("RTS Action Replay: native Kick clip response could not be parsed: " + ex.Message); return null; }
    }

    private string ResolveKickBotUrl(JObject replay)
    {
        var sourceUrl = (string)replay["sourceUrl"]; var sourceId = (string)replay["sourceId"]; var html = DownloadString(sourceUrl); var clipId = ExtractKickBotClipId(sourceId); if (string.IsNullOrWhiteSpace(clipId)) clipId = ExtractKickBotClipId(sourceUrl); if (string.IsNullOrWhiteSpace(clipId)) clipId = ExtractKickBotClipId(html); if (string.IsNullOrWhiteSpace(clipId)) return null;
        var directMp4 = "https://clips.kickbotcdn.com/kickbot-hls/" + clipId + "/" + clipId + ".mp4"; CPH.LogInfo($"RTS Action Replay TRACE: KickBot media candidate generated; clipId={clipId}; url={directMp4}."); if (!WaitForKickBotMedia(directMp4, clipId)) return null; return directMp4;
    }

    private bool WaitForKickBotMedia(string url, string clipId)
    {
        for (var attempt = 1; attempt <= 40; attempt++)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url); request.Method = "GET"; request.AddRange(0, 0); request.Timeout = 5000; request.ReadWriteTimeout = 5000; request.UserAgent = "RTS-Action-Replay";
                using (var response = (HttpWebResponse)request.GetResponse()) using (var stream = response.GetResponseStream()) { var status = (int)response.StatusCode; var contentType = response.ContentType ?? ""; if (status >= 200 && status < 300 && !contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) { CPH.LogInfo($"RTS Action Replay TRACE: KickBot media ready; clipId={clipId}; attempt={attempt}; status={status}; contentType={contentType}."); return true; } CPH.LogInfo($"RTS Action Replay TRACE: KickBot media not ready; clipId={clipId}; attempt={attempt}; status={status}; contentType={contentType}."); }
            }
            catch (WebException ex) { CPH.LogInfo($"RTS Action Replay TRACE: KickBot media not ready; clipId={clipId}; attempt={attempt}; error={ex.Message}."); }
            if (attempt < 40) CPH.Wait(5000);
        }
        CPH.LogWarn($"RTS Action Replay TRACE: KickBot media did not become ready within 200 seconds; clipId={clipId}."); return false;
    }

    private string DownloadKickClip(JObject replay, string mediaUrl)
    {
        var folder = CPH.GetGlobalVar<string>(KickFolderKey, true); var replayFolder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        if (string.IsNullOrWhiteSpace(folder)) { CPH.LogWarn("RTS Action Replay: Kick Clip Folder is not configured."); return null; }
        if (!string.IsNullOrWhiteSpace(replayFolder) && PathsEqual(folder, replayFolder)) { CPH.LogError("RTS Action Replay: Kick Clip Folder must be different from the OBS Replay Folder."); return null; }
        Directory.CreateDirectory(folder);
        var sourceId = (string)replay["sourceId"] ?? "clip"; var destination = Path.Combine(folder, "kick-" + Sanitize(sourceId) + ".mp4"); if (File.Exists(destination) && new FileInfo(destination).Length > 0) return destination;
        var temp = destination + ".tmp"; try { if (IsHlsUrl(mediaUrl)) { if (!RunFfmpeg(mediaUrl, temp)) return null; } else { using (var client = new WebClient()) client.DownloadFile(mediaUrl, temp); } if (File.Exists(temp) && new FileInfo(temp).Length > 0) { if (File.Exists(destination)) File.Delete(destination); File.Move(temp, destination); return destination; } }
        catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Kick clip download failed: " + ex.Message); }
        try { if (File.Exists(temp)) File.Delete(temp); } catch { } return null;
    }

    private bool RunFfmpeg(string inputUrl, string outputPath)
    {
        try
        {
            var start = new ProcessStartInfo { FileName = "ffmpeg", Arguments = "-y -i " + Quote(inputUrl) + " -c copy -movflags +faststart " + Quote(outputPath), UseShellExecute = false, CreateNoWindow = true };
            using (var process = Process.Start(start)) { if (process == null) return false; if (!process.WaitForExit(120000)) { try { process.Kill(); } catch { } CPH.LogWarn("RTS Action Replay: ffmpeg timed out while downloading a Kick clip."); return false; } return process.ExitCode == 0; }
        }
        catch (Exception ex) { CPH.LogWarn("RTS Action Replay: ffmpeg could not be started for Kick clip download: " + ex.Message); return false; }
    }

    private static string Quote(string value) => "\"" + (value ?? "").Replace("\"", "\\\"") + "\"";
    private static bool IsHlsUrl(string url) => (url ?? "").IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0;
    private string BuildKickHttpUrl(string fileName) { var mapping = CPH.GetGlobalVar<string>(KickMappingKey, true) ?? "kick"; var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474; return "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode(fileName ?? ""); }
    private bool ModeNeedsLocalCopy(string mode) => string.Equals(mode, "Download Locally", StringComparison.OrdinalIgnoreCase) || string.Equals(mode, "Both", StringComparison.OrdinalIgnoreCase);
    private string GetKickPlaybackMode() { var mode = CPH.GetGlobalVar<string>(KickModeKey, true); if (string.Equals(mode, "Kick URL", StringComparison.OrdinalIgnoreCase)) return "Kick URL"; if (string.Equals(mode, "Both", StringComparison.OrdinalIgnoreCase)) return "Both"; return "Kick URL"; }
    private void SaveReplayFile(JObject replay) { try { var data = Load(); var item = GetCatalog(data).OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], (string)replay["id"], StringComparison.OrdinalIgnoreCase)); if (item == null) return; item["file"] = (string)replay["file"] ?? ""; item["filePath"] = (string)replay["filePath"] ?? ""; SaveData(data); } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: could not save Kick local file metadata: " + ex.Message); } }

    private string ExtractKickClipId(string value) { var match = Regex.Match(value ?? "", @"^clip_[A-Za-z0-9_-]+$", RegexOptions.IgnoreCase); if (match.Success) return match.Value; match = Regex.Match(value ?? "", @"[?&]clip=(clip_[A-Za-z0-9_-]+)", RegexOptions.IgnoreCase); if (match.Success) return match.Groups[1].Value; match = Regex.Match(value ?? "", @"/clips?/(clip_[A-Za-z0-9_-]+)", RegexOptions.IgnoreCase); return match.Success ? match.Groups[1].Value : null; }
    private string ExtractKickBotClipId(string value) { var match = Regex.Match(value ?? "", @"(?:kickbot\.com|kickbot\.app)/clip/([A-Za-z0-9]+)", RegexOptions.IgnoreCase); return match.Success ? match.Groups[1].Value : null; }
    private string DownloadString(string url) { if (string.IsNullOrWhiteSpace(url)) return null; try { using (var client = new WebClient()) { client.Headers[HttpRequestHeader.UserAgent] = "RTS-Action-Replay"; return client.DownloadString(url); } } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Kick request failed: " + ex.Message); return null; } }

    private string ResolveTwitchUrl(JObject replay)
    {
        var mode = GetTwitchPlaybackMode(); var clipId = (string)replay["sourceId"]; if (string.IsNullOrWhiteSpace(clipId)) return null;
        if (string.Equals(mode, "Twitch URL", StringComparison.OrdinalIgnoreCase)) return GetTwitchMediaUrl(clipId);
        var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true); var localPath = (string)replay["filePath"]; if (string.IsNullOrWhiteSpace(localPath)) localPath = (string)replay["file"];
        if (!string.IsNullOrWhiteSpace(localPath)) { var fullPath = Path.IsPathRooted(localPath) ? localPath : Path.Combine(folder ?? "", localPath); if (File.Exists(fullPath)) return BuildTwitchHttpUrl(Path.GetFileName(fullPath)); }
        var downloaded = DownloadTwitchClip(clipId); if (!string.IsNullOrWhiteSpace(downloaded)) { replay["file"] = Path.GetFileName(downloaded); replay["filePath"] = downloaded; return BuildTwitchHttpUrl(Path.GetFileName(downloaded)); }
        return GetTwitchMediaUrl(clipId);
    }

    private string BuildTwitchHttpUrl(string fileName) { var mapping = CPH.GetGlobalVar<string>(TwitchMappingKey, true) ?? "twitch"; var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474; return "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode(fileName ?? ""); }
    private string GetTwitchMediaUrl(string clipId) { for (var attempt = 1; attempt <= 10; attempt++) { try { var urls = CPH.TwitchGetClipDownloadUrls(clipId); var url = urls == null ? null : urls.LandscapeDownloadUrl; if (string.IsNullOrWhiteSpace(url)) url = urls == null ? null : urls.PortraitDownloadUrl; if (!string.IsNullOrWhiteSpace(url)) return url; } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Twitch media URL attempt " + attempt + " failed for " + clipId + ": " + ex.Message); } if (attempt < 10) CPH.Wait(2000); } return null; }
    private string DownloadTwitchClip(string clipId) { var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true); var replayFolder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true); if (string.IsNullOrWhiteSpace(folder)) return null; if (!string.IsNullOrWhiteSpace(replayFolder) && PathsEqual(folder, replayFolder)) { CPH.LogError("RTS Action Replay: Twitch Clip Folder must be different from the OBS Replay Folder."); return null; } Directory.CreateDirectory(folder); var destination = Path.Combine(folder, "twitch-" + Sanitize(clipId) + ".mp4"); if (File.Exists(destination) && new FileInfo(destination).Length > 0) return destination; var url = GetTwitchMediaUrl(clipId); if (string.IsNullOrWhiteSpace(url)) return null; try { using (var client = new WebClient()) client.DownloadFile(url, destination + ".tmp"); if (File.Exists(destination + ".tmp") && new FileInfo(destination + ".tmp").Length > 0) { if (File.Exists(destination)) File.Delete(destination); File.Move(destination + ".tmp", destination); return destination; } } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Twitch clip download failed for " + clipId + ": " + ex.Message); } try { if (File.Exists(destination + ".tmp")) File.Delete(destination + ".tmp"); } catch { } return null; }

    public bool SetPlayerPosition() { if (!CPH.TryGetArg("rawInput", out string input) || string.IsNullOrWhiteSpace(input)) return false; var parts = input.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); if (parts.Length == 0) return false; var duration = 1000; if (parts.Length > 1 && int.TryParse(parts[1], out var requested) && requested >= 0) duration = requested; CPH.SetArgument("replayCommand", "move"); CPH.SetArgument("replayPosition", parts[0]); CPH.SetArgument("replayAnimationDuration", duration); CPH.SetArgument("replayAnimationEasing", CPH.GetGlobalVar<string>("rts.actionreplay.animation.default.easing", true) ?? "ease-in-out"); CPH.SetArgument("replayPositions", CPH.GetGlobalVar<string>(PlayerPositionsHandoffKey, true) ?? "{\"Full Screen\":{\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}"); CPH.TriggerEvent(EventName, true); return true; }
    public bool HidePlayer() { CPH.SetArgument("replayCommand", "hide"); CPH.TriggerEvent(EventName, true); return true; }
    public bool ConfirmPlayback() { var replayId = Arg("replayId"); if (string.IsNullOrWhiteSpace(replayId)) return false; var data = Load(); var list = GetCatalog(data); var replay = list.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], replayId, StringComparison.OrdinalIgnoreCase)); if (replay == null) return false; replay["plays"] = ((int?)replay["plays"] ?? 0) + 1; SaveData(data); return true; }
    private JObject Load() { var raw = CPH.GetGlobalVar<string>(DataKey, true); if (string.IsNullOrWhiteSpace(raw)) raw = CPH.GetGlobalVar<string>(LegacyCatalogKey, true); if (string.IsNullOrWhiteSpace(raw)) return new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() }; try { return JObject.Parse(raw); } catch { return new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() }; } }
    private JArray GetCatalog(JObject data) => data["catalog"] as JArray ?? new JArray();
    private string Arg(string name) { try { CPH.TryGetArg(name, out string value); return value ?? ""; } catch { return ""; } }
    private void SaveData(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
    private void SendMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var platform = Arg("requesterPlatform");
        if (string.IsNullOrWhiteSpace(platform)) platform = Arg("userType");
        if (string.Equals(platform, "Kick", StringComparison.OrdinalIgnoreCase)) { CPH.SendKickMessage(text); return; }
        if (string.Equals(platform, "YouTube", StringComparison.OrdinalIgnoreCase))
        {
            var broadcastId = Arg("requesterBroadcastId");
            if (!string.IsNullOrWhiteSpace(broadcastId)) { CPH.SendYouTubeMessage(text, true, true, broadcastId); return; }
            CPH.SendYouTubeMessageToLatestMonitored(text); return;
        }
        if (string.Equals(platform, "Twitch", StringComparison.OrdinalIgnoreCase)) { CPH.SendMessage(text); return; }
        CPH.LogWarn("RTS Action Replay: unable to route playback chat response because the originating platform is unknown.");
    }
    private bool PathsEqual(string a, string b) => string.Equals(Path.GetFullPath(a ?? "").TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(b ?? "").TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    private string Sanitize(string value) { foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_'); return value; }
    private string GetTwitchPlaybackMode() => CPH.GetGlobalVar<string>(TwitchModeKey, true) ?? "Twitch URL";
}
