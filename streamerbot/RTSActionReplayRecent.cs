using System;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string LegacyCatalogKey = "rts.actionreplay.catalog";
    private const string EventName = "RTS-Action Replay";
    private const string TwitchFolderKey = "rts.actionreplay.twitch.folder";
    private const string TwitchMappingKey = "rts.actionreplay.twitch.httpMapping";
    private const string TwitchModeKey = "rts.actionreplay.twitch.playbackMode";

    public bool Execute() => PlayRecent();

    public bool PlayRecent()
    {
        var data = Load();
        var catalog = (JArray)data["catalog"] ?? new JArray();
        var recentIds = (JArray)data["recentIds"] ?? new JArray();
        if (recentIds.Count == 0)
        {
            CPH.SendMessage("There are no recent replays.");
            return false;
        }

        var selector = "1";
        CPH.TryGetArg("rawInput", out string rawInput);
        if (!string.IsNullOrWhiteSpace(rawInput)) selector = rawInput.Trim();

        JObject replay = null;
        if (int.TryParse(selector, out var index) && index > 0 && index <= recentIds.Count)
        {
            var id = Convert.ToString(recentIds[index - 1]);
            replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals(Convert.ToString(x["id"]), id, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            replay = recentIds.Select(item => Convert.ToString(item)).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => catalog.OfType<JObject>().FirstOrDefault(x => string.Equals(Convert.ToString(x["id"]), id, StringComparison.OrdinalIgnoreCase))).FirstOrDefault(x => x != null && string.Equals(Convert.ToString(x["title"]), selector, StringComparison.OrdinalIgnoreCase));
        }

        if (replay == null)
        {
            CPH.SendMessage("Recent replay not found.");
            return false;
        }

        return PlayReplayFromCatalog(data, replay);
    }

    private bool PlayReplayFromCatalog(JObject data, JObject replay)
    {
        var url = ResolveReplayUrl(replay);
        if (string.IsNullOrWhiteSpace(url))
        {
            CPH.SendMessage($"Replay media is unavailable: {Convert.ToString(replay["title"])}");
            return false;
        }

        CPH.TryGetArg("userId", out string userId);
        CPH.TryGetArg("userName", out string userName);
        var creator = replay["creator"] as JObject;
        CPH.SetArgument("replayCommand", "load");
        CPH.SetArgument("replayId", Convert.ToString(replay["id"]));
        CPH.SetArgument("replayUrl", url);
        CPH.SetArgument("replayAutoplay", true);
        CPH.SetArgument("replayUserId", userId ?? "");
        CPH.SetArgument("replayUserName", userName ?? "");
        CPH.SetArgument("replayDirector", Convert.ToString(creator?["name"]));
        CPH.SetArgument("replayNumber", FindCatalogIndex((JArray)data["catalog"], replay).ToString());
        CPH.SetArgument("replayTitle", Convert.ToString(replay["title"]));
        CPH.SetArgument("replayPlayedCount", (((int?)replay["plays"] ?? 0) + 1).ToString());
        CPH.SetArgument("replaySource", Convert.ToString(replay["sourceType"]) ?? "OBS");
        CPH.SetArgument("replaySourceId", Convert.ToString(replay["sourceId"]));
        ApplyPlayerSettings("recent");
        CPH.TriggerEvent(EventName, true);
        return true;
    }

    private string ResolveReplayUrl(JObject replay)
    {
        if (string.Equals(Convert.ToString(replay["sourceType"]), "Twitch", StringComparison.OrdinalIgnoreCase))
        {
            var clipId = Convert.ToString(replay["sourceId"]);
            if (string.IsNullOrWhiteSpace(clipId)) return null;
            var mode = CPH.GetGlobalVar<string>(TwitchModeKey, true) ?? "Download Locally";
            if (!string.Equals(mode, "Twitch URL", StringComparison.OrdinalIgnoreCase))
            {
                var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true);
                var localPath = Convert.ToString(replay["filePath"]);
                if (string.IsNullOrWhiteSpace(localPath)) localPath = Convert.ToString(replay["file"]);
                if (!string.IsNullOrWhiteSpace(localPath))
                {
                    var fullPath = Path.IsPathRooted(localPath) ? localPath : Path.Combine(folder ?? "", localPath);
                    if (File.Exists(fullPath)) return BuildTwitchHttpUrl(Path.GetFileName(fullPath));
                }

                var downloaded = DownloadTwitchClip(clipId);
                if (!string.IsNullOrWhiteSpace(downloaded))
                {
                    replay["file"] = Path.GetFileName(downloaded);
                    replay["filePath"] = downloaded;
                    SaveCurrentCatalog(replay);
                    return BuildTwitchHttpUrl(Path.GetFileName(downloaded));
                }
            }

            try
            {
                var urls = CPH.TwitchGetClipDownloadUrls(clipId);
                return urls?.LandscapeDownloadUrl ?? urls?.PortraitDownloadUrl;
            }
            catch { return null; }
        }

        var obsFolder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.httpMapping", true) ?? "replays";
        var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
        var file = Convert.ToString(replay["file"]);
        if (string.IsNullOrWhiteSpace(obsFolder) || string.IsNullOrWhiteSpace(file)) return null;
        if (!File.Exists(Path.Combine(obsFolder, file))) return null;
        return "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode(file);
    }

    private string BuildTwitchHttpUrl(string fileName)
    {
        var mapping = CPH.GetGlobalVar<string>(TwitchMappingKey, true) ?? "twitch";
        var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
        return "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode(fileName ?? "");
    }

    private string DownloadTwitchClip(string clipId)
    {
        var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true);
        var replayFolder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        if (string.IsNullOrWhiteSpace(folder)) return null;
        if (!string.IsNullOrWhiteSpace(replayFolder) && PathsEqual(folder, replayFolder)) return null;
        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, "twitch-" + Sanitize(clipId) + ".mp4");
        if (File.Exists(destination) && new FileInfo(destination).Length > 0) return destination;
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                var urls = CPH.TwitchGetClipDownloadUrls(clipId);
                var url = urls?.LandscapeDownloadUrl ?? urls?.PortraitDownloadUrl;
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
            catch (Exception ex) { CPH.LogWarn("RTS Action Replay: recent Twitch download attempt " + attempt + " failed for " + clipId + ": " + ex.Message); }
            if (attempt < 10) CPH.Wait(2000);
        }
        return null;
    }

    private bool PathsEqual(string a, string b)
    {
        try { return string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase); }
        catch { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); }
    }

    private string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars(); var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++) for (var j = 0; j < invalid.Length; j++) if (chars[i] == invalid[j]) chars[i] = '_';
        return new string(chars);
    }

    private void SaveCurrentCatalog(JObject replay)
    {
        var data = Load();
        var catalog = (JArray)data["catalog"];
        var target = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], (string)replay["id"], StringComparison.OrdinalIgnoreCase));
        if (target == null) return;
        target["file"] = replay["file"];
        target["filePath"] = replay["filePath"];
        Save(data);
    }

    private void ApplyPlayerSettings(string profile)
    {
        CPH.SetArgument("replayShowControls", CPH.GetGlobalVar<bool?>("rts.actionreplay.showControls", true) ?? false);
        CPH.SetArgument("replayShowProgress", CPH.GetGlobalVar<bool?>("rts.actionreplay.showProgress", true) ?? true);
        CPH.SetArgument("replayPlaybackSpeed", GetDouble("rts.actionreplay.playbackSpeed", 1.0));
        CPH.SetArgument("replayPlaybackSpeedVisibility", CPH.GetGlobalVar<string>("rts.actionreplay.playbackSpeedVisibility", true) ?? "Only when greater or less than 1");
        CPH.SetArgument("replayPositions", CPH.GetGlobalVar<string>("rts.actionreplay.positions", true) ?? "{\"Full Screen\":{\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}");
        CPH.SetArgument("replayStartPosition", GetAnimationProfileString(profile, "startPosition", "Full Screen"));
        CPH.SetArgument("replayEndPosition", GetAnimationProfileString(profile, "endPosition", "Full Screen"));
        CPH.SetArgument("replayAnimationDuration", GetDouble("rts.actionreplay.animation." + profile + ".duration", .5));
        CPH.SetArgument("replayAnimationEasing", GetAnimationProfileString(profile, "easing", "ease-in-out"));
    }

    private string GetAnimationProfileString(string profile, string field, string fallback)
    {
        var value = CPH.GetGlobalVar<string>("rts.actionreplay.animation." + profile + "." + field, true);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private int FindCatalogIndex(JArray catalog, JObject replay)
    {
        for (var i = 0; i < catalog.Count; i++) if (ReferenceEquals(catalog[i], replay)) return i + 1;
        return 1;
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

    private void MergeCatalog(JArray target, JArray source)
    {
        foreach (var token in source)
        {
            var item = token as JObject; if (item == null) continue;
            var id = (string)item["id"]; var sourceType = (string)item["sourceType"] ?? "OBS"; var sourceId = (string)item["sourceId"];
            var exists = target.OfType<JObject>().Any(x => (!string.IsNullOrWhiteSpace(id) && string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrWhiteSpace(sourceId) && string.Equals((string)x["sourceType"] ?? "OBS", sourceType, StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceId"], sourceId, StringComparison.OrdinalIgnoreCase)));
            if (exists) continue;
            var clone = (JObject)item.DeepClone(); if (string.IsNullOrWhiteSpace((string)clone["sourceType"])) clone["sourceType"] = "OBS"; if (string.IsNullOrWhiteSpace((string)clone["sourceId"])) clone["sourceId"] = (string)clone["id"] ?? ""; if (clone["plays"] == null) clone["plays"] = 0; if (clone["users"] == null) clone["users"] = new JObject(); target.Add(clone);
        }
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

    private double GetDouble(string key, double fallback)
    {
        try { object value = CPH.GetGlobalVar<object>(key, true); return value == null ? fallback : Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture); }
        catch { return fallback; }
    }
}