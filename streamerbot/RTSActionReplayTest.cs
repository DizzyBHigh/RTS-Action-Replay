using System;

public class CPHInline
{
    public bool Execute()
    {
        var replayUrl = "http://127.0.0.1:7474/replays/Area18%20Valet%20Service.mp4";
        CPH.LogInfo("RTS Action Replay test: loading replay");
        CPH.WebsocketBroadcast("RTS Action Replay", new
        {
            type = "replay",
            command = "load",
            url = replayUrl
        });
        return true;
    }
}
