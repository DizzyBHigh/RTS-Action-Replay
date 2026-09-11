using System;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string QueueKey = "rts.actionreplay.playlist";
    private const string PausedKey = "rts.actionreplay.playlistPaused";
    private const string ActiveKey = "rts.actionreplay.playlistActive";
    private const string DataKey = "rts.actionreplay.data";
    private const string PlaybackCode = "RTS Action Replay Playback";

    public bool Execute() => View();

    public bool EnqueueCurrentReplay()
    {
        if (!CPH.TryGetArg("replayId", out string replayId) || string.IsNullOrWhiteSpace(replayId)) return false;
        var data = Load();
        var replay = Catalog(data).OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], replayId, StringComparison.OrdinalIgnoreCase));
        if (replay == null) return false;
        CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName);
        var creator = replay["creator"] as JObject;
        var requester = string.IsNullOrWhiteSpace(userName) ? (string)creator?["name"] ?? "" : userName;
        var queue = LoadQueue();
        queue.Add(new JObject { ["entryId"] = Guid.NewGuid().ToString("N"), ["replayId"] = replayId, ["title"] = (string)replay["title"] ?? "Replay", ["requesterId"] = userId ?? "", ["requesterName"] = requester, ["queued"] = DateTime.Now.ToString("o") });
        SaveQueue(queue);
        if (!IsPaused() && ActiveId() == null) return PlayNext(queue);
        return true;
    }

    public bool View()
    {
        var queue = LoadQueue();
        var message = queue.Count == 0 ? "Playlist is empty." : string.Join(" | ", queue.Select((x, i) => "#" + (i + 1) + " " + (string)x["title"] + " — " + (string)x["requesterName"]));
        CPH.SetArgument("replayPlaylist", message); CPH.SendMessage(message); return true;
    }

    public bool Remove()
    {
        if (!CPH.TryGetArg("rawInput", out string input) || !int.TryParse(input, out var index)) return false;
        var queue = LoadQueue(); if (index < 1 || index > queue.Count) return false;
        if (string.Equals((string)queue[index - 1]["entryId"], ActiveId(), StringComparison.OrdinalIgnoreCase)) return false;
        queue.RemoveAt(index - 1); SaveQueue(queue); return true;
    }

    public bool Pause()
    {
        CPH.SetGlobalVar(PausedKey, true, false); return true;
    }

    public bool Resume()
    {
        CPH.SetGlobalVar(PausedKey, false, false);
        var queue = LoadQueue(); return ActiveId() != null || queue.Count == 0 || PlayNext(queue);
    }

    public bool PlaybackEnded()
    {
        if (!CPH.TryGetArg("replayId", out string replayId)) return false;
        CPH.TryGetArg("replayQueueEntryId", out string entryId);
        var queue = LoadQueue();
        var current = queue.FirstOrDefault(x => string.Equals((string)x["replayId"], replayId, StringComparison.OrdinalIgnoreCase) && (string.IsNullOrWhiteSpace(entryId) || string.Equals((string)x["entryId"], entryId, StringComparison.OrdinalIgnoreCase)));
        if (current == null || !string.Equals((string)current["entryId"], ActiveId(), StringComparison.OrdinalIgnoreCase)) return false;
        queue.Remove(current); SaveQueue(queue); CPH.SetGlobalVar(ActiveKey, "", false);
        if (IsPaused()) return true;
        if (queue.Count == 0) { HidePlayer(); return true; }
        return PlayNext(queue);
    }

    private bool PlayNext(JArray queue)
    {
        var item = queue.FirstOrDefault(); if (item == null) return true;
        var catalog = Catalog(Load());
        var replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], (string)item["replayId"], StringComparison.OrdinalIgnoreCase));
        var index = replay == null ? -1 : catalog.IndexOf(replay);
        if (index < 0) return false;
        CPH.SetArgument("rawInput", (index + 1).ToString()); CPH.SetArgument("replayQueueEntryId", (string)item["entryId"]);
        var started = CPH.ExecuteMethod(PlaybackCode, "PlayReplay");
        if (started) CPH.SetGlobalVar(ActiveKey, (string)item["entryId"], false);
        return started;
    }

    private string ActiveId() => CPH.GetGlobalVar<string>(ActiveKey, false);
    private bool IsPaused() => CPH.GetGlobalVar<bool?>(PausedKey, false) ?? false;
    private void HidePlayer() { CPH.SetArgument("replayCommand", "hide"); CPH.TriggerEvent("RTS-Action Replay", true); }
    private JArray LoadQueue() { var raw = CPH.GetGlobalVar<string>(QueueKey, false); try { return string.IsNullOrWhiteSpace(raw) ? new JArray() : JArray.Parse(raw); } catch { return new JArray(); } }
    private void SaveQueue(JArray queue) => CPH.SetGlobalVar(QueueKey, queue.ToString(Newtonsoft.Json.Formatting.None), false);
    private JObject Load() { var raw = CPH.GetGlobalVar<string>(DataKey, true); try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { return new JObject(); } }
    private JArray Catalog(JObject data) => data["catalog"] as JArray ?? new JArray();
}
