using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";

    public bool Execute() => PlayHistory();

    public bool PlayHistory()
    {
        var input = Arg("rawInput").Trim();
        if (!int.TryParse(input, out var index) || index < 1)
        {
            SendMessage("Please provide a history item number.");
            return false;
        }

        var data = Load();
        var history = data["playHistory"] as JArray ?? new JArray();
        if (index > history.Count)
        {
            SendMessage($"History item #{index} does not exist.");
            return false;
        }

        var replayId = (string)(history[index - 1] as JObject)?["replayId"];
        if (string.IsNullOrWhiteSpace(replayId))
        {
            SendMessage($"History item #{index} is unavailable.");
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
