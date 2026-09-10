// Streamer.bot C# action: run this action once per hour to reconcile Twitch clips.
// It adds missing Twitch clips to the unified Action Replay Catalog and Recent Clips,
// but deliberately does not queue or play clips discovered by this scan.
// Requires Streamer.bot 1.0.3+.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using Newtonsoft.Json.Linq;
using Twitch.Common.Models.Api;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string LegacyCatalogKey = "rts.actionreplay.catalog";
    private const string MaxRecentKey = "rts.actionreplay.maxHistory";
    private const string PlaybackModeKey = "rts.actionreplay.twitch.playbackMode";
    private const string TwitchFolderKey = "rts.actionreplay.twitch.folder";

    public bool Execute()
    {
        var data = Load();
        var catalog = (JArray)data["catalog"];
        var recent = (JArray)data["recentIds"];

        List<ClipData> clips;
        try { clips = CPH.GetClips(1000, null); }
        catch (Exception ex)
        {
            CPH.LogError("RTS Action Replay: Twitch reconciliation failed: " + ex.Message);
            return false;
        }

        var added = 0;
        var backfilled = 0;
        var mode = GetPlaybackMode();

        foreach (var clip in clips ?? new List<ClipData>())
        {
            if (clip == null || string.IsNullOrWhiteSpace(clip.Id)) continue;

            var existing = Find(catalog, clip.Id);
            if (existing != null)
            {
                if (ModeNeedsLocalCopy(mode) && EnsureLocalCopy(existing, clip.Id)) backfilled++;
                continue;
            }

            var path = ModeNeedsLocalCopy(mode) ? Download(clip.Id) : null;
            if (ModeNeedsLocalCopy(mode) && string.IsNullOrWhiteSpace(path))
                CPH.LogWarn("RTS Action Replay: sync could not create a local copy for Twitch clip " + clip.Id + "; retaining its Twitch URL in the Catalog.");

            var item = new JObject
            {
                ["id"] = "twitch-" + clip.Id,
                ["sourceType"] = "Twitch",
                ["sourceId"] = clip.Id,
                ["title"] = string.IsNullOrWhiteSpace(clip.Title) ? "Twitch Clip" : clip.Title,
                ["customTitle"] = false,
                ["added"] = DateTime.Now.ToString("o"),
                ["captured"] = clip.CreatedAt.ToString("o"),
                ["creator"] = new JObject { ["id"] = clip.CreatorId.ToString(), ["name"] = clip.CreatorName ?? "" },
                ["broadcaster"] = new JObject { ["id"] = clip.BroadcasterId ?? "", ["name"] = clip.BroadcasterName ?? "" },
                ["gameId"] = clip.GameId ?? "",
                ["language"] = clip.Language ?? "",
                ["duration"] = clip.Duration,
                ["viewCount"] = clip.ViewCount,
                ["featured"] = clip.IsFeatured,
                ["externalUrl"] = clip.Url ?? "",
                ["embedUrl"] = clip.EmbedUrl ?? "",
                ["thumbnailUrl"] = clip.ThumbnailUrl ?? "",
                ["file"] = string.IsNullOrWhiteSpace(path) ? "" : Path.GetFileName(path),
                ["filePath"] = path ?? "",
                ["acquisitionMethod"] = "TwitchDiscovery",
                ["plays"] = 0,
                ["users"] = new JObject()
            };

            catalog.Insert(0, item);
            AddRecent(recent, (string)item["id"]);
            added++;
        }

        TrimRecent(recent);
        data["version"] = 2;
        data["catalog"] = catalog;
        data["recentIds"] = recent;
        Save(data);

        CPH.LogInfo("RTS Action Replay: Twitch reconciliation added " + added + " new clip(s), backfilled " + backfilled + " local copy/copies, and did not play discovered clips.");
        return true;
    }

    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true);
        JObject data;
        if (string.IsNullOrWhiteSpace(raw)) data = new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() };
        else { try { data = JObject.Parse(raw); } catch { data = new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() }; } }

        var catalog = data["catalog"] as JArray;
        var legacy = data["replays"] as JArray;
        if (catalog == null) catalog = legacy ?? new JArray();
        else if (legacy != null && legacy.Count > 0) MergeCatalog(catalog, legacy);

        var external = CPH.GetGlobalVar<string>(LegacyCatalogKey, true);
        if (!string.IsNullOrWhiteSpace(external))
        {
            try { var externalData = JObject.Parse(external); var externalCatalog = externalData["catalog"] as JArray; if (externalCatalog != null) MergeCatalog(catalog, externalCatalog); } catch { }
        }

        data["version"] = 2;
        data["catalog"] = catalog;
        data["recentIds"] = data["recentIds"] as JArray ?? new JArray();
        data.Remove("replays");
        return data;
    }

    private void Save(JObject data)
    {
        data["version"] = 2;
        data["catalog"] = data["catalog"] as JArray ?? new JArray();
        data["recentIds"] = data["recentIds"] as JArray ?? new JArray();
        data.Remove("replays");
        CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
        CPH.SetGlobalVar("rts.actionreplay.recentIds", ((JArray)data["recentIds"]).ToString(Newtonsoft.Json.Formatting.None), true);
    }

    private void MergeCatalog(JArray target, JArray source)
    {
        foreach (var token in source)
        {
            var item = token as JObject;
            if (item == null) continue;
            var id = (string)item["id"];
            var sourceType = (string)item["sourceType"] ?? "OBS";
            var sourceId = (string)item["sourceId"];
            var exists = target.OfType<JObject>().Any(x =>
                (!string.IsNullOrWhiteSpace(id) && string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(sourceId) && string.Equals((string)x["sourceType"] ?? "OBS", sourceType, StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceId"], sourceId, StringComparison.OrdinalIgnoreCase)));
            if (exists) continue;
            var clone = (JObject)item.DeepClone();
            if (string.IsNullOrWhiteSpace((string)clone["sourceType"])) clone["sourceType"] = "OBS";
            if (string.IsNullOrWhiteSpace((string)clone["sourceId"])) clone["sourceId"] = (string)clone["id"] ?? "";
            if (clone["plays"] == null) clone["plays"] = 0;
            if (clone["users"] == null) clone["users"] = new JObject();
            target.Add(clone);
        }
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

    private string GetPlaybackMode()
    {
        var mode = CPH.GetGlobalVar<string>(PlaybackModeKey, true);
        if (string.Equals(mode, "Twitch URL", StringComparison.OrdinalIgnoreCase)) return "Twitch URL";
        if (string.Equals(mode, "Both", StringComparison.OrdinalIgnoreCase)) return "Both";
        return "Download Locally";
    }

    private bool ModeNeedsLocalCopy(string mode)
    {
        return string.Equals(mode, "Download Locally", StringComparison.OrdinalIgnoreCase) || string.Equals(mode, "Both", StringComparison.OrdinalIgnoreCase);
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

    private void AddRecent(JArray recent, string id)
    {
        for (var i = recent.Count - 1; i >= 0; i--) if (string.Equals((string)recent[i], id, StringComparison.OrdinalIgnoreCase)) recent.RemoveAt(i);
        recent.Insert(0, id);
    }

    private void TrimRecent(JArray recent)
    {
        var max = CPH.GetGlobalVar<int?>(MaxRecentKey, true) ?? 20;
        while (recent.Count > Math.Max(1, max)) recent.RemoveAt(recent.Count - 1);
    }

    private bool PathsEqual(string a, string b)
    {
        try { return string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase); }
        catch { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); }
    }

    private string Sanitize(string value)
    {
        var chars = value.ToCharArray(); var invalid = Path.GetInvalidFileNameChars();
        for (var i = 0; i < chars.Length; i++) for (var j = 0; j < invalid.Length; j++) if (chars[i] == invalid[j]) chars[i] = '_';
        return new string(chars);
    }
}