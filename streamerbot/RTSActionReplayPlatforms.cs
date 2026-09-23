using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Twitch.Common.Models.Api;

public class CPHInline
{
private const string DataKey = "rts.actionreplay.data";

private const string PlaybackModeKey = "rts.actionreplay.twitch.playbackMode";

private const string TwitchFolderKey = "rts.actionreplay.twitch.folder";

private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";

private const string ResolverAction = "RTS - Action Replay - Core - Resolver";

private const string ReplayIdHandoffKey = "rts.actionreplay.handoff.replayId";

private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";

private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";

public bool Execute() => CreateTwitchClip();

public bool ResolveIdentity()
    {
        var platform = NormalizePlatform(Arg("userType"));
        var id = Arg("userId");
        var name = Arg("userName");
        if (string.IsNullOrWhiteSpace(id)) return false;

        CPH.SetArgument("replayPlatform", platform);
        CPH.SetArgument("replayUserId", id);
        CPH.SetArgument("replayUserName", name);
        CPH.SetArgument("replayIdentityKey", IdentityKey(platform, id));
        CPH.SetArgument("replayIdentity", new JObject
        {
            ["platform"] = platform,
            ["id"] = id,
            ["name"] = name
        }.ToString(Newtonsoft.Json.Formatting.None));
        return true;
    }

public static string NormalizePlatform(string userType) => string.Equals(userType, "YouTube", StringComparison.OrdinalIgnoreCase)
    ? "YouTube"
    : string.Equals(userType, "Kick", StringComparison.OrdinalIgnoreCase)
        ? "Kick"
        : "Twitch";

public static string IdentityKey(string platform, string id) =>
    NormalizePlatform(platform).ToLowerInvariant() + ":" + (id ?? "").Trim();

public bool CreateTwitchClip()
    {
        CPH.TryGetArg("rawInput", out string rawInput); rawInput = rawInput == null ? "" : rawInput.Trim();
        var duration = Math.Max(5, Math.Min(60, GetSettingInt("rts.actionreplay.twitch.clipDuration", 30))); string title = null;
        if (!string.IsNullOrWhiteSpace(rawInput))
        {
            var remaining = rawInput;
            if (remaining[0] != '"') { var firstSpace = remaining.IndexOf(' '); var durationText = firstSpace < 0 ? remaining : remaining.Substring(0, firstSpace).Trim(); if (!int.TryParse(durationText, out var requestedDuration)) { CPH.SendMessage("Usage: !clip [5-60] [\"title\"]"); return false; } duration = Math.Max(5, Math.Min(60, requestedDuration)); remaining = firstSpace < 0 ? "" : remaining.Substring(firstSpace).Trim(); }
            if (!string.IsNullOrWhiteSpace(remaining)) { if (remaining[0] != '"') { CPH.SendMessage("Usage: !clip [5-60] [\"title\"]"); return false; } var closingQuote = remaining.IndexOf('"', 1); if (closingQuote < 0) { CPH.SendMessage("Usage: !clip [5-60] [\"title\"]"); return false; } title = remaining.Substring(1, closingQuote - 1); if (!string.IsNullOrWhiteSpace(remaining.Substring(closingQuote + 1).Trim())) { CPH.SendMessage("Usage: !clip [5-60] [\"title\"]"); return false; } if (string.IsNullOrWhiteSpace(title)) title = null; }
        }

        CPH.LogInfo("RTS Action Replay: creating Twitch Clip (" + duration + "s, title: " + (title ?? "<stream title>") + ").");
        ClipData clip;
        try { clip = CPH.CreateClip(title, duration); } catch (Exception ex) { CPH.LogError("RTS Action Replay: CreateClip failed: " + ex.Message); CPH.SendMessage("I couldn't create a Twitch clip."); return false; }
        if (clip == null || string.IsNullOrWhiteSpace(clip.Id)) { CPH.SendMessage("I couldn't create a Twitch clip."); return false; }
        ClipData published = null;
        for (var attempt = 1; attempt <= 4; attempt++) { CPH.Wait(5000); try { var clips = CPH.GetClips(1000, null); published = clips == null ? null : clips.FirstOrDefault(x => x != null && string.Equals(x.Id, clip.Id, StringComparison.OrdinalIgnoreCase)); if (published != null) break; } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Twitch publication check " + attempt + " failed: " + ex.Message); } }
        if (published == null) { CPH.SendMessage("I couldn't confirm the Twitch clip was created."); return false; }

        var item = AddTwitchClip(published, true); if (item == null) return false;
        CPH.SetArgument("twitchClipId", published.Id); CPH.SetArgument("twitchClipUrl", published.Url ?? ""); CPH.SetArgument("twitchClipTitle", (string)item["title"] ?? ""); CPH.SetArgument("replayId", (string)item["id"] ?? ""); CPH.SetArgument("replayTitle", (string)item["title"] ?? ""); CPH.SetArgument("replaySource", "Twitch");
        return BroadcastReplay(item, true);
    }

public bool SyncTwitchClips()
    {
        var data = Load(); List<ClipData> clips;
        try { clips = CPH.GetClips(1000, null); } catch (Exception ex) { CPH.LogError("RTS Action Replay: Twitch reconciliation failed: " + ex.Message); return false; }
        var catalog = (JArray)data["catalog"]; var added = 0;
        foreach (var clip in clips ?? new List<ClipData>())
        {
            if (clip == null || string.IsNullOrWhiteSpace(clip.Id)) continue;
            var existing = FindTwitchClip(catalog, clip.Id);
            if (existing != null) { EnsureLocalCopyIfConfigured(data, existing, clip.Id); continue; }
            if (AddTwitchClip(clip, false) != null) added++;
            data = Load(); catalog = (JArray)data["catalog"];
        }
        Save(data); CPH.LogInfo("RTS Action Replay: Twitch reconciliation added " + added + " new clip(s); discovered clips were not played."); return true;
    }

private JObject AddTwitchClip(ClipData clip, bool playAfterAdd)
    {
        var data = Load(); var catalog = (JArray)data["catalog"]; var existing = FindTwitchClip(catalog, clip.Id);
        if (existing != null) { EnsureLocalCopyIfConfigured(data, existing, clip.Id); return existing; }
        var mode = GetPlaybackMode(); var localPath = ModeNeedsLocalCopy(mode) ? DownloadClip(clip.Id) : null; var now = DateTime.Now;
        var item = new JObject
        {
            ["id"] = "twitch-" + clip.Id, ["sourceType"] = "Twitch", ["sourceId"] = clip.Id,
            ["title"] = string.IsNullOrWhiteSpace(clip.Title) ? "Twitch Clip" : clip.Title, ["customTitle"] = false,
            ["added"] = now.ToString("o"), ["captured"] = clip.CreatedAt.ToString("o"),
            ["creator"] = new JObject { ["platform"] = "Twitch", ["id"] = clip.CreatorId.ToString(), ["name"] = clip.CreatorName ?? "" },
            ["broadcaster"] = new JObject { ["platform"] = "Twitch", ["id"] = clip.BroadcasterId ?? "", ["name"] = clip.BroadcasterName ?? "" },
            ["gameId"] = clip.GameId ?? "", ["language"] = clip.Language ?? "", ["duration"] = clip.Duration,
            ["viewCount"] = clip.ViewCount, ["featured"] = clip.IsFeatured, ["externalUrl"] = clip.Url ?? "", ["embedUrl"] = clip.EmbedUrl ?? "", ["thumbnailUrl"] = clip.ThumbnailUrl ?? "",
            ["file"] = string.IsNullOrWhiteSpace(localPath) ? "" : Path.GetFileName(localPath), ["filePath"] = localPath ?? "", ["acquisitionMethod"] = playAfterAdd ? "TwitchCommand" : "TwitchDiscovery", ["plays"] = 0, ["users"] = new JObject()
        };
        catalog.Insert(0, item); data["catalog"] = catalog; Save(data);
        if (ModeNeedsLocalCopy(mode) && string.IsNullOrWhiteSpace(localPath)) CPH.LogWarn("RTS Action Replay: local Twitch copy could not be created for " + clip.Id + "; retaining Twitch playback as fallback.");
        if (playAfterAdd) EnqueueReplayCreated(item);
        return item;
    }

private void EnsureLocalCopyIfConfigured(JObject data, JObject item, string clipId)
    {
        if (!ModeNeedsLocalCopy(GetPlaybackMode())) return;
        var current = (string)item["filePath"]; if (!string.IsNullOrWhiteSpace(current) && File.Exists(current)) return;
        var path = DownloadClip(clipId); if (string.IsNullOrWhiteSpace(path)) return;
        item["file"] = Path.GetFileName(path); item["filePath"] = path; Save(data);
    }

private void EnqueueReplayCreated(JObject item)
    {
        var creator = item["creator"] as JObject;
        CPH.SetArgument("messageEvent", "Replay Created");
        CPH.SetArgument("replayId", (string)item["id"] ?? "");
        CPH.SetArgument("replayNumber", 1);
        CPH.SetArgument("replayTitle", (string)item["title"] ?? "Replay");
        CPH.SetArgument("replayUserId", (string)creator?["id"] ?? "");
        CPH.SetArgument("replayUser", (string)creator?["name"] ?? "");
        CPH.SetArgument("replayPlatform", (string)creator?["platform"] ?? (string)item["sourceType"] ?? "");
        CPH.SetArgument("replaySourcePlatform", (string)item["sourceType"] ?? "");
        CPH.SetArgument("requesterId", Arg("userId"));
        CPH.SetArgument("requesterName", Arg("userName"));
        CPH.SetArgument("requesterPlatform", Arg("userType"));
        CPH.SetArgument("requesterBroadcastId", Arg("broadcast.id"));
        CPH.ExecuteMethod("RTS - Action Replay - Core - Messaging", "Enqueue");
    }

private bool BroadcastReplay(JObject item, bool showClapperboard)
    {
        CPH.SetGlobalVar(ReplayIdHandoffKey, (string)item["id"] ?? "", false);
        var sourceType = ((string)item["sourceType"] ?? "").ToLowerInvariant();
        var entryPoint = sourceType == "youtube" ? "youtube" : sourceType == "kick" ? "kick" : "twitch";
        CPH.SetGlobalVar(EntryPointHandoffKey, entryPoint, false);
        CPH.UnsetGlobalVar(ResolvedProfileHandoffKey, false);
        CPH.SetGlobalVar("rts.actionreplay.handoff.showClapperboard", showClapperboard && (CPH.GetGlobalVar<bool?>("rts.actionreplay.clapper.showOnNewClip", true) ?? true), false);
        CPH.SetArgument("replaySource", (string)item["sourceType"] ?? "");
        if (!CPH.ExecuteMethod(ResolverAction, "ResolveEntryPointProfile")) return false;
        return CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay");
    }

private string ResolvePlaybackUrl(JObject item)
    {
        var mode = GetPlaybackMode(); var clipId = (string)item["sourceId"];
        if (string.Equals(mode, "Twitch URL", StringComparison.OrdinalIgnoreCase)) return GetTwitchMediaUrl(clipId);
        var localPath = (string)item["filePath"]; if (string.IsNullOrWhiteSpace(localPath)) localPath = (string)item["file"];
        var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true);
        if (!string.IsNullOrWhiteSpace(localPath)) { var fullPath = Path.IsPathRooted(localPath) ? localPath : Path.Combine(folder ?? "", localPath); if (File.Exists(fullPath)) return BuildTwitchHttpUrl(Path.GetFileName(fullPath)); }
        var downloaded = DownloadClip(clipId);
        if (!string.IsNullOrWhiteSpace(downloaded)) { item["file"] = Path.GetFileName(downloaded); item["filePath"] = downloaded; var data = Load(); var target = FindTwitchClip((JArray)data["catalog"], clipId); if (target != null) { target["file"] = item["file"]; target["filePath"] = item["filePath"]; Save(data); } return BuildTwitchHttpUrl(Path.GetFileName(downloaded)); }
        return GetTwitchMediaUrl(clipId);
    }

private string GetTwitchMediaUrl(string clipId)
    {
        if (string.IsNullOrWhiteSpace(clipId)) return null;
        for (var attempt = 1; attempt <= 10; attempt++) { try { var urls = CPH.TwitchGetClipDownloadUrls(clipId); var url = urls?.LandscapeDownloadUrl ?? urls?.PortraitDownloadUrl; if (!string.IsNullOrWhiteSpace(url)) return url; } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Twitch media URL attempt " + attempt + " failed: " + ex.Message); } if (attempt < 10) CPH.Wait(2000); }
        return null;
    }

private string DownloadClip(string clipId)
    {
        var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true); var replayFolder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        if (string.IsNullOrWhiteSpace(folder) || (!string.IsNullOrWhiteSpace(replayFolder) && PathsEqual(folder, replayFolder))) return null;
        Directory.CreateDirectory(folder); var destination = Path.Combine(folder, "twitch-" + Sanitize(clipId) + ".mp4"); if (File.Exists(destination) && new FileInfo(destination).Length > 0) return destination;
        var url = GetTwitchMediaUrl(clipId); if (string.IsNullOrWhiteSpace(url)) return null;
        try { var temp = destination + ".tmp"; using (var client = new WebClient()) client.DownloadFile(url, temp); if (File.Exists(temp) && new FileInfo(temp).Length > 0) { if (File.Exists(destination)) File.Delete(destination); File.Move(temp, destination); return destination; } } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Twitch clip download failed: " + ex.Message); }
        try { if (File.Exists(destination + ".tmp")) File.Delete(destination + ".tmp"); } catch { }
        return null;
    }

private JObject Load()
{
    var raw = CPH.GetGlobalVar<string>(DataKey, true);
    try { return string.IsNullOrWhiteSpace(raw) ? CreateDataDefaults() : JObject.Parse(raw); }
    catch { return CreateDataDefaults(); }
}

private JObject CreateDataDefaults() => new JObject { ["version"] = "1.0", ["catalog"] = new JArray(), ["playHistory"] = new JArray() };

private void Save(JObject data)
{
    data["version"] = "1.0";
    data["catalog"] = data["catalog"] as JArray ?? new JArray();
    data["playHistory"] = data["playHistory"] as JArray ?? new JArray();
    CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
}

private JObject FindTwitchClip(JArray catalog, string clipId) { if (catalog == null) return null; return catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["sourceType"], "Twitch", StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceId"], clipId, StringComparison.OrdinalIgnoreCase)); }



private bool ModeNeedsLocalCopy(string mode) => string.Equals(mode, "Download Locally", StringComparison.OrdinalIgnoreCase) || string.Equals(mode, "Both", StringComparison.OrdinalIgnoreCase);

private string GetPlaybackMode() { var mode = CPH.GetGlobalVar<string>(PlaybackModeKey, true); if (string.Equals(mode, "Twitch URL", StringComparison.OrdinalIgnoreCase)) return "Twitch URL"; if (string.Equals(mode, "Both", StringComparison.OrdinalIgnoreCase)) return "Both"; return "Download Locally"; }

private string BuildTwitchHttpUrl(string fileName) { var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.twitch.httpMapping", true) ?? "twitch"; var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474; return "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode(fileName ?? ""); }

private bool PathsEqual(string a, string b) { try { return string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase); } catch { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); } }

private string Sanitize(string value) { var invalid = Path.GetInvalidFileNameChars(); var chars = value.ToCharArray(); for (var i = 0; i < chars.Length; i++) for (var j = 0; j < invalid.Length; j++) if (chars[i] == invalid[j]) chars[i] = '_'; return new string(chars); }

private int GetSettingInt(string key, int fallback) { try { object value = CPH.GetGlobalVar<object>(key, true); return value == null ? fallback : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture); } catch { return fallback; } }

private const string YouTubeBroadcastIdKey = "rts.actionreplay.youtube.broadcastId";

private const string YouTubeStartTimeKey = "rts.actionreplay.youtube.actualStartTime";

public bool BroadcastStarted()
    {
        var broadcastId = Arg("broadcast.id").Trim();
        if (string.IsNullOrWhiteSpace(broadcastId))
        {
            CPH.LogWarn("RTS Action Replay: YouTube Broadcast Started event did not provide broadcast.id.");
            return false;
        }

        var startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CPH.SetGlobalVar(YouTubeBroadcastIdKey, broadcastId, true);
        CPH.SetGlobalVar(YouTubeStartTimeKey, startTime, true);
        CPH.LogInfo($"RTS Action Replay: YouTube broadcast {broadcastId} started at Unix timestamp {startTime}.");
        return true;
    }

public bool CreateYouTubeClip()
    {
        var duration = Math.Max(5, Math.Min(60, GetSettingInt("rts.actionreplay.youtube.clipDuration", 30)));
        var rawInput = Arg("rawInput").Trim();
        var title = "YouTube Clip";
        if (!string.IsNullOrWhiteSpace(rawInput))
        {
            var parts = rawInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1 || !int.TryParse(parts[0], out var requested))
            {
                SendOriginMessage("Usage: !create-clip [5-60] [title]");
                return false;
            }
            duration = Math.Max(5, Math.Min(60, requested));
            if (parts.Length > 1) title = string.Join(" ", parts.Skip(1));
        }

        var videoId = Arg("broadcast.id").Trim();
        if (string.IsNullOrWhiteSpace(videoId)) videoId = GetGlobalString("broadcast.id");
        if (string.IsNullOrWhiteSpace(videoId)) { SendOriginMessage("I couldn't determine the current YouTube stream."); return false; }
        if (!TryGetStartTime(videoId, out var startTime))
        {
            SendOriginMessage("I couldn't determine when the current YouTube stream started.");
            return false;
        }

        var data = Load();
        var catalog = (JArray)(data["catalog"] ?? new JArray());
        var id = "youtube-" + videoId + "-" + startTime + "-" + duration;
        var existing = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase));
        if (existing != null) return BroadcastReplay(existing, false);
        var creatorPlatform = NormalizePlatform(Arg("userType"));
        var now = DateTime.Now;
        var item = new JObject
        {
            ["id"] = id, ["sourceType"] = "YouTube", ["sourceId"] = videoId,
            ["sourceUrl"] = "https://youtu.be/" + videoId,
            ["startTime"] = startTime, ["duration"] = duration,
            ["title"] = title, ["customTitle"] = title != "YouTube Clip",
            ["added"] = now.ToString("o"), ["captured"] = now.ToString("o"), ["acquisitionMethod"] = "YouTubeCommand",
            ["creator"] = new JObject { ["platform"] = creatorPlatform, ["id"] = Arg("userId"), ["name"] = Arg("userName") },
            ["plays"] = 0, ["users"] = new JObject()
        };
        catalog.Insert(0, item); data["catalog"] = catalog;  Save(data);
        CPH.LogInfo($"RTS Action Replay: added YouTube timestamp replay {id} ({startTime}s + {duration}s) title='{title}'.");
        EnqueueReplayCreated(item);
        return BroadcastReplay(item, false);
    }

private bool TryGetStartTime(string videoId, out long startTime)
    {
        startTime = 0;
        var storedId = GetGlobalString(YouTubeBroadcastIdKey);
        var actualStart = GetGlobalLong(YouTubeStartTimeKey);
        if (!string.Equals(storedId, videoId, StringComparison.OrdinalIgnoreCase) || actualStart <= 0) return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        startTime = now - actualStart;
        return startTime >= 0;
    }

private string GetGlobalString(string key) { try { return CPH.GetGlobalVar<string>(key, true) ?? ""; } catch { return ""; } }

private long GetGlobalLong(string key) { try { return CPH.GetGlobalVar<long?>(key, true) ?? 0L; } catch { return 0L; } }

private string Arg(string name) { CPH.TryGetArg(name, out string value); return value ?? ""; }

private void SendOriginMessage(string message)
    {
        var platform = Arg("userType");
        if (string.Equals(platform, "Kick", StringComparison.OrdinalIgnoreCase))
        {
            CPH.SendKickMessage(message);
            return;
        }
        if (string.Equals(platform, "YouTube", StringComparison.OrdinalIgnoreCase))
        {
            CPH.SendYouTubeMessageToLatestMonitored(message);
            return;
        }
        CPH.SendMessage(message);
    }

private const string PendingKey = "rts.actionreplay.kick.pending";

public bool RequestKickBotClip()
    {
        var message = Arg("text");
        if (string.IsNullOrWhiteSpace(message)) message = Arg("message");
        if (string.IsNullOrWhiteSpace(message)) message = Arg("rawInput");
        message = NormalizeCreateClipMessage(message);
        if (!Regex.IsMatch(message ?? "", @"^!create-clip(?:\s|$)", RegexOptions.IgnoreCase)) return false;
        return RequestKickBotClipInternal(message);
    }

public bool CaptureKickBotClip()
    {
        var message = Arg("text");
        if (string.IsNullOrWhiteSpace(message)) message = Arg("message");
        if (string.IsNullOrWhiteSpace(message)) message = Arg("rawInput");
        var kickBotUrl = ExtractKickBotUrl(message);
        if (string.IsNullOrWhiteSpace(kickBotUrl)) return false;
        var pending = LoadPending();
        if (pending == null) return false;

        var data = Load(); var catalog = (JArray)data["catalog"] ?? new JArray();
        var kickBotId = ExtractKickBotId(kickBotUrl); var title = (string)pending["title"] ?? "Kick Clip";
        var duration = (int?)pending["duration"] ?? 30; var creatorId = (string)pending["creatorId"] ?? ""; var creatorName = (string)pending["creatorName"] ?? "";
        var existing = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["sourceType"], "Kick", StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceUrl"], kickBotUrl, StringComparison.OrdinalIgnoreCase));
        if (existing != null) { existing["title"] = title; existing["customTitle"] = title != "Kick Clip"; existing["duration"] = duration; ClearPending(); Save(data); return BroadcastReplay(existing, false); }
        if (string.IsNullOrWhiteSpace(creatorId)) CPH.TryGetArg("userId", out creatorId);
        if (string.IsNullOrWhiteSpace(creatorName)) CPH.TryGetArg("userName", out creatorName);
        var creator = new JObject { ["platform"] = "Kick", ["id"] = creatorId ?? "", ["name"] = creatorName ?? "" };
        var item = new JObject
        {
            ["id"] = "kick-" + (kickBotId ?? Guid.NewGuid().ToString("N")), ["sourceType"] = "Kick", ["sourceId"] = kickBotId ?? "", ["sourceUrl"] = kickBotUrl,
            ["title"] = title, ["customTitle"] = title != "Kick Clip", ["duration"] = duration, ["added"] = DateTime.Now.ToString("o"), ["captured"] = DateTime.Now.ToString("o"),
            ["acquisitionMethod"] = "KickBot", ["creator"] = creator, ["plays"] = 0, ["users"] = new JObject()
        };
        catalog.Insert(0, item); data["catalog"] = catalog;  ClearPending(); Save(data); EnqueueReplayCreated(item); return BroadcastReplay(item, false);
    }

public bool CaptureKickClip()
    {
        var message = Arg("text");
        if (string.IsNullOrWhiteSpace(message)) message = Arg("message");
        if (string.IsNullOrWhiteSpace(message)) message = Arg("rawInput");
        var url = ExtractKickClipUrl(message);
        if (string.IsNullOrWhiteSpace(url)) return false;
        var clipId = ExtractKickClipId(url);
        if (string.IsNullOrWhiteSpace(clipId)) return false;

        var data = Load(); var catalog = (JArray)data["catalog"] ?? new JArray();
        var existing = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["sourceType"], "Kick", StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceId"], clipId, StringComparison.OrdinalIgnoreCase));
        if (existing != null) return BroadcastReplay(existing, false);

        var title = "Kick Clip"; var duration = 0; var creatorId = Arg("userId"); var creatorName = Arg("userName");
        var metadata = GetNativeKickClipMetadata(clipId);
        var clip = metadata?["clip"] as JObject;
        if (!string.IsNullOrWhiteSpace((string)clip?["title"])) title = (string)clip["title"];
        duration = (int)Math.Round((double?)clip?["duration"] ?? 0);
        var creator = clip?["creator"] as JObject;
        if (string.IsNullOrWhiteSpace(creatorId)) creatorId = creator?["id"]?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(creatorName)) creatorName = creator?["username"]?.ToString() ?? "";
        var creatorInfo = new JObject { ["platform"] = "Kick", ["id"] = creatorId ?? "", ["name"] = creatorName ?? "" };
        var item = new JObject
        {
            ["id"] = "kick-" + clipId, ["sourceType"] = "Kick", ["sourceId"] = clipId, ["sourceUrl"] = url,
            ["title"] = title, ["customTitle"] = false, ["duration"] = duration, ["added"] = DateTime.Now.ToString("o"), ["captured"] = DateTime.Now.ToString("o"),
            ["acquisitionMethod"] = "Kick", ["creator"] = creatorInfo, ["plays"] = 0, ["users"] = new JObject()
        };
        catalog.Insert(0, item); data["catalog"] = catalog;  Save(data);
        CPH.LogInfo($"RTS Action Replay: native Kick clip captured; clipId={clipId}; title={title}; duration={duration}.");
        EnqueueReplayCreated(item);
        return BroadcastReplay(item, false);
    }

private JObject GetNativeKickClipMetadata(string clipId)
    {
        var json = DownloadString("https://kick.com/api/v2/clips/" + CPH.UrlEncode(clipId) + "/play");
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JObject.Parse(json); }
        catch (Exception ex) { CPH.LogWarn("RTS Action Replay: native Kick clip metadata could not be parsed: " + ex.Message); return null; }
    }

private string NormalizeCreateClipMessage(string message)
    {
        if (Regex.IsMatch(message ?? "", @"^!create-clip(?:\s|$)", RegexOptions.IgnoreCase)) return message;
        var command = Arg("command"); if (!string.Equals(command, "!create-clip", StringComparison.OrdinalIgnoreCase)) return message;
        var rawInput = Arg("rawInput"); return string.IsNullOrWhiteSpace(rawInput) ? command : command + " " + rawInput;
    }

private bool RequestKickBotClipInternal(string message)
    {
        var duration = ParseDuration(message); var title = ParseTitle(message); CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName);
        var pending = new JObject { ["duration"] = duration, ["title"] = title, ["creatorId"] = userId ?? "", ["creatorName"] = userName ?? "", ["requestedAt"] = DateTime.Now.ToString("o") };
        CPH.SetGlobalVar(PendingKey, pending.ToString(Newtonsoft.Json.Formatting.None), false); CPH.SendKickMessage("!clip " + duration, true, true);
        CPH.LogInfo($"RTS Action Replay: KickBot clip requested; duration={duration}; title={title}; creator={userName}."); return true;
    }

private int ParseDuration(string message)
    {
        var match = Regex.Match(message ?? "", @"^!create-clip(?:\s+(\d+))?", RegexOptions.IgnoreCase);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var duration)) return 30; return Math.Max(5, Math.Min(240, duration));
    }

private string ParseTitle(string message)
    {
        var match = Regex.Match(message ?? "", @"^!create-clip(?:\s+\d+)?(?:\s+(.*))?$", RegexOptions.IgnoreCase);
        var title = match.Success ? match.Groups[1].Value.Trim() : ""; return string.IsNullOrWhiteSpace(title) ? "Kick Clip" : title;
    }

private JObject LoadPending()
    {
        var raw = CPH.GetGlobalVar<string>(PendingKey, false); if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            var pending = JObject.Parse(raw);
            if (DateTime.TryParse((string)pending["requestedAt"], out var requestedAt) && DateTime.Now - requestedAt > TimeSpan.FromMinutes(5)) { ClearPending(); return null; }
            return pending;
        }
        catch { return null; }
    }

private void ClearPending() => CPH.UnsetGlobalVar(PendingKey, false);

private string ExtractKickBotUrl(string text)
    {
        var match = Regex.Match(text ?? "", @"https?://(?:www\.)?kickbot\.com/clip/[A-Za-z0-9]+", RegexOptions.IgnoreCase); return match.Success ? match.Value : null;
    }

private string ExtractKickBotId(string url)
    {
        var match = Regex.Match(url ?? "", @"kickbot\.com/clip/([A-Za-z0-9]+)", RegexOptions.IgnoreCase); return match.Success ? match.Groups[1].Value : null;
    }

private string ExtractKickClipUrl(string text)
    {
        var match = Regex.Match(text ?? "", @"https?://(?:www\.)?kick\.com/[A-Za-z0-9_-]+/clips/clip_[A-Za-z0-9_-]+", RegexOptions.IgnoreCase); return match.Success ? match.Value : null;
    }

private string ExtractKickClipId(string value)
    {
        var match = Regex.Match(value ?? "", @"/clips/(clip_[A-Za-z0-9_-]+)", RegexOptions.IgnoreCase); return match.Success ? match.Groups[1].Value : null;
    }

private string DownloadString(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        try { using (var client = new WebClient()) { client.Headers[HttpRequestHeader.Accept] = "application/json"; client.Headers[HttpRequestHeader.UserAgent] = "RTS-Action-Replay"; return client.DownloadString(url); } }
        catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Kick request failed: " + ex.Message); return null; }
    }

private JObject Find(JArray catalog, string clipId)
    {
        if (catalog == null) return null;
        return catalog.OfType<JObject>().FirstOrDefault(item =>
            string.Equals((string)item["sourceType"], "Twitch", StringComparison.OrdinalIgnoreCase) &&
            string.Equals((string)item["sourceId"], clipId, StringComparison.OrdinalIgnoreCase));
    }

private bool EnsureLocalCopy(JObject item, string clipId)
    {
        var current = (string)item["filePath"];
        if (!string.IsNullOrWhiteSpace(current) && File.Exists(current)) return false;
        var path = Download(clipId);
        if (string.IsNullOrWhiteSpace(path)) return false;
        item["file"] = Path.GetFileName(path);
        item["filePath"] = path;
        return true;
    }

private string Download(string clipId)
    {
        var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true);
        var replayFolder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        if (string.IsNullOrWhiteSpace(folder)) return null;
        if (!string.IsNullOrWhiteSpace(replayFolder) && PathsEqual(folder, replayFolder))
        {
            CPH.LogError("RTS Action Replay: Twitch Clip Folder must be different from the OBS Replay Folder.");
            return null;
        }

        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, "twitch-" + Sanitize(clipId) + ".mp4");
        if (File.Exists(destination) && new FileInfo(destination).Length > 0) return destination;

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                var urls = CPH.TwitchGetClipDownloadUrls(clipId);
                var url = urls == null ? null : urls.LandscapeDownloadUrl;
                if (string.IsNullOrWhiteSpace(url)) url = urls == null ? null : urls.PortraitDownloadUrl;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    var temp = destination + ".tmp";
                    using (var client = new WebClient()) client.DownloadFile(url, temp);
                    if (File.Exists(temp) && new FileInfo(temp).Length > 0)
                    {
                        if (File.Exists(destination)) File.Delete(destination);
                        File.Move(temp, destination);
                        return destination;
                    }
                }
            }
            catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Twitch download attempt " + attempt + " failed for " + clipId + ": " + ex.Message); }
            if (attempt < 10) CPH.Wait(2000);
        }
        return null;
    }
}
