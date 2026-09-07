public class CPHInline
{
    public bool Execute()
    {
        var replayUrl = "http://127.0.0.1:7474/replays/Area18%20Valet%20Service.mp4";
        var json = "{\"type\":\"replay\",\"command\":\"load\",\"url\":\"" + replayUrl + "\"}";

        CPH.LogInfo("RTS Action Replay test: loading replay");
        CPH.WebsocketBroadcastJson(json);
        return true;
    }
}
