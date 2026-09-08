using System;
using System.IO;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string TitleKey = "rts.actionreplay.replayTitle";
    private const string MaxHistoryKey = "rts.actionreplay.maxHistory";
    private const string PendingKey = "rts.actionreplay.pendingSaves";
    private const string FileTypesKey = "rts.actionreplay.replayFileTypes";

    public bool Execute() => Initialize();

    public bool Initialize()
    {
        if (CPH.GetGlobalVar<string>(DataKey, true) != null) return true;
        Save(new JObject { ["version"] = 1, ["replays"] = new JArray() });
        return true;
    }

    public bool AddReplay()
    {
        if (!(CPH.GetGlobalVar<bool?>("rts.actionreplay.autoAdd", true) ?? true)) return true;
        string path;
        if (!CPH.TryGetArg("fullPath", out path) || string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        if (!IsReplayFile(path)) return false;
        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        if (!string.IsNullOrWhiteSpace(folder) && !Path.GetFullPath(path).StartsWith(Path.GetFullPath(folder).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) return false;
        if (Path.GetExtension(path).Equals(".tmp", StringComparison.OrdinalIgnoreCase) || !Stable(path)) return false;

        var data = Load();
        var list = (JArray)data["replays"];
        var file = Path.GetFileName(path);
        for (int i = 0; i < list.Count; i++)
            if (string.Equals((string)list[i]["file"], file, StringComparison.OrdinalIgnoreCase)) return true;

        var now = DateTime.Now;
        var id = now.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        var creatorId = Get("userId");
        var creatorName = Get("userName");
        ApplyPendingCreator(ref creatorId, ref creatorName);

        CPH.SetArgument("replayId", id);
        CPH.SetArgument("replayFile", file);
        CPH.SetArgument("replayPath", path);
        CPH.SetArgument("replayName", Path.GetFileNameWithoutExtension(path));
        CPH.SetArgument("replayNumber", 1);
        CPH.SetArgument("replayDate", now.ToString("yyyy-MM-dd"));
        CPH.SetArgument("replayTime", now.ToString("HH:mm:ss"));
        CPH.SetArgument("replayPlays", 0);
        CPH.SetArgument("replayUser", creatorName);
        CPH.SetArgument("replayUserId", creatorId);
        CPH.SetArgument("replayUserPlays", 0);
        CPH.SetArgument("replayTitle", "");

        var title = CPH.Parse(CPH.GetGlobalVar<string>(TitleKey, true) ?? "%replayName%");
        if (string.IsNullOrWhiteSpace(title)) title = Path.GetFileNameWithoutExtension(path);

        var replay = new JObject {
            ["id"] = id,
            ["file"] = file,
            ["title"] = title,
            ["customTitle"] = false,
            ["added"] = now.ToString("o"),
            ["creator"] = new JObject { ["id"] = creatorId, ["name"] = creatorName },
            ["plays"] = 0,
            ["users"] = new JObject()
        };

        list.Insert(0, replay);
        Trim(list);
        Save(data);
        CPH.LogInfo($"RTS Action Replay: added {title} ({id})");
        CPH.SetArgument("replayTitle", title);
        SendMessage("save");
        if (CPH.GetGlobalVar<bool?>("rts.actionreplay.autoPlay", true) ?? false) BroadcastReplay(replay);
        return true;
    }

    public bool NameReplay()
    {
        string indexInput;
        string rawInput;
        if (!CPH.TryGetArg("input0", out indexInput) || !CPH.TryGetArg("rawInput", out rawInput)) return false;
        int index;
        if (!int.TryParse(indexInput, out index)) { CPH.SendMessage("Please provide a valid replay number."); return false; }
        var title = (rawInput ?? "").Trim();
        if (title.StartsWith(indexInput + " ", StringComparison.OrdinalIgnoreCase)) title = title.Substring(indexInput.Length).Trim();
        if (title.Length == 0) { CPH.SendMessage("Please provide a replay title."); return false; }
        var data = Load();
        var list = (JArray)data["replays"];
        if (index < 1 || index > list.Count) { CPH.SendMessage($"Replay #{index} does not exist."); return false; }
        var target = (JObject)list[index - 1];
        for (int i = 0; i < list.Count; i++)
        {
            var other = (JObject)list[i];
            if (object.ReferenceEquals(other, target) || !((bool?)other["customTitle"] ?? false)) continue;
            if (string.Equals((string)other["title"], title, StringComparison.OrdinalIgnoreCase))
            {
                CPH.SendMessage("That title already exists.");
                return false;
            }
        }
        target["title"] = title;
        target["customTitle"] = true;
        Save(data);
        CPH.SetArgument("replayNumber", index);
        CPH.SetArgument("replayTitle", title);
        SendMessage("name");
        return true;
    }

    public bool ListPlaylist()
    {
        var list = (JArray)Load()["replays"];
        var message = "";
        if (list.Count == 0) message = "The replay playlist is empty.";
        else for (int i = 0; i < list.Count; i++) message += (i > 0 ? " | " : "") + "#" + (i + 1) + " " + (string)list[i]["title"];
        CPH.SetArgument("replayPlaylist", message);
        SendMessage("playlist");
        return true;
    }

    public bool CreatorLeaderboard()
    {
        var list = (JArray)Load()["replays"];
        var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.KeyValuePair<string, int>>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < list.Count; i++)
        {
            var creator = (JObject)list[i]["creator"];
            var id = (string)creator["id"];
            if (string.IsNullOrWhiteSpace(id)) continue;
            var name = (string)creator["name"] ?? id;
            if (groups.ContainsKey(id)) groups[id] = new System.Collections.Generic.KeyValuePair<string, int>(name, groups[id].Value + 1);
            else groups[id] = new System.Collections.Generic.KeyValuePair<string, int>(name, 1);
        }
        var results = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>(groups.Values);
        results.Sort((a, b) => b.Value.CompareTo(a.Value));
        var message = "Replay creators: ";
        var count = Math.Min(5, results.Count);
        if (count == 0) message += "No replay creators yet.";
        else for (int i = 0; i < count; i++) message += (i > 0 ? " | " : "") + "#" + (i + 1) + " " + results[i].Key + " (" + results[i].Value + ")";
        CPH.SetArgument("replayLeaderboard", message);
        SendMessage("creatorLeaderboard");
        return true;
    }

    public bool PlaybackLeaderboard()
    {
        var list = (JArray)Load()["replays"];
        var users = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.KeyValuePair<string, int>>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < list.Count; i++)
        {
            var replayUsers = (JObject)list[i]["users"];
            if (replayUsers == null) continue;
            foreach (JProperty property in replayUsers.Properties())
            {
                var user = (JObject)property.Value;
                var name = (string)user["name"] ?? property.Name;
                var plays = (int?)user["plays"] ?? 0;
                if (users.ContainsKey(property.Name)) users[property.Name] = new System.Collections.Generic.KeyValuePair<string, int>(name, users[property.Name].Value + plays);
                else users[property.Name] = new System.Collections.Generic.KeyValuePair<string, int>(name, plays);
            }
        }
        var results = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>(users.Values);
        results.Sort((a, b) => b.Value.CompareTo(a.Value));
        var message = "Replay viewers: ";
        var count = Math.Min(5, results.Count);
        if (count == 0) message += "No replay plays yet.";
        else for (int i = 0; i < count; i++) message += (i > 0 ? " | " : "") + "#" + (i + 1) + " " + results[i].Key + " (" + results[i].Value + " plays)";
        CPH.SetArgument("replayLeaderboard", message);
        SendMessage("playbackLeaderboard");
        return true;
    }

    private void SendMessage(string type)
    {
        var key = "rts.actionreplay.message." + type;
        var text = CPH.GetGlobalVar<string>(key + ".text", true);
        if (string.IsNullOrWhiteSpace(text)) return;
        text = CPH.Parse(text);
        if (CPH.GetGlobalVar<bool?>(key + ".chat", true) ?? true) CPH.SendMessage(text);
        if (CPH.GetGlobalVar<bool?>(key + ".overlay", true) ?? false)
        {
            CPH.SetArgument("replayCommand", "message");
            CPH.SetArgument("replayMessage", text);
            SetMessageStyleArguments();
            CPH.TriggerEvent("RTS-Action Replay", true);
        }
    }

    private void SetMessageStyleArguments()
    {
        CPH.SetArgument("replayLogoUrl", CPH.GetGlobalVar<string>("rts.actionreplay.brandLogoUrl", true) ?? "");
        CPH.SetArgument("replayMessageBoardColor", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.boardColor", true) ?? "#101416");
        CPH.SetArgument("replayMessageStripeLight", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.stripeLight", true) ?? "#EEEEEE");
        CPH.SetArgument("replayMessageStripeDark", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.stripeDark", true) ?? "#111111");
        CPH.SetArgument("replayMessageAccent", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.accent", true) ?? "#0384CB");
        CPH.SetArgument("replayMessageTextColor", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.textColor", true) ?? "#0384CB");
        CPH.SetArgument("replayMessageFont", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.font", true) ?? "Arial, sans-serif");
        CPH.SetArgument("replayMessageSize", CPH.GetGlobalVar<int?>("rts.actionreplay.clapper.size", true) ?? 100);
        CPH.SetArgument("replayMessagePositionX", CPH.GetGlobalVar<int?>("rts.actionreplay.clapper.positionX", true) ?? 50);
        CPH.SetArgument("replayMessagePositionY", CPH.GetGlobalVar<int?>("rts.actionreplay.clapper.positionY", true) ?? 50);
    }

    private bool IsReplayFile(string path)
    {
        var extension = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(extension)) return false;
        var configured = CPH.GetGlobalVar<string>(FileTypesKey, true) ?? ".mp4, .mkv";
        var types = configured.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < types.Length; i++)
        {
            var type = types[i].Trim();
            if (type.Length == 0) continue;
            if (!type.StartsWith(".")) type = "." + type;
            if (string.Equals(extension, type, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private JObject Load() => JObject.Parse(CPH.GetGlobalVar<string>(DataKey, true) ?? "{\"version\":1,\"replays\":[]}");
    private void Save(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
    private string Get(string key) { string value; return CPH.TryGetArg(key, out value) ? value ?? "" : ""; }

    private bool Stable(string path)
    {
        for (var i = 0; i < 5; i++)
        {
            var a = new FileInfo(path).Length;
            CPH.Wait(500);
            var b = new FileInfo(path).Length;
            if (a == b) return true;
        }
        return false;
    }

    private void Trim(JArray list)
    {
        var max = CPH.GetGlobalVar<int?>(MaxHistoryKey, true) ?? 20;
        while (list.Count > Math.Max(1, max)) list.RemoveAt(list.Count - 1);
    }

    private void ApplyPendingCreator(ref string id, ref string name)
    {
        if (!string.IsNullOrWhiteSpace(id)) return;
        var raw = CPH.GetGlobalVar<string>(PendingKey, false);
        if (string.IsNullOrWhiteSpace(raw)) return;
        var queue = JArray.Parse(raw);
        if (queue.Count == 0) return;
        var item = (JObject)queue[0];
        queue.RemoveAt(0);
        CPH.SetGlobalVar(PendingKey, queue.ToString(Newtonsoft.Json.Formatting.None), false);
        DateTime queued;
        if (DateTime.TryParse((string)item["queued"], out queued) && DateTime.UtcNow - queued <= TimeSpan.FromSeconds(60))
        {
            id = (string)item["id"] ?? "";
            name = (string)item["name"] ?? "";
        }
    }

    private void BroadcastReplay(JObject replay)
    {
        var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.httpMapping", true) ?? "replays";
        var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
        CPH.SetArgument("replayCommand", "load");
        CPH.SetArgument("replayId", (string)replay["id"]);
        CPH.SetArgument("replayUrl", $"http://127.0.0.1:{port}/{mapping.Trim('/')}/{CPH.UrlEncode((string)replay["file"])}");
        CPH.SetArgument("replayAutoplay", true);
        CPH.TriggerEvent("RTS-Action Replay", true);
    }
}
