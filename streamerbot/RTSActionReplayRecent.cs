using System;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string LegacyCatalogKey = "rts.actionreplay.catalog";
    private const string EventName = "RTS-Action Replay";
    private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";
    private const string AnimationAction = "RTS - Action Replay - Core - Animation";
    private const string ReplayIdHandoffKey = "rts.actionreplay.handoff.replayId";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";
    private const int MaxChatMessageLength = 500;

    public bool Execute() => PlayRecent();

    public bool ListRecent()
    {
        var data = Load();
        var catalog = (JArray)data["catalog"] ?? new JArray();
        var recentIds = (JArray)data["recentIds"] ?? new JArray();
        var entries = recentIds.Select((item, i) =>
        {
            var id = Convert.ToString(item);
            var replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals(Convert.ToString(x["id"]), id, StringComparison.OrdinalIgnoreCase));
            if (replay == null) return null;
            var title = Convert.ToString(replay["title"]);
            var creator = replay["creator"] as JObject;
            var requester = Convert.ToString(creator?["name"]);
            if (string.IsNullOrWhiteSpace(requester)) requester = "Unknown";
            return "#" + (i + 1) + " " + title + " — " + requester;
        }).Where(x => x != null).ToList();

        var panelEntries = new JArray();
        foreach (var item in recentIds.Select((token, i) => new { token, i }))
        {
            var id = Convert.ToString(item.token);
            var replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals(Convert.ToString(x["id"]), id, StringComparison.OrdinalIgnoreCase));
            if (replay == null) continue;
            var creator = replay["creator"] as JObject;
            var requester = Convert.ToString(creator?["name"]);
            if (string.IsNullOrWhiteSpace(requester)) requester = "Unknown";
            panelEntries.Add(new JObject { ["number"] = item.i + 1, ["title"] = Convert.ToString(replay["title"]), ["requester"] = requester, ["avatarUrl"] = GetAvatarUrl(creator) });
        }

        var fullList = entries.Count == 0 ? "There are no recent replays." : string.Join(" | ", entries);
        CPH.SetArgument("replayRecent", fullList); CPH.SetArgument("replayRecentData", panelEntries.ToString(Newtonsoft.Json.Formatting.None));
        SendRecentMessage(fullList);
        return true;
    }

    public bool PlayRecent()
    {
        var data = Load(); var catalog = (JArray)data["catalog"] ?? new JArray(); var recentIds = (JArray)data["recentIds"] ?? new JArray();
        if (recentIds.Count == 0) { CPH.SendMessage("There are no recent replays."); return false; }
        var selector = "1"; CPH.TryGetArg("rawInput", out string rawInput); if (!string.IsNullOrWhiteSpace(rawInput)) selector = rawInput.Trim();
        JObject replay = null;
        if (int.TryParse(selector, out var index) && index > 0 && index <= recentIds.Count) { var id = Convert.ToString(recentIds[index - 1]); replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals(Convert.ToString(x["id"]), id, StringComparison.OrdinalIgnoreCase)); }
        else replay = recentIds.Select(item => Convert.ToString(item)).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => catalog.OfType<JObject>().FirstOrDefault(x => string.Equals(Convert.ToString(x["id"]), id, StringComparison.OrdinalIgnoreCase))).FirstOrDefault(x => x != null && string.Equals(Convert.ToString(x["title"]), selector, StringComparison.OrdinalIgnoreCase));
        if (replay == null) { CPH.SendMessage("Recent replay not found."); return false; }
        CPH.SetGlobalVar(ReplayIdHandoffKey, Convert.ToString(replay["id"]), false); CPH.SetGlobalVar(EntryPointHandoffKey, "recent", false); CPH.UnsetGlobalVar(ResolvedProfileHandoffKey, false);
        if (!CPH.ExecuteMethod(AnimationAction, "ResolveEntryPointProfile")) return false;
        return CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay");
    }

    private string GetAvatarUrl(JObject creator)
    {
        var id = Convert.ToString(creator?["id"]); var name = Convert.ToString(creator?["name"]);
        try { if (!string.IsNullOrWhiteSpace(id)) return CPH.TwitchGetExtendedUserInfoById(id)?.ProfileImageUrl ?? ""; if (!string.IsNullOrWhiteSpace(name)) return CPH.TwitchGetExtendedUserInfoByLogin(name)?.ProfileImageUrl ?? ""; } catch { }
        return "";
    }

    private void SendRecentMessage(string fullList)
    {
        var key = "rts.actionreplay.message.recent"; var text = CPH.GetGlobalVar<string>(key + ".text", true); if (string.IsNullOrWhiteSpace(text)) text = "%replayRecent%"; text = CPH.Parse(text);
        if (CPH.GetGlobalVar<bool?>(key + ".chat", true) ?? true)
        {
            var message = "";
            foreach (var entry in text.Split(new[] { " | " }, StringSplitOptions.None)) { var next = message.Length == 0 ? entry : message + " | " + entry; if (next.Length > MaxChatMessageLength) { if (message.Length > 0) CPH.SendMessage(message); message = entry.Length <= MaxChatMessageLength ? entry : entry.Substring(0, MaxChatMessageLength); } else message = next; }
            if (message.Length > 0) CPH.SendMessage(message);
        }
        if (CPH.GetGlobalVar<bool?>(key + ".overlay", true) ?? true)
        {
            var panelPosition = CPH.GetGlobalVar<string>("rts.actionreplay.panel.position", true); var panelPositions = CPH.GetGlobalVar<string>("rts.actionreplay.panel.positions", true); if (string.IsNullOrWhiteSpace(panelPosition)) panelPosition = "Center";
            CPH.SetArgument("replayPanelPosition", panelPosition); CPH.SetArgument("replayPanelPositions", panelPositions ?? "");
            CPH.SetArgument("replayPanelAnimation", ResolvePanelAnimation(panelPosition));
            CPH.SetArgument("replayCommand", "recent-list"); CPH.SetArgument("replayRecent", fullList); CPH.TriggerEvent(EventName, true);
        }
    }

    private string ResolvePanelAnimation(string panelPosition)
    {
        const string prefix = "rts.actionreplay.panel.animation.";
        var profilesRaw = CPH.GetGlobalVar<string>(prefix + "profiles", true); JArray profiles;
        try { profiles = string.IsNullOrWhiteSpace(profilesRaw) ? new JArray() : JArray.Parse(profilesRaw); } catch { profiles = new JArray(); }
        var configured = CPH.GetGlobalVar<string>(prefix + "entry.recent", true) ?? "Default"; string profile = "default";
        foreach (var item in profiles.OfType<JObject>()) if (string.Equals((string)item["id"], configured, StringComparison.Ordinal) || string.Equals((string)item["name"], configured, StringComparison.OrdinalIgnoreCase)) { profile = (string)item["id"] ?? "default"; break; }
        var start = ReadPanelSequence(prefix + profile + ".startSequence"); var end = ReadPanelSequence(prefix + profile + ".endSequence");
        if (start.Count == 0) start = new JArray(); if (end.Count == 0) end = new JArray();
        ReplacePanelPosition(start, panelPosition); ReplacePanelPosition(end, panelPosition);
        if (start.Count == 0) start.Add(new JObject { ["position"] = "Hidden Left", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });
        if (start.Count == 1) start.Add(new JObject { ["position"] = panelPosition, ["duration"] = 600, ["delay"] = 0, ["easing"] = "ease-out" });
        if (end.Count == 0) end.Add(new JObject { ["position"] = panelPosition, ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });
        if (end.Count == 1) end.Add(new JObject { ["position"] = "Hidden Left", ["duration"] = 600, ["delay"] = 0, ["easing"] = "ease-in" });
        return new JObject { ["id"] = profile, ["name"] = profile == "default" ? "Default" : profile, ["start"] = start, ["end"] = end }.ToString(Newtonsoft.Json.Formatting.None);
    }

    private JArray ReadPanelSequence(string key) { var raw = CPH.GetGlobalVar<string>(key, true); if (string.IsNullOrWhiteSpace(raw)) return new JArray(); try { return JArray.Parse(raw); } catch { return new JArray(); } }
    private void ReplacePanelPosition(JArray sequence, string panelPosition) { foreach (var item in sequence.OfType<JObject>()) if (string.Equals((string)item["position"], "__PANEL_POSITION__", StringComparison.Ordinal)) item["position"] = panelPosition; }

    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true); JObject data;
        if (string.IsNullOrWhiteSpace(raw)) data = new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() };
        else { try { data = JObject.Parse(raw); } catch { data = new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() }; } }
        var catalog = data["catalog"] as JArray; var legacy = data["replays"] as JArray;
        if (catalog == null) catalog = legacy ?? new JArray(); else if (legacy != null && legacy.Count > 0) MergeCatalog(catalog, legacy);
        var external = CPH.GetGlobalVar<string>(LegacyCatalogKey, true); if (!string.IsNullOrWhiteSpace(external)) { try { var externalData = JObject.Parse(external); var externalCatalog = externalData["catalog"] as JArray; if (externalCatalog != null) MergeCatalog(catalog, externalCatalog); } catch { } }
        data["version"] = 2; data["catalog"] = catalog; data["recentIds"] = data["recentIds"] as JArray ?? new JArray(); data.Remove("replays"); return data;
    }

    private void MergeCatalog(JArray target, JArray source)
    {
        foreach (var token in source)
        {
            var item = token as JObject; if (item == null) continue; var id = (string)item["id"]; var type = (string)item["sourceType"] ?? "OBS"; var sourceId = (string)item["sourceId"];
            if (target.OfType<JObject>().Any(x => (!string.IsNullOrWhiteSpace(id) && string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrWhiteSpace(sourceId) && string.Equals((string)x["sourceType"] ?? "OBS", type, StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceId"], sourceId, StringComparison.OrdinalIgnoreCase)))) continue;
            var clone = (JObject)item.DeepClone(); if (string.IsNullOrWhiteSpace((string)clone["sourceType"])) clone["sourceType"] = "OBS"; if (string.IsNullOrWhiteSpace((string)clone["sourceId"])) clone["sourceId"] = (string)clone["id"] ?? ""; if (clone["plays"] == null) clone["plays"] = 0; if (clone["users"] == null) clone["users"] = new JObject(); target.Add(clone);
        }
    }
}