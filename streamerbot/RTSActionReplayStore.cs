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

    public bool Execute() => Initialize();

    public bool Initialize()
    {
        var data = Load();
        Save(data);
        return true;
    }

    public bool AddReplay()
    {
        if (!(CPH.GetGlobalVar<bool?>("rts.actionreplay.autoAdd", true) ?? true)) return true;
        string path; if (!CPH.TryGetArg("fullPath", out path) || string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        if (!IsReplayFile(path)) return false;
        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        if (!string.IsNullOrWhiteSpace(folder) && !Path.GetFullPath(path).StartsWith(Path.GetFullPath(folder).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) return false;
        if (Path.GetExtension(path).Equals(".tmp", StringComparison.OrdinalIgnoreCase) || !Stable(path)) return false;

        var data = Load();
        var list = GetCatalog(data);
        var file = Path.GetFileName(path);
        for (int i = 0; i < list.Count; i++)
            if (string.Equals((string)list[i]["file"], file, StringComparison.OrdinalIgnoreCase) && string.Equals((string)list[i]["sourceType"] ?? "OBS", "OBS", StringComparison.OrdinalIgnoreCase)) return true;

        var now = DateTime.Now;
        var id = now.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        var creatorId = Get("userId"); var creatorName = Get("userName"); ApplyPendingCreator(ref creatorId, ref creatorName);
        CPH.SetArgument("replayId", id); CPH.SetArgument("replayFile", file); CPH.SetArgument("replayPath", path); CPH.SetArgument("replayName", Path.GetFileNameWithoutExtension(path)); CPH.SetArgument("replayNumber", 1); CPH.SetArgument("replayDate", now.ToString("yyyy-MM-dd")); CPH.SetArgument("replayTime", now.ToString("HH:mm:ss")); CPH.SetArgument("replayPlays", 0); CPH.SetArgument("replayUser", creatorName); CPH.SetArgument("replayUserId", creatorId); CPH.SetArgument("replayUserPlays", 0); CPH.SetArgument("replayTitle", "");
        var title = CPH.Parse(CPH.GetGlobalVar<string>(TitleKey, true) ?? "%replayName%"); if (string.IsNullOrWhiteSpace(title)) title = Path.GetFileNameWithoutExtension(path);
        var replay = new JObject
        {
            ["id"] = id,
            ["sourceType"] = "OBS",
            ["sourceId"] = id,
            ["file"] = file,
            ["filePath"] = path,
            ["title"] = title,
            ["customTitle"] = false,
            ["added"] = now.ToString("o"),
            ["captured"] = now.ToString("o"),
            ["acquisitionMethod"] = "OBSReplayBuffer",
            ["creator"] = new JObject { ["id"] = creatorId, ["name"] = creatorName },
            ["plays"] = 0,
            ["users"] = new JObject()
        };
        list.Insert(0, replay);
        AddRecent(data, id);
        Trim(list);
        TrimRecent(data);
        Save(data);
        CPH.LogInfo($"RTS Action Replay: added {title} ({id})");
        CPH.SetArgument("replayTitle", title);
        SendMessage("save");
        if (CPH.GetGlobalVar<bool?>("rts.actionreplay.autoPlay", true) ?? false) BroadcastReplay(replay);
        return true;
    }

    public bool NameReplay()
    {
        string indexInput; string rawInput; if (!CPH.TryGetArg("input0", out indexInput) || !CPH.TryGetArg("rawInput", out rawInput)) return false;
        int index; if (!int.TryParse(indexInput, out index)) { CPH.SendMessage("Please provide a valid replay number."); return false; }
        var title = (rawInput ?? "").Trim(); if (title.StartsWith(indexInput + " ", StringComparison.OrdinalIgnoreCase)) title = title.Substring(indexInput.Length).Trim(); if (title.Length == 0) { CPH.SendMessage("Please provide a replay title."); return false; }
        var data = Load(); var list = GetCatalog(data); if (index < 1 || index > list.Count) { CPH.SendMessage($"Replay #{index} does not exist."); return false; }
        var target = (JObject)list[index - 1];
        for (int i = 0; i < list.Count; i++) { var other = (JObject)list[i]; if (object.ReferenceEquals(other, target) || !((bool?)other["customTitle"] ?? false)) continue; if (string.Equals((string)other["title"], title, StringComparison.OrdinalIgnoreCase)) { CPH.SendMessage("That title already exists."); return false; } }
        target["title"] = title; target["customTitle"] = true; Save(data); CPH.SetArgument("replayNumber", index); CPH.SetArgument("replayTitle", title); SendMessage("name"); return true;
    }

    public bool ListPlaylist()
    {
        var list = GetCatalog(Load()); var message = ""; if (list.Count == 0) message = "The replay playlist is empty."; else for (int i = 0; i < list.Count; i++) message += (i > 0 ? " | " : "") + "#" + (i + 1) + " " + (string)list[i]["title"]; CPH.SetArgument("replayPlaylist", message); SendMessage("playlist"); return true;
    }

    public bool CreatorLeaderboard()
    {
        var list = GetCatalog(Load()); var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.KeyValuePair<string, int>>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < list.Count; i++) { var creator = (JObject)list[i]["creator"]; if (creator == null) continue; var id = (string)creator["id"]; if (string.IsNullOrWhiteSpace(id)) continue; var name = (string)creator["name"] ?? id; if (groups.ContainsKey(id)) groups[id] = new System.Collections.Generic.KeyValuePair<string, int>(name, groups[id].Value + 1); else groups[id] = new System.Collections.Generic.KeyValuePair<string, int>(name, 1); }
        var results = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>(groups.Values); results.Sort((a, b) => b.Value.CompareTo(a.Value)); var message = "Replay creators: "; var count = Math.Min(5, results.Count); if (count == 0) message += "No replay creators yet."; else for (int i = 0; i < count; i++) message += (i > 0 ? " | " : "") + "#" + (i + 1) + " " + results[i].Key + " (" + results[i].Value + ")"; CPH.SetArgument("replayLeaderboard", message); SendMessage("creatorLeaderboard"); return true;
    }

    public bool PlaybackLeaderboard()
    {
        var list = GetCatalog(Load()); var users = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.KeyValuePair<string, int>>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < list.Count; i++) { var replayUsers = (JObject)list[i]["users"]; if (replayUsers == null) continue; foreach (JProperty property in replayUsers.Properties()) { var user = (JObject)property.Value; var name = (string)user["name"] ?? property.Name; var plays = (int?)user["plays"] ?? 0; if (users.ContainsKey(property.Name)) users[property.Name] = new System.Collections.Generic.KeyValuePair<string, int>(name, users[property.Name].Value + plays); else users[property.Name] = new System.Collections.Generic.KeyValuePair<string, int>(name, plays); } }
        var results = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>(users.Values); results.Sort((a, b) => b.Value.CompareTo(a.Value)); var message = "Replay viewers: "; var count = Math.Min(5, results.Count); if (count == 0) message += "No replay plays yet."; else for (int i = 0; i < count; i++) message += (i > 0 ? " | " : "") + "#" + (i + 1) + " " + results[i].Key + " (" + results[i].Value + " plays)"; CPH.SetArgument("replayLeaderboard", message); SendMessage("playbackLeaderboard"); return true;
    }

    private void SendMessage(string type)
    {
        var key = "rts.actionreplay.message." + type; var text = CPH.GetGlobalVar<string>(key + ".text", true); if (string.IsNullOrWhiteSpace(text)) return; text = CPH.Parse(text); if (CPH.GetGlobalVar<bool?>(key + ".chat", true) ?? true) CPH.SendMessage(text);
        if (CPH.GetGlobalVar<bool?>(key + ".overlay", true) ?? false) { CPH.SetArgument("replayCommand", "message"); CPH.SetArgument("replayMessage", text); SetMessageStyleArguments(); CPH.TriggerEvent("RTS-Action Replay", true); }
    }

    private void SetMessageStyleArguments()
    {
        CPH.SetArgument("replayLogoUrl", CPH.GetGlobalVar<string>("rts.actionreplay.brandLogoUrl", true) ?? "");
        CPH.SetArgument("replayMessageBoardColor", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.boardColor", true) ?? "#101416"); CPH.SetArgument("replayMessageStripeLight", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.stripeLight", true) ?? "#EEEEEE"); CPH.SetArgument("replayMessageStripeDark", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.stripeDark", true) ?? "#111111"); CPH.SetArgument("replayMessageAccent", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.accent", true) ?? "#0384CB"); CPH.SetArgument("replayMessageTextColor", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.textColor", true) ?? "#0384CB"); CPH.SetArgument("replayMessageFont", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.font", true) ?? "Arial, sans-serif");
        var size = GetSettingInt("rts.actionreplay.clapper.size", 100); var x = GetSettingInt("rts.actionreplay.clapper.positionX", 50); var y = GetSettingInt("rts.actionreplay.clapper.positionY", 50);
        CPH.SetArgument("replayMessageSize", size); CPH.SetArgument("replayMessagePositionX", x); CPH.SetArgument("replayMessagePositionY", y); CPH.LogInfo("RTS Action Replay: clapper settings size=" + size + "%, position=" + x + "," + y + "%");
    }

    private int GetSettingInt(string key, int fallback) { try { object value = CPH.GetGlobalVar<object>(key, true); if (value == null) return fallback; return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture); } catch { return fallback; } }
    private double GetSettingDouble(string key, double fallback) { try { object value = CPH.GetGlobalVar<object>(key, true); if (value == null) return fallback; return Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture); } catch { return fallback; } }
    private bool IsReplayFile(string path) { var extension = Path.GetExtension(path); if (string.IsNullOrWhiteSpace(extension)) return false; var configured = CPH.GetGlobalVar<string>(FileTypesKey, true) ?? ".mp4, .mkv"; var types = configured.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries); for (int i = 0; i < types.Length; i++) { var type = types[i].Trim(); if (type.Length == 0) continue; if (!type.StartsWith(".")) type = "." + type; if (string.Equals(extension, type, StringComparison.OrdinalIgnoreCase)) return true; } return false; }

    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true);
        JObject data;
        if (string.IsNullOrWhiteSpace(raw)) data = new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() };
        else
        {
            try { data = JObject.Parse(raw); }
            catch { data = new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() }; }
        }

        var catalog = data["catalog"] as JArray;
        var legacy = data["replays"] as JArray;
        if (catalog == null) catalog = legacy ?? new JArray();
        else if (legacy != null && legacy.Count > 0) MergeCatalog(catalog, legacy);

        var external = CPH.GetGlobalVar<string>(LegacyCatalogKey, true);
        if (!string.IsNullOrWhiteSpace(external))
        {
            try
            {
                var externalData = JObject.Parse(external);
                var externalCatalog = externalData["catalog"] as JArray;
                if (externalCatalog != null) MergeCatalog(catalog, externalCatalog);
            }
            catch { }
        }

        data["version"] = 2;
        data["catalog"] = catalog;
        var recent = data["recentIds"] as JArray ?? new JArray();
        if (recent.Count == 0)
        {
            var ordered = catalog.OfType<JObject>().OrderByDescending(x => ParseDate((string)x["added"])).ToList();
            foreach (var item in ordered.Take(20)) recent.Add((string)item["id"]);
        }
        data["recentIds"] = recent;
        data.Remove("replays");
        return data;
    }

    private JArray GetCatalog(JObject data) => (JArray)data["catalog"] ?? new JArray();

    private void Save(JObject data)
    {
        data["version"] = 2;
        if (data["catalog"] == null) data["catalog"] = new JArray();
        if (data["recentIds"] == null) data["recentIds"] = new JArray();
        data.Remove("replays");
        CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
        CPH.SetGlobalVar("rts.actionreplay.recentIds", ((JArray)data["recentIds"]).ToString(Newtonsoft.Json.Formatting.None), true);
    }

    private void MergeCatalog(JArray target, JArray source)
    {
        foreach (var token in source)
        {
            var item = token as JObject;
            if (item == null) continue;
            var id = (string)item["id"];
            var sourceType = (string)item["sourceType"] ?? "OBS";
            var sourceId = (string)item["sourceId"];
            var exists = target.OfType<JObject>().FirstOrDefault(x =>
                (!string.IsNullOrWhiteSpace(id) && string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(sourceId) && string.Equals((string)x["sourceType"] ?? "OBS", sourceType, StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceId"], sourceId, StringComparison.OrdinalIgnoreCase)));
            if (exists == null)
            {
                var clone = (JObject)item.DeepClone();
                if (string.IsNullOrWhiteSpace((string)clone["sourceType"])) clone["sourceType"] = "OBS";
                if (string.IsNullOrWhiteSpace((string)clone["sourceId"])) clone["sourceId"] = (string)clone["id"] ?? "";
                if (clone["plays"] == null) clone["plays"] = 0;
                if (clone["users"] == null) clone["users"] = new JObject();
                target.Add(clone);
            }
        }
    }

    private void AddRecent(JObject data, string id)
    {
        var recent = (JArray)data["recentIds"] ?? new JArray();
        for (var i = recent.Count - 1; i >= 0; i--) if (string.Equals((string)recent[i], id, StringComparison.OrdinalIgnoreCase)) recent.RemoveAt(i);
        recent.Insert(0, id);
        data["recentIds"] = recent;
    }

    private void TrimRecent(JObject data)
    {
        var recent = (JArray)data["recentIds"] ?? new JArray();
        var max = CPH.GetGlobalVar<int?>(MaxHistoryKey, true) ?? 20;
        while (recent.Count > Math.Max(1, max)) recent.RemoveAt(recent.Count - 1);
        data["recentIds"] = recent;
    }

    private DateTime ParseDate(string value) { DateTime parsed; return DateTime.TryParse(value, out parsed) ? parsed : DateTime.MinValue; }
    private string Get(string key) { string value; return CPH.TryGetArg(key, out value) ? value ?? "" : ""; }
    private bool Stable(string path) { for (var i = 0; i < 5; i++) { var a = new FileInfo(path).Length; CPH.Wait(500); var b = new FileInfo(path).Length; if (a == b) return true; } return false; }

    private void ApplyPendingCreator(ref string id, ref string name) { if (!string.IsNullOrWhiteSpace(id)) return; var raw = CPH.GetGlobalVar<string>(PendingKey, false); if (string.IsNullOrWhiteSpace(raw)) return; var queue = JArray.Parse(raw); if (queue.Count == 0) return; var item = (JObject)queue[0]; queue.RemoveAt(0); CPH.SetGlobalVar(PendingKey, queue.ToString(Newtonsoft.Json.Formatting.None), false); DateTime queued; if (DateTime.TryParse((string)item["queued"], out queued) && DateTime.UtcNow - queued <= TimeSpan.FromSeconds(60)) { id = (string)item["id"] ?? ""; name = (string)item["name"] ?? ""; } }

    private string GetAnimationProfileString(string profile, string field, string fallback)
    {
        var value = CPH.GetGlobalVar<string>("rts.actionreplay.animation." + profile + "." + field, true);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private double GetAnimationProfileDouble(string profile, string field, double fallback) { return GetSettingDouble("rts.actionreplay.animation." + profile + "." + field, fallback); }

    private void BroadcastReplay(JObject replay)
    {
        var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.httpMapping", true) ?? "replays";
        var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
        CPH.SetArgument("replayCommand", "load"); CPH.SetArgument("replayId", (string)replay["id"]); CPH.SetArgument("replayTitle", CPH.GetGlobalVar<string>("rts.actionreplay.newReplayTitle", true) ?? "New Replay"); CPH.SetArgument("replayUrl", $"http://localhost:{port}/{mapping.Trim('/')}/{CPH.UrlEncode((string)replay["file"])}"); CPH.SetArgument("replayAutoplay", true);
        CPH.SetArgument("replayShowControls", CPH.GetGlobalVar<bool?>("rts.actionreplay.showControls", true) ?? false); CPH.SetArgument("replayShowProgress", CPH.GetGlobalVar<bool?>("rts.actionreplay.showProgress", true) ?? true); CPH.SetArgument("replayPlaybackSpeed", GetSettingDouble("rts.actionreplay.playbackSpeed", 1.0)); CPH.SetArgument("replayPlaybackSpeedVisibility", CPH.GetGlobalVar<string>("rts.actionreplay.playbackSpeedVisibility", true) ?? "Only when greater or less than 1");
        CPH.SetArgument("replayFrameColor", CPH.GetGlobalVar<string>("rts.actionreplay.frameColor", true) ?? "#0384CB"); CPH.SetArgument("replayBorderColor", CPH.GetGlobalVar<string>("rts.actionreplay.borderColor", true) ?? "#FFFFFF"); CPH.SetArgument("replayBorderStyle", CPH.GetGlobalVar<string>("rts.actionreplay.borderStyle", true) ?? "Solid"); CPH.SetArgument("replayPositions", CPH.GetGlobalVar<string>("rts.actionreplay.positions", true) ?? "{\"Full Screen\":{\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}");
        CPH.SetArgument("replayStartPosition", GetAnimationProfileString("obsClip", "startPosition", "Full Screen")); CPH.SetArgument("replayEndPosition", GetAnimationProfileString("obsClip", "endPosition", "Full Screen")); CPH.SetArgument("replayAnimationDuration", GetAnimationProfileDouble("obsClip", "duration", .5)); CPH.SetArgument("replayAnimationEasing", GetAnimationProfileString("obsClip", "easing", "ease-in-out"));
        CPH.SetArgument("replayShowTitle", CPH.GetGlobalVar<bool?>("rts.actionreplay.showTitle", true) ?? true); CPH.SetArgument("replayShowBranding", CPH.GetGlobalVar<bool?>("rts.actionreplay.showBranding", true) ?? true); CPH.SetArgument("replayTitleDecorationPosition", CPH.GetGlobalVar<string>("rts.actionreplay.titleDecorationPosition", true) ?? "Suffix"); CPH.SetArgument("replayTitleDecoration", CPH.GetGlobalVar<string>("rts.actionreplay.titleDecoration", true) ?? " - Replay Capture"); CPH.SetArgument("replayTitleStyle", CPH.GetGlobalVar<string>("rts.actionreplay.titleBarStyle", true) ?? "Broadcast"); CPH.SetArgument("replayTitlePosition", CPH.GetGlobalVar<string>("rts.actionreplay.titlePosition", true) ?? "Bottom"); CPH.SetArgument("replayTitleAnimation", CPH.GetGlobalVar<string>("rts.actionreplay.titleAnimation", true) ?? "Slide up/down"); CPH.SetArgument("replayTitleDelay", GetSettingInt("rts.actionreplay.titleDelay", 0)); CPH.SetArgument("replayTitleDuration", GetSettingInt("rts.actionreplay.titleDuration", 5000)); CPH.SetArgument("replayTitleAnimationDuration", GetSettingInt("rts.actionreplay.titleAnimationDuration", 450));
        CPH.SetArgument("replayTitleFont", CPH.GetGlobalVar<string>("rts.actionreplay.titleFont", true) ?? "Inter"); CPH.SetArgument("replayTitleFontSize", GetSettingInt("rts.actionreplay.titleFontSize", 34)); CPH.SetArgument("replayTitleTextColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleTextColor", true) ?? "#FFFFFFFF"); CPH.SetArgument("replayTitleShadowColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleShadowColor", true) ?? "#FF000000"); CPH.SetArgument("replayTitlePrimaryColor", CPH.GetGlobalVar<string>("rts.actionreplay.titlePrimaryColor", true) ?? "#FF0384CB"); CPH.SetArgument("replayTitleSecondaryColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleSecondaryColor", true) ?? "#FF101416");
        CPH.TriggerEvent("RTS-Action Replay", true);
    }
}