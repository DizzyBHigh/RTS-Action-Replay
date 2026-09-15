using System;
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
    private const string AnimationAction = "RTS - Action Replay - Core - Animation";
    private const string TitleAction = "RTS - Action Replay - Core - Title";
    private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";
    private const string ReplayIdHandoffKey = "rts.actionreplay.handoff.replayId";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";
    private const string PlaybackProfileHandoffKey = "rts.actionreplay.handoff.playbackProfile";
    private const string PlaybackTitleProfileHandoffKey = "rts.actionreplay.handoff.playbackTitleProfile";
    private const string PlaybackQueueEntryHandoffKey = "rts.actionreplay.handoff.playbackQueueEntryId";
    private const string AnimationProfileHandoffKey = "rts.actionreplay.handoff.animationProfile";
    private const string PlayerPositionsHandoffKey = "rts.actionreplay.handoff.playerPositions";

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
            if (replay == null) { CPH.LogWarn($"RTS Action Replay TRACE: PlayReplay failed - handoff replay {handoffReplayId} not found in catalog."); CPH.SendMessage("Replay not found."); return false; }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(selector)) { CPH.LogWarn("RTS Action Replay TRACE: PlayReplay failed - no queue handoff and rawInput is empty."); return false; }
            selector = selector.Trim();
            if (int.TryParse(selector, out var index) && index > 0 && index <= list.Count) replay = (JObject)list[index - 1];
            else replay = list.OfType<JObject>().FirstOrDefault(x => ((bool?)x["customTitle"] ?? false) && string.Equals((string)x["title"], selector, StringComparison.OrdinalIgnoreCase));
            if (replay == null) { CPH.LogWarn($"RTS Action Replay TRACE: PlayReplay failed - selector '{selector}' did not resolve to a replay."); CPH.SendMessage("Replay not found."); return false; }
        }

        var queueEntryId = handoffQueueEntryId;
        if (string.IsNullOrWhiteSpace(queueEntryId)) CPH.TryGetArg("replayQueueEntryId", out queueEntryId);
        CPH.LogInfo($"RTS Action Replay TRACE: PlayReplay resolved replayId={(string)replay["id"]}; title={(string)replay["title"]}; queueEntryId={queueEntryId ?? "<none>"}.");
        if (string.IsNullOrWhiteSpace(queueEntryId))
        {
            CPH.SetGlobalVar(ReplayIdHandoffKey, (string)replay["id"] ?? "", false);
            CPH.SetGlobalVar(EntryPointHandoffKey, "catalog", false);
            CPH.UnsetGlobalVar(ResolvedProfileHandoffKey, false);
            CPH.SetArgument("replayTitleEntryPoint", "catalog");
            CPH.LogInfo("RTS Action Replay TRACE: PlayReplay catalog path; resolving catalog animation and title profiles.");
            if (!CPH.ExecuteMethod(AnimationAction, "ResolveEntryPointProfile")) { CPH.LogWarn("RTS Action Replay TRACE: PlayReplay failed - ResolveEntryPointProfile returned false."); return false; }
            if (!CPH.ExecuteMethod(TitleAction, "ResolveEntryPointProfile")) { CPH.LogWarn("RTS Action Replay TRACE: PlayReplay failed - title ResolveEntryPointProfile returned false."); return false; }
            return CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay");
        }

        var source = (string)replay["sourceType"] ?? "OBS";
        var url = ResolveReplayUrl(replay);
        if (string.Equals(source, "Kick", StringComparison.OrdinalIgnoreCase))
        {
            url = ResolveKickUrl(replay);
            if (string.IsNullOrWhiteSpace(url))
            {
                CPH.LogWarn($"RTS Action Replay TRACE: Kick media resolution failed for replay {(string)replay["id"]}.");
                CPH.SendMessage("Unable to resolve Kick media file.");
                return false;
            }
            CPH.LogInfo($"RTS Action Replay TRACE: Kick media resolved for playback; replayId={(string)replay["id"]}; url={url}.");
        }
        CPH.LogInfo($"RTS Action Replay TRACE: PlayReplay media resolution returned {(string.IsNullOrWhiteSpace(url) ? "<null>" : url)}.");
        if (string.IsNullOrWhiteSpace(url)) { CPH.LogWarn($"RTS Action Replay TRACE: PlayReplay failed - media URL unavailable for replay {(string)replay["id"]}; file={(string)replay["file"]}; filePath={(string)replay["filePath"]}."); CPH.SendMessage($"Replay media is unavailable: {(string)replay["title"]}"); return false; }
        CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName);
        var creator = replay["creator"] as JObject; var creatorName = (string)creator?["name"] ?? "";
        CPH.SetArgument("replayCommand", "load"); CPH.SetArgument("replayId", (string)replay["id"]); CPH.SetArgument("replayUrl", url); CPH.SetArgument("replayAutoplay", true);
        CPH.SetArgument("replayQueueEntryId", queueEntryId); CPH.SetArgument("replayUserId", userId ?? ""); CPH.SetArgument("replayUserName", userName ?? ""); CPH.SetArgument("replayDirector", creatorName);
        CPH.SetArgument("replayNumber", Array.IndexOf(list.ToArray(), replay) + 1); CPH.SetArgument("replayTitle", (string)replay["title"] ?? ""); CPH.SetArgument("replayPlayedCount", ((int?)replay["plays"] ?? 0) + 1);
        CPH.SetArgument("replaySource", source); CPH.SetArgument("replaySourceId", (string)replay["sourceId"] ?? "");
        if (string.Equals(source, "YouTube", StringComparison.OrdinalIgnoreCase))
        {
            CPH.SetArgument("replayStartTime", (long?)replay["startTime"] ?? 0);
            CPH.SetArgument("replayDuration", (int?)replay["duration"] ?? 0);
        }
        var profile = CPH.TryGetArg("replayAnimationProfileId", out string requestedProfile) && !string.IsNullOrWhiteSpace(requestedProfile) ? requestedProfile.Trim() : CPH.GetGlobalVar<string>(PlaybackProfileHandoffKey, false);
        if (string.IsNullOrWhiteSpace(profile)) profile = "default";
        var titleProfile = CPH.TryGetArg("replayTitleProfileId", out string requestedTitleProfile) && !string.IsNullOrWhiteSpace(requestedTitleProfile) ? requestedTitleProfile.Trim() : CPH.GetGlobalVar<string>(PlaybackTitleProfileHandoffKey, false);
        if (string.IsNullOrWhiteSpace(titleProfile)) titleProfile = "default";
        CPH.LogInfo($"RTS Action Replay TRACE: PlayReplay dispatching load; replayId={(string)replay["id"]}; url={url}; animationProfile={profile}; titleProfile={titleProfile}; queueEntryId={queueEntryId}; source={source}.");
        if (!ApplyPlayerSettings(profile))
        {
            CPH.LogWarn($"RTS Action Replay TRACE: PlayReplay failed - animation profile '{profile}' could not be applied.");
            return false;
        }
        if (!ApplyTitleSettings(titleProfile))
        {
            CPH.LogWarn($"RTS Action Replay TRACE: PlayReplay failed - title profile '{titleProfile}' could not be applied.");
            return false;
        }
        CPH.TriggerEvent(EventName, true); SendMessage("play");
        CPH.LogInfo($"RTS Action Replay TRACE: PlayReplay completed dispatch for replay {(string)replay["id"]}.");
        return true;
    }

    private string ResolveReplayUrl(JObject replay)
    {
        var source = (string)replay["sourceType"];
        if (string.Equals(source, "Twitch", StringComparison.OrdinalIgnoreCase)) return ResolveTwitchUrl(replay);
        if (string.Equals(source, "YouTube", StringComparison.OrdinalIgnoreCase))
        {
            var id = (string)replay["sourceId"];
            return string.IsNullOrWhiteSpace(id) ? null : "https://www.youtube.com/embed/" + CPH.UrlEncode(id);
        }
        if (string.Equals(source, "Kick", StringComparison.OrdinalIgnoreCase)) return null;
        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true); var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.httpMapping", true) ?? "replays"; var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
        var file = (string)replay["file"]; var path = Path.Combine(folder ?? "", file ?? "");
        CPH.LogInfo($"RTS Action Replay TRACE: ResolveReplayUrl OBS; folder={folder ?? "<null>"}; file={file ?? "<null>"}; path={path}; exists={File.Exists(path)}; mapping={mapping}; port={port}.");
        if (!File.Exists(path)) return null;
        return "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode(file ?? "");
    }

    private string ResolveKickUrl(JObject replay)
    {
        var sourceUrl = (string)replay["sourceUrl"];
        var sourceId = (string)replay["sourceId"];
        var html = DownloadString(sourceUrl);
        var clipId = ExtractKickClipId(sourceId);
        if (string.IsNullOrWhiteSpace(clipId)) clipId = ExtractKickClipId(sourceUrl);
        if (string.IsNullOrWhiteSpace(clipId)) clipId = ExtractKickClipId(html);
        if (string.IsNullOrWhiteSpace(clipId)) return null;

        var directMp4 = "https://clips.kickbotcdn.com/kickbot-hls/" + clipId + "/" + clipId + ".mp4";
        CPH.LogInfo($"RTS Action Replay TRACE: KickBot media candidate generated; clipId={clipId}; url={directMp4}.");
        if (!WaitForKickBotMedia(directMp4, clipId)) return null;
        return directMp4;
    }

    private bool WaitForKickBotMedia(string url, string clipId)
    {
        for (var attempt = 1; attempt <= 40; attempt++)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.AddRange(0, 0);
                request.Timeout = 5000;
                request.ReadWriteTimeout = 5000;
                request.UserAgent = "RTS-Action-Replay";
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    var status = (int)response.StatusCode;
                    var contentType = response.ContentType ?? "";
                    if (status >= 200 && status < 300 && !contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
                    {
                        CPH.LogInfo($"RTS Action Replay TRACE: KickBot media ready; clipId={clipId}; attempt={attempt}; status={status}; contentType={contentType}.");
                        return true;
                    }
                    CPH.LogInfo($"RTS Action Replay TRACE: KickBot media not ready; clipId={clipId}; attempt={attempt}; status={status}; contentType={contentType}.");
                }
            }
            catch (WebException ex)
            {
                CPH.LogInfo($"RTS Action Replay TRACE: KickBot media not ready; clipId={clipId}; attempt={attempt}; error={ex.Message}.");
            }
            if (attempt < 40) CPH.Wait(5000);
        }
        CPH.LogWarn($"RTS Action Replay TRACE: KickBot media did not become ready within 200 seconds; clipId={clipId}.");
        return false;
    }

    private string ExtractKickClipId(string value)
    {
        var match = Regex.Match(value ?? "", @"[?&]clip=(clip_[A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value;
        match = Regex.Match(value ?? "", @"/clips?/(clip_[A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value;
        match = Regex.Match(value ?? "", @"(?:kickbot\.com|kickbot\.app)/clip/([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private string DownloadString(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        try { using (var client = new WebClient()) { client.Headers[HttpRequestHeader.UserAgent] = "RTS-Action-Replay"; return client.DownloadString(url); } }
        catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Kick request failed: " + ex.Message); return null; }
    }

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

    public bool SetPlayerPosition()
    {
        if (!CPH.TryGetArg("rawInput", out string input) || string.IsNullOrWhiteSpace(input)) return false;
        var parts = input.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); if (parts.Length == 0) return false;
        var duration = 1000; if (parts.Length > 1 && int.TryParse(parts[1], out var requested) && requested >= 0) duration = requested;
        CPH.SetArgument("replayCommand", "move"); CPH.SetArgument("replayPosition", parts[0]); CPH.SetArgument("replayAnimationDuration", duration); CPH.SetArgument("replayAnimationEasing", CPH.GetGlobalVar<string>("rts.actionreplay.animation.default.easing", true) ?? "ease-in-out"); CPH.SetArgument("replayPositions", CPH.GetGlobalVar<string>(PlayerPositionsHandoffKey, true) ?? "{\"Full Screen\":{\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}"); CPH.TriggerEvent(EventName, true); return true;
    }

    public bool HidePlayer() { CPH.SetArgument("replayCommand", "hide"); CPH.TriggerEvent(EventName, true); return true; }

    public bool ConfirmPlayback()
    {
        var replayId = Arg("replayId"); if (string.IsNullOrWhiteSpace(replayId)) return false;
        var data = Load(); var list = GetCatalog(data); var replay = list.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], replayId, StringComparison.OrdinalIgnoreCase)); if (replay == null) return false;
        replay["plays"] = ((int?)replay["plays"] ?? 0) + 1; SaveData(data); return true;
    }

    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true); if (string.IsNullOrWhiteSpace(raw)) raw = CPH.GetGlobalVar<string>(LegacyCatalogKey, true);
        if (string.IsNullOrWhiteSpace(raw)) return new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() };
        try { return JObject.Parse(raw); } catch { return new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() }; }
    }

    private JArray GetCatalog(JObject data) => data["catalog"] as JArray ?? new JArray();
    private string Arg(string name) { try { CPH.TryGetArg(name, out string value); return value ?? ""; } catch { return ""; } }
    private void SaveData(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
    private void SendMessage(string text) { if (!string.IsNullOrWhiteSpace(text)) CPH.SendMessage(text); }
    private bool PathsEqual(string a, string b) => string.Equals(Path.GetFullPath(a ?? "").TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(b ?? "").TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    private string Sanitize(string value) { foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_'); return value; }
    private string GetTwitchPlaybackMode() => CPH.GetGlobalVar<string>(TwitchModeKey, true) ?? "Twitch URL";
    private bool ApplyPlayerSettings(string profile)
    {
        CPH.SetArgument("replayAnimationProfileId", profile);
        var applied = CPH.ExecuteMethod(AnimationAction, "ApplyProfile");
        CPH.SetArgument("replayPositions", CPH.GetGlobalVar<string>(PlayerPositionsHandoffKey, false) ?? "");
        return applied;
    }
}
