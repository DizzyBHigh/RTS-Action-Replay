using System;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string MaxRecentKey = "rts.actionreplay.maxHistory";
    private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";
    private const string AnimationAction = "RTS - Action Replay - Core - Animation";

    public bool Execute() => CreateYouTubeClip();

    public bool CreateYouTubeClip()
    {
        var duration = Math.Max(5, Math.Min(60, GetSettingInt("rts.actionreplay.youtube.clipDuration", 30)));
        var rawInput = Arg("rawInput").Trim();
        if (!string.IsNullOrWhiteSpace(rawInput))
        {
            var parts = rawInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 1 || !int.TryParse(parts[0], out var requested)) { CPH.SendMessage("Usage: !Create-clip [5-60]"); return false; }
            duration = Math.Max(5, Math.Min(60, requested));
        }

        var videoId = Arg("broadcastId").Trim();
        if (string.IsNullOrWhiteSpace(videoId)) { CPH.SendMessage("I couldn't determine the current YouTube stream."); return false; }
        var startTime = CPH.GetGlobalVar<long?>("streamTimeSeconds", false) ?? 0;
        if (startTime < 0) { CPH.SendMessage("I couldn't determine the current YouTube timestamp."); return false; }

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
            ["title"] = "YouTube Clip", ["customTitle"] = false,
            ["added"] = now.ToString("o"), ["captured"] = now.ToString("o"), ["acquisitionMethod"] = "YouTubeCommand",
            ["creator"] = new JObject { ["platform"] = creatorPlatform, ["id"] = Arg("userId"), ["name"] = Arg("userName") },
            ["plays"] = 0, ["users"] = new JObject()
        };
        catalog.Insert(0, item); data["catalog"] = catalog; AddRecent(data, id); Save(data);
        CPH.LogInfo($"RTS Action Replay: added YouTube timestamp replay {id} ({startTime}s + {duration}s).");
        return Broadcast(item);
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
    private string NormalizePlatform(string userType) => string.Equals(userType, "YouTube", StringComparison.OrdinalIgnoreCase) ? "YouTube" : string.Equals(userType, "Kick", StringComparison.OrdinalIgnoreCase) ? "Kick" : "Twitch";
    private string Arg(string name) { CPH.TryGetArg(name, out string value); return value ?? ""; }
}
