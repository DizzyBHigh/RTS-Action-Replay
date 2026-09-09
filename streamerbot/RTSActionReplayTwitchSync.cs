// Streamer.bot C# action: run this action once per hour to reconcile Twitch clips.
// It adds missing Twitch clips to the Action Replay Catalog and Recent Clips,
// but deliberately does not queue or play clips discovered by this scan.
// Requires Streamer.bot 1.0.3+.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using Twitch.Common.Models.Api;

public class CPHInline
{
    private const string CatalogKey = "rts.actionreplay.catalog";
    private const string DataKey = "rts.actionreplay.data";
    private const string MaxRecentKey = "rts.actionreplay.maxHistory";

    public bool Execute()
    {
        var raw = CPH.GetGlobalVar<string>(CatalogKey, true);
        if (string.IsNullOrWhiteSpace(raw))
        {
            CPH.LogInfo("RTS Action Replay: Twitch sync skipped because the Catalog has not been initialized yet.");
            return true;
        }

        List<ClipData> clips;
        try
        {
            clips = CPH.GetClips(1000, null);
        }
        catch (Exception ex)
        {
            CPH.LogError("RTS Action Replay: Twitch reconciliation failed: " + ex.Message);
            return false;
        }

        var data = JObject.Parse(raw);
        var catalog = (JArray)data["catalog"] ?? new JArray();
        var recent = (JArray)data["recentIds"] ?? new JArray();
        var added = 0;

        foreach (var clip in clips ?? new List<ClipData>())
        {
            if (clip == null || string.IsNullOrWhiteSpace(clip.Id) || Find(catalog, clip.Id) != null) continue;
            var path = Download(clip.Id);
            if (string.IsNullOrWhiteSpace(path)) continue;

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
                ["file"] = Path.GetFileName(path),
                ["filePath"] = path,
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
        CPH.SetGlobalVar(CatalogKey, data.ToString(Newtonsoft.Json.Formatting.None), true);

        CPH.LogInfo("RTS Action Replay: Twitch reconciliation added " + added + " new clip(s) to Catalog/Recent Clips. No discovered clips were played.");
        return true;
    }

    private string Download(string clipId)
    {
        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        if (string.IsNullOrWhiteSpace(folder)) return null;
        var twitchFolder = Path.Combine(folder, "Twitch");
        Directory.CreateDirectory(twitchFolder);
        var destination = Path.Combine(twitchFolder, "twitch-" + Sanitize(clipId) + ".mp4");
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
            catch (Exception ex)
            {
                CPH.LogWarn("RTS Action Replay: Twitch download attempt " + attempt + " failed for " + clipId + ": " + ex.Message);
            }
            if (attempt < 10) CPH.Wait(2000);
        }
        return null;
    }

    private JObject Find(JArray catalog, string clipId)
    {
        if (catalog == null) return null;
        foreach (var token in catalog)
        {
            var item = token as JObject;
            if (item != null && string.Equals((string)item["sourceType"], "Twitch", StringComparison.OrdinalIgnoreCase) && string.Equals((string)item["sourceId"], clipId, StringComparison.OrdinalIgnoreCase)) return item;
        }
        return null;
    }

    private void AddRecent(JArray recent, string id)
    {
        for (var i = recent.Count - 1; i >= 0; i--)
            if (string.Equals((string)recent[i], id, StringComparison.OrdinalIgnoreCase)) recent.RemoveAt(i);
        recent.Insert(0, id);
    }

    private void TrimRecent(JArray recent)
    {
        var max = CPH.GetGlobalVar<int?>(MaxRecentKey, true) ?? 20;
        while (recent.Count > Math.Max(1, max)) recent.RemoveAt(recent.Count - 1);
    }

    private string Sanitize(string value)
    {
        var chars = value.ToCharArray();
        var invalid = Path.GetInvalidFileNameChars();
        for (var i = 0; i < chars.Length; i++)
            for (var j = 0; j < invalid.Length; j++)
                if (chars[i] == invalid[j]) chars[i] = '_';
        return new string(chars);
    }
}
