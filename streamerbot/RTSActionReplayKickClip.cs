using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string MaxRecentKey = "rts.actionreplay.maxHistory";
    private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";
    private const string AnimationAction = "RTS - Action Replay - Core - Animation";
    private const string ReplayIdHandoffKey = "rts.actionreplay.handoff.replayId";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";

    public bool Execute() => CaptureKickClip();

    public bool CaptureKickClip()
    {
        var message = Arg("text");
        if (string.IsNullOrWhiteSpace(message)) message = Arg("message");
        if (string.IsNullOrWhiteSpace(message)) message = Arg("rawInput");

        var url = ExtractKickClipUrl(message);
        if (string.IsNullOrWhiteSpace(url)) return false;

        var clipId = ExtractKickClipId(url);
        if (string.IsNullOrWhiteSpace(clipId)) return false;

        var data = Load();
        var catalog = (JArray)data["catalog"] ?? new JArray();
        var existing = catalog.OfType<JObject>().FirstOrDefault(x =>
            string.Equals((string)x["sourceType"], "Kick", StringComparison.OrdinalIgnoreCase) &&
            string.Equals((string)x["sourceId"], clipId, StringComparison.OrdinalIgnoreCase));
        if (existing != null) return BroadcastReplay(existing);

        var metadata = LoadClipMetadata(clipId);
        var clip = metadata?["clip"] as JObject;
        var title = (string)clip?["title"];
        if (string.IsNullOrWhiteSpace(title)) title = "Kick Clip";
        var duration = (int)Math.Round((double?)clip?["duration"] ?? 0);
        var creator = clip?["creator"] as JObject;
        var creatorId = creator?["id"]?.ToString() ?? Arg("userId");
        var creatorName = creator?["username"]?.ToString() ?? Arg("userName");
        var creatorInfo = new JObject { ["platform"] = "Kick", ["id"] = creatorId ?? "", ["name"] = creatorName ?? "" };

        var item = new JObject
        {
            ["id"] = "kick-" + clipId,
            ["sourceType"] = "Kick",
            ["sourceId"] = clipId,
            ["sourceUrl"] = url,
            ["title"] = title,
            ["customTitle"] = !string.Equals(title, "Kick Clip", StringComparison.OrdinalIgnoreCase),
            ["duration"] = duration,
            ["added"] = DateTime.Now.ToString("o"),
            ["captured"] = DateTime.Now.ToString("o"),
            ["acquisitionMethod"] = "Kick",
            ["creator"] = creatorInfo,
            ["plays"] = 0,
            ["users"] = new JObject()
        };
        catalog.Insert(0, item);
        data["catalog"] = catalog;
        AddRecent(data, (string)item["id"]);
        Save(data);
        CPH.LogInfo($"RTS Action Replay: native Kick clip captured; clipId={clipId}; title={title}; duration={duration}.");
        return BroadcastReplay(item);
    }

    private JObject LoadClipMetadata(string clipId)
    {
        var url = "https://kick.com/api/v2/clips/" + CPH.UrlEncode(clipId) + "/play";
        var json = DownloadString(url);
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JObject.Parse(json); }
        catch { return null; }
    }

    private bool BroadcastReplay(JObject item)
    {
        CPH.SetGlobalVar(ReplayIdHandoffKey, (string)item["id"] ?? "", false);
        CPH.SetGlobalVar(EntryPointHandoffKey, "kick", false);
        CPH.UnsetGlobalVar(ResolvedProfileHandoffKey, false);
        if (!CPH.ExecuteMethod(AnimationAction, "ResolveEntryPointProfile")) return false;
        return CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay");
    }

    private string ExtractKickClipUrl(string text)
    {
        var match = Regex.Match(text ?? "", @"https?://(?:www\.)?kick\.com/[A-Za-z0-9_-]+/clips/clip_[A-Za-z0-9_-]+", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : null;
    }

    private string ExtractKickClipId(string value)
    {
        var match = Regex.Match(value ?? "", @"/clips/(clip_[A-Za-z0-9_-]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true);
        if (string.IsNullOrWhiteSpace(raw)) return new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() };
        try
        {
            var data = JObject.Parse(raw);
            data["catalog"] = data["catalog"] as JArray ?? new JArray();
            data["recentIds"] = data["recentIds"] as JArray ?? new JArray();
            return data;
        }
        catch { return new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() }; }
    }

    private void Save(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);

    private void AddRecent(JObject data, string id)
    {
        var recent = (JArray)data["recentIds"] ?? new JArray();
        for (var i = recent.Count - 1; i >= 0; i--) if (string.Equals((string)recent[i], id, StringComparison.OrdinalIgnoreCase)) recent.RemoveAt(i);
        recent.Insert(0, id);
        var max = CPH.GetGlobalVar<int?>(MaxRecentKey, true) ?? 20;
        while (recent.Count > Math.Max(1, max)) recent.RemoveAt(recent.Count - 1);
        data["recentIds"] = recent;
    }

    private string DownloadString(string url)
    {
        try
        {
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.Accept] = "application/json";
                client.Headers[HttpRequestHeader.UserAgent] = "RTS-Action-Replay";
                return client.DownloadString(url);
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn("RTS Action Replay: native Kick clip metadata request failed: " + ex.Message);
            return null;
        }
    }

    private string Arg(string name)
    {
        try { CPH.TryGetArg(name, out string value); return value ?? ""; }
        catch { return ""; }
    }
}
