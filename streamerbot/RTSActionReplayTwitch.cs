// Streamer.bot C# actions for RTS Action Replay Twitch integration.
//
// CreateTwitchClip:
//   Creates a Twitch Clip, waits for Twitch to publish it, stores its Twitch metadata/URL in the Catalog,
//   optionally downloads a local copy, adds it to Recent Clips, then plays it.
//
// SyncTwitchClips:
//   Reconciles Twitch clips into the Catalog/Recent Clips without queueing or playing them.
//
// Twitch playback mode is Action Replay configuration, not Catalog data.
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
    private const string PlaybackModeKey = "rts.actionreplay.twitch.playbackMode";
    private const string TwitchFolderKey = "rts.actionreplay.twitch.folder";

    public bool Execute() => CreateTwitchClip();

    public bool CreateTwitchClip()
    {
        CPH.TryGetArg("rawInput", out string rawInput);
        rawInput = rawInput == null ? "" : rawInput.Trim();
    
        var duration = GetSettingInt("rts.actionreplay.twitch.clipDuration", 30);
        duration = Math.Max(5, Math.Min(60, duration));
    
        string title = null;
    
        // Command syntax:
        //
        // !clip
        // !clip 30
        // !clip "My clip title"
        // !clip 30 "My clip title"
        //
        // Titles MUST be surrounded by double quotes.
        // This means numbers inside a quoted title can never be confused with duration.
    
        if (!string.IsNullOrWhiteSpace(rawInput))
        {
            var remaining = rawInput;
    
            // If the first character is not a quote, the first token must be a duration.
            if (remaining[0] != '"')
            {
                var firstSpace = remaining.IndexOf(' ');
                string durationText;
                string afterDuration;
    
                if (firstSpace < 0)
                {
                    durationText = remaining;
                    afterDuration = "";
                }
                else
                {
                    durationText = remaining.Substring(0, firstSpace).Trim();
                    afterDuration = remaining.Substring(firstSpace).Trim();
                }
    
                int requestedDuration;
    
                if (!int.TryParse(durationText, out requestedDuration))
                {
                    CPH.LogWarn("RTS Action Replay: invalid Twitch Clip command. Titles must be surrounded by double quotes.");
                    CPH.SendMessage("Usage: !clip [5-60] [\"title\"]");
                    return false;
                }
    
                duration = Math.Max(5, Math.Min(60, requestedDuration));
                remaining = afterDuration;
    
                // A duration may be supplied without a title.
                if (string.IsNullOrWhiteSpace(remaining))
                {
                    title = null;
                }
            }
    
            // If there is remaining input, it MUST be a quoted title.
            if (!string.IsNullOrWhiteSpace(remaining))
            {
                if (remaining[0] != '"')
                {
                    CPH.LogWarn("RTS Action Replay: invalid Twitch Clip title. Titles must be surrounded by double quotes.");
                    CPH.SendMessage("Usage: !clip [5-60] [\"title\"]");
                    return false;
                }
    
                // Find the closing quote.
                var closingQuote = remaining.IndexOf('"', 1);
    
                if (closingQuote < 0)
                {
                    CPH.LogWarn("RTS Action Replay: invalid Twitch Clip title. Missing closing double quote.");
                    CPH.SendMessage("Usage: !clip [5-60] [\"title\"]");
                    return false;
                }
    
                title = remaining.Substring(1, closingQuote - 1);
    
                // Anything after the closing quote is invalid.
                var trailing = remaining.Substring(closingQuote + 1).Trim();
    
                if (!string.IsNullOrWhiteSpace(trailing))
                {
                    CPH.LogWarn("RTS Action Replay: invalid Twitch Clip command. Nothing is allowed after the quoted title.");
                    CPH.SendMessage("Usage: !clip [5-60] [\"title\"]");
                    return false;
                }
    
                // Don't send an empty title to Twitch.
                if (string.IsNullOrWhiteSpace(title))
                    title = null;
            }
        }
    
        CPH.LogInfo(
            "RTS Action Replay: creating Twitch Clip (requested duration: "
            + duration
            + "s, title: "
            + (title ?? "<stream title>")
            + ")."
        );
    
        ClipData clip;
    
        try
        {
            clip = CPH.CreateClip(title, duration);
    
            CPH.LogInfo(
                "RTS Action Replay: CreateClip returned "
                + (clip == null ? "null" : "a ClipData object")
                + "."
            );
    
            if (clip != null)
            {
                CPH.LogInfo(
                    "RTS Action Replay: CreateClip returned clip ID: "
                    + (string.IsNullOrWhiteSpace(clip.Id) ? "<empty>" : clip.Id)
                    + "."
                );
            }
        }
        catch (Exception ex)
        {
            CPH.LogError(
                "RTS Action Replay: CreateClip threw an exception: "
                + ex.Message
            );
    
            CPH.SendMessage("I couldn't create a Twitch clip.");
            return false;
        }
    
        if (clip == null || string.IsNullOrWhiteSpace(clip.Id))
        {
            CPH.LogWarn(
                "RTS Action Replay: Twitch did not return a clip ID, so there is nothing to poll. "
                + "Twitch clip creation failed before publication could be confirmed."
            );
    
            CPH.SendMessage("I couldn't create a Twitch clip.");
            return false;
        }
    
        var createdClipId = clip.Id;
    
        CPH.LogInfo(
            "RTS Action Replay: Twitch clip "
            + createdClipId
            + " is asynchronous; waiting 5 seconds before checking publication."
        );
    
        // Twitch documents Create Clip as asynchronous.
        // A clip may not appear in Get Clips immediately.
        // Poll for up to 20 seconds, then treat it as failed if Twitch still does not return the clip.
        ClipData publishedClip = null;
    
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            CPH.Wait(5000);
    
            CPH.LogInfo(
                "RTS Action Replay: checking Twitch clip "
                + createdClipId
                + " (publication check "
                + attempt
                + "/3)."
            );
    
            try
            {
                var clips = CPH.GetClips(1000, null);
    
                if (clips != null)
                {
                    for (var i = 0; i < clips.Count; i++)
                    {
                        var candidate = clips[i];
    
                        if (candidate != null &&
                            string.Equals(
                                candidate.Id,
                                createdClipId,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            publishedClip = candidate;
                            break;
                        }
                    }
                }
    
                if (publishedClip != null)
                {
                    CPH.LogInfo(
                        "RTS Action Replay: Twitch clip "
                        + createdClipId
                        + " is now published after approximately "
                        + (attempt * 5)
                        + " seconds."
                    );
    
                    break;
                }
    
                CPH.LogInfo(
                    "RTS Action Replay: Twitch clip "
                    + createdClipId
                    + " is not visible in GetClips yet."
                );
            }
            catch (Exception ex)
            {
                CPH.LogWarn(
                    "RTS Action Replay: Twitch publication check "
                    + attempt
                    + " failed for "
                    + createdClipId
                    + ": "
                    + ex.Message
                );
            }
        }
    
        if (publishedClip == null)
        {
            CPH.LogWarn(
                "RTS Action Replay: Twitch clip "
                + createdClipId
                + " was not returned by GetClips after 15 seconds. Treating creation as failed."
            );
    
            CPH.SendMessage("I couldn't confirm the Twitch clip was created.");
            return false;
        }
    
        // Use the published GetClips result for the Catalog because it contains
        // the finished clip metadata.
        clip = publishedClip;
    
        var item = AddTwitchClip(clip, true);
    
        if (item == null)
            return false;
    
        CPH.SetArgument("twitchClipId", clip.Id);
        CPH.SetArgument("twitchClipUrl", clip.Url ?? "");
        CPH.SetArgument("twitchClipTitle", (string)item["title"] ?? "");
        CPH.SetArgument("replayId", (string)item["id"] ?? "");
        CPH.SetArgument("replayTitle", (string)item["title"] ?? "");
        CPH.SetArgument("replaySource", "Twitch");
    
        if (!BroadcastReplay(item))
            return false;    
        return true;
    }
    
    public bool SyncTwitchClips()
    {
        InitializeCatalog();
        List<ClipData> clips;
        try { clips = CPH.GetClips(1000, null); }
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
            // A Catalog item may pre-date the current storage/playback setting.
            // Do not rewrite its mode; optionally create a missing local copy.
            EnsureLocalCopyIfConfigured(existing, clip.Id);
            return existing;
        }

        var mode = GetPlaybackMode();
        string localPath = null;
        if (ModeNeedsLocalCopy(mode))
            localPath = DownloadClip(clip.Id);

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
            ["file"] = string.IsNullOrWhiteSpace(localPath) ? "" : Path.GetFileName(localPath),
            ["filePath"] = localPath ?? "",
            ["sourceTypeDisplay"] = "Twitch",
            ["acquisitionMethod"] = playAfterAdd ? "TwitchCommand" : "TwitchDiscovery",
            ["plays"] = 0,
            ["users"] = new JObject()
        };

        if (ModeNeedsLocalCopy(mode) && string.IsNullOrWhiteSpace(localPath))
            CPH.LogWarn("RTS Action Replay: local Twitch copy could not be created for " + clip.Id + ". The Catalog entry retains the Twitch URL.");

        catalog.Insert(0, item);
        AddRecent(recent, (string)item["id"]);
        TrimRecent(recent);
        data["catalog"] = catalog;
        data["recentIds"] = recent;
        data["version"] = 2;
        SaveCatalog(data);
        AddLegacyReplayProjection(item);

        CPH.LogInfo("RTS Action Replay: added Twitch clip to Catalog: " + title + " (" + clip.Id + ")");
        return item;
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

    private void EnsureLocalCopyIfConfigured(JObject item, string clipId)
    {
        if (!ModeNeedsLocalCopy(GetPlaybackMode())) return;
        var current = (string)item["filePath"];
        if (!string.IsNullOrWhiteSpace(current) && File.Exists(current)) return;
        var path = DownloadClip(clipId);
        if (string.IsNullOrWhiteSpace(path)) return;
        item["file"] = Path.GetFileName(path);
        item["filePath"] = path;
        var data = LoadCatalog();
        var catalog = (JArray)data["catalog"];
        var target = FindTwitchClip(catalog, clipId);
        if (target != null)
        {
            target["file"] = Path.GetFileName(path);
            target["filePath"] = path;
            SaveCatalog(data);
        }
    }

    private string GetTwitchMediaUrl(string clipId)
    {
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                var urls = CPH.TwitchGetClipDownloadUrls(clipId);
                var url = urls == null ? null : urls.LandscapeDownloadUrl;
                if (string.IsNullOrWhiteSpace(url)) url = urls == null ? null : urls.PortraitDownloadUrl;
                if (!string.IsNullOrWhiteSpace(url)) return url;
            }
            catch (Exception ex)
            {
                CPH.LogWarn("RTS Action Replay: Twitch media URL attempt " + attempt + " failed for " + clipId + ": " + ex.Message);
            }
            if (attempt < 10) CPH.Wait(2000);
        }
        return null;
    }

    private string DownloadClip(string clipId)
    {
        var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true);
        if (string.IsNullOrWhiteSpace(folder))
        {
            CPH.LogError("RTS Action Replay: Twitch Clip Folder is not configured; cannot create a local Twitch copy.");
            return null;
        }

        var replayFolder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        if (!string.IsNullOrWhiteSpace(replayFolder) && PathsEqual(folder, replayFolder))
        {
            CPH.LogError("RTS Action Replay: Twitch Clip Folder must be different from the OBS Replay Folder.");
            return null;
        }

        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, "twitch-" + SanitizeFileName(clipId) + ".mp4");
        if (File.Exists(destination) && new FileInfo(destination).Length > 0) return destination;

        var url = GetTwitchMediaUrl(clipId);
        if (string.IsNullOrWhiteSpace(url)) return null;

        try
        {
            using (var client = new WebClient()) client.DownloadFile(url, destination + ".tmp");
            if (File.Exists(destination + ".tmp") && new FileInfo(destination + ".tmp").Length > 0)
            {
                if (File.Exists(destination)) File.Delete(destination);
                File.Move(destination + ".tmp", destination);
                return destination;
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn("RTS Action Replay: Twitch clip download failed for " + clipId + ": " + ex.Message);
        }
        try { if (File.Exists(destination + ".tmp")) File.Delete(destination + ".tmp"); } catch { }
        return null;
    }

    private bool BroadcastReplay(JObject item)
    {
        var mode = GetPlaybackMode();
        var replayUrl = ResolvePlaybackUrl(item, mode);
        if (string.IsNullOrWhiteSpace(replayUrl))
        {
            CPH.SendMessage("I couldn't get a playable Twitch clip.");
            return false;
        }

        var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.httpMapping", true) ?? "replays";
        var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
        CPH.SetArgument("replayCommand", "load");
        CPH.SetArgument("replayId", (string)item["id"]);
        CPH.SetArgument("replayTitle", (string)item["title"] ?? "Twitch Clip");
        CPH.SetArgument("replayUrl", replayUrl);
        CPH.SetArgument("replayAutoplay", true);
        CPH.SetArgument("replaySource", "Twitch");
        CPH.SetArgument("replaySourceId", (string)item["sourceId"] ?? "");
        CPH.SetArgument("replayShowControls", CPH.GetGlobalVar<bool?>("rts.actionreplay.showControls", true) ?? false);
        CPH.SetArgument("replayShowProgress", CPH.GetGlobalVar<bool?>("rts.actionreplay.showProgress", true) ?? true);
        CPH.SetArgument("replayPlaybackSpeed", GetSettingDouble("rts.actionreplay.playbackSpeed", 1.0));
        CPH.SetArgument("replayPlaybackSpeedVisibility", CPH.GetGlobalVar<string>("rts.actionreplay.playbackSpeedVisibility", true) ?? "Only when greater or less than 1");
        CPH.SetArgument("replayFrameColor", CPH.GetGlobalVar<string>("rts.actionreplay.frameColor", true) ?? "#0384CBFF");
        CPH.SetArgument("replayPositions", CPH.GetGlobalVar<string>("rts.actionreplay.positions", true) ?? "{\"Full Screen\":{\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}");
        CPH.SetArgument("replayStartPosition", GetAnimationProfileString("twitchClip", "startPosition", "Full Screen"));
        CPH.SetArgument("replayEndPosition", GetAnimationProfileString("twitchClip", "endPosition", "Full Screen"));
        CPH.SetArgument("replayAnimationDuration", GetAnimationProfileDouble("twitchClip", "duration", .5));
        CPH.SetArgument("replayAnimationEasing", GetAnimationProfileString("twitchClip", "easing", "ease-in-out"));
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
        return true;
    }

    private string GetAnimationProfileString(string profile, string field, string fallback)
    {
        var value = CPH.GetGlobalVar<string>("rts.actionreplay.animation." + profile + "." + field, true);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private double GetAnimationProfileDouble(string profile, string field, double fallback)
    {
        return GetSettingDouble("rts.actionreplay.animation." + profile + "." + field, fallback);
    }

    private string ResolvePlaybackUrl(JObject item, string mode)
    {
        if (string.Equals(mode, "Twitch URL", StringComparison.OrdinalIgnoreCase))
            return GetTwitchMediaUrl((string)item["sourceId"]);

        var localPath = (string)item["filePath"];
        if (string.IsNullOrWhiteSpace(localPath)) localPath = (string)item["file"];
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true);
            var fullPath = Path.IsPathRooted(localPath) ? localPath : Path.Combine(folder ?? "", localPath);
            if (File.Exists(fullPath))
            {
                var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.twitch.httpMapping", true) ?? "twitch";
                var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
                return "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode(Path.GetFileName(fullPath));
            }
        }

        var path = DownloadClip((string)item["sourceId"]);
        if (!string.IsNullOrWhiteSpace(path))
        {
            item["file"] = Path.GetFileName(path);
            item["filePath"] = path;
            return "http://localhost:" + (CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474) + "/" + (CPH.GetGlobalVar<string>("rts.actionreplay.twitch.httpMapping", true) ?? "twitch").Trim('/') + "/" + CPH.UrlEncode(Path.GetFileName(path));
        }

        // If a local copy is unavailable, retain a usable Twitch playback path.
        return GetTwitchMediaUrl((string)item["sourceId"]);
    }

    private void AddLegacyReplayProjection(JObject item)
    {
        var data = JObject.Parse(CPH.GetGlobalVar<string>(DataKey, true) ?? "{\"version\":1,\"replays\":[]}");
        var list = (JArray)data["replays"] ?? new JArray();
        var id = (string)item["id"];
        for (var i = 0; i < list.Count; i++) if (string.Equals((string)list[i]["id"], id, StringComparison.OrdinalIgnoreCase)) return;

        var replay = new JObject
        {
            ["id"] = id,
            ["file"] = (string)item["file"] ?? "",
            ["filePath"] = (string)item["filePath"] ?? "",
            ["title"] = (string)item["title"],
            ["customTitle"] = false,
            ["added"] = (string)item["added"],
            ["creator"] = item["creator"].DeepClone(),
            ["plays"] = 0,
            ["users"] = new JObject(),
            ["sourceType"] = "Twitch",
            ["sourceId"] = (string)item["sourceId"],
            ["externalUrl"] = (string)item["externalUrl"] ?? ""
        };
        list.Insert(0, replay);
        var max = CPH.GetGlobalVar<int?>(MaxRecentKey, true) ?? 20;
        while (list.Count > Math.Max(1, max)) list.RemoveAt(list.Count - 1);
        data["replays"] = list;
        CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
    }

    private bool CatalogContainsTwitchClip(string clipId) => FindTwitchClip(LoadCatalog()["catalog"] as JArray, clipId) != null;

    private JObject FindTwitchClip(JArray catalog, string clipId)
    {
        if (catalog == null) return null;
        for (var i = 0; i < catalog.Count; i++)
        {
            var item = catalog[i] as JObject;
            if (item != null && string.Equals((string)item["sourceType"], "Twitch", StringComparison.OrdinalIgnoreCase) && string.Equals((string)item["sourceId"], clipId, StringComparison.OrdinalIgnoreCase)) return item;
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

    private void InitializeCatalog() { LoadCatalog(); }

    private void SaveCatalog(JObject data)
    {
        CPH.SetGlobalVar(CatalogKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
        CPH.SetGlobalVar(RecentKey, ((JArray)data["recentIds"]).ToString(Newtonsoft.Json.Formatting.None), true);
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

    private string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++) for (var j = 0; j < invalid.Length; j++) if (chars[i] == invalid[j]) chars[i] = '_';
        return new string(chars);
    }

    private int GetSettingInt(string key, int fallback)
    {
        try { object value = CPH.GetGlobalVar<object>(key, true); return value == null ? fallback : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture); }
        catch { return fallback; }
    }

    private double GetSettingDouble(string key, double fallback)
    {
        try { object value = CPH.GetGlobalVar<object>(key, true); return value == null ? fallback : Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture); }
        catch { return fallback; }
    }
}
