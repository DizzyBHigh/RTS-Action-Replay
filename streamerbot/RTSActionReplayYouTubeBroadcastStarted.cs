using System;

public class CPHInline
{
    private const string BroadcastIdKey = "rts.actionreplay.youtube.broadcastId";
    private const string StartTimeKey = "rts.actionreplay.youtube.actualStartTime";

    public bool Execute()
    {
        var broadcastId = Arg("broadcast.id").Trim();
        var startTime = Arg("broadcast.actualStartTime").Trim();

        if (string.IsNullOrWhiteSpace(broadcastId))
        {
            CPH.LogWarn("RTS Action Replay: YouTube Broadcast Started event did not provide broadcast.id.");
            return false;
        }

        if (!DateTimeOffset.TryParse(startTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out var actualStartTime))
        {
            CPH.LogWarn($"RTS Action Replay: invalid YouTube broadcast.actualStartTime '{startTime}'.");
            return false;
        }

        CPH.SetGlobalVar(BroadcastIdKey, broadcastId, true);
        CPH.SetGlobalVar(StartTimeKey, actualStartTime.ToUniversalTime().ToString("o"), true);
        CPH.LogInfo($"RTS Action Replay: YouTube broadcast {broadcastId} started at {actualStartTime.ToUniversalTime():o}.");
        return true;
    }

    private string Arg(string name)
    {
        CPH.TryGetArg(name, out string value);
        return value ?? "";
    }
}
