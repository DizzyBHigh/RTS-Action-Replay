using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string QueueKey = "rts.actionreplay.playlist";
    private const string PersistKey = "rts.actionreplay.playlistPersist";
    private const string PausedKey = "rts.actionreplay.playlistPaused";
    private const string ActiveKey = "rts.actionreplay.playlistActive";
    private const string ActiveReplayKey = "rts.actionreplay.playlistActiveReplayId";
    private const string DataKey = "rts.actionreplay.data";
    private const string PlaybackCode = "RTS - Action Replay - Core - Playback";
    private const string CatalogAction = "RTS - Action Replay - Core - Catalog";
    private const string ResolverAction = "RTS - Action Replay - Core - Resolver";
    private const string PanelOperationKey = "rts.actionreplay.operation.panel";
    private const string ReplayIdHandoffKey = "rts.actionreplay.handoff.replayId";
    private const string PlaybackProfileHandoffKey = "rts.actionreplay.handoff.playbackProfile";
    private const string PlaybackQueueEntryHandoffKey = "rts.actionreplay.handoff.playbackQueueEntryId";

    public bool Execute() => View();

    public bool EnqueueCurrentReplay()
    {
        CPH.LogInfo("RTS Action Replay TRACE: EnqueueCurrentReplay entered.");
        var replayId = ReadArgumentOrGlobal("replayId", ReplayIdHandoffKey);
        if (string.IsNullOrWhiteSpace(replayId)) { CPH.LogWarn("RTS Action Replay TRACE: EnqueueCurrentReplay failed - replayId argument/handoff missing or empty."); return false; }
        var replay = FindReplay(Catalog(Load()), replayId);
        if (replay == null) { CPH.LogWarn($"RTS Action Replay TRACE: EnqueueCurrentReplay failed - replay {replayId} not found in catalog."); CPH.UnsetGlobalVar(ReplayIdHandoffKey, false); return false; }
        CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName); CPH.TryGetArg("userType", out string userType); CPH.TryGetArg("broadcast.id", out string broadcastId);
        var creator = replay["creator"] as JObject; var requester = string.IsNullOrWhiteSpace(userName) ? (string)creator?["name"] ?? "" : userName;
        var requesterPlatform = NormalizePlatform(userType);
        CPH.SetArgument("entryPoint", "play");
        CPH.SetArgument("replaySource", (string)replay["sourceType"] ?? "OBS");
        if (!CPH.ExecuteMethod(ResolverAction, "ResolvePlayerConfiguration")) return false;
        var profile = Arg("animationProfile", "default");
        var designProfile = Arg("designPreset", Arg("visualPreset", "broadcast"));
        var titleProfile = Arg("titlePreset", "default");
        var brandingProfile = Arg("brandingPreset", "default");
        var queue = LoadQueue();
        var playlistNotEmpty = queue.Count > 0;
        var queueEntry = new JObject { ["entryId"] = Guid.NewGuid().ToString("N"), ["replayId"] = replayId, ["title"] = (string)replay["title"] ?? "Replay", ["requesterId"] = userId ?? "", ["requesterName"] = requester, ["requesterPlatform"] = requesterPlatform ?? "", ["requesterBroadcastId"] = broadcastId ?? "", ["animationProfileId"] = profile, ["designPresetId"] = designProfile, ["titlePresetId"] = titleProfile, ["brandingPresetId"] = brandingProfile, ["showClapperboard"] = CPH.GetGlobalVar<bool?>("rts.actionreplay.handoff.showClapperboard", false) ?? false, ["queued"] = DateTime.Now.ToString("o") };
        queue.Add(queueEntry);
        SaveQueue(queue);
        if (playlistNotEmpty) EnqueueReplayQueued(replay, userId, requester, requesterPlatform, broadcastId); CPH.UnsetGlobalVar(ReplayIdHandoffKey, false); CPH.UnsetGlobalVar("rts.actionreplay.handoff.showClapperboard", false);
        if (!IsPaused() && ActiveId() == null) return PlayNext(queue);
        return true;
    }

    public bool View()
    {
        var queue = LoadQueue();
        if (queue.Count == 0) { SendPlaylistMessage("Playlist is empty."); return true; }
        var lines = "";
        for (var i = 0; i < queue.Count; i++) { var item = queue[i] as JObject; if (item == null) continue; var requester = (string)item["requesterName"]; if (string.IsNullOrWhiteSpace(requester)) requester = "Created automatically"; lines += (lines.Length == 0 ? "" : " | ") + "#" + (i + 1) + " " + (string)item["title"] + " — " + requester; }
        SendPlaylistMessage(lines);
        return true;
    }

    public bool Clear()
    {
        var queue = LoadQueue(); var activeId = ActiveId(); var cleared = 0;
        for (var i = queue.Count - 1; i >= 0; i--)
        {
            var item = queue[i] as JObject;
            if (string.IsNullOrWhiteSpace(activeId) || item == null || !string.Equals((string)item["entryId"], activeId, StringComparison.OrdinalIgnoreCase)) { queue.RemoveAt(i); cleared++; }
        }
        SaveQueueAndClearOtherStore(queue); CPH.LogInfo($"RTS Action Replay: playlist Clear removed {cleared} waiting item(s); active={(string.IsNullOrWhiteSpace(activeId) ? "<none>" : activeId)}; remaining={queue.Count}."); SendPlaylistMessage(queue.Count == 0 ? "Playlist cleared." : "Playlist cleared; active replay retained."); return true;
    }

    public bool ClearAll()
    {
        var queue = LoadQueue(); var cleared = queue.Count; queue.Clear(); SaveQueueAndClearOtherStore(queue); CPH.SetGlobalVar(ActiveKey, "", false); CPH.SetGlobalVar(PausedKey, false, false); CPH.LogInfo($"RTS Action Replay: playlist ClearAll removed {cleared} item(s); active playback was not stopped; playlist pause state reset."); SendPlaylistMessage("Playlist completely cleared."); return true;
    }

    public bool Remove()
    {
        if (!CPH.TryGetArg("rawInput", out string input) || !int.TryParse(input, out var index)) return false;
        var queue = LoadQueue(); if (index < 1 || index > queue.Count) return false; if (string.Equals((string)queue[index - 1]["entryId"], ActiveId(), StringComparison.OrdinalIgnoreCase)) return false;
        var removed = queue[index - 1] as JObject;
        var replay = FindReplay(Catalog(Load()), (string)removed?["replayId"] ?? "");
        queue.RemoveAt(index - 1); SaveQueue(queue);
        EnqueueReplayRemoved(removed, replay, userId: Arg("userId", ""), userName: Arg("userName", ""), userPlatform: Arg("userType", ""), broadcastId: Arg("broadcast.id", ""));
        return true;
    }

    public bool Pause() { CPH.SetGlobalVar(PausedKey, true, false); return true; }

    public bool Resume()
    {
        CPH.SetGlobalVar(PausedKey, false, false); var queue = LoadQueue();
        if (ActiveId() != null || queue.Count == 0) return true;
        return PlayNext(queue);
    }

    public bool PlaybackEnded()
    {
        CPH.LogInfo("RTS Action Replay TRACE: PlaybackEnded entered.");
        if (!CPH.TryGetArg("replayId", out string replayId) || string.IsNullOrWhiteSpace(replayId)) return false;
        CPH.TryGetArg("replayQueueEntryId", out string entryId); var activeId = ActiveId(); var queue = LoadQueue(); JObject current = null;
        for (var i = 0; i < queue.Count; i++) { var item = queue[i] as JObject; if (item != null && string.Equals((string)item["replayId"], replayId, StringComparison.OrdinalIgnoreCase) && (string.IsNullOrWhiteSpace(entryId) || string.Equals((string)item["entryId"], entryId, StringComparison.OrdinalIgnoreCase))) { current = item; break; } }
        if (current == null || !string.Equals((string)current["entryId"], activeId, StringComparison.OrdinalIgnoreCase)) return false;
        queue.Remove(current); SaveQueue(queue); CPH.SetGlobalVar(ActiveKey, "", false); CPH.SetGlobalVar(ActiveReplayKey, "", false);
        if (IsPaused()) return true; if (queue.Count == 0) { HidePlayer(); return true; } return PlayNext(queue);
    }

    private bool PlayNext(JArray queue)
    {
        if (queue.Count == 0) return true; var item = queue[0] as JObject; if (item == null) return false;
        var catalog = Catalog(Load()); var index = -1;
        for (var i = 0; i < catalog.Count; i++) { var replay = catalog[i] as JObject; if (replay != null && string.Equals((string)replay["id"], (string)item["replayId"], StringComparison.OrdinalIgnoreCase)) { index = i; break; } }
        if (index < 0) return false;
        var profile = ArgItem(item, "animationProfileId", "default"); var designProfile = ArgItem(item, "designPresetId", "broadcast"); var titleProfile = ArgItem(item, "titlePresetId", "default"); var brandingProfile = ArgItem(item, "brandingPresetId", "default");
        CPH.SetGlobalVar(ReplayIdHandoffKey, (string)item["replayId"] ?? "", false); CPH.SetGlobalVar(PlaybackQueueEntryHandoffKey, (string)item["entryId"] ?? "", false); CPH.SetGlobalVar(PlaybackProfileHandoffKey, profile, false);
        CPH.SetArgument("rawInput", (index + 1).ToString()); CPH.SetArgument("replayQueueEntryId", (string)item["entryId"]); CPH.SetArgument("replayAnimationProfileId", profile); CPH.SetArgument("animationProfile", profile); CPH.SetArgument("designPreset", designProfile); CPH.SetArgument("titlePreset", titleProfile); CPH.SetArgument("brandingPreset", brandingProfile); CPH.SetArgument("requesterPlatform", (string)item["requesterPlatform"] ?? ""); CPH.SetArgument("requesterBroadcastId", (string)item["requesterBroadcastId"] ?? ""); CPH.SetArgument("replaySource", (string)FindReplay(catalog, (string)item["replayId"])?["sourceType"] ?? "OBS");
        if ((bool?)item["showClapperboard"] == true)
        {
            CPH.SetArgument("replayTitle", (string)item["title"] ?? "Replay");
            CPH.ExecuteMethod("RTS - Action Replay - Core - Store", "ShowClapperboard");
        }
        var started = CPH.ExecuteMethod(PlaybackCode, "PlayReplay");
        CPH.UnsetGlobalVar(ReplayIdHandoffKey, false); CPH.UnsetGlobalVar(PlaybackQueueEntryHandoffKey, false); CPH.UnsetGlobalVar(PlaybackProfileHandoffKey, false);
        if (started) { CPH.SetGlobalVar(ActiveKey, (string)item["entryId"], false); CPH.SetGlobalVar(ActiveReplayKey, (string)item["replayId"], false); CPH.SetArgument("historyReplayId", (string)item["replayId"] ?? ""); CPH.SetArgument("historyReplayTitle", (string)item["title"] ?? "Replay"); CPH.SetArgument("historyReplayCreator", (string)FindReplay(catalog, (string)item["replayId"])?["creator"]?["name"] ?? ""); CPH.SetArgument("historyReplayRequester", (string)item["requesterName"] ?? ""); CPH.SetArgument("historyReplayPlatform", (string)item["requesterPlatform"] ?? ""); CPH.ExecuteMethod(CatalogAction, "RecordPlayed"); }
        return started;
    }

    private void EnqueueReplayQueued(JObject replay, string requesterId, string requesterName, string requesterPlatform, string broadcastId)
    {
        var creator = replay?["creator"] as JObject;
        CPH.SetArgument("messageEvent", "Replay Queued");
        CPH.SetArgument("replayId", (string)replay?["id"] ?? "");
        CPH.SetArgument("replayNumber", FindReplay(Catalog(Load()), (string)replay?["id"] ?? "") != null ? Array.IndexOf(Catalog(Load()).ToObject<JObject[]>(), FindReplay(Catalog(Load()), (string)replay?["id"] ?? "")) + 1 : 0);
        CPH.SetArgument("replayTitle", (string)replay?["title"] ?? "Replay");
        CPH.SetArgument("replayUserId", (string)creator?["id"] ?? "");
        CPH.SetArgument("replayUser", (string)creator?["name"] ?? "");
        CPH.SetArgument("replayPlatform", (string)creator?["platform"] ?? "");
        CPH.SetArgument("replaySourcePlatform", (string)replay?["sourceType"] ?? "OBS");
        CPH.SetArgument("requesterId", requesterId ?? "");
        CPH.SetArgument("requesterName", requesterName ?? "");
        CPH.SetArgument("requesterPlatform", requesterPlatform ?? "");
        CPH.SetArgument("requesterBroadcastId", broadcastId ?? "");
        CPH.ExecuteMethod("RTS - Action Replay - Core - Messaging", "Enqueue");
    }

    private void EnqueueReplayRemoved(JObject queueItem, JObject replay, string userId, string userName, string userPlatform, string broadcastId)
    {
        var creator = replay?["creator"] as JObject;
        CPH.SetArgument("messageEvent", "Replay Removed");
        CPH.SetArgument("replayId", (string)queueItem?["replayId"] ?? (string)replay?["id"] ?? "");
        CPH.SetArgument("replayNumber", "");
        CPH.SetArgument("replayTitle", (string)queueItem?["title"] ?? (string)replay?["title"] ?? "Replay");
        CPH.SetArgument("replayUserId", (string)creator?["id"] ?? "");
        CPH.SetArgument("replayUser", (string)creator?["name"] ?? "");
        CPH.SetArgument("replayPlatform", (string)creator?["platform"] ?? "");
        CPH.SetArgument("replaySourcePlatform", (string)replay?["sourceType"] ?? "OBS");
        CPH.SetArgument("requesterId", userId ?? "");
        CPH.SetArgument("requesterName", userName ?? "");
        CPH.SetArgument("requesterPlatform", NormalizePlatform(userPlatform) ?? "");
        CPH.SetArgument("requesterBroadcastId", broadcastId ?? "");
        CPH.ExecuteMethod("RTS - Action Replay - Core - Messaging", "Enqueue");
    }

    private void SendPlaylistMessage(string text)
    {
        var key = "rts.actionreplay.message.playlist"; var playlistText = text; CPH.SetArgument("replayPlaylist", playlistText); var configured = CPH.GetGlobalVar<string>(key + ".text", true); var chatText = string.IsNullOrWhiteSpace(configured) ? playlistText : CPH.Parse(configured, new Dictionary<string, object> { ["replayPlaylist"] = playlistText });
        if (CPH.GetGlobalVar<bool?>(key + ".chat", true) ?? true) SendOriginMessage(chatText);
        {
            var operation = new JObject {
                ["replayCommand"] = "playlist-panel",
                ["replayPlaylist"] = playlistText,
                ["panelType"] = "playlist",
                ["replayPanelWidth"] = CPH.GetGlobalVar<int?>("rts.actionreplay.panel.width", true) ?? 500,
                ["replayPanelHeight"] = CPH.GetGlobalVar<int?>("rts.actionreplay.panel.height", true) ?? 700
            };
            CPH.SetGlobalVar(PanelOperationKey, operation.ToString(Newtonsoft.Json.Formatting.None), false);
            CPH.ExecuteMethod(ResolverAction, "ResolvePanel");
        }
    }


    private void SendOriginMessage(string message)
    {
        var platform = GetRequestPlatform(); if (string.Equals(platform, "Kick", StringComparison.OrdinalIgnoreCase)) { CPH.SendKickMessage(message); return; } if (string.Equals(platform, "YouTube", StringComparison.OrdinalIgnoreCase)) { CPH.SendYouTubeMessageToLatestMonitored(message); return; } if (string.Equals(platform, "Twitch", StringComparison.OrdinalIgnoreCase)) { CPH.SendMessage(message); return; } CPH.LogWarn("RTS Action Replay: unable to route playlist chat response because the originating platform is unknown.");
    }

    private string GetRequestPlatform() { if (CPH.TryGetArg("userType", out string userType) && !string.IsNullOrWhiteSpace(userType)) return NormalizePlatform(userType); try { return NormalizePlatform(CPH.GetSource().ToString()); } catch { return null; } }
    private string NormalizePlatform(string platform) { if (string.Equals(platform, "Kick", StringComparison.OrdinalIgnoreCase)) return "Kick"; if (string.Equals(platform, "YouTube", StringComparison.OrdinalIgnoreCase)) return "YouTube"; if (string.Equals(platform, "Twitch", StringComparison.OrdinalIgnoreCase)) return "Twitch"; return null; }
    private string ReadArgumentOrGlobal(string argument, string globalKey) { if (CPH.TryGetArg(argument, out string value) && !string.IsNullOrWhiteSpace(value)) return value.Trim(); return CPH.GetGlobalVar<string>(globalKey, false); }
    private string Arg(string name, string fallback) { return CPH.TryGetArg(name, out string value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback; }
    private string ArgItem(JObject item,string name,string fallback) { var value=(string)item[name]; return string.IsNullOrWhiteSpace(value)?fallback:value; }
    private JObject FindReplay(JArray catalog, string replayId) { for (var i = 0; i < catalog.Count; i++) { var replay = catalog[i] as JObject; if (replay != null && string.Equals((string)replay["id"], replayId, StringComparison.OrdinalIgnoreCase)) return replay; } return null; }
    private string ActiveId() { var active = CPH.GetGlobalVar<string>(ActiveKey, false); return string.IsNullOrWhiteSpace(active) ? null : active; }
    private bool IsPaused() => CPH.GetGlobalVar<bool?>(PausedKey, false) ?? false;
    private bool PersistQueue() => CPH.GetGlobalVar<bool?>(PersistKey, true) ?? false;
    private void HidePlayer() { CPH.SetArgument("replayCommand", "hide"); CPH.TriggerEvent("RTS-Action Replay", true); }
    private JArray LoadQueue() { var persist = PersistQueue(); var raw = CPH.GetGlobalVar<string>(QueueKey, persist); try { return string.IsNullOrWhiteSpace(raw) ? new JArray() : JArray.Parse(raw); } catch { return new JArray(); } }
    private void SaveQueue(JArray queue) => CPH.SetGlobalVar(QueueKey, queue.ToString(Newtonsoft.Json.Formatting.None), PersistQueue());
    private void SaveQueueAndClearOtherStore(JArray queue) { var persist = PersistQueue(); SaveQueue(queue); CPH.UnsetGlobalVar(QueueKey, !persist); }
    private JObject Load() { var raw = CPH.GetGlobalVar<string>(DataKey, true); try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { return new JObject(); } }
    private JArray Catalog(JObject data) => data["catalog"] as JArray ?? new JArray();
}
