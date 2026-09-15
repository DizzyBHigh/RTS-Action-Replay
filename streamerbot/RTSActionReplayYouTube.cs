using System;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string MaxRecentKey = "rts.actionreplay.maxHistory";
    private const string YouTubeBroadcastIdKey = "rts.actionreplay.youtube.broadcastId";
    private const string YouTubeStartTimeKey = "rts.actionreplay.youtube.actualStartTime";
    private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";
    private const string AnimationAction = "RTS - Action Replay - Core - Animation";

    public bool Execute() => CreateYouTubeClip();

    public bool BroadcastStarted()
    {
        var broadcastId = Arg("broadcast.id").Trim();
        if (string.IsNullOrWhiteSpace(broadcastId))
        {
            CPH.LogWarn("RTS Action Replay: YouTube Broadcast Started event did not provide broadcast.id.");
            return false;
        }

        var startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CPH.SetGlobalVar(YouTubeBroadcastIdKey, broadcastId, true);
        CPH.SetGlobalVar(YouTubeStartTimeKey, startTime, true);
        CPH.LogInfo($"RTS Action Replay: YouTube broadcast {broadcastId} started at Unix timestamp {startTime}.");
        return true;
    }

    public bool CreateYouTubeClip()
    {
        var duration = Math.Max(5, Math.Min(60, GetSettingInt("rts.actionreplay.youtube.clipDuration", 30)));
        var rawInput = Arg("rawInput").Trim();
        var title = "YouTube Clip";
        if (!string.IsNullOrWhiteSpace(rawInput))
        {
            var parts = rawInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1 || !int.TryParse(parts[0], out var requested))
            {
                SendOriginMessage("Usage: !create-clip [5-60] [title]");
                return false;
            }
            duration = Math.Max(5, Math.Min(60, requested));
            if (parts.Length > 1) title = string.Join(" ", parts.Skip(1));
        }

        var videoId = Arg("broadcast.id").Trim();
        if (string.IsNullOrWhiteSpace(videoId)) videoId = GetGlobalString("broadcast.id");
        if (string.IsNullOrWhiteSpace(videoId)) { SendOriginMessage("I couldn't determine the current YouTube stream."); return false; }
        if (!TryGetStartTime(videoId, out var startTime))
        {
            SendOriginMessage("I couldn't determine when the current YouTube stream started.");
            return false;
        }

        var data = Load();
        var catalog = (JArray)(data["catalog"] ?? new JArray());
        var id = "youtube-" + videoId + "-" + startTime + "-" + duration;
        var existing = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase));
        if (existing != null) return Broadcast(existing);
        var creatorPlatform = NormalizePlatform(Arg("userType"));
        var now = DateTime.Now;
        var item = new JObject
        {
            ["id"] = id, ["sourceType"] = "YouTube", ["sourceId"] = videoId,
            ["sourceUrl"] = "https://youtu.be/" + videoId,
            ["startTime"] = startTime, ["duration"] = duration,
            ["title"] = title, ["customTitle"] = title != "YouTube Clip",
            ["added"] = now.ToString("o"), ["captured"] = now.ToString("o"), ["acquisitionMethod"] = "YouTubeCommand",
            ["creator"] = new JObject { ["platform"] = creatorPlatform, ["id"] = Arg("userId"), ["name"] = Arg("userName") },
            ["plays"] = 0, ["users"] = new JObject()
        };
        catalog.Insert(0, item); data["catalog"] = catalog; AddRecent(data, id); Save(data);
        CPH.LogInfo($"RTS Action Replay: added YouTube timestamp replay {id} ({startTime}s + {duration}s) title='{title}'.");
        return Broadcast(item);
    }

    private bool TryGetStartTime(string videoId, out long startTime)
    {
        startTime = 0;
        var storedId = GetGlobalString(YouTubeBroadcastIdKey);
        var actualStart = GetGlobalLong(YouTubeStartTimeKey);
        if (!string.Equals(storedId, videoId, StringComparison.OrdinalIgnoreCase) || actualStart <= 0) return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        startTime = now - actualStart;
        return startTime >= 0;
    }

    private bool Broadcast(JObject item)
    {
        CPH.SetGlobalVar("rts.actionreplay.handoff.replayId", (string)item["id"] ?? "", false);
        CPH.SetGlobalVar("rts.actionreplay.handoff.entryPoint", "youtube", false);
        CPH.UnsetGlobalVar("rts.actionreplay.handoff.resolvedProfile", false);
        if (!CPH.ExecuteMethod(AnimationAction, "ResolveEntryPointProfile")) return false;
        return CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay");
    }

    private void AddRecent(JObject data, string id)
    {
        var recent = (JArray)(data["recentIds"] ?? new JArray());
        for (var i = recent.Count - 1; i >= 0; i--) if (string.Equals((string)recent[i], id, StringComparison.OrdinalIgnoreCase)) recent.RemoveAt(i);
        recent.Insert(0, id);
        var max = Math.Max(1, CPH.GetGlobalVar<int?>(MaxRecentKey, true) ?? 20);
        while (recent.Count > max) recent.RemoveAt(recent.Count - 1);
        data["recentIds"] = recent;
    }

    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true);
        if (string.IsNullOrWhiteSpace(raw)) return new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() };
        try { return JObject.Parse(raw); }
        catch { return new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() }; }
    }

    private void Save(JObject data) { data["version"] = 2; data["catalog"] = data["catalog"] as JArray ?? new JArray(); data["recentIds"] = data["recentIds"] as JArray ?? new JArray(); CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true); CPH.SetGlobalVar("rts.actionreplay.recentIds", ((JArray)data["recentIds"]).ToString(Newtonsoft.Json.Formatting.None), true); }
    private int GetSettingInt(string key, int fallback) { try { return CPH.GetGlobalVar<int?>(key, true) ?? fallback; } catch { return fallback; } }
    private string GetGlobalString(string key) { try { return CPH.GetGlobalVar<string>(key, true) ?? ""; } catch { return ""; } }
    private long GetGlobalLong(string key) { try { return CPH.GetGlobalVar<long?>(key, true) ?? 0L; } catch { return 0L; } }
    private string NormalizePlatform(string userType) => string.Equals(userType, "YouTube", StringComparison.OrdinalIgnoreCase) ? "YouTube" : string.Equals(userType, "Kick", StringComparison.OrdinalIgnoreCase) ? "Kick" : "Twitch";
    private string Arg(string name) { CPH.TryGetArg(name, out string value); return value ?? ""; }

    private void SendOriginMessage(string message)
    {
        var platform = Arg("userType");
        if (string.Equals(platform, "Kick", StringComparison.OrdinalIgnoreCase))
        {
            CPH.SendKickMessage(message);
            return;
        }
        if (string.Equals(platform, "YouTube", StringComparison.OrdinalIgnoreCase))
        {
            CPH.SendYouTubeMessageToLatestMonitored(message);
            return;
        }
        CPH.SendMessage(message);
    }
}
