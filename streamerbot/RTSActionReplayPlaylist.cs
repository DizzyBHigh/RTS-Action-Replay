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
    private const string AnimationAction = "RTS - Action Replay - Core - Animation";
    private const string TitleAction = "RTS - Action Replay - Core - Title";
    private const string ReplayIdHandoffKey = "rts.actionreplay.handoff.replayId";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";
    private const string PlaybackProfileHandoffKey = "rts.actionreplay.handoff.playbackProfile";
    private const string ResolvedTitleProfileHandoffKey = "rts.actionreplay.handoff.resolvedTitleProfile";
    private const string PlaybackTitleProfileHandoffKey = "rts.actionreplay.handoff.playbackTitleProfile";
    private const string PlaybackQueueEntryHandoffKey = "rts.actionreplay.handoff.playbackQueueEntryId";

    public bool Execute() => View();

    public bool EnqueueCurrentReplay()
    {
        CPH.LogInfo("RTS Action Replay TRACE: EnqueueCurrentReplay entered.");
        var replayId = ReadArgumentOrGlobal("replayId", ReplayIdHandoffKey);
        if (string.IsNullOrWhiteSpace(replayId)) { CPH.LogWarn("RTS Action Replay TRACE: EnqueueCurrentReplay failed - replayId argument/handoff missing or empty."); return false; }
        CPH.LogInfo($"RTS Action Replay TRACE: EnqueueCurrentReplay replayId={replayId}.");
        var replay = FindReplay(Catalog(Load()), replayId);
        if (replay == null) { CPH.LogWarn($"RTS Action Replay TRACE: EnqueueCurrentReplay failed - replay {replayId} not found in catalog."); ClearHandoff(ReplayIdHandoffKey, ResolvedProfileHandoffKey, ResolvedTitleProfileHandoffKey); return false; }
        CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName); CPH.TryGetArg("userType", out string userType); CPH.TryGetArg("broadcast.id", out string broadcastId);
        var creator = replay["creator"] as JObject; var requester = string.IsNullOrWhiteSpace(userName) ? (string)creator?["name"] ?? "" : userName;
        var requesterPlatform = NormalizePlatform(userType);
        var profile = ResolveRequestedProfile();
        var titleProfile = ResolveRequestedTitleProfile(replay);
        CPH.LogInfo($"RTS Action Replay TRACE: EnqueueCurrentReplay resolved animationProfile={profile ?? "<null>"}; titleProfile={titleProfile ?? "<null>"}; requesterPlatform={requesterPlatform ?? "<none>"}.");
        var queue = LoadQueue();
        queue.Add(new JObject { ["entryId"] = Guid.NewGuid().ToString("N"), ["replayId"] = replayId, ["title"] = (string)replay["title"] ?? "Replay", ["requesterId"] = userId ?? "", ["requesterName"] = requester, ["requesterPlatform"] = requesterPlatform ?? "", ["requesterBroadcastId"] = broadcastId ?? "", ["animationProfileId"] = profile, ["titleProfileId"] = titleProfile, ["queued"] = DateTime.Now.ToString("o") });
        SaveQueue(queue); ClearHandoff(ReplayIdHandoffKey, ResolvedProfileHandoffKey, ResolvedTitleProfileHandoffKey);
        CPH.LogInfo($"RTS Action Replay TRACE: EnqueueCurrentReplay queued replay {replayId}; queueCount={queue.Count}; paused={IsPaused()}; active={ActiveId() ?? "<none>"}.");
        if (!IsPaused() && ActiveId() == null) { var started = PlayNext(queue); CPH.LogInfo($"RTS Action Replay TRACE: EnqueueCurrentReplay PlayNext returned {started}."); return started; }
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
        var queue = LoadQueue();
        var activeId = ActiveId();
        var cleared = 0;
        for (var i = queue.Count - 1; i >= 0; i--)
        {
            var item = queue[i] as JObject;
            if (string.IsNullOrWhiteSpace(activeId) || item == null || !string.Equals((string)item["entryId"], activeId, StringComparison.OrdinalIgnoreCase))
            {
                queue.RemoveAt(i);
                cleared++;
            }
        }
        SaveQueueAndClearOtherStore(queue);
        CPH.LogInfo($"RTS Action Replay: playlist Clear removed {cleared} waiting item(s); active={(string.IsNullOrWhiteSpace(activeId) ? "<none>" : activeId)}; remaining={queue.Count}.");
        SendPlaylistMessage(queue.Count == 0 ? "Playlist cleared." : "Playlist cleared; active replay retained.");
        return true;
    }

    public bool ClearAll()
    {
        var queue = LoadQueue();
        var cleared = queue.Count;
        queue.Clear();
        SaveQueueAndClearOtherStore(queue);
        CPH.SetGlobalVar(ActiveKey, "", false);
        CPH.SetGlobalVar(PausedKey, false, false);
        CPH.LogInfo($"RTS Action Replay: playlist ClearAll removed {cleared} item(s); active playback was not stopped; playlist pause state reset.");
        SendPlaylistMessage("Playlist completely cleared.");
        return true;
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
        CPH.SetGlobalVar(PausedKey, false, false); var queue = LoadQueue();
        if (ActiveId() != null || queue.Count == 0) return true;
        queue[0]["animationProfileId"] = ResolvePlaylistProfile();
        queue[0]["titleProfileId"] = ResolvePlaylistTitleProfile(); SaveQueue(queue); return PlayNext(queue);
    }

    public bool PlaybackEnded()
    {
        CPH.LogInfo("RTS Action Replay TRACE: PlaybackEnded entered.");
        if (!CPH.TryGetArg("replayId", out string replayId) || string.IsNullOrWhiteSpace(replayId))
        {
            CPH.LogWarn("RTS Action Replay TRACE: PlaybackEnded failed - replayId argument missing or empty.");
            return false;
        }
        CPH.TryGetArg("replayQueueEntryId", out string entryId);
        var activeId = ActiveId();
        var queue = LoadQueue();
        CPH.LogInfo($"RTS Action Replay TRACE: PlaybackEnded replayId={replayId}; queueEntryId={entryId ?? "<none>"}; active={activeId ?? "<none>"}; queueCount={queue.Count}.");
        JObject current = null;
        for (var i = 0; i < queue.Count; i++)
        {
            var item = queue[i] as JObject;
            if (item != null && string.Equals((string)item["replayId"], replayId, StringComparison.OrdinalIgnoreCase) && (string.IsNullOrWhiteSpace(entryId) || string.Equals((string)item["entryId"], entryId, StringComparison.OrdinalIgnoreCase)))
            {
                current = item;
                break;
            }
        }
        if (current == null)
        {
            CPH.LogWarn("RTS Action Replay TRACE: PlaybackEnded failed - matching queue entry was not found.");
            return false;
        }
        var currentEntryId = (string)current["entryId"];
        if (!string.Equals(currentEntryId, activeId, StringComparison.OrdinalIgnoreCase))
        {
            CPH.LogWarn($"RTS Action Replay TRACE: PlaybackEnded failed - matching entry is not active; matching={currentEntryId}; active={activeId ?? "<none>"}.");
            return false;
        }
        queue.Remove(current);
        SaveQueue(queue);
        CPH.SetGlobalVar(ActiveKey, "", false);
        CPH.SetGlobalVar(ActiveReplayKey, "", false);
        CPH.LogInfo($"RTS Action Replay TRACE: PlaybackEnded removed active entry {currentEntryId}; remaining={queue.Count}; paused={IsPaused()}.");
        if (IsPaused()) return true;
        if (queue.Count == 0) { HidePlayer(); CPH.LogInfo("RTS Action Replay TRACE: PlaybackEnded queue empty; hide requested."); return true; }
        var started = PlayNext(queue);
        CPH.LogInfo($"RTS Action Replay TRACE: PlaybackEnded PlayNext returned {started}; remaining={queue.Count}.");
        return started;
    }

    private bool PlayNext(JArray queue)
    {
        CPH.LogInfo($"RTS Action Replay TRACE: PlayNext entered; queueCount={queue.Count}.");
        if (queue.Count == 0) return true; var item = queue[0] as JObject; if (item == null) { CPH.LogWarn("RTS Action Replay TRACE: PlayNext failed - queue item is not an object."); return false; }
        var catalog = Catalog(Load()); var index = -1;
        for (var i = 0; i < catalog.Count; i++) { var replay = catalog[i] as JObject; if (replay != null && string.Equals((string)replay["id"], (string)item["replayId"], StringComparison.OrdinalIgnoreCase)) { index = i; break; } }
        if (index < 0) { CPH.LogWarn($"RTS Action Replay TRACE: PlayNext failed - replay {(string)item["replayId"]} not found in catalog."); return false; }
        var profile = (string)item["animationProfileId"]; if (string.IsNullOrWhiteSpace(profile)) profile = ResolvePlaylistProfile();
        var titleProfile = (string)item["titleProfileId"]; if (string.IsNullOrWhiteSpace(titleProfile)) titleProfile = ResolvePlaylistTitleProfile();
        CPH.SetGlobalVar(ReplayIdHandoffKey, (string)item["replayId"] ?? "", false);
        CPH.SetGlobalVar(PlaybackQueueEntryHandoffKey, (string)item["entryId"] ?? "", false);
        CPH.SetGlobalVar(PlaybackProfileHandoffKey, profile, false);
        CPH.SetGlobalVar(PlaybackTitleProfileHandoffKey, titleProfile, false);
        CPH.SetArgument("rawInput", (index + 1).ToString()); CPH.SetArgument("replayQueueEntryId", (string)item["entryId"]); CPH.SetArgument("replayAnimationProfileId", profile); CPH.SetArgument("replayTitleProfileId", titleProfile); CPH.SetArgument("requesterPlatform", (string)item["requesterPlatform"] ?? ""); CPH.SetArgument("requesterBroadcastId", (string)item["requesterBroadcastId"] ?? "");
        CPH.LogInfo($"RTS Action Replay TRACE: PlayNext calling Playback; catalogIndex={index + 1}; entryId={(string)item["entryId"]}; animationProfile={profile ?? "<null>"}; titleProfile={titleProfile ?? "<null>"}; requesterPlatform={(string)item["requesterPlatform"] ?? "<none>"}.");
        var started = CPH.ExecuteMethod(PlaybackCode, "PlayReplay");
        CPH.UnsetGlobalVar(ReplayIdHandoffKey, false); CPH.UnsetGlobalVar(PlaybackQueueEntryHandoffKey, false); CPH.UnsetGlobalVar(PlaybackProfileHandoffKey, false); CPH.UnsetGlobalVar(PlaybackTitleProfileHandoffKey, false);
        CPH.LogInfo($"RTS Action Replay TRACE: Playback PlayReplay returned {started}.");
        if (started)
        {
            CPH.SetGlobalVar(ActiveKey, (string)item["entryId"], false);
            CPH.SetGlobalVar(ActiveReplayKey, (string)item["replayId"], false);
            CPH.SetArgument("historyReplayId", (string)item["replayId"] ?? "");
            CPH.SetArgument("historyReplayTitle", (string)item["title"] ?? "Replay");
            CPH.SetArgument("historyReplayCreator", (string)FindReplay(catalog, (string)item["replayId"])?["creator"]?["name"] ?? "");
            CPH.SetArgument("historyReplayRequester", (string)item["requesterName"] ?? "");
            CPH.SetArgument("historyReplayPlatform", (string)item["requesterPlatform"] ?? "");
            CPH.ExecuteMethod(CatalogAction, "RecordPlayed");
        }
        return started;
    }

    private string ResolveRequestedProfile()
    {
        if (CPH.TryGetArg("replayAnimationProfileId", out string requested) && !string.IsNullOrWhiteSpace(requested)) return requested.Trim();
        var resolved = CPH.GetGlobalVar<string>(ResolvedProfileHandoffKey, false); if (!string.IsNullOrWhiteSpace(resolved)) return resolved.Trim();
        return ResolvePlaylistProfile();
    }

    private string ResolveRequestedTitleProfile(JObject replay)
    {
        if (CPH.TryGetArg("replayTitleProfileId", out string requested) && !string.IsNullOrWhiteSpace(requested)) return requested.Trim();
        var resolved = CPH.GetGlobalVar<string>(ResolvedTitleProfileHandoffKey, false); if (!string.IsNullOrWhiteSpace(resolved)) return resolved.Trim();
        var entryPoint = CPH.TryGetArg("replayTitleEntryPoint", out string requestedEntry) && !string.IsNullOrWhiteSpace(requestedEntry) ? requestedEntry.Trim() : SourceEntryPoint((string)replay["sourceType"]);
        if (!string.IsNullOrWhiteSpace(entryPoint)) return ResolveTitleEntryPoint(entryPoint);
        return "default";
    }

    private string ResolveTitleEntryPoint(string entryPoint)
    {
        CPH.SetArgument("titleEntryPoint", entryPoint); CPH.UnsetGlobalVar(ResolvedTitleProfileHandoffKey, false);
        if (CPH.ExecuteMethod(TitleAction, "ResolveEntryPointProfile")) { var profile = CPH.GetGlobalVar<string>(ResolvedTitleProfileHandoffKey, false); CPH.UnsetGlobalVar(ResolvedTitleProfileHandoffKey, false); if (!string.IsNullOrWhiteSpace(profile)) return profile.Trim(); }
        return "default";
    }

    private string ResolvePlaylistProfile()
    {
        CPH.SetGlobalVar(EntryPointHandoffKey, "playlist", false); CPH.UnsetGlobalVar(ResolvedProfileHandoffKey, false);
        if (CPH.ExecuteMethod(AnimationAction, "ResolveEntryPointProfile")) { var profile = CPH.GetGlobalVar<string>(ResolvedProfileHandoffKey, false); CPH.UnsetGlobalVar(ResolvedProfileHandoffKey, false); if (!string.IsNullOrWhiteSpace(profile)) return profile.Trim(); }
        return "default";
    }

    private string ResolvePlaylistTitleProfile()
    {
        CPH.SetArgument("titleEntryPoint", "playlist"); CPH.UnsetGlobalVar(ResolvedTitleProfileHandoffKey, false);
        if (CPH.ExecuteMethod(TitleAction, "ResolveEntryPointProfile")) { var profile = CPH.GetGlobalVar<string>(ResolvedTitleProfileHandoffKey, false); CPH.UnsetGlobalVar(ResolvedTitleProfileHandoffKey, false); if (!string.IsNullOrWhiteSpace(profile)) return profile.Trim(); }
        return "default";
    }

    private string SourceEntryPoint(string source)
    {
        if (string.Equals(source, "Kick", StringComparison.OrdinalIgnoreCase)) return "kick";
        if (string.Equals(source, "YouTube", StringComparison.OrdinalIgnoreCase)) return "youtube";
        if (string.Equals(source, "Twitch", StringComparison.OrdinalIgnoreCase)) return "twitch";
        if (string.Equals(source, "OBS", StringComparison.OrdinalIgnoreCase)) return "obs";
        return "catalog";
    }

    private void SendPlaylistMessage(string text)
    {
        var key = "rts.actionreplay.message.playlist";
        var playlistText = text;
        CPH.SetArgument("replayPlaylist", playlistText);
        var configured = CPH.GetGlobalVar<string>(key + ".text", true);
        var chatText = string.IsNullOrWhiteSpace(configured)
            ? playlistText
            : CPH.Parse(configured, new Dictionary<string, object> { ["replayPlaylist"] = playlistText });
        if (CPH.GetGlobalVar<bool?>(key + ".chat", true) ?? true) SendOriginMessage(chatText);
        if (CPH.GetGlobalVar<bool?>(key + ".overlay", true) ?? false)
        {
            CPH.SetArgument("replayPanelWidth", CPH.GetGlobalVar<int?>("rts.actionreplay.panel.width", true) ?? 500);
            CPH.SetArgument("replayPanelHeight", CPH.GetGlobalVar<int?>("rts.actionreplay.panel.height", true) ?? 700);
            CPH.SetArgument("panelType", "playlist");
            CPH.ExecuteMethod(AnimationAction, "ResolvePanelAnimation");
            CPH.SetArgument("replayCommand", "playlist-panel");
            CPH.SetArgument("replayPlaylist", playlistText);
            CPH.TriggerEvent("RTS-Action Replay", true);
        }
    }

    private void SendOriginMessage(string message)
    {
        var platform = GetRequestPlatform();
        if (string.Equals(platform, "Kick", StringComparison.OrdinalIgnoreCase)) { CPH.SendKickMessage(message); return; }
        if (string.Equals(platform, "YouTube", StringComparison.OrdinalIgnoreCase)) { CPH.SendYouTubeMessageToLatestMonitored(message); return; }
        if (string.Equals(platform, "Twitch", StringComparison.OrdinalIgnoreCase)) { CPH.SendMessage(message); return; }
        CPH.LogWarn("RTS Action Replay: unable to route playlist chat response because the originating platform is unknown.");
    }

    private string GetRequestPlatform()
    {
        if (CPH.TryGetArg("userType", out string userType) && !string.IsNullOrWhiteSpace(userType)) return NormalizePlatform(userType);
        try { return NormalizePlatform(CPH.GetSource().ToString()); } catch { return null; }
    }

    private string NormalizePlatform(string platform)
    {
        if (string.Equals(platform, "Kick", StringComparison.OrdinalIgnoreCase)) return "Kick";
        if (string.Equals(platform, "YouTube", StringComparison.OrdinalIgnoreCase)) return "YouTube";
        if (string.Equals(platform, "Twitch", StringComparison.OrdinalIgnoreCase)) return "Twitch";
        return null;
    }

    private string ReadArgumentOrGlobal(string argument, string globalKey) { if (CPH.TryGetArg(argument, out string value) && !string.IsNullOrWhiteSpace(value)) return value.Trim(); return CPH.GetGlobalVar<string>(globalKey, false); }
    private void ClearHandoff(params string[] keys) { foreach (var key in keys) CPH.UnsetGlobalVar(key, false); }
    private JObject FindReplay(JArray catalog, string replayId) { for (var i = 0; i < catalog.Count; i++) { var replay = catalog[i] as JObject; if (replay != null && string.Equals((string)replay["id"], replayId, StringComparison.OrdinalIgnoreCase)) return replay; } return null; }
    private string ActiveId()
    {
        var active = CPH.GetGlobalVar<string>(ActiveKey, false);
        return string.IsNullOrWhiteSpace(active) ? null : active;
    }
    private bool IsPaused() => CPH.GetGlobalVar<bool?>(PausedKey, false) ?? false;
    private bool PersistQueue() => CPH.GetGlobalVar<bool?>(PersistKey, true) ?? false;
    private void HidePlayer() { CPH.SetArgument("replayCommand", "hide"); CPH.TriggerEvent("RTS-Action Replay", true); }
    private JArray LoadQueue() { var persist = PersistQueue(); var raw = CPH.GetGlobalVar<string>(QueueKey, persist); try { return string.IsNullOrWhiteSpace(raw) ? new JArray() : JArray.Parse(raw); } catch { return new JArray(); } }
    private void SaveQueue(JArray queue) => CPH.SetGlobalVar(QueueKey, queue.ToString(Newtonsoft.Json.Formatting.None), PersistQueue());
    private void SaveQueueAndClearOtherStore(JArray queue)
    {
        var persist = PersistQueue();
        SaveQueue(queue);
        CPH.UnsetGlobalVar(QueueKey, !persist);
    }
    private JObject Load() { var raw = CPH.GetGlobalVar<string>(DataKey, true); try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { return new JObject(); } }
    private JArray Catalog(JObject data) => data["catalog"] as JArray ?? new JArray();
}
