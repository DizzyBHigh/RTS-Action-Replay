using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string QueueKey = "rts.actionreplay.searchQueue";
    private const string ActiveKey = "rts.actionreplay.searchActive";
    private const string CatalogAction = "RTS - Action Replay - Catalog - Search";

    public bool Execute() => Enqueue();

    public bool Enqueue()
    {
        var json = Arg("replaySearchRequest");
        if (string.IsNullOrWhiteSpace(json)) return false;
        JObject request;
        try { request = JObject.Parse(json); } catch { return false; }
        if (string.IsNullOrWhiteSpace((string)request["requestId"])) request["requestId"] = Guid.NewGuid().ToString("N");
        var queue = LoadQueue();
        queue.Add(request);
        SaveQueue(queue);
        var active = IsActive();
        CPH.LogDebug($"RTS Action Replay Search Queue: Enqueue active={active}, queueCount={queue.Count}");
        if (!active) return ShowNext();
        return true;
    }

    public bool SearchPanelEnded()
    {
        var queue = LoadQueue();
        var requestId = Arg("replaySearchRequestId");
        if (queue.Count == 0)
        {
            CPH.SetGlobalVar(ActiveKey, false, false);
            CPH.LogDebug("RTS Action Replay Search Queue: SearchPanelEnded queue empty; active=false");
            return true;
        }
        if (!string.IsNullOrWhiteSpace(requestId) && !string.Equals(requestId, (string)queue[0]["requestId"], StringComparison.OrdinalIgnoreCase)) return false;
        queue.RemoveAt(0);
        SaveQueue(queue);
        CPH.SetGlobalVar(ActiveKey, false, false);
        return ShowNext();
    }

    public bool Clear()
    {
        SaveQueue(new JArray());
        CPH.SetGlobalVar(ActiveKey, false, false);
        CPH.LogDebug("RTS Action Replay Search Queue: Clear queue; active=false");
        return true;
    }

    private bool ShowNext()
    {
        var queue = LoadQueue();
        CPH.LogDebug($"RTS Action Replay Search Queue: ShowNext queueCount={queue.Count}");
        if (queue.Count == 0)
        {
            CPH.SetGlobalVar(ActiveKey, false, false);
            return true;
        }
        var request = queue[0] as JObject;
        if (request == null) { queue.RemoveAt(0); SaveQueue(queue); return ShowNext(); }
        CPH.SetGlobalVar(ActiveKey, true, false);
        CPH.SetArgument("replaySearchRequest", request.ToString(Newtonsoft.Json.Formatting.None));
        CPH.SetArgument("replaySearchRequestId", (string)request["requestId"] ?? "");
        CPH.LogDebug($"RTS Action Replay Search Queue: calling {CatalogAction}.RenderSearchRequest");
        var shown = CPH.ExecuteMethod(CatalogAction, "RenderSearchRequest");
        CPH.LogDebug($"RTS Action Replay Search Queue: RenderSearchRequest result={shown}");
        if (!shown)
        {
            queue.RemoveAt(0);
            SaveQueue(queue);
            CPH.SetGlobalVar(ActiveKey, false, false);
            return ShowNext();
        }
        return true;
    }

    private bool IsActive() => CPH.GetGlobalVar<bool?>(ActiveKey, false) ?? false;
    private string Arg(string name) { CPH.TryGetArg(name, out string value); return value ?? ""; }
    private JArray LoadQueue()
    {
        var raw = CPH.GetGlobalVar<string>(QueueKey, false);
        try { return string.IsNullOrWhiteSpace(raw) ? new JArray() : JArray.Parse(raw); }
        catch { return new JArray(); }
    }
    private void SaveQueue(JArray queue) => CPH.SetGlobalVar(QueueKey, queue.ToString(Newtonsoft.Json.Formatting.None), false);
}