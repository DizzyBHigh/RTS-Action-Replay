using System;

public class CPHInline
{
    private const string EventName = "RTS-Action Replay";

    public bool PauseVideo() { Send("pause"); return true; }
    public bool PlayVideo() { Send("play"); return true; }

    public bool SetVideoSpeed()
    {
        if (!CPH.TryGetArg("rawInput", out string input) ||
            !double.TryParse(input, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var speed)) return false;
        speed = Math.Max(.25, Math.Min(4.0, speed));
        CPH.SetArgument("replayPlaybackSpeed", speed); Send("speed"); return true;
    }

    public bool HidePlayer() { Send("hide"); return true; }
    public bool ShowPlayer() { Send("show"); return true; }

    private void Send(string command)
    {
        CPH.SetArgument("replayCommand", command);
        CPH.TriggerEvent(EventName, true);
    }
}
