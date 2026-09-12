using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string QueueKey = "rts.actionreplay.playlist";
    private const string PersistKey = "rts.actionreplay.playlistPersist";
    private const string PausedKey = "rts.actionreplay.playlistPaused";
    private const string ActiveKey = "rts.actionreplay.playlistActive";
    private const string DataKey = "rts.actionreplay.data";
    private const string PlaybackCode = "RTS Action Replay Playback";
    private const string AnimationAction = "RTS - Action Replay - Core - Animation";

    public bool Execute() => View();

    public bool EnqueueCurrentReplay()
    {
        CPH.LogInfo("RTS Action Replay TRACE: EnqueueCurrentReplay entered.");
        if (!CPH.TryGetArg("replayId", out string replayId) || string.IsNullOrWhiteSpace(replayId))
        {
            CPH.LogWarn("RTS Action Replay TRACE: EnqueueCurrentReplay failed - replayId argument missing or empty.");
            return false;
        }
        CPH.LogInfo($"RTS Action Replay TRACE: EnqueueCurrentReplay replayId={replayId}.");
        var replay = FindReplay(Catalog(Load()), replayId);
        if (replay == null)
        {
            CPH.LogWarn($"RTS Action Replay TRACE: EnqueueCurrentReplay failed - replay {replayId} not found in catalog.");
            return false;
        }
        CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName);
        var creator = replay["creator"] as JObject; var requester = string.IsNullOrWhiteSpace(userName) ? (string)creator?["name"] ?? "" : userName;
        var profile = ResolveRequestedProfile();
        CPH.LogInfo($"RTS Action Replay TRACE: EnqueueCurrentReplay resolved profile={profile ?? "<null>"}.");
        var queue = LoadQueue();
        queue.Add(new JObject { ["entryId"] = Guid.NewGuid().ToString("N"), ["replayId"] = replayId, ["title"] = (string)replay["title"] ?? "Replay", ["requesterId"] = userId ?? "", ["requesterName"] = requester, ["animationProfileId"] = profile, ["queued"] = DateTime.Now.ToString("o") });
        SaveQueue(queue);
        CPH.LogInfo($"RTS Action Replay TRACE: EnqueueCurrentReplay queued replay {replayId}; queueCount={queue.Count}; paused={IsPaused()}; active={ActiveId() ?? "<none>"}.");
        if (!IsPaused() && ActiveId() == null)
        {
            var started = PlayNext(queue);
            CPH.LogInfo($"RTS Action Replay TRACE: EnqueueCurrentReplay PlayNext returned {started}.");
            return started;
        }
        return true;
    }

    public bool View()
    {
        var queue = LoadQueue(); if (queue.Count == 0) { CPH.SendMessage("Playlist is empty."); return true; }
        var lines = "";
        for (var i = 0; i < queue.Count; i++) { var item = queue[i] as JObject; if (item == null) continue; var requester = (string)item["requesterName"]; if (string.IsNullOrWhiteSpace(requester)) requester = "Created automatically"; lines += (lines.Length == 0 ? "" : " | ") + "#" + (i + 1) + " " + (string)item["title"] + " — " + requester; }
        CPH.SetArgument("replayPlaylist", lines); CPH.SendMessage(lines); return true;
    }

    public bool Remove()
    {
        if (!CPH.TryGetArg("rawInput", out string input) || !int.TryParse(input, out var index)) return false;
        var queue = LoadQueue(); if (index < 1 || index > queue.Count) return false;
        if (string.Equals((string)queue[index - 1]["entryId"], ActiveId(), StringComparison.OrdinalIgnoreCase)) return false;
        queue.RemoveAt(index - 1); SaveQueue(queue); return true;
    }

    public bool Pause() { CPH.SetGlobalVar(PausedKey, true, false); return true; }

    public bool Resume()
    {
        CPH.SetGlobalVar(PausedKey, false, false);
        var queue = LoadQueue();
        if (ActiveId() != null || queue.Count == 0) return true;
        queue[0]["animationProfileId"] = ResolvePlaylistProfile();
        SaveQueue(queue);
        return PlayNext(queue);
    }

    public bool PlaybackEnded()
    {
        if (!CPH.TryGetArg("replayId", out string replayId)) return false;
        CPH.TryGetArg("replayQueueEntryId", out string entryId); var queue = LoadQueue(); JObject current = null;
        for (var i = 0; i < queue.Count; i++) { var item = queue[i] as JObject; if (item != null && string.Equals((string)item["replayId"], replayId, StringComparison.OrdinalIgnoreCase) && (string.IsNullOrWhiteSpace(entryId) || string.Equals((string)item["entryId"], entryId, StringComparison.OrdinalIgnoreCase))) { current = item; break; } }
        if (current == null || !string.Equals((string)current["entryId"], ActiveId(), StringComparison.OrdinalIgnoreCase)) return false;
        queue.Remove(current); SaveQueue(queue); CPH.SetGlobalVar(ActiveKey, "", false);
        if (IsPaused()) return true; if (queue.Count == 0) { HidePlayer(); return true; } return PlayNext(queue);
    }

    private bool PlayNext(JArray queue)
    {
        CPH.LogInfo($"RTS Action Replay TRACE: PlayNext entered; queueCount={queue.Count}.");
        if (queue.Count == 0) return true; var item = queue[0] as JObject; if (item == null) { CPH.LogWarn("RTS Action Replay TRACE: PlayNext failed - queue item is not an object."); return false; }
        var catalog = Catalog(Load()); var index = -1;
        for (var i = 0; i < catalog.Count; i++) { var replay = catalog[i] as JObject; if (replay != null && string.Equals((string)replay["id"], (string)item["replayId"], StringComparison.OrdinalIgnoreCase)) { index = i; break; } }
        if (index < 0) { CPH.LogWarn($"RTS Action Replay TRACE: PlayNext failed - replay {(string)item["replayId"]} not found in catalog."); return false; }
        var profile = (string)item["animationProfileId"]; if (string.IsNullOrWhiteSpace(profile)) profile = ResolvePlaylistProfile();
        CPH.SetArgument("rawInput", (index + 1).ToString()); CPH.SetArgument("replayQueueEntryId", (string)item["entryId"]); CPH.SetArgument("replayAnimationProfileId", profile);
        CPH.LogInfo($"RTS Action Replay TRACE: PlayNext calling Playback; catalogIndex={index + 1}; entryId={(string)item["entryId"]}; profile={profile ?? "<null>"}.");
        var started = CPH.ExecuteMethod(PlaybackCode, "PlayReplay");
        CPH.LogInfo($"RTS Action Replay TRACE: Playback PlayReplay returned {started}.");
        if (started) CPH.SetGlobalVar(ActiveKey, (string)item["entryId"], false); return started;
    }

    private string ResolveRequestedProfile()
    {
        if (CPH.TryGetArg("replayAnimationProfileId", out string requested) && !string.IsNullOrWhiteSpace(requested)) return requested.Trim();
        return ResolvePlaylistProfile();
    }

    private string ResolvePlaylistProfile()
    {
        CPH.SetArgument("animationEntryPoint", "playlist");
        if (CPH.ExecuteMethod(AnimationAction, "ResolveEntryPointProfile") && CPH.TryGetArg("replayAnimationProfileId", out string profile) && !string.IsNullOrWhiteSpace(profile)) return profile.Trim();
        return "default";
    }

    private JObject FindReplay(JArray catalog, string replayId) { for (var i = 0; i < catalog.Count; i++) { var replay = catalog[i] as JObject; if (replay != null && string.Equals((string)replay["id"], replayId, StringComparison.OrdinalIgnoreCase)) return replay; } return null; }
    private string ActiveId() => CPH.GetGlobalVar<string>(ActiveKey, false);
    private bool IsPaused() => CPH.GetGlobalVar<bool?>(PausedKey, false) ?? false;
    private bool PersistQueue() => CPH.GetGlobalVar<bool?>(PersistKey, true) ?? false;
    private void HidePlayer() { CPH.SetArgument("replayCommand", "hide"); CPH.TriggerEvent("RTS-Action Replay", true); }
    private JArray LoadQueue() { var persist = PersistQueue(); if (!persist) CPH.SetGlobalVar(QueueKey, "", true); var raw = CPH.GetGlobalVar<string>(QueueKey, persist); try { return string.IsNullOrWhiteSpace(raw) ? new JArray() : JArray.Parse(raw); } catch { return new JArray(); } }
    private void SaveQueue(JArray queue) => CPH.SetGlobalVar(QueueKey, queue.ToString(Newtonsoft.Json.Formatting.None), PersistQueue());
    private JObject Load() { var raw = CPH.GetGlobalVar<string>(DataKey, true); try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { return new JObject(); } }
    private JArray Catalog(JObject data) => data["catalog"] as JArray ?? new JArray();
}