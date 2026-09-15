using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string StoreAction = "RTS - Action Replay - Core - Store";
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
            if (parts.Length > 1 || !int.TryParse(parts[0], out var requested))
            {
                CPH.SendMessage("Usage: !Create-clip [5-60]");
                return false;
            }
            duration = Math.Max(5, Math.Min(60, requested));
        }

        var broadcastId = Arg("broadcastId").Trim();
        if (string.IsNullOrWhiteSpace(broadcastId))
        {
            CPH.SendMessage("I couldn't determine the current YouTube stream.");
            return false;
        }

        var startTime = CPH.GetGlobalVar<long?>("rts.actionreplay.youtube.streamTimeSeconds", false) ?? 0;
        if (startTime <= 0) startTime = CPH.GetGlobalVar<long?>("streamTimeSeconds", false) ?? 0;
        if (startTime < 0)
        {
            CPH.SendMessage("I couldn't determine the current YouTube timestamp.");
            return false;
        }

        CPH.SetArgument("youtubeVideoId", broadcastId);
        CPH.SetArgument("youtubeStartTime", startTime);
        CPH.SetArgument("youtubeDuration", duration);
        CPH.SetArgument("youtubeUserId", Arg("userId"));
        CPH.SetArgument("youtubeUserName", Arg("userName"));
        CPH.SetArgument("youtubeUserType", Arg("userType"));

        if (!CPH.ExecuteMethod(StoreAction, "AddYouTubeReplay"))
        {
            CPH.LogWarn("RTS Action Replay: AddYouTubeReplay returned false.");
            return false;
        }

        var replayId = CPH.GetGlobalVar<string>("rts.actionreplay.handoff.replayId", false);
        if (string.IsNullOrWhiteSpace(replayId)) return true;
        CPH.SetGlobalVar("rts.actionreplay.handoff.entryPoint", "youtube", false);
        CPH.UnsetGlobalVar("rts.actionreplay.handoff.resolvedProfile", false);
        if (!CPH.ExecuteMethod(AnimationAction, "ResolveEntryPointProfile")) return false;
        return CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay");
    }

    private int GetSettingInt(string key, int fallback)
    {
        try { return CPH.GetGlobalVar<int?>(key, true) ?? fallback; }
        catch { return fallback; }
    }

    private string Arg(string name)
    {
        CPH.TryGetArg(name, out string value);
        return value ?? "";
    }
}
