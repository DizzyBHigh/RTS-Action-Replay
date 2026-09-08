using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string PendingKey = "rts.actionreplay.pendingSaves";
    private const string EventName = "RTS-Action Replay";

    public bool Execute() => PlayReplay();

    public bool SaveReplay()
    {
        CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName);
        var pending = CPH.GetGlobalVar<string>(PendingKey, false);
        var queue = string.IsNullOrWhiteSpace(pending) ? new JArray() : JArray.Parse(pending);
        if (!string.IsNullOrWhiteSpace(userId)) queue.Add(new JObject { ["id"] = userId, ["name"] = userName ?? "", ["queued"] = DateTime.UtcNow.ToString("o") });
        CPH.SetGlobalVar(PendingKey, queue.ToString(Newtonsoft.Json.Formatting.None), false);
        CPH.ObsReplayBufferSave();
        CPH.LogInfo("RTS Action Replay: requested OBS Replay Buffer save.");
        return true;
    }

    public bool PlayReplay()
    {
        var data = Load(); var list = (JArray)data["replays"];
        if (!CPH.TryGetArg("rawInput", out string selector) || string.IsNullOrWhiteSpace(selector)) return false;
        selector = selector.Trim();
        JObject replay = null;
        if (int.TryParse(selector, out var index) && index > 0 && index <= list.Count) replay = (JObject)list[index - 1];
        else replay = list.OfType<JObject>().FirstOrDefault(x => ((bool?)x["customTitle"] ?? false) && string.Equals((string)x["title"], selector, StringComparison.OrdinalIgnoreCase));
        if (replay == null) { CPH.SendMessage("Replay not found."); return false; }

        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true);
        var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.httpMapping", true) ?? "replays";
        var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
        var path = Path.Combine(folder ?? "", (string)replay["file"]);
        if (!File.Exists(path)) { CPH.SendMessage($"Replay file is missing: {(string)replay["title"]}"); return false; }

        CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName);
        var url = $"http://127.0.0.1:{port}/{mapping.Trim('/')}/{CPH.UrlEncode((string)replay["file"])}";
        CPH.SetArgument("replayCommand", "load"); CPH.SetArgument("replayId", (string)replay["id"]);
        CPH.SetArgument("replayUrl", url); CPH.SetArgument("replayAutoplay", true);
        CPH.SetArgument("replayUserId", userId ?? ""); CPH.SetArgument("replayUserName", userName ?? "");
        CPH.SetArgument("replayNumber", Array.IndexOf(list.ToArray(), replay) + 1);
        CPH.SetArgument("replayTitle", (string)replay["title"] ?? "");
        CPH.TriggerEvent(EventName, true);
        SendMessage("play");
        return true;
    }

    public bool ConfirmPlayback()
    {
        if (!CPH.TryGetArg("replayId", out string replayId)) return false;
        var data = Load(); var replay = ((JArray)data["replays"]).OfType<JObject>().FirstOrDefault(x => (string)x["id"] == replayId);
        if (replay == null) return false;
        replay["plays"] = ((int?)replay["plays"] ?? 0) + 1;
        if (CPH.TryGetArg("userId", out string userId) && !string.IsNullOrWhiteSpace(userId))
        {
            var name = CPH.TryGetArg("userName", out string userName) ? userName : userId;
            var users = (JObject)(replay["users"] ?? new JObject()); replay["users"] = users;
            var user = (JObject)(users[userId] ?? new JObject { ["name"] = name, ["plays"] = 0 });
            user["name"] = string.IsNullOrWhiteSpace(name) ? (string)user["name"] : name;
            user["plays"] = ((int?)user["plays"] ?? 0) + 1; users[userId] = user;
        }
        Save(data); return true;
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
            CPH.TriggerEvent(EventName, true);
        }
    }

    private JObject Load() => JObject.Parse(CPH.GetGlobalVar<string>(DataKey, true) ?? "{\"version\":1,\"replays\":[]}");
    private void Save(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
}
