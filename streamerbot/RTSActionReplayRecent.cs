using System;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string EventName = "RTS-Action Replay";

    public bool Execute() => PlayRecent();

    public bool PlayRecent()
    {
        var data = Load();
        var catalog = (JArray)(data["catalog"] ?? data["replays"] ?? new JArray());
        var recentIds = LoadRecentIds(data);
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
            var id = recentIds[index - 1].ToString();
            replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            replay = recentIds
                .Select(item => item.ToString())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault(x => x != null && string.Equals((string)x["title"], selector, StringComparison.OrdinalIgnoreCase));
        }

        if (replay == null)
        {
            CPH.SendMessage("Recent replay not found.");
            return false;
        }

        return PlayReplayFromCatalog(catalog, replay);
    }

    private bool PlayReplayFromCatalog(JArray catalog, JObject replay)
    {
        var url = ResolveReplayUrl(replay);
        if (string.IsNullOrWhiteSpace(url))
        {
            CPH.SendMessage($"Replay media is unavailable: {(string)replay["title"]}");
            return false;
        }

        CPH.TryGetArg("userId", out string userId);
        CPH.TryGetArg("userName", out string userName);
        var creator = replay["creator"] as JObject;

        CPH.SetArgument("replayCommand", "load");
        CPH.SetArgument("replayId", (string)replay["id"] ?? "");
        CPH.SetArgument("replayUrl", url);
        CPH.SetArgument("replayAutoplay", true);
        CPH.SetArgument("replayUserId", userId ?? "");
        CPH.SetArgument("replayUserName", userName ?? "");
        CPH.SetArgument("replayDirector", (string)creator?["name"] ?? "");
        CPH.SetArgument("replayNumber", FindCatalogIndex(catalog, replay).ToString());
        CPH.SetArgument("replayTitle", (string)replay["title"] ?? "");
        CPH.SetArgument("replayPlayedCount", (((int?)replay["plays"] ?? 0) + 1).ToString());
        CPH.SetArgument("replaySource", (string)replay["sourceType"] ?? "OBS");
        CPH.SetArgument("replaySourceId", (string)replay["sourceId"] ?? "");
        ApplyPlayerSettings();
        CPH.TriggerEvent(EventName, true);
        return true;
    }

    private string ResolveReplayUrl(JObject replay)
    {
        if (string.Equals((string)replay["sourceType"], "Twitch", StringComparison.OrdinalIgnoreCase))
        {
            var clipId = (string)replay["sourceId"];
            if (string.IsNullOrWhiteSpace(clipId)) return null;
            try
            {
                var urls = CPH.TwitchGetClipDownloadUrls(clipId);
                return urls?.LandscapeDownloadUrl ?? urls?.PortraitDownloadUrl;
            }
            catch { return null; }
        }

        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.httpMapping", true) ?? "replays";
        var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
        var file = (string)replay["file"];
        if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(file)) return null;
        if (!System.IO.File.Exists(System.IO.Path.Combine(folder, file))) return null;
        return "http://localhost:" + port + "/" + mapping.Trim('/') + "/" + CPH.UrlEncode(file);
    }

    private void ApplyPlayerSettings()
    {
        CPH.SetArgument("replayShowControls", CPH.GetGlobalVar<bool?>("rts.actionreplay.showControls", true) ?? false);
        CPH.SetArgument("replayShowProgress", CPH.GetGlobalVar<bool?>("rts.actionreplay.showProgress", true) ?? true);
        CPH.SetArgument("replayPlaybackSpeed", GetDouble("rts.actionreplay.playbackSpeed", 1.0));
        CPH.SetArgument("replayPlaybackSpeedVisibility", CPH.GetGlobalVar<string>("rts.actionreplay.playbackSpeedVisibility", true) ?? "Only when greater or less than 1");
        CPH.SetArgument("replayPositions", CPH.GetGlobalVar<string>("rts.actionreplay.positions", true) ?? "{\"Full Screen\":{\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}");
        CPH.SetArgument("replayStartPosition", CPH.GetGlobalVar<string>("rts.actionreplay.defaultStartPosition", true) ?? "Full Screen");
        CPH.SetArgument("replayEndPosition", CPH.GetGlobalVar<string>("rts.actionreplay.defaultEndPosition", true) ?? "Full Screen");
        CPH.SetArgument("replayAnimationDuration", GetDouble("rts.actionreplay.animationDuration", .5));
        CPH.SetArgument("replayAnimationEasing", CPH.GetGlobalVar<string>("rts.actionreplay.animationEasing", true) ?? "ease-in-out");
    }

    private JArray LoadRecentIds(JObject data)
    {
        var ids = data["recentIds"] as JArray;
        if (ids != null && ids.Count > 0) return ids;
        return ((JArray)data["replays"] ?? new JArray()).OfType<JObject>().Select(x => (string)x["id"]).Where(x => !string.IsNullOrWhiteSpace(x)).Take(20).Aggregate(new JArray(), (a, x) => { a.Add(x); return a; });
    }

    private int FindCatalogIndex(JArray catalog, JObject replay)
    {
        for (var i = 0; i < catalog.Count; i++) if (ReferenceEquals(catalog[i], replay)) return i + 1;
        return 1;
    }

    private JObject Load() => JObject.Parse(CPH.GetGlobalVar<string>(DataKey, true) ?? "{\"version\":1,\"replays\":[]}");
    private double GetDouble(string key, double fallback) { try { object value = CPH.GetGlobalVar<object>(key, true); return value == null ? fallback : Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture); } catch { return fallback; } }
}