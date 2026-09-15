using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string KickFolderKey = "rts.actionreplay.kick.folder";
    private const string KickMappingKey = "rts.actionreplay.kick.httpMapping";
    private const string HttpPortKey = "rts.actionreplay.httpPort";
    private const string MaxRecentKey = "rts.actionreplay.maxHistory";
    private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";
    private const string AnimationAction = "RTS - Action Replay - Core - Animation";
    private const string ReplayIdHandoffKey = "rts.actionreplay.handoff.replayId";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";

    public bool Execute() => CaptureKickBotClip();

    public bool CaptureKickBotClip()
    {
        var message = Arg("text");
        if (string.IsNullOrWhiteSpace(message)) message = Arg("message");
        if (string.IsNullOrWhiteSpace(message)) message = Arg("rawInput");
        var kickBotUrl = ExtractKickBotUrl(message);
        if (string.IsNullOrWhiteSpace(kickBotUrl)) return false;

        var media = ResolveMedia(kickBotUrl, out var clipId);
        if (string.IsNullOrWhiteSpace(media) || string.IsNullOrWhiteSpace(clipId))
        {
            CPH.LogWarn("RTS Action Replay: KickBot clip could not be resolved to a direct MP4: " + kickBotUrl);
            return false;
        }

        var filePath = DownloadMp4(media, clipId);
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        var data = Load(); var catalog = (JArray)data["catalog"] ?? new JArray(); var existing = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["sourceType"], "Kick", StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceId"], clipId, StringComparison.OrdinalIgnoreCase));
        if (existing != null) return BroadcastReplay(existing);

        CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName);
        var creator = new JObject { ["platform"] = "Kick", ["id"] = userId ?? "", ["name"] = userName ?? "" };
        var item = new JObject
        {
            ["id"] = "kick-" + clipId, ["sourceType"] = "Kick", ["sourceId"] = clipId, ["sourceUrl"] = kickBotUrl,
            ["title"] = "Kick Clip", ["customTitle"] = false, ["added"] = DateTime.Now.ToString("o"), ["captured"] = DateTime.Now.ToString("o"),
            ["creator"] = creator, ["file"] = Path.GetFileName(filePath), ["filePath"] = filePath,
            ["acquisitionMethod"] = "KickBot", ["plays"] = 0, ["users"] = new JObject()
        };
        catalog.Insert(0, item); data["catalog"] = catalog; AddRecent(data, (string)item["id"]); Save(data);
        return BroadcastReplay(item);
    }

    private string ResolveMedia(string url, out string clipId)
    {
        clipId = ExtractKickClipId(url);
        if (!string.IsNullOrWhiteSpace(clipId))
        {
            var apiUrl = "https://kick.com/api/v2/clips/" + clipId + "/play";
            var json = DownloadString(apiUrl);
            var media = TryReadClipUrl(json);
            if (IsMp4(media)) return media;
            if (!string.IsNullOrWhiteSpace(media)) CPH.LogWarn("RTS Action Replay: Kick returned a non-MP4 clip URL; HLS is intentionally unsupported.");
        }

        var html = DownloadString(url);
        var embeddedKick = Regex.Match(html ?? "", @"https?://(?:www\.)?kick\.com/[^\"'<>\s]+[?&]clip=(clip_[A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        if (embeddedKick.Success)
        {
            clipId = embeddedKick.Groups[1].Value;
            var media = TryReadClipUrl(DownloadString("https://kick.com/api/v2/clips/" + clipId + "/play"));
            if (IsMp4(media)) return media;
        }

        var mp4 = Regex.Match(html ?? "", @"https?://[^\"'<>\s]+\.mp4(?:\?[^\"'<>\s]*)?", RegexOptions.IgnoreCase);
        if (mp4.Success)
        {
            clipId = clipId ?? ExtractKickBotId(url);
            if (!string.IsNullOrWhiteSpace(clipId)) return mp4.Value;
        }

        var kickBotId = ExtractKickBotId(url);
        if (!string.IsNullOrWhiteSpace(kickBotId))
        {
            var legacyCdn = "https://clips.kickbotcdn.com/kickbot-hls/" + kickBotId + "/" + kickBotId + ".mp4";
            if (UrlExists(legacyCdn)) { clipId = "kickbot-" + kickBotId; return legacyCdn; }
        }
        return null;
    }

    private string TryReadClipUrl(string json)
    {
        try { return (string)JObject.Parse(json ?? "")["clip"]?["clip_url"]; } catch { return null; }
    }

    private string DownloadMp4(string url, string clipId)
    {
        var folder = CPH.GetGlobalVar<string>(KickFolderKey, true);
        if (string.IsNullOrWhiteSpace(folder)) { CPH.LogWarn("RTS Action Replay: Kick Clip Folder is not configured."); return null; }
        var replayFolder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        if (!string.IsNullOrWhiteSpace(replayFolder) && PathsEqual(folder, replayFolder)) { CPH.LogError("RTS Action Replay: Kick Clip Folder must be separate from the OBS Replay Folder."); return null; }
        Directory.CreateDirectory(folder); var destination = Path.Combine(folder, "kick-" + Sanitize(clipId) + ".mp4");
        if (File.Exists(destination) && new FileInfo(destination).Length > 0) return destination;
        try
        {
            var temp = destination + ".tmp";
            using (var client = new WebClient()) { client.Headers[HttpRequestHeader.UserAgent] = "RTS-Action-Replay"; client.DownloadFile(url, temp); }
            if (File.Exists(temp) && new FileInfo(temp).Length > 0) { if (File.Exists(destination)) File.Delete(destination); File.Move(temp, destination); return destination; }
        }
        catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Kick MP4 download failed: " + ex.Message); }
        return null;
    }

    private bool BroadcastReplay(JObject item)
    {
        CPH.SetGlobalVar(ReplayIdHandoffKey, (string)item["id"] ?? "", false); CPH.SetGlobalVar(EntryPointHandoffKey, "kick", false); CPH.UnsetGlobalVar(ResolvedProfileHandoffKey, false);
        if (!CPH.ExecuteMethod(AnimationAction, "ResolveEntryPointProfile")) return false;
        return CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay");
    }

    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true); if (string.IsNullOrWhiteSpace(raw)) return new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() };
        try { var data = JObject.Parse(raw); data["catalog"] = data["catalog"] as JArray ?? new JArray(); data["recentIds"] = data["recentIds"] as JArray ?? new JArray(); return data; } catch { return new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() }; }
    }

    private void Save(JObject data) { CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true); }
    private void AddRecent(JObject data, string id) { var recent = (JArray)data["recentIds"] ?? new JArray(); for (var i = recent.Count - 1; i >= 0; i--) if (string.Equals((string)recent[i], id, StringComparison.OrdinalIgnoreCase)) recent.RemoveAt(i); recent.Insert(0, id); var max = CPH.GetGlobalVar<int?>(MaxRecentKey, true) ?? 20; while (recent.Count > Math.Max(1, max)) recent.RemoveAt(recent.Count - 1); data["recentIds"] = recent; }
    private string DownloadString(string url) { try { using (var client = new WebClient()) { client.Headers[HttpRequestHeader.UserAgent] = "RTS-Action-Replay"; return client.DownloadString(url); } } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Kick request failed: " + ex.Message); return null; } }
    private bool UrlExists(string url) { try { var request = (HttpWebRequest)WebRequest.Create(url); request.Method = "HEAD"; request.UserAgent = "RTS-Action-Replay"; using (var response = (HttpWebResponse)request.GetResponse()) return response.StatusCode == HttpStatusCode.OK; } catch { return false; } }
    private bool IsMp4(string url) => !string.IsNullOrWhiteSpace(url) && url.IndexOf(".mp4", StringComparison.OrdinalIgnoreCase) >= 0;
    private string ExtractKickBotUrl(string text) { var match = Regex.Match(text ?? "", @"https?://(?:www\.)?kickbot\.com/clip/[A-Za-z0-9]+", RegexOptions.IgnoreCase); return match.Success ? match.Value : null; }
    private string ExtractKickBotId(string url) { var match = Regex.Match(url ?? "", @"kickbot\.com/clip/([A-Za-z0-9]+)", RegexOptions.IgnoreCase); return match.Success ? match.Groups[1].Value : null; }
    private string ExtractKickClipId(string url) { var match = Regex.Match(url ?? "", @"[?&]clip=(clip_[A-Za-z0-9]+)", RegexOptions.IgnoreCase); if (match.Success) return match.Groups[1].Value; match = Regex.Match(url ?? "", @"/clips?/(clip_[A-Za-z0-9]+)", RegexOptions.IgnoreCase); return match.Success ? match.Groups[1].Value : null; }
    private string Arg(string name) { try { CPH.TryGetArg(name, out string value); return value ?? ""; } catch { return ""; } }
    private bool PathsEqual(string a, string b) { try { return string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase); } catch { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); } }
    private string Sanitize(string value) { var invalid = Path.GetInvalidFileNameChars(); var chars = value.ToCharArray(); for (var i = 0; i < chars.Length; i++) for (var j = 0; j < invalid.Length; j++) if (chars[i] == invalid[j]) chars[i] = '_'; return new string(chars); }
}