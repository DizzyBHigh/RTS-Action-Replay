using System;
using System.Linq;
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

    public bool Execute() => CaptureKickBotClip();

    public bool CaptureKickBotClip()
    {
        var message = Arg("text");
        if (string.IsNullOrWhiteSpace(message)) message = Arg("message");
        if (string.IsNullOrWhiteSpace(message)) message = Arg("rawInput");
        var kickBotUrl = ExtractKickBotUrl(message);
        if (string.IsNullOrWhiteSpace(kickBotUrl)) return false;

        var data = Load();
        var catalog = (JArray)data["catalog"] ?? new JArray();
        var kickBotId = ExtractKickBotId(kickBotUrl);
        var existing = catalog.OfType<JObject>().FirstOrDefault(x =>
            string.Equals((string)x["sourceType"], "Kick", StringComparison.OrdinalIgnoreCase) &&
            string.Equals((string)x["sourceUrl"], kickBotUrl, StringComparison.OrdinalIgnoreCase));
        if (existing != null) return BroadcastReplay(existing);

        CPH.TryGetArg("userId", out string userId);
        CPH.TryGetArg("userName", out string userName);
        var creator = new JObject { ["platform"] = "Kick", ["id"] = userId ?? "", ["name"] = userName ?? "" };
        var item = new JObject
        {
            ["id"] = "kick-" + (kickBotId ?? Guid.NewGuid().ToString("N")),
            ["sourceType"] = "Kick",
            ["sourceId"] = kickBotId ?? "",
            ["sourceUrl"] = kickBotUrl,
            ["title"] = "Kick Clip",
            ["customTitle"] = false,
            ["added"] = DateTime.Now.ToString("o"),
            ["captured"] = DateTime.Now.ToString("o"),
            ["acquisitionMethod"] = "KickBot",
            ["creator"] = creator,
            ["plays"] = 0,
            ["users"] = new JObject()
        };
        catalog.Insert(0, item);
        data["catalog"] = catalog;
        AddRecent(data, (string)item["id"]);
        Save(data);
        return BroadcastReplay(item);
    }

    private bool BroadcastReplay(JObject item)
    {
        CPH.SetGlobalVar(ReplayIdHandoffKey, (string)item["id"] ?? "", false);
        CPH.SetGlobalVar(EntryPointHandoffKey, "kick", false);
        CPH.UnsetGlobalVar(ResolvedProfileHandoffKey, false);
        if (!CPH.ExecuteMethod(AnimationAction, "ResolveEntryPointProfile")) return false;
        return CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay");
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

    private string ExtractKickBotUrl(string text)
    {
        var match = Regex.Match(text ?? "", @"https?://(?:www\.)?kickbot\.com/clip/[A-Za-z0-9]+", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : null;
    }

    private string ExtractKickBotId(string url)
    {
        var match = Regex.Match(url ?? "", @"kickbot\.com/clip/([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private string Arg(string name)
    {
        try { CPH.TryGetArg(name, out string value); return value ?? ""; }
        catch { return ""; }
    }
}