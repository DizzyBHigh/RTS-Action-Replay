using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string QueueKey = "rts.actionreplay.searchQueue";
    private const string ActiveKey = "rts.actionreplay.searchActive";
    private const string CatalogAction = "RTS - Action Replay - Core - Catalog";
    private const string ResolverAction = "RTS - Action Replay - Core - Resolver";
    private const string PanelOperationKey = "rts.actionreplay.operation.panel";

    public bool Execute()
    {
        if (string.IsNullOrWhiteSpace(Arg("replaySearchRequest")) && string.IsNullOrWhiteSpace(Arg("replayPanelRequest"))) return Resume();
        return Enqueue();
    }

    public bool Enqueue()
    {
        var json = Arg("replayPanelRequest");
        if (string.IsNullOrWhiteSpace(json)) json = Arg("replaySearchRequest");
        if (string.IsNullOrWhiteSpace(json)) return false;
        JObject request;
        try { request = JObject.Parse(json); } catch { return false; }
        if (string.IsNullOrWhiteSpace((string)request["requestId"])) request["requestId"] = Guid.NewGuid().ToString("N");
        var queue = LoadQueue();
        queue.Add(request);
        SaveQueue(queue);
        if (!IsActive()) return ShowNext();
        return true;
    }

    public bool Resume()
    {
        if (!IsActive()) return false;
        return ShowNext();
    }

    public bool SearchPanelEnded()
    {
        var queue = LoadQueue();
        var requestId = Arg("replayPanelRequestId");
        if (string.IsNullOrWhiteSpace(requestId)) requestId = Arg("replaySearchRequestId");
        if (queue.Count == 0)
        {
            CPH.SetGlobalVar(ActiveKey, false, false);
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
        return true;
    }

    private bool ShowNext()
    {
        var queue = LoadQueue();
        if (queue.Count == 0)
        {
            CPH.SetGlobalVar(ActiveKey, false, false);
            return true;
        }
        var request = queue[0] as JObject;
        if (request == null) { queue.RemoveAt(0); SaveQueue(queue); return ShowNext(); }
        CPH.SetGlobalVar(ActiveKey, true, false);
        bool shown;
        if (string.Equals((string)request["panelType"], "playlist", StringComparison.OrdinalIgnoreCase))
        {
            CPH.SetGlobalVar(PanelOperationKey, request.ToString(Newtonsoft.Json.Formatting.None), false);
            shown = CPH.ExecuteMethod(ResolverAction, "ResolvePanel");
        }
        else
        {
            CPH.SetArgument("replaySearchRequest", request.ToString(Newtonsoft.Json.Formatting.None));
            CPH.SetArgument("replaySearchRequestId", (string)request["requestId"] ?? "");
            shown = CPH.ExecuteMethod(CatalogAction, "RenderSearchRequest");
        }
        var panelEnabled = true;
        var panelType = ((string)request["panelType"] ?? "").ToLowerInvariant();
        if (panelType == "recent") panelEnabled = CPH.GetGlobalVar<bool?>("rts.actionreplay.message.recent.panel", true) ?? true;
        else if (panelType == "playlist") panelEnabled = CPH.GetGlobalVar<bool?>("rts.actionreplay.message.playlist.panel", true) ?? true;
        else panelEnabled = CPH.GetGlobalVar<bool?>("rts.actionreplay.search.panel", true) ?? true;

        if (!shown || !panelEnabled)
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
