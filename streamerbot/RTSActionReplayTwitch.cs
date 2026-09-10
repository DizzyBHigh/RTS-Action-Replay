// Streamer.bot C# actions for RTS Action Replay Twitch integration.
// Twitch clips and OBS replays share the same catalog/recent data store.
// Requires Streamer.bot 1.0.3+ for TwitchGetClipDownloadUrls.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using Twitch.Common.Models.Api;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string LegacyCatalogKey = "rts.actionreplay.catalog";
    private const string MaxRecentKey = "rts.actionreplay.maxHistory";
    private const string PlaybackModeKey = "rts.actionreplay.twitch.playbackMode";
    private const string TwitchFolderKey = "rts.actionreplay.twitch.folder";

    public bool Execute() => CreateTwitchClip();

    public bool CreateTwitchClip()
    {
        CPH.TryGetArg("rawInput", out string rawInput);
        rawInput = rawInput == null ? "" : rawInput.Trim();
        var duration = Math.Max(5, Math.Min(60, GetSettingInt("rts.actionreplay.twitch.clipDuration", 30)));
        string title = null;
        if (!string.IsNullOrWhiteSpace(rawInput))
        {
            var remaining = rawInput;
            if (remaining[0] != '"')
            {
                var firstSpace = remaining.IndexOf(' ');
                var durationText = firstSpace < 0 ? remaining : remaining.Substring(0, firstSpace).Trim();
                if (!int.TryParse(durationText, out var requestedDuration)) { CPH.SendMessage("Usage: !clip [5-60] [\"title\"]"); return false; }
                duration = Math.Max(5, Math.Min(60, requestedDuration));
                remaining = firstSpace < 0 ? "" : remaining.Substring(firstSpace).Trim();
            }
            if (!string.IsNullOrWhiteSpace(remaining))
            {
                if (remaining[0] != '"') { CPH.SendMessage("Usage: !clip [5-60] [\"title\"]"); return false; }
                var closingQuote = remaining.IndexOf('"', 1);
                if (closingQuote < 0) { CPH.SendMessage("Usage: !clip [5-60] [\"title\"]"); return false; }
                title = remaining.Substring(1, closingQuote - 1);
                if (!string.IsNullOrWhiteSpace(remaining.Substring(closingQuote + 1).Trim())) { CPH.SendMessage("Usage: !clip [5-60] [\"title\"]"); return false; }
                if (string.IsNullOrWhiteSpace(title)) title = null;
            }
        }

        CPH.LogInfo("RTS Action Replay: creating Twitch Clip (" + duration + "s, title: " + (title ?? "<stream title>") + ").");
        ClipData clip;
        try { clip = CPH.CreateClip(title, duration); }
        catch (Exception ex) { CPH.LogError("RTS Action Replay: CreateClip failed: " + ex.Message); CPH.SendMessage("I couldn't create a Twitch clip."); return false; }
        if (clip == null || string.IsNullOrWhiteSpace(clip.Id)) { CPH.SendMessage("I couldn't create a Twitch clip."); return false; }

        ClipData published = null;
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            CPH.Wait(5000);
            try
            {
                var clips = CPH.GetClips(1000, null);
                published = clips == null ? null : clips.FirstOrDefault(x => x != null && string.Equals(x.Id, clip.Id, StringComparison.OrdinalIgnoreCase));
                if (published != null) break;
            }
            catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Twitch publication check " + attempt + " failed: " + ex.Message); }
        }
        if (published == null) { CPH.SendMessage("I couldn't confirm the Twitch clip was created."); return false; }

        var item = AddTwitchClip(published, true);
        if (item == null) return false;
        CPH.SetArgument("twitchClipId", published.Id);
        CPH.SetArgument("twitchClipUrl", published.Url ?? "");
        CPH.SetArgument("twitchClipTitle", (string)item["title"] ?? "");
        CPH.SetArgument("replayId", (string)item["id"] ?? "");
        CPH.SetArgument("replayTitle", (string)item["title"] ?? "");
        CPH.SetArgument("replaySource", "Twitch");
        return BroadcastReplay(item);
    }

    public bool SyncTwitchClips()
    {
        var data = Load();
        List<ClipData> clips;
        try { clips = CPH.GetClips(1000, null); }
        catch (Exception ex) { CPH.LogError("RTS Action Replay: Twitch reconciliation failed: " + ex.Message); return false; }
        var catalog = (JArray)data["catalog"]; var recent = (JArray)data["recentIds"]; var added = 0;
        foreach (var clip in clips ?? new List<ClipData>())
        {
            if (clip == null || string.IsNullOrWhiteSpace(clip.Id)) continue;
            var existing = FindTwitchClip(catalog, clip.Id);
            if (existing != null) { EnsureLocalCopyIfConfigured(data, existing, clip.Id); continue; }
            if (AddTwitchClip(clip, false) != null) added++;
            data = Load(); catalog = (JArray)data["catalog"]; recent = (JArray)data["recentIds"];
        }
        TrimRecent(recent); data["recentIds"] = recent; Save(data);
        CPH.LogInfo("RTS Action Replay: Twitch reconciliation added " + added + " new clip(s); discovered clips were not played.");
        return true;
    }

    private JObject AddTwitchClip(ClipData clip, bool playAfterAdd)
    {
        var data = Load(); var catalog = (JArray)data["catalog"]; var recent = (JArray)data["recentIds"];
        var existing = FindTwitchClip(catalog, clip.Id);
        if (existing != null) { EnsureLocalCopyIfConfigured(data, existing, clip.Id); return existing; }
        var mode = GetPlaybackMode(); var localPath = ModeNeedsLocalCopy(mode) ? DownloadClip(clip.Id) : null; var now = DateTime.Now;
        var item = new JObject
        {
            ["id"] = "twitch-" + clip.Id, ["sourceType"] = "Twitch", ["sourceId"] = clip.Id,
            ["title"] = string.IsNullOrWhiteSpace(clip.Title) ? "Twitch Clip" : clip.Title, ["customTitle"] = false,
            ["added"] = now.ToString("o"), ["captured"] = clip.CreatedAt.ToString("o"),
            ["creator"] = new JObject { ["id"] = clip.CreatorId.ToString(), ["name"] = clip.CreatorName ?? "" },
            ["broadcaster"] = new JObject { ["id"] = clip.BroadcasterId ?? "", ["name"] = clip.BroadcasterName ?? "" },
            ["gameId"] = clip.GameId ?? "", ["language"] = clip.Language ?? "", ["duration"] = clip.Duration,
            ["viewCount"] = clip.ViewCount, ["featured"] = clip.IsFeatured, ["externalUrl"] = clip.Url ?? "",
            ["embedUrl"] = clip.EmbedUrl ?? "", ["thumbnailUrl"] = clip.ThumbnailUrl ?? "",
            ["file"] = string.IsNullOrWhiteSpace(localPath) ? "" : Path.GetFileName(localPath), ["filePath"] = localPath ?? "",
            ["acquisitionMethod"] = playAfterAdd ? "TwitchCommand" : "TwitchDiscovery", ["plays"] = 0, ["users"] = new JObject()
        };
        catalog.Insert(0, item); AddRecent(recent, (string)item["id"]); TrimRecent(recent); data["catalog"] = catalog; data["recentIds"] = recent; Save(data);
        if (ModeNeedsLocalCopy(mode) && string.IsNullOrWhiteSpace(localPath)) CPH.LogWarn("RTS Action Replay: local Twitch copy could not be created for " + clip.Id + "; retaining Twitch playback as fallback.");
        return item;
    }

    private void EnsureLocalCopyIfConfigured(JObject data, JObject item, string clipId)
    {
        if (!ModeNeedsLocalCopy(GetPlaybackMode())) return;
        var current = (string)item["filePath"]; if (!string.IsNullOrWhiteSpace(current) && File.Exists(current)) return;
        var path = DownloadClip(clipId); if (string.IsNullOrWhiteSpace(path)) return;
        item["file"] = Path.GetFileName(path); item["filePath"] = path; Save(data);
    }

    private bool BroadcastReplay(JObject item)
    {
        var url = ResolvePlaybackUrl(item); if (string.IsNullOrWhiteSpace(url)) { CPH.SendMessage("I couldn't get a playable Twitch clip."); return false; }
        CPH.SetArgument("replayCommand", "load"); CPH.SetArgument("replayId", (string)item["id"]); CPH.SetArgument("replayTitle", (string)item["title"] ?? "Twitch Clip"); CPH.SetArgument("replayUrl", url); CPH.SetArgument("replayAutoplay", true); CPH.SetArgument("replaySource", "Twitch"); CPH.SetArgument("replaySourceId", (string)item["sourceId"] ?? "");
        ApplyPlayerSettings("twitchClip"); CPH.TriggerEvent("RTS-Action Replay", true); return true;
    }

    private string ResolvePlaybackUrl(JObject item)
    {
        var mode = GetPlaybackMode(); var clipId = (string)item["sourceId"];
        if (string.Equals(mode, "Twitch URL", StringComparison.OrdinalIgnoreCase)) return GetTwitchMediaUrl(clipId);
        var localPath = (string)item["filePath"]; if (string.IsNullOrWhiteSpace(localPath)) localPath = (string)item["file"];
        var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true);
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            var fullPath = Path.IsPathRooted(localPath) ? localPath : Path.Combine(folder ?? "", localPath);
            if (File.Exists(fullPath)) return BuildTwitchHttpUrl(Path.GetFileName(fullPath));
        }
        var downloaded = DownloadClip(clipId);
        if (!string.IsNullOrWhiteSpace(downloaded))
        {
            item["file"] = Path.GetFileName(downloaded); item["filePath"] = downloaded;
            var data = Load(); var target = FindTwitchClip((JArray)data["catalog"], clipId);
            if (target != null) { target["file"] = item["file"]; target["filePath"] = item["filePath"]; Save(data); }
            return BuildTwitchHttpUrl(Path.GetFileName(downloaded));
        }
        return GetTwitchMediaUrl(clipId);
    }

    private string GetTwitchMediaUrl(string clipId)
    {
        if (string.IsNullOrWhiteSpace(clipId)) return null;
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try { var urls = CPH.TwitchGetClipDownloadUrls(clipId); var url = urls?.LandscapeDownloadUrl ?? urls?.PortraitDownloadUrl; if (!string.IsNullOrWhiteSpace(url)) return url; }
            catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Twitch media URL attempt " + attempt + " failed: " + ex.Message); }
            if (attempt < 10) CPH.Wait(2000);
        }
        return null;
    }

    private string DownloadClip(string clipId)
    {
        var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true); var replayFolder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        if (string.IsNullOrWhiteSpace(folder) || (!string.IsNullOrWhiteSpace(replayFolder) && PathsEqual(folder, replayFolder))) return null;
        Directory.CreateDirectory(folder); var destination = Path.Combine(folder, "twitch-" + Sanitize(clipId) + ".mp4");
        if (File.Exists(destination) && new FileInfo(destination).Length > 0) return destination;
        var url = GetTwitchMediaUrl(clipId); if (string.IsNullOrWhiteSpace(url)) return null;
        try { var temp = destination + ".tmp"; using (var client = new WebClient()) client.DownloadFile(url, temp); if (File.Exists(temp) && new FileInfo(temp).Length > 0) { if (File.Exists(destination)) File.Delete(destination); File.Move(temp, destination); return destination; } }
        catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Twitch clip download failed: " + ex.Message); }
        try { if (File.Exists(destination + ".tmp")) File.Delete(destination + ".tmp"); } catch { }
        return null;
    }

    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true); JObject data;
        if (string.IsNullOrWhiteSpace(raw)) data = new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() };
        else { try { data = JObject.Parse(raw); } catch { data = new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() }; } }
        var catalog = data["catalog"] as JArray; var legacy = data["replays"] as JArray; if (catalog == null) catalog = legacy ?? new JArray(); else if (legacy != null) MergeCatalog(catalog, legacy);
        var external = CPH.GetGlobalVar<string>(LegacyCatalogKey, true); if (!string.IsNullOrWhiteSpace(external)) { try { var externalCatalog = JObject.Parse(external)["catalog"] as JArray; if (externalCatalog != null) MergeCatalog(catalog, externalCatalog); } catch { } }
        data["version"] = 2; data["catalog"] = catalog; data["recentIds"] = data["recentIds"] as JArray ?? new JArray(); data.Remove("replays"); return data;
    }

    private void Save(JObject data) { data["version"] = 2; data["catalog"] = data["catalog"] as JArray ?? new JArray(); data["recentIds"] = data["recentIds"] as JArray ?? new JArray(); data.Remove("replays"); CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true); CPH.SetGlobalVar("rts.actionreplay.recentIds", ((JArray)data["recentIds"]).ToString(Newtonsoft.Json.Formatting.None), true); }

    private void MergeCatalog(JArray target, JArray source)
    {
        foreach (var token in source)
        {
            var item = token as JObject; if (item == null) continue; var id = (string)item["id"]; var type = (string)item["sourceType"] ?? "OBS"; var sourceId = (string)item["sourceId"];
            if (target.OfType<JObject>().Any(x => (!string.IsNullOrWhiteSpace(id) && string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrWhiteSpace(sourceId) && string.Equals((string)x["sourceType"] ?? "OBS", type, StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceId"], sourceId, StringComparison.OrdinalIgnoreCase)))) continue;
            var clone = (JObject)item.DeepClone(); if (string.IsNullOrWhiteSpace((string)clone["sourceType"])) clone["sourceType"] = "OBS"; if (string.IsNullOrWhiteSpace((string)clone["sourceId"])) clone["sourceId"] = (string)clone["id"] ?? ""; if (clone["plays"] == null) clone["plays"] = 0; if (clone["users"] == null) clone["users"] = new JObject(); target.Add(clone);
        }
    }

    private JObject FindTwitchClip(JArray catalog, string clipId) { if (catalog == null) return null; return catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["sourceType"], "Twitch", StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceId"], clipId, StringComparison.OrdinalIgnoreCase)); }
    private void AddRecent(JArray recent, string id) { for (var i = recent.Count - 1; i >= 0; i--) if (string.Equals((string)recent[i], id, StringComparison.OrdinalIgnoreCase)) recent.RemoveAt(i); recent.Insert(0, id); }
    private void TrimRecent(JArray recent) { var max = CPH.GetGlobalVar<int?>(MaxRecentKey, true) ?? 20; while (recent.Count > Math.Max(1, max)) recent.RemoveAt(recent.Count - 1); }
    private bool ModeNeedsLocalCopy(string mode) => string.Equals(mode, "Download Locally", StringComparison.OrdinalIgnoreCase) || string.Equals(mode, "Both", StringComparison.OrdinalIgnoreCase);
    private string GetPlaybackMode() { var mode = CPH.GetGlobalVar<string>(PlaybackModeKey, true); if (string.Equals(mode, "Twitch URL", StringComparison.OrdinalIgnoreCase)) return "Twitch URL"; if (string.Equals(mode, "Both", StringComparison.OrdinalIgnoreCase)) return "Both"; return "Download Locally"; }
    private string BuildTwitchHttpUrl(string fileName) { var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.twitch.httpMapping", true) ?? "twitch"; var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474; return "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode(fileName ?? ""); }
    private bool PathsEqual(string a, string b) { try { return string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase); } catch { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); } }
    private string Sanitize(string value) { var invalid = Path.GetInvalidFileNameChars(); var chars = value.ToCharArray(); for (var i = 0; i < chars.Length; i++) for (var j = 0; j < invalid.Length; j++) if (chars[i] == invalid[j]) chars[i] = '_'; return new string(chars); }
    private int GetSettingInt(string key, int fallback) { try { object value = CPH.GetGlobalVar<object>(key, true); return value == null ? fallback : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture); } catch { return fallback; } }
    private double GetSettingDouble(string key, double fallback) { try { object value = CPH.GetGlobalVar<object>(key, true); return value == null ? fallback : Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture); } catch { return fallback; } }

    private void ApplyPlayerSettings(string profile)
    {
        CPH.SetArgument("replayShowControls", CPH.GetGlobalVar<bool?>("rts.actionreplay.showControls", true) ?? false);
        CPH.SetArgument("replayShowProgress", CPH.GetGlobalVar<bool?>("rts.actionreplay.showProgress", true) ?? true);
        CPH.SetArgument("replayPlaybackSpeed", GetSettingDouble("rts.actionreplay.playbackSpeed", 1.0));
        CPH.SetArgument("replayPlaybackSpeedVisibility", CPH.GetGlobalVar<string>("rts.actionreplay.playbackSpeedVisibility", true) ?? "Only when greater or less than 1");
        CPH.SetArgument("replayFrameColor", CPH.GetGlobalVar<string>("rts.actionreplay.frameColor", true) ?? "#0384CBFF");
        CPH.SetArgument("replayBorderWidth", GetSettingInt("rts.actionreplay.borderWidth", 4));
        CPH.SetArgument("replayCornerRadius", GetSettingInt("rts.actionreplay.cornerRadius", 0));
        CPH.SetArgument("replayBorderGlow", CPH.GetGlobalVar<bool?>("rts.actionreplay.borderGlow", true) ?? true);
        CPH.SetArgument("replayPositions", CPH.GetGlobalVar<string>("rts.actionreplay.positions", true) ?? "{\"Full Screen\":{\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}");
        CPH.SetArgument("replayStartPosition", GetAnimationProfileString(profile, "startPosition", "Full Screen"));
        CPH.SetArgument("replayEndPosition", GetAnimationProfileString(profile, "endPosition", "Full Screen"));
        CPH.SetArgument("replayAnimationDuration", GetAnimationProfileDouble(profile, "duration", .5));
        CPH.SetArgument("replayAnimationEasing", GetAnimationProfileString(profile, "easing", "ease-in-out"));
        CPH.SetArgument("replayShowBranding", CPH.GetGlobalVar<bool?>("rts.actionreplay.showBranding", true) ?? true);
        CPH.SetArgument("replayBrandLogoUrl", CPH.GetGlobalVar<string>("rts.actionreplay.brandLogoUrl", true) ?? "");
        CPH.SetArgument("replayBrandFallbackText", CPH.GetGlobalVar<string>("rts.actionreplay.brandFallbackText", true) ?? "RTS");
        CPH.SetArgument("replayBrandFallbackTextColor", CPH.GetGlobalVar<string>("rts.actionreplay.brandFallbackTextColor", true) ?? "#0384CBFF");
        CPH.SetArgument("replayBrandLabel", CPH.GetGlobalVar<string>("rts.actionreplay.brandLabel", true) ?? "ACTION REPLAY");
        CPH.SetArgument("replayBrandLabelColor", CPH.GetGlobalVar<string>("rts.actionreplay.brandLabelColor", true) ?? "#FFFFFFFF");
        CPH.SetArgument("replayShowTitle", CPH.GetGlobalVar<bool?>("rts.actionreplay.showTitle", true) ?? true);
        CPH.SetArgument("replayTitleDecorationPosition", CPH.GetGlobalVar<string>("rts.actionreplay.titleDecorationPosition", true) ?? "Suffix");
        CPH.SetArgument("replayTitleDecoration", CPH.GetGlobalVar<string>("rts.actionreplay.titleDecoration", true) ?? " - Replay Capture");
        CPH.SetArgument("replayTitleStyle", CPH.GetGlobalVar<string>("rts.actionreplay.titleBarStyle", true) ?? "Broadcast");
        CPH.SetArgument("replayTitlePosition", CPH.GetGlobalVar<string>("rts.actionreplay.titlePosition", true) ?? "Bottom");
        CPH.SetArgument("replayTitleAnimation", CPH.GetGlobalVar<string>("rts.actionreplay.titleAnimation", true) ?? "Slide up/down");
        CPH.SetArgument("replayTitleDelay", GetSettingInt("rts.actionreplay.titleDelay", 0));
        CPH.SetArgument("replayTitleDuration", GetSettingInt("rts.actionreplay.titleDuration", 5000));
        CPH.SetArgument("replayTitleAnimationDuration", GetSettingInt("rts.actionreplay.titleAnimationDuration", 450));
        CPH.SetArgument("replayTitleFont", CPH.GetGlobalVar<string>("rts.actionreplay.titleFont", true) ?? "Inter");
        CPH.SetArgument("replayTitleFontSize", GetSettingInt("rts.actionreplay.titleFontSize", 34));
        CPH.SetArgument("replayTitleTextColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleTextColor", true) ?? "#FFFFFFFF");
        CPH.SetArgument("replayTitleShadowColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleShadowColor", true) ?? "#000000FF");
        CPH.SetArgument("replayTitlePrimaryColor", CPH.GetGlobalVar<string>("rts.actionreplay.titlePrimaryColor", true) ?? "#0384CBFF");
        CPH.SetArgument("replayTitleSecondaryColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleSecondaryColor", true) ?? "#101416FF");
    }

    private string GetAnimationProfileString(string profile, string field, string fallback) { var value = CPH.GetGlobalVar<string>("rts.actionreplay.animation." + profile + "." + field, true); return string.IsNullOrWhiteSpace(value) ? fallback : value; }
    private double GetAnimationProfileDouble(string profile, string field, double fallback) { return GetSettingDouble("rts.actionreplay.animation." + profile + "." + field, fallback); }
}