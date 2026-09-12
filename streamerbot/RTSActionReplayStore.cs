using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string LegacyCatalogKey = "rts.actionreplay.catalog";
    private const string TitleKey = "rts.actionreplay.replayTitle";
    private const string MaxHistoryKey = "rts.actionreplay.maxHistory";
    private const string PendingKey = "rts.actionreplay.pendingSaves";
    private const string FileTypesKey = "rts.actionreplay.replayFileTypes";
    private const string PlaylistAction = "RTS Action Replay Playlist";
    private const string AnimationAction = "RTS - Action Replay - Core - Animation";
    private const string ReplayIdHandoffKey = "rts.actionreplay.handoff.replayId";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";

    public bool Execute() => Initialize();
    public bool Initialize() { Save(Load()); return true; }

    public bool AddReplay()
    {
        if (!(CPH.GetGlobalVar<bool?>("rts.actionreplay.autoAdd", true) ?? true)) return true;
        string path; if (!CPH.TryGetArg("fullPath", out path) || string.IsNullOrWhiteSpace(path) || !File.Exists(path) || !IsReplayFile(path)) return false;
        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        if (!string.IsNullOrWhiteSpace(folder) && !Path.GetFullPath(path).StartsWith(Path.GetFullPath(folder).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) return false;
        if (Path.GetExtension(path).Equals(".tmp", StringComparison.OrdinalIgnoreCase) || !Stable(path)) return false;
        var data = Load(); var catalog = GetCatalog(data); var file = Path.GetFileName(path);
        if (catalog.OfType<JObject>().Any(x => string.Equals((string)x["file"], file, StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceType"] ?? "OBS", "OBS", StringComparison.OrdinalIgnoreCase))) return true;
        var now = DateTime.Now; var id = now.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        var creatorId = Get("userId"); var creatorName = Get("userName"); ApplyPendingCreator(ref creatorId, ref creatorName);
        CPH.SetArgument("replayId", id); CPH.SetArgument("replayFile", file); CPH.SetArgument("replayPath", path); CPH.SetArgument("replayName", Path.GetFileNameWithoutExtension(path)); CPH.SetArgument("replayNumber", 1); CPH.SetArgument("replayDate", now.ToString("yyyy-MM-dd")); CPH.SetArgument("replayTime", now.ToString("HH:mm:ss")); CPH.SetArgument("replayPlays", 0); CPH.SetArgument("replayUser", creatorName); CPH.SetArgument("replayUserId", creatorId); CPH.SetArgument("replayUserPlays", 0); CPH.SetArgument("replayTitle", "");
        var title = CPH.Parse(CPH.GetGlobalVar<string>(TitleKey, true) ?? "%replayName%"); if (string.IsNullOrWhiteSpace(title)) title = Path.GetFileNameWithoutExtension(path);
        var replay = new JObject { ["id"] = id, ["sourceType"] = "OBS", ["sourceId"] = id, ["file"] = file, ["filePath"] = path, ["title"] = title, ["customTitle"] = false, ["added"] = now.ToString("o"), ["captured"] = now.ToString("o"), ["acquisitionMethod"] = "OBSReplayBuffer", ["creator"] = new JObject { ["id"] = creatorId, ["name"] = creatorName }, ["plays"] = 0, ["users"] = new JObject() };
        catalog.Insert(0, replay); AddRecent(data, id); TrimRecent(data); Save(data);
        CPH.LogInfo($"RTS Action Replay: added {title} ({id})"); CPH.SetArgument("replayTitle", title); SendStoreMessage("save"); if (CPH.GetGlobalVar<bool?>("rts.actionreplay.autoPlay", true) ?? false) BroadcastReplay(replay); return true;
    }

    public bool NameReplay()
    {
        string indexInput; string rawInput; if (!CPH.TryGetArg("input0", out indexInput) || !CPH.TryGetArg("rawInput", out rawInput)) return false;
        if (!int.TryParse(indexInput, out var index)) { CPH.SendMessage("Please provide a valid replay number."); return false; }
        var title = (rawInput ?? "").Trim(); if (title.StartsWith(indexInput + " ", StringComparison.OrdinalIgnoreCase)) title = title.Substring(indexInput.Length).Trim(); if (title.Length == 0) { CPH.SendMessage("Please provide a replay title."); return false; }
        var data = Load(); var list = GetCatalog(data); if (index < 1 || index > list.Count) { CPH.SendMessage($"Replay #{index} does not exist."); return false; }
        var target = (JObject)list[index - 1];
        for (var i = 0; i < list.Count; i++) { var other = (JObject)list[i]; if (ReferenceEquals(other, target) || !((bool?)other["customTitle"] ?? false)) continue; if (string.Equals((string)other["title"], title, StringComparison.OrdinalIgnoreCase)) { CPH.SendMessage("That title already exists."); return false; } }
        target["title"] = title; target["customTitle"] = true; Save(data); CPH.SetArgument("replayNumber", index); CPH.SetArgument("replayTitle", title); SendStoreMessage("name"); return true;
    }

    public bool ListPlaylist()
    {
        var list = GetCatalog(Load()); var message = list.Count == 0 ? "The replay playlist is empty." : string.Join(" | ", list.OfType<JObject>().Select((x, i) => "#" + (i + 1) + " " + (string)x["title"])); CPH.SetArgument("replayPlaylist", message); SendStoreMessage("playlist"); return true;
    }

    public bool CreatorLeaderboard()
    {
        var list = GetCatalog(Load()); var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.KeyValuePair<string, int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in list) { var creator = token["creator"] as JObject; var id = (string)creator?["id"]; if (string.IsNullOrWhiteSpace(id)) continue; var name = (string)creator["name"] ?? id; groups[id] = groups.ContainsKey(id) ? new System.Collections.Generic.KeyValuePair<string, int>(name, groups[id].Value + 1) : new System.Collections.Generic.KeyValuePair<string, int>(name, 1); }
        var results = groups.Values.OrderByDescending(x => x.Value).Take(5).ToList(); var message = results.Count == 0 ? "Replay creators: No replay creators yet." : "Replay creators: " + string.Join(" | ", results.Select((x, i) => "#" + (i + 1) + " " + x.Key + " (" + x.Value + ")")); CPH.SetArgument("replayLeaderboard", message); SendStoreMessage("creatorLeaderboard"); return true;
    }

    public bool PlaybackLeaderboard()
    {
        var list = GetCatalog(Load()); var users = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.KeyValuePair<string, int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in list) { var replayUsers = token["users"] as JObject; if (replayUsers == null) continue; foreach (var property in replayUsers.Properties()) { var user = property.Value as JObject; var name = (string)user?["name"] ?? property.Name; var plays = (int?)user?["plays"] ?? 0; users[property.Name] = users.ContainsKey(property.Name) ? new System.Collections.Generic.KeyValuePair<string, int>(name, users[property.Name].Value + plays) : new System.Collections.Generic.KeyValuePair<string, int>(name, plays); } }
        var results = users.Values.OrderByDescending(x => x.Value).Take(5).ToList(); var message = results.Count == 0 ? "Replay viewers: No replay plays yet." : "Replay viewers: " + string.Join(" | ", results.Select((x, i) => "#" + (i + 1) + " " + x.Key + " (" + x.Value + " plays)")); CPH.SetArgument("replayLeaderboard", message); SendStoreMessage("playbackLeaderboard"); return true;
    }

    private void SendStoreMessage(string type)
    {
        var key = "rts.actionreplay.message." + type; var text = CPH.GetGlobalVar<string>(key + ".text", true); if (string.IsNullOrWhiteSpace(text)) return; text = CPH.Parse(text); if (CPH.GetGlobalVar<bool?>(key + ".chat", true) ?? true) CPH.SendMessage(text); if (CPH.GetGlobalVar<bool?>(key + ".overlay", true) ?? false) { CPH.SetArgument("replayCommand", "message"); CPH.SetArgument("replayMessage", text); SetMessageStyleArguments(); CPH.TriggerEvent("RTS-Action Replay", true); }
    }

    private void SetMessageStyleArguments()
    {
        CPH.SetArgument("replayLogoUrl", CPH.GetGlobalVar<string>("rts.actionreplay.brandLogoUrl", true) ?? ""); CPH.SetArgument("replayMessageBoardColor", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.boardColor", true) ?? "#101416"); CPH.SetArgument("replayMessageStripeLight", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.stripeLight", true) ?? "#EEEEEE"); CPH.SetArgument("replayMessageStripeDark", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.stripeDark", true) ?? "#111111"); CPH.SetArgument("replayMessageAccent", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.accent", true) ?? "#0384CB"); CPH.SetArgument("replayMessageTextColor", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.textColor", true) ?? "#0384CB"); CPH.SetArgument("replayMessageFont", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.font", true) ?? "Arial, sans-serif"); CPH.SetArgument("replayMessageSize", GetSettingInt("rts.actionreplay.clapper.size", 100)); CPH.SetArgument("replayMessagePositionX", GetSettingInt("rts.actionreplay.clapper.positionX", 50)); CPH.SetArgument("replayMessagePositionY", GetSettingInt("rts.actionreplay.clapper.positionY", 50));
    }

    private int GetSettingInt(string key, int fallback) { try { object value = CPH.GetGlobalVar<object>(key, true); return value == null ? fallback : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture); } catch { return fallback; } }
    private bool IsReplayFile(string path) { var extension = Path.GetExtension(path); if (string.IsNullOrWhiteSpace(extension)) return false; var configured = CPH.GetGlobalVar<string>(FileTypesKey, true) ?? ".mp4, .mkv"; foreach (var raw in configured.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)) { var type = raw.Trim(); if (!type.StartsWith(".")) type = "." + type; if (string.Equals(extension, type, StringComparison.OrdinalIgnoreCase)) return true; } return false; }

    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true); JObject data; try { data = string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { data = new JObject(); }
        var catalog = data["catalog"] as JArray; var legacy = data["replays"] as JArray; if (catalog == null) catalog = legacy ?? new JArray(); else if (legacy != null) MergeCatalog(catalog, legacy);
        var external = CPH.GetGlobalVar<string>(LegacyCatalogKey, true); if (!string.IsNullOrWhiteSpace(external)) { try { var externalCatalog = JObject.Parse(external)["catalog"] as JArray; if (externalCatalog != null) MergeCatalog(catalog, externalCatalog); } catch { } }
        foreach (var item in catalog.OfType<JObject>()) { if (string.IsNullOrWhiteSpace((string)item["sourceType"])) item["sourceType"] = "OBS"; if (string.IsNullOrWhiteSpace((string)item["sourceId"])) item["sourceId"] = (string)item["id"] ?? ""; if (item["plays"] == null) item["plays"] = 0; if (item["users"] == null) item["users"] = new JObject(); }
        data["version"] = 2; data["catalog"] = catalog; data["recentIds"] = data["recentIds"] as JArray ?? BuildRecent(catalog); data.Remove("replays"); return data;
    }

    private JArray BuildRecent(JArray catalog)
    {
        var recent = new JArray(); var max = CPH.GetGlobalVar<int?>(MaxHistoryKey, true) ?? 20; foreach (var item in catalog.OfType<JObject>().OrderByDescending(x => ParseDate((string)x["added"])).Take(Math.Max(1, max))) recent.Add((string)item["id"]); return recent;
    }

    private JArray GetCatalog(JObject data) => (JArray)data["catalog"] ?? new JArray();
    private void Save(JObject data) { data["version"] = 2; data["catalog"] = data["catalog"] as JArray ?? new JArray(); data["recentIds"] = data["recentIds"] as JArray ?? new JArray(); data.Remove("replays"); CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true); CPH.SetGlobalVar("rts.actionreplay.recentIds", ((JArray)data["recentIds"]).ToString(Newtonsoft.Json.Formatting.None), true); }
    private void MergeCatalog(JArray target, JArray source) { foreach (var token in source) { var item = token as JObject; if (item == null) continue; var id = (string)item["id"]; var type = (string)item["sourceType"] ?? "OBS"; var sourceId = (string)item["sourceId"]; if (target.OfType<JObject>().Any(x => (!string.IsNullOrWhiteSpace(id) && string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrWhiteSpace(sourceId) && string.Equals((string)x["sourceType"] ?? "OBS", type, StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceId"], sourceId, StringComparison.OrdinalIgnoreCase)))) continue; var clone = (JObject)item.DeepClone(); if (string.IsNullOrWhiteSpace((string)clone["sourceType"])) clone["sourceType"] = "OBS"; if (string.IsNullOrWhiteSpace((string)clone["sourceId"])) clone["sourceId"] = (string)clone["id"] ?? ""; target.Add(clone); } }
    private void AddRecent(JObject data, string id) { var recent = (JArray)data["recentIds"] ?? new JArray(); for (var i = recent.Count - 1; i >= 0; i--) if (string.Equals((string)recent[i], id, StringComparison.OrdinalIgnoreCase)) recent.RemoveAt(i); recent.Insert(0, id); data["recentIds"] = recent; }
    private void TrimRecent(JObject data) { var recent = (JArray)data["recentIds"] ?? new JArray(); var max = CPH.GetGlobalVar<int?>(MaxHistoryKey, true) ?? 20; while (recent.Count > Math.Max(1, max)) recent.RemoveAt(recent.Count - 1); data["recentIds"] = recent; }
    private DateTime ParseDate(string value) { DateTime parsed; return DateTime.TryParse(value, out parsed) ? parsed : DateTime.MinValue; }
    private string Get(string key) { string value; return CPH.TryGetArg(key, out value) ? value ?? "" : ""; }
    private bool Stable(string path) { for (var i = 0; i < 5; i++) { var a = new FileInfo(path).Length; CPH.Wait(500); var b = new FileInfo(path).Length; if (a == b) return true; } return false; }
    private void ApplyPendingCreator(ref string id, ref string name) { if (!string.IsNullOrWhiteSpace(id)) return; var raw = CPH.GetGlobalVar<string>(PendingKey, false); if (string.IsNullOrWhiteSpace(raw)) return; try { var queue = JArray.Parse(raw); if (queue.Count == 0) return; var item = (JObject)queue[0]; queue.RemoveAt(0); CPH.SetGlobalVar(PendingKey, queue.ToString(Newtonsoft.Json.Formatting.None), false); DateTime queued; if (DateTime.TryParse((string)item["queued"], out queued) && DateTime.UtcNow - queued <= TimeSpan.FromSeconds(60)) { id = (string)item["id"] ?? ""; name = (string)item["name"] ?? ""; } } catch { } }

    private void BroadcastReplay(JObject replay)
    {
        CPH.SetGlobalVar(ReplayIdHandoffKey, (string)replay["id"] ?? "", false);
        CPH.SetGlobalVar(EntryPointHandoffKey, "obs", false);
        CPH.UnsetGlobalVar(ResolvedProfileHandoffKey, false);
        if (!CPH.ExecuteMethod(AnimationAction, "ResolveEntryPointProfile")) return;
        CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay");
    }
}