using System;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string PendingKey = "rts.actionreplay.pendingSaves";
    private const string EventName = "RTS-Action Replay";
    private const string TwitchFolderKey = "rts.actionreplay.twitch.folder";
    private const string TwitchMappingKey = "rts.actionreplay.twitch.httpMapping";
    private const string TwitchModeKey = "rts.actionreplay.twitch.playbackMode";

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
        var data = Load(); var list = (JArray)data["replays"];
        if (!CPH.TryGetArg("rawInput", out string selector) || string.IsNullOrWhiteSpace(selector)) return false;
        selector = selector.Trim(); JObject replay = null;
        if (int.TryParse(selector, out var index) && index > 0 && index <= list.Count) replay = (JObject)list[index - 1];
        else replay = list.OfType<JObject>().FirstOrDefault(x => ((bool?)x["customTitle"] ?? false) && string.Equals((string)x["title"], selector, StringComparison.OrdinalIgnoreCase));
        if (replay == null) { CPH.SendMessage("Replay not found."); return false; }

        var url = ResolveReplayUrl(replay);
        if (string.IsNullOrWhiteSpace(url))
        {
            CPH.SendMessage($"Replay media is unavailable: {(string)replay["title"]}");
            return false;
        }

        CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName);
        var creator = replay["creator"] as JObject; var creatorName = (string)creator?["name"] ?? "";
        CPH.SetArgument("replayCommand", "load");
        CPH.SetArgument("replayId", (string)replay["id"]);
        CPH.SetArgument("replayUrl", url);
        CPH.SetArgument("replayAutoplay", true);
        CPH.SetArgument("replayUserId", userId ?? "");
        CPH.SetArgument("replayUserName", userName ?? "");
        CPH.SetArgument("replayDirector", creatorName);
        CPH.SetArgument("replayNumber", Array.IndexOf(list.ToArray(), replay) + 1);
        CPH.SetArgument("replayTitle", (string)replay["title"] ?? "");
        CPH.SetArgument("replayPlayedCount", ((int?)replay["plays"] ?? 0) + 1);
        CPH.SetArgument("replaySource", (string)replay["sourceType"] ?? "OBS");
        CPH.SetArgument("replaySourceId", (string)replay["sourceId"] ?? "");
        ApplyPlayerSettings("playlist"); CPH.TriggerEvent(EventName, true); SendMessage("play"); return true;
    }

    private string ResolveReplayUrl(JObject replay)
    {
        if (string.Equals((string)replay["sourceType"], "Twitch", StringComparison.OrdinalIgnoreCase))
            return ResolveTwitchUrl(replay);

        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.httpMapping", true) ?? "replays";
        var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
        var file = (string)replay["file"];
        var path = Path.Combine(folder ?? "", file ?? "");
        if (!File.Exists(path)) return null;
        return "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode(file ?? "");
    }

    private string ResolveTwitchUrl(JObject replay)
    {
        var mode = GetTwitchPlaybackMode();
        var clipId = (string)replay["sourceId"];
        if (string.IsNullOrWhiteSpace(clipId)) return null;

        if (string.Equals(mode, "Twitch URL", StringComparison.OrdinalIgnoreCase))
            return GetTwitchMediaUrl(clipId);

        var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true);
        var localPath = (string)replay["filePath"];
        if (string.IsNullOrWhiteSpace(localPath)) localPath = (string)replay["file"];
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
            return BuildTwitchHttpUrl(Path.GetFileName(downloaded));
        }

        // Local modes can still play if the temporary Twitch media URL is available.
        return GetTwitchMediaUrl(clipId);
    }

    private string BuildTwitchHttpUrl(string fileName)
    {
        var mapping = CPH.GetGlobalVar<string>(TwitchMappingKey, true) ?? "twitch";
        var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
        return "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode(fileName ?? "");
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
            catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Twitch media URL attempt " + attempt + " failed for " + clipId + ": " + ex.Message); }
            if (attempt < 10) CPH.Wait(2000);
        }
        return null;
    }

    private string DownloadTwitchClip(string clipId)
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
        catch (Exception ex) { CPH.LogWarn("RTS Action Replay: Twitch clip download failed for " + clipId + ": " + ex.Message); }
        try { if (File.Exists(destination + ".tmp")) File.Delete(destination + ".tmp"); } catch { }
        return null;
    }

    public bool SetPlayerPosition() { if (!CPH.TryGetArg("rawInput", out string position) || string.IsNullOrWhiteSpace(position)) return false; ApplyPlayerSettings("default"); CPH.SetArgument("replayCommand", "move"); CPH.SetArgument("replayPosition", position.Trim()); CPH.TriggerEvent(EventName, true); return true; }
    public bool HidePlayer() { ApplyPlayerSettings("default"); CPH.SetArgument("replayCommand", "hide"); CPH.TriggerEvent(EventName, true); return true; }

    public bool ConfirmPlayback()
    {
        if (!CPH.TryGetArg("replayId", out string replayId)) return false; var data = Load(); var replay = ((JArray)data["replays"]).OfType<JObject>().FirstOrDefault(x => (string)x["id"] == replayId); if (replay == null) return false;
        replay["plays"] = ((int?)replay["plays"] ?? 0) + 1;
        if (CPH.TryGetArg("userId", out string userId) && !string.IsNullOrWhiteSpace(userId)) { var name = CPH.TryGetArg("userName", out string userName) ? userName : userId; var users = (JObject)(replay["users"] ?? new JObject()); replay["users"] = users; var user = (JObject)(users[userId] ?? new JObject { ["name"] = name, ["plays"] = 0 }); user["name"] = string.IsNullOrWhiteSpace(name) ? (string)user["name"] : name; user["plays"] = ((int?)user["plays"] ?? 0) + 1; users[userId] = user; }
        Save(data); return true;
    }

    private string GetTwitchPlaybackMode()
    {
        var mode = CPH.GetGlobalVar<string>(TwitchModeKey, true);
        if (string.Equals(mode, "Twitch URL", StringComparison.OrdinalIgnoreCase)) return "Twitch URL";
        if (string.Equals(mode, "Both", StringComparison.OrdinalIgnoreCase)) return "Both";
        return "Download Locally";
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
        CPH.SetArgument("replayShowControls", CPH.GetGlobalVar<bool?>("rts.actionreplay.showControls", true) ?? false); CPH.SetArgument("replayShowProgress", CPH.GetGlobalVar<bool?>("rts.actionreplay.showProgress", true) ?? true); CPH.SetArgument("replayPlaybackSpeed", GetSettingDouble("rts.actionreplay.playbackSpeed", 1.0)); CPH.SetArgument("replayPlaybackSpeedVisibility", CPH.GetGlobalVar<string>("rts.actionreplay.playbackSpeedVisibility", true) ?? "Only when greater or less than 1");
        CPH.SetArgument("replayFrameColor", CPH.GetGlobalVar<string>("rts.actionreplay.frameColor", true) ?? "#0384CBFF"); CPH.SetArgument("replayBorderWidth", GetSettingInt("rts.actionreplay.borderWidth", 4)); CPH.SetArgument("replayCornerRadius", GetSettingInt("rts.actionreplay.cornerRadius", 0)); CPH.SetArgument("replayBorderGlow", CPH.GetGlobalVar<bool?>("rts.actionreplay.borderGlow", true) ?? true);
        CPH.SetArgument("replayPositions", CPH.GetGlobalVar<string>("rts.actionreplay.positions", true) ?? "{\"Full Screen\":{\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}"); CPH.SetArgument("replayStartPosition", GetAnimationProfileString(profile, "startPosition", "Full Screen")); CPH.SetArgument("replayEndPosition", GetAnimationProfileString(profile, "endPosition", "Full Screen"));
        CPH.SetArgument("replayAnimationDuration", GetAnimationProfileDouble(profile, "duration", .5)); CPH.SetArgument("replayAnimationEasing", GetAnimationProfileString(profile, "easing", "ease-in-out"));
        CPH.SetArgument("replayShowBranding", CPH.GetGlobalVar<bool?>("rts.actionreplay.showBranding", true) ?? true); CPH.SetArgument("replayBrandLogoUrl", CPH.GetGlobalVar<string>("rts.actionreplay.brandLogoUrl", true) ?? ""); CPH.SetArgument("replayBrandFallbackText", CPH.GetGlobalVar<string>("rts.actionreplay.brandFallbackText", true) ?? "RTS"); CPH.SetArgument("replayBrandFallbackTextColor", CPH.GetGlobalVar<string>("rts.actionreplay.brandFallbackTextColor", true) ?? "#0384CBFF"); CPH.SetArgument("replayBrandLabel", CPH.GetGlobalVar<string>("rts.actionreplay.brandLabel", true) ?? "ACTION REPLAY"); CPH.SetArgument("replayBrandLabelColor", CPH.GetGlobalVar<string>("rts.actionreplay.brandLabelColor", true) ?? "#FFFFFFFF");
        CPH.SetArgument("replayShowTitle", CPH.GetGlobalVar<bool?>("rts.actionreplay.showTitle", true) ?? true); CPH.SetArgument("replayTitleDecorationPosition", CPH.GetGlobalVar<string>("rts.actionreplay.titleDecorationPosition", true) ?? "Suffix"); CPH.SetArgument("replayTitleDecoration", CPH.GetGlobalVar<string>("rts.actionreplay.titleDecoration", true) ?? " - Replay Capture"); CPH.SetArgument("replayTitleStyle", CPH.GetGlobalVar<string>("rts.actionreplay.titleBarStyle", true) ?? "Broadcast"); CPH.SetArgument("replayTitlePosition", CPH.GetGlobalVar<string>("rts.actionreplay.titlePosition", true) ?? "Bottom");
        CPH.SetArgument("replayTitleAnimation", CPH.GetGlobalVar<string>("rts.actionreplay.titleAnimation", true) ?? "Slide up/down"); CPH.SetArgument("replayTitleDelay", GetSettingInt("rts.actionreplay.titleDelay", 0)); CPH.SetArgument("replayTitleDuration", GetSettingInt("rts.actionreplay.titleDuration", 5000)); CPH.SetArgument("replayTitleAnimationDuration", GetSettingInt("rts.actionreplay.titleAnimationDuration", 450));
        CPH.SetArgument("replayTitleFont", CPH.GetGlobalVar<string>("rts.actionreplay.titleFont", true) ?? "Inter"); CPH.SetArgument("replayTitleFontSize", GetSettingInt("rts.actionreplay.titleFontSize", 34)); CPH.SetArgument("replayTitleTextColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleTextColor", true) ?? "#FFFFFFFF"); CPH.SetArgument("replayTitleShadowColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleShadowColor", true) ?? "#000000FF"); CPH.SetArgument("replayTitlePrimaryColor", CPH.GetGlobalVar<string>("rts.actionreplay.titlePrimaryColor", true) ?? "#0384CBFF"); CPH.SetArgument("replayTitleSecondaryColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleSecondaryColor", true) ?? "#101416FF");
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

    private void SendMessage(string type)
    {
        var key = "rts.actionreplay.message." + type; var text = CPH.GetGlobalVar<string>(key + ".text", true); if (string.IsNullOrWhiteSpace(text)) return; text = CPH.Parse(text); if (CPH.GetGlobalVar<bool?>(key + ".chat", true) ?? true) CPH.SendMessage(text); if (!(CPH.GetGlobalVar<bool?>(key + ".overlay", true) ?? false)) return;
        CPH.SetArgument("replayCommand", "message"); CPH.SetArgument("replayMessage", text); CPH.SetArgument("replayLogoUrl", CPH.GetGlobalVar<string>("rts.actionreplay.brandLogoUrl", true) ?? ""); CPH.SetArgument("replayMessageBoardColor", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.boardColor", true) ?? "#101416"); CPH.SetArgument("replayMessageStripeLight", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.stripeLight", true) ?? "#EEEEEE"); CPH.SetArgument("replayMessageStripeDark", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.stripeDark", true) ?? "#111111"); CPH.SetArgument("replayMessageAccent", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.accent", true) ?? "#0384CB"); CPH.SetArgument("replayMessageTextColor", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.textColor", true) ?? "#0384CB"); CPH.SetArgument("replayMessageFont", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.font", true) ?? "Arial, sans-serif"); CPH.SetArgument("replayMessageSize", GetSettingInt("rts.actionreplay.clapper.size", 100)); CPH.SetArgument("replayMessagePositionX", GetSettingInt("rts.actionreplay.clapper.positionX", 50)); CPH.SetArgument("replayMessagePositionY", GetSettingInt("rts.actionreplay.clapper.positionY", 50)); CPH.TriggerEvent(EventName, true);
    }

    private int GetSettingInt(string key, int fallback) { try { return Convert.ToInt32(CPH.GetGlobalVar<object>(key, true), System.Globalization.CultureInfo.InvariantCulture); } catch { return fallback; } }
    private double GetSettingDouble(string key, double fallback) { try { return Convert.ToDouble(CPH.GetGlobalVar<object>(key, true), System.Globalization.CultureInfo.InvariantCulture); } catch { return fallback; } }
    private JObject Load() => JObject.Parse(CPH.GetGlobalVar<string>(DataKey, true) ?? "{\"version\":1,\"replays\":[]}");
    private void Save(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
}
