using System;
using System.IO;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string TitleKey = "rts.actionreplay.replayTitle";
    private const string MaxHistoryKey = "rts.actionreplay.maxHistory";
    private const string PendingKey = "rts.actionreplay.pendingSaves";

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
        if (!CPH.TryGetArg("fullPath", out string path) || string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
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
        if (CPH.GetGlobalVar<bool?>("rts.actionreplay.autoPlay", true) ?? false) BroadcastReplay(replay);
        return true;
    }

    public bool NameReplay()
    {
        if (!CPH.TryGetArg("replayIndex", out int index) || !CPH.TryGetArg("replayTitleInput", out string title)) return false;
        title = (title ?? "").Trim();
        if (title.Length == 0) { CPH.SendMessage("Please provide a replay title."); return false; }

        var data = Load();
        var list = (JArray)data["replays"];
        if (index < 1 || index > list.Count) { CPH.SendMessage($"Replay #{index} does not exist."); return false; }
        var target = (JObject)list[index - 1];

        for (int i = 0; i < list.Count; i++)
        {
            var other = (JObject)list[i];
            if (other == target || !((bool?)other["customTitle"] ?? false)) continue;
            if (string.Equals((string)other["title"], title, StringComparison.OrdinalIgnoreCase))
            {
                CPH.SendMessage("That title already exists.");
                return false;
            }
        }

        target["title"] = title;
        target["customTitle"] = true;
        Save(data);
        return true;
    }

    public bool ListPlaylist()
    {
        var list = (JArray)Load()["replays"];
        if (list.Count == 0)
        {
            CPH.SendMessage("The replay playlist is empty.");
            return true;
        }

        var output = "";
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) output += " | ";
            output += $"#{i + 1} {(string)list[i]["title"]}";
        }
        CPH.SendMessage(output);
        return true;
    }

    public bool CreatorLeaderboard()
    {
        var list = (JArray)Load()["replays"];
        var creators = new JObject();

        for (int i = 0; i < list.Count; i++)
        {
            var creator = (JObject)list[i]["creator"];
            var id = (string)creator["id"];
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (creators[id] == null) creators[id] = new JObject { ["name"] = (string)creator["name"], ["count"] = 0 };
            creators[id]["count"] = (int)creators[id]["count"] + 1;
        }

        var entries = new JArray();
        foreach (var property in creators.Properties()) entries.Add(property.Value);
        SortLeaderboard(entries, "count");

        var message = "Replay creators: ";
        if (entries.Count == 0) message += "No replay creators yet.";
        else
            for (int i = 0; i < Math.Min(5, entries.Count); i++)
            {
                if (i > 0) message += " | ";
                message += $"#{i + 1} {(string)entries[i]["name"]} ({(int)entries[i]["count"]})";
            }
        CPH.SendMessage(message);
        return true;
    }

    public bool PlaybackLeaderboard()
    {
        var list = (JArray)Load()["replays"];
        var users = new JObject();

        for (int i = 0; i < list.Count; i++)
        {
            var replayUsers = (JObject)list[i]["users"];
            if (replayUsers == null) continue;
            foreach (var property in replayUsers.Properties())
            {
                var id = property.Name;
                var user = (JObject)property.Value;
                if (users[id] == null) users[id] = new JObject { ["name"] = (string)user["name"], ["plays"] = 0 };
                users[id]["plays"] = (int)users[id]["plays"] + ((int?)user["plays"] ?? 0);
                if (!string.IsNullOrWhiteSpace((string)user["name"])) users[id]["name"] = (string)user["name"];
            }
        }

        var entries = new JArray();
        foreach (var property in users.Properties()) entries.Add(property.Value);
        SortLeaderboard(entries, "plays");

        var message = "Replay viewers: ";
        if (entries.Count == 0) message += "No replay plays yet.";
        else
            for (int i = 0; i < Math.Min(5, entries.Count); i++)
            {
                if (i > 0) message += " | ";
                message += $"#{i + 1} {(string)entries[i]["name"]} ({(int)entries[i]["plays"]})";
            }
        CPH.SendMessage(message);
        return true;
    }

    private JObject Load() => JObject.Parse(CPH.GetGlobalVar<string>(DataKey, true) ?? "{\"version\":1,\"replays\":[]}");
    private void Save(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
    private string Get(string key) => CPH.TryGetArg(key, out string value) ? value ?? "" : "";

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
        if (DateTime.TryParse((string)item["queued"], out var queued) && DateTime.UtcNow - queued <= TimeSpan.FromSeconds(60))
        {
            id = (string)item["id"] ?? "";
            name = (string)item["name"] ?? "";
        }
    }

    private void SortLeaderboard(JArray entries, string countKey)
    {
        for (int i = 0; i < entries.Count - 1; i++)
        {
            for (int j = i + 1; j < entries.Count; j++)
            {
                if ((int)entries[j][countKey] > (int)entries[i][countKey])
                {
                    var temp = entries[i];
                    entries[i] = entries[j];
                    entries[j] = temp;
                }
            }
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
