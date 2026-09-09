// Streamer.bot C# actions for RTS Action Replay Twitch integration.
//
// CreateTwitchClip:
//   Run from the !twitchclip command. Creates a Twitch Clip, downloads it,
//   adds it to the Catalog + Recent Clips, then immediately plays it.
//
// SyncTwitchClips:
//   Run on a recurring schedule (recommended: once per hour). Retrieves
//   Twitch clips, adds only clips not already in the Catalog, and never
//   queues or plays clips discovered by the reconciliation pass.
//
// Requires Streamer.bot 1.0.3+ for TwitchGetClipDownloadUrls.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using Twitch.Common.Models.Api;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string CatalogKey = "rts.actionreplay.catalog";
    private const string RecentKey = "rts.actionreplay.recentIds";
    private const string MaxRecentKey = "rts.actionreplay.maxHistory";

    public bool Execute() => CreateTwitchClip();

    public bool CreateTwitchClip()
    {
        string rawInput;
        CPH.TryGetArg("rawInput", out rawInput);
        var title = string.IsNullOrWhiteSpace(rawInput) ? null : rawInput.Trim();
        var duration = GetSettingInt("rts.actionreplay.twitch.clipDuration", 30);
        duration = Math.Max(5, Math.Min(60, duration));

        CPH.LogInfo("RTS Action Replay: creating Twitch Clip.");
        var clip = CPH.CreateClip(title, duration);
        if (clip == null || string.IsNullOrWhiteSpace(clip.Id))
        {
            CPH.LogWarn("RTS Action Replay: Twitch did not return a clip.");
            CPH.SendMessage("I couldn't create a Twitch clip.");
            return false;
        }

        var item = AddTwitchClip(clip, true);
        if (item == null) return false;

        CPH.SetArgument("twitchClipId", clip.Id);
        CPH.SetArgument("twitchClipUrl", clip.Url ?? "");
        CPH.SetArgument("twitchClipTitle", (string)item["title"] ?? "");
        CPH.SetArgument("replayId", (string)item["id"] ?? "");
        CPH.SetArgument("replayTitle", (string)item["title"] ?? "");
        CPH.SetArgument("replaySource", "Twitch");

        BroadcastReplay(item);
        return true;
    }

    public bool SyncTwitchClips()
    {
        InitializeCatalog();
        List<ClipData> clips;
        try
        {
            clips = CPH.GetClips(1000, null);
        }
        catch (Exception ex)
        {
            CPH.LogError("RTS Action Replay: Twitch clip reconciliation failed: " + ex.Message);
            return false;
        }

        if (clips == null || clips.Count == 0)
        {
            CPH.LogInfo("RTS Action Replay: Twitch reconciliation found no clips.");
            return true;
        }

        var added = 0;
        foreach (var clip in clips)
        {
            if (clip == null || string.IsNullOrWhiteSpace(clip.Id)) continue;
            if (CatalogContainsTwitchClip(clip.Id)) continue;
            if (AddTwitchClip(clip, false) != null) added++;
        }

        CPH.LogInfo("RTS Action Replay: Twitch reconciliation complete; added " + added + " new clip(s). Existing clips were not re-queued or played.");
        return true;
    }

    private JObject AddTwitchClip(ClipData clip, bool playAfterAdd)
    {
        var data = LoadCatalog();
        var catalog = (JArray)data["catalog"];
        var recent = (JArray)data["recentIds"];

        var existing = FindTwitchClip(catalog, clip.Id);
        if (existing != null)
        {
            CPH.LogInfo("RTS Action Replay: Twitch clip already in Catalog: " + clip.Id);
            return existing;
        }

        var localPath = DownloadClip(clip.Id);
        if (string.IsNullOrWhiteSpace(localPath))
        {
            CPH.LogWarn("RTS Action Replay: could not download Twitch clip " + clip.Id + "; Catalog entry was not created.");
            return null;
        }

        var now = DateTime.Now;
        var title = string.IsNullOrWhiteSpace(clip.Title) ? "Twitch Clip" : clip.Title;
        var item = new JObject
        {
            ["id"] = "twitch-" + clip.Id,
            ["sourceType"] = "Twitch",
            ["sourceId"] = clip.Id,
            ["title"] = title,
            ["customTitle"] = false,
            ["added"] = now.ToString("o"),
            ["captured"] = clip.CreatedAt.ToString("o"),
            ["creator"] = new JObject
            {
                ["id"] = clip.CreatorId.ToString(),
                ["name"] = clip.CreatorName ?? ""
            },
            ["broadcaster"] = new JObject
            {
                ["id"] = clip.BroadcasterId,
                ["name"] = clip.BroadcasterName ?? ""
            },
            ["gameId"] = clip.GameId ?? "",
            ["language"] = clip.Language ?? "",
            ["duration"] = clip.Duration,
            ["viewCount"] = clip.ViewCount,
            ["featured"] = clip.IsFeatured,
            ["externalUrl"] = clip.Url ?? "",
            ["embedUrl"] = clip.EmbedUrl ?? "",
            ["thumbnailUrl"] = clip.ThumbnailUrl ?? "",
            ["file"] = Path.GetFileName(localPath),
            ["filePath"] = localPath,
            ["sourceTypeDisplay"] = "Twitch",
            ["acquisitionMethod"] = playAfterAdd ? "TwitchCommand" : "TwitchDiscovery",
            ["plays"] = 0,
            ["users"] = new JObject()
        };

        catalog.Insert(0, item);
        AddRecent(recent, (string)item["id"]);
        TrimRecent(recent);
        data["catalog"] = catalog;
        data["recentIds"] = recent;
        data["version"] = 2;
        SaveCatalog(data);

        // Keep the current playback implementation compatible while the Catalog
        // becomes the long-term source of truth. This legacy projection is only
        // used by the existing player until its lookup is switched to Catalog.
        AddLegacyReplayProjection(item);

        CPH.LogInfo("RTS Action Replay: added Twitch clip to Catalog: " + title + " (" + clip.Id + ")");
        return item;
    }

    private string DownloadClip(string clipId)
    {
        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        if (string.IsNullOrWhiteSpace(folder))
        {
            CPH.LogError("RTS Action Replay: Replay Folder is not configured; cannot download Twitch clips.");
            return null;
        }

        var twitchFolder = Path.Combine(folder, "Twitch");
        Directory.CreateDirectory(twitchFolder);
        var destination = Path.Combine(twitchFolder, "twitch-" + SanitizeFileName(clipId) + ".mp4");

        if (File.Exists(destination) && new FileInfo(destination).Length > 0)
            return destination;

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                var urls = CPH.TwitchGetClipDownloadUrls(clipId);
                var url = urls == null ? null : urls.LandscapeDownloadUrl;
                if (string.IsNullOrWhiteSpace(url))
                    url = urls == null ? null : urls.PortraitDownloadUrl;

                if (!string.IsNullOrWhiteSpace(url))
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(url, destination + ".tmp");
                    }

                    if (File.Exists(destination + ".tmp") && new FileInfo(destination + ".tmp").Length > 0)
                    {
                        if (File.Exists(destination)) File.Delete(destination);
                        File.Move(destination + ".tmp", destination);
                        return destination;
                    }
                }
            }
            catch (Exception ex)
            {
                CPH.LogWarn("RTS Action Replay: Twitch clip download attempt " + attempt + " failed for " + clipId + ": " + ex.Message);
            }

            if (attempt < 10) CPH.Wait(2000);
        }

        try { if (File.Exists(destination + ".tmp")) File.Delete(destination + ".tmp"); } catch { }
        return null;
    }

    private void AddLegacyReplayProjection(JObject item)
    {
        var data = JObject.Parse(CPH.GetGlobalVar<string>(DataKey, true) ?? "{\"version\":1,\"replays\":[]}");
        var list = (JArray)data["replays"] ?? new JArray();
        var file = (string)item["file"];
        for (var i = 0; i < list.Count; i++)
            if (string.Equals((string)list[i]["file"], file, StringComparison.OrdinalIgnoreCase)) return;

        var replay = new JObject
        {
            ["id"] = (string)item["id"],
            ["file"] = file,
            ["title"] = (string)item["title"],
            ["customTitle"] = false,
            ["added"] = (string)item["added"],
            ["creator"] = item["creator"].DeepClone(),
            ["plays"] = 0,
            ["users"] = new JObject(),
            ["sourceType"] = "Twitch",
            ["sourceId"] = (string)item["sourceId"]
        };
        list.Insert(0, replay);
        var max = CPH.GetGlobalVar<int?>(MaxRecentKey, true) ?? 20;
        while (list.Count > Math.Max(1, max)) list.RemoveAt(list.Count - 1);
        data["replays"] = list;
        CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
    }

    private void BroadcastReplay(JObject item)
    {
        var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.httpMapping", true) ?? "replays";
        var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
        CPH.SetArgument("replayCommand", "load");
        CPH.SetArgument("replayId", (string)item["id"]);
        CPH.SetArgument("replayTitle", (string)item["title"] ?? "Twitch Clip");
        CPH.SetArgument("replayUrl", "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode((string)item["file"]));
        CPH.SetArgument("replayAutoplay", true);
        CPH.SetArgument("replaySource", "Twitch");
        CPH.SetArgument("replayShowControls", CPH.GetGlobalVar<bool?>("rts.actionreplay.showControls", true) ?? false);
        CPH.SetArgument("replayShowProgress", CPH.GetGlobalVar<bool?>("rts.actionreplay.showProgress", true) ?? true);
        CPH.SetArgument("replayPlaybackSpeed", GetSettingDouble("rts.actionreplay.playbackSpeed", 1.0));
        CPH.SetArgument("replayPlaybackSpeedVisibility", CPH.GetGlobalVar<string>("rts.actionreplay.playbackSpeedVisibility", true) ?? "Only when greater or less than 1");
        CPH.SetArgument("replayFrameColor", CPH.GetGlobalVar<string>("rts.actionreplay.frameColor", true) ?? "#0384CBFF");
        CPH.SetArgument("replayPositions", CPH.GetGlobalVar<string>("rts.actionreplay.positions", true) ?? "{\"Full Screen\":{\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}");
        CPH.SetArgument("replayStartPosition", CPH.GetGlobalVar<string>("rts.actionreplay.defaultStartPosition", true) ?? "Full Screen");
        CPH.SetArgument("replayEndPosition", CPH.GetGlobalVar<string>("rts.actionreplay.defaultEndPosition", true) ?? "Full Screen");
        CPH.SetArgument("replayAnimationDuration", GetSettingDouble("rts.actionreplay.animationDuration", .5));
        CPH.SetArgument("replayAnimationEasing", CPH.GetGlobalVar<string>("rts.actionreplay.animationEasing", true) ?? "ease-in-out");
        CPH.SetArgument("replayShowTitle", CPH.GetGlobalVar<bool?>("rts.actionreplay.showTitle", true) ?? true);
        CPH.SetArgument("replayShowBranding", CPH.GetGlobalVar<bool?>("rts.actionreplay.showBranding", true) ?? true);
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
        CPH.TriggerEvent("RTS-Action Replay", true);
    }

    private bool CatalogContainsTwitchClip(string clipId)
    {
        return FindTwitchClip(LoadCatalog()["catalog"] as JArray, clipId) != null;
    }

    private JObject FindTwitchClip(JArray catalog, string clipId)
    {
        if (catalog == null) return null;
        for (var i = 0; i < catalog.Count; i++)
        {
            var item = catalog[i] as JObject;
            if (item == null) continue;
            if (string.Equals((string)item["sourceType"], "Twitch", StringComparison.OrdinalIgnoreCase) && string.Equals((string)item["sourceId"], clipId, StringComparison.OrdinalIgnoreCase)) return item;
        }
        return null;
    }

    private JObject LoadCatalog()
    {
        var raw = CPH.GetGlobalVar<string>(CatalogKey, true);
        if (!string.IsNullOrWhiteSpace(raw)) return JObject.Parse(raw);

        var data = new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() };
        var legacyRaw = CPH.GetGlobalVar<string>(DataKey, true);
        if (!string.IsNullOrWhiteSpace(legacyRaw))
        {
            try
            {
                var legacy = JObject.Parse(legacyRaw);
                var legacyList = legacy["replays"] as JArray;
                if (legacyList != null)
                {
                    var catalog = (JArray)data["catalog"];
                    var recent = (JArray)data["recentIds"];
                    for (var i = 0; i < legacyList.Count; i++)
                    {
                        var source = (JObject)legacyList[i];
                        var id = (string)source["id"];
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        var migrated = (JObject)source.DeepClone();
                        migrated["sourceType"] = (string)migrated["sourceType"] ?? "OBS";
                        migrated["sourceId"] = (string)migrated["sourceId"] ?? id;
                        catalog.Add(migrated);
                        recent.Add(id);
                    }
                }
            }
            catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Catalog migration skipped: " + ex.Message); }
        }
        SaveCatalog(data);
        return data;
    }

    private void InitializeCatalog()
    {
        LoadCatalog();
    }

    private void SaveCatalog(JObject data)
    {
        CPH.SetGlobalVar(CatalogKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
        // Keep the canonical state available under the existing data key for
        // older Action Replay actions until those actions are migrated to the
        // Catalog lookup.
        CPH.SetGlobalVar(RecentKey, ((JArray)data["recentIds"]).ToString(Newtonsoft.Json.Formatting.None), true);
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

    private string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            for (var j = 0; j < invalid.Length; j++)
                if (chars[i] == invalid[j]) chars[i] = '_';
        return new string(chars);
    }

    private int GetSettingInt(string key, int fallback)
    {
        try
        {
            object value = CPH.GetGlobalVar<object>(key, true);
            return value == null ? fallback : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch { return fallback; }
    }

    private double GetSettingDouble(string key, double fallback)
    {
        try
        {
            object value = CPH.GetGlobalVar<object>(key, true);
            return value == null ? fallback : Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch { return fallback; }
    }
}
