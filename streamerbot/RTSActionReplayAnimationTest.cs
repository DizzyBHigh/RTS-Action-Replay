using System;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

// Developer/test action: play a Recent catalog item using any animation profile.
// This deliberately does not modify the catalog or recent history.
public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string EventName = "RTS-Action Replay";
    private const string TwitchFolderKey = "rts.actionreplay.twitch.folder";
    private const string TwitchMappingKey = "rts.actionreplay.twitch.httpMapping";
    private const string TwitchModeKey = "rts.actionreplay.twitch.playbackMode";

    public bool Execute() => TestAnimationProfile();

    public bool TestAnimationProfile()
    {
        CPH.TryGetArg("rawInput", out string rawInput);
        var input = (rawInput ?? "").Trim();
        var parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var profile = parts.Length > 0 ? parts[0].Trim() : "recent";
        var selector = parts.Length > 1 ? parts[1].Trim() : "1";

        profile = NormalizeProfile(profile);
        if (profile == null)
        {
            CPH.SendMessage("Usage: !test-animation [default|twitch|obs|playlist|recent] [recent-number]");
            return false;
        }

        var data = Load();
        var catalog = (JArray)data["catalog"] ?? new JArray();
        var recentIds = (JArray)data["recentIds"] ?? new JArray();
        if (recentIds.Count == 0)
        {
            CPH.SendMessage("There are no recent replays to test.");
            return false;
        }

        JObject replay = null;
        if (int.TryParse(selector, out var index) && index > 0 && index <= recentIds.Count)
        {
            var id = Convert.ToString(recentIds[index - 1]);
            replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals(Convert.ToString(x["id"]), id, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            replay = recentIds.Select(item => Convert.ToString(item))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => catalog.OfType<JObject>().FirstOrDefault(x => string.Equals(Convert.ToString(x["id"]), id, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault(x => x != null && string.Equals(Convert.ToString(x["title"]), selector, StringComparison.OrdinalIgnoreCase));
        }

        if (replay == null)
        {
            CPH.SendMessage("Recent replay not found.");
            return false;
        }

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
        CPH.SetArgument("replayNumber", FindCatalogIndex(catalog, replay).ToString());
        CPH.SetArgument("replayTitle", Convert.ToString(replay["title"]));
        CPH.SetArgument("replayPlayedCount", (((int?)replay["plays"] ?? 0) + 1).ToString());
        CPH.SetArgument("replaySource", Convert.ToString(replay["sourceType"]) ?? "OBS");
        CPH.SetArgument("replaySourceId", Convert.ToString(replay["sourceId"]));
        ApplyPlayerSettings(profile);

        CPH.LogInfo("RTS Action Replay: testing animation profile '" + profile + "' with recent replay #" + selector + " (" + Convert.ToString(replay["title"]) + ").");
        CPH.TriggerEvent(EventName, true);
        return true;
    }

    private string NormalizeProfile(string value)
    {
        var profile = (value ?? "").Trim().ToLowerInvariant();
        if (profile == "default") return "default";
        if (profile == "twitch" || profile == "twitchclip") return "twitchClip";
        if (profile == "obs" || profile == "obsclip") return "obsClip";
        if (profile == "playlist") return "playlist";
        if (profile == "recent") return "recent";
        return null;
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
                if (!string.IsNullOrWhiteSpace(downloaded)) return BuildTwitchHttpUrl(Path.GetFileName(downloaded));
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
            catch (Exception ex) { CPH.LogWarn("RTS Action Replay: animation test Twitch download attempt " + attempt + " failed: " + ex.Message); }
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
        CPH.SetArgument("replayTitleDelay", GetInt("rts.actionreplay.titleDelay", 0));
        CPH.SetArgument("replayTitleDuration", GetInt("rts.actionreplay.titleDuration", 5000));
        CPH.SetArgument("replayTitleAnimationDuration", GetInt("rts.actionreplay.titleAnimationDuration", 450));
        CPH.SetArgument("replayTitleFont", CPH.GetGlobalVar<string>("rts.actionreplay.titleFont", true) ?? "Inter");
        CPH.SetArgument("replayTitleFontSize", GetInt("rts.actionreplay.titleFontSize", 34));
        CPH.SetArgument("replayTitleTextColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleTextColor", true) ?? "#FFFFFFFF");
        CPH.SetArgument("replayTitleShadowColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleShadowColor", true) ?? "#000000FF");
        CPH.SetArgument("replayTitlePrimaryColor", CPH.GetGlobalVar<string>("rts.actionreplay.titlePrimaryColor", true) ?? "#0384CBFF");
        CPH.SetArgument("replayTitleSecondaryColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleSecondaryColor", true) ?? "#101416FF");
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
        data["version"] = 2;
        data["catalog"] = data["catalog"] as JArray ?? new JArray();
        data["recentIds"] = data["recentIds"] as JArray ?? new JArray();
        return data;
    }

    private double GetDouble(string key, double fallback)
    {
        try { object value = CPH.GetGlobalVar<object>(key, true); return value == null ? fallback : Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture); }
        catch { return fallback; }
    }

    private int GetInt(string key, int fallback)
    {
        try { object value = CPH.GetGlobalVar<object>(key, true); return value == null ? fallback : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture); }
        catch { return fallback; }
    }
}
