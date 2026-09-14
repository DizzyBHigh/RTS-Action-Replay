using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string QueueKey = "rts.actionreplay.searchQueue";
    private const string ActiveKey = "rts.actionreplay.searchActive";
    private const string CatalogAction = "RTS - Action Replay - Core - Catalog";

    public bool Execute() => Enqueue();

    public bool Enqueue()
    {
        var json = Arg("replaySearchRequest");
        if (string.IsNullOrWhiteSpace(json)) return false;
        try { JObject.Parse(json); } catch { return false; }
        var queue = LoadQueue();
        queue.Add(json);
        SaveQueue(queue);
        if (!IsActive()) return ShowNext();
        return true;
    }

    public bool SearchPanelEnded()
    {
        var queue = LoadQueue();
        if (queue.Count > 0) queue.RemoveAt(0);
        SaveQueue(queue);
        CPH.SetGlobalVar(ActiveKey, false, false);
        return ShowNext();
    }

    public bool Clear()
    {
        SaveQueue(new JArray());
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
        var request = (string)queue[0];
        if (string.IsNullOrWhiteSpace(request)) { queue.RemoveAt(0); SaveQueue(queue); return ShowNext(); }
        CPH.SetGlobalVar(ActiveKey, true, false);
        CPH.SetArgument("replaySearchRequest", request);
        var shown = CPH.ExecuteMethod(CatalogAction, "RenderSearchRequest");
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