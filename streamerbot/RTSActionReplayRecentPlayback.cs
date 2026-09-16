using System;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string MaxHistoryKey = "rts.actionreplay.maxHistory";
    private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";

    public bool Execute() => PlayRecent();

    public bool PlayRecent()
    {
        var input = Arg("rawInput").Trim();
        if (!int.TryParse(input, out var index) || index < 1)
        {
            SendMessage("Please provide a recent replay number.");
            return false;
        }

        var data = Load();
        var catalog = data["catalog"] as JArray ?? new JArray();
        var limit = Math.Max(1, CPH.GetGlobalVar<int?>(MaxHistoryKey, true) ?? 20);
        var recent = catalog.OfType<JObject>()
            .OrderByDescending(x => ParseDate((string)x["captured"] ?? (string)x["added"]))
            .Take(limit)
            .ToList();

        if (index > recent.Count)
        {
            SendMessage($"Recent replay #{index} does not exist.");
            return false;
        }

        var replayId = (string)recent[index - 1]["id"];
        if (string.IsNullOrWhiteSpace(replayId))
        {
            SendMessage($"Recent replay #{index} is unavailable.");
            return false;
        }

        CPH.SetArgument("replayId", replayId);
        return CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay");
    }

    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true);
        try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); }
        catch { return new JObject(); }
    }

    private DateTime ParseDate(string value) => DateTime.TryParse(value, out var date) ? date : DateTime.MinValue;

    private string Arg(string name) { CPH.TryGetArg(name, out string value); return value ?? ""; }

    private void SendMessage(string text)
    {
        var platform = Arg("userType");
        if (string.Equals(platform, "Kick", StringComparison.OrdinalIgnoreCase)) { CPH.SendKickMessage(text); return; }
        if (string.Equals(platform, "YouTube", StringComparison.OrdinalIgnoreCase))
        {
            var broadcastId = Arg("broadcast.id");
            if (!string.IsNullOrWhiteSpace(broadcastId)) { CPH.SendYouTubeMessage(text, true, true, broadcastId); return; }
            CPH.SendYouTubeMessageToLatestMonitored(text); return;
        }
        CPH.SendMessage(text);
    }
}
