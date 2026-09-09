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
        var pending = CPH.GetGlobalVar<string>(PendingKey, false); var queue = string.IsNullOrWhiteSpace(pending) ? new JArray() : JArray.Parse(pending);
        if (!string.IsNullOrWhiteSpace(userId)) queue.Add(new JObject { ["id"] = userId, ["name"] = userName ?? "", ["queued"] = DateTime.UtcNow.ToString("o") });
        CPH.SetGlobalVar(PendingKey, queue.ToString(Newtonsoft.Json.Formatting.None), false); CPH.ObsReplayBufferSave(); CPH.LogInfo("RTS Action Replay: requested OBS Replay Buffer save."); return true;
    }

    public bool PlayReplay()
    {
        var data = Load(); var list = (JArray)data["replays"];
        if (!CPH.TryGetArg("rawInput", out string selector) || string.IsNullOrWhiteSpace(selector)) return false;
        selector = selector.Trim(); JObject replay = null;
        if (int.TryParse(selector, out var index) && index > 0 && index <= list.Count) replay = (JObject)list[index - 1];
        else replay = list.OfType<JObject>().FirstOrDefault(x => ((bool?)x["customTitle"] ?? false) && string.Equals((string)x["title"], selector, StringComparison.OrdinalIgnoreCase));
        if (replay == null) { CPH.SendMessage("Replay not found."); return false; }
        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true); var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.httpMapping", true) ?? "replays"; var port = CPH.GetGlobalVar<int?>("rts.actionreplay.httpPort", true) ?? 7474;
        var path = Path.Combine(folder ?? "", (string)replay["file"]); if (!File.Exists(path)) { CPH.SendMessage($"Replay file is missing: {(string)replay["title"]}"); return false; }
        CPH.TryGetArg("userId", out string userId); CPH.TryGetArg("userName", out string userName); var url = $"http://localhost:{port}/{mapping.Trim('/')}/{CPH.UrlEncode((string)replay["file"])}";
        CPH.SetArgument("replayCommand", "load"); CPH.SetArgument("replayId", (string)replay["id"]); CPH.SetArgument("replayUrl", url); CPH.SetArgument("replayAutoplay", true); CPH.SetArgument("replayUserId", userId ?? ""); CPH.SetArgument("replayUserName", userName ?? "");
        CPH.SetArgument("replayNumber", Array.IndexOf(list.ToArray(), replay) + 1); CPH.SetArgument("replayTitle", (string)replay["title"] ?? ""); ApplyPlayerSettings(); CPH.TriggerEvent(EventName, true); SendMessage("play"); return true;
    }

    public bool SetPlayerPosition()
    {
        if (!CPH.TryGetArg("rawInput", out string position) || string.IsNullOrWhiteSpace(position)) return false;
        ApplyPlayerSettings(); CPH.SetArgument("replayCommand", "move"); CPH.SetArgument("replayPosition", position.Trim()); CPH.TriggerEvent(EventName, true); return true;
    }

    public bool HidePlayer() { ApplyPlayerSettings(); CPH.SetArgument("replayCommand", "hide"); CPH.TriggerEvent(EventName, true); return true; }

    public bool ConfirmPlayback()
    {
        if (!CPH.TryGetArg("replayId", out string replayId)) return false;
        var data = Load(); var replay = ((JArray)data["replays"]).OfType<JObject>().FirstOrDefault(x => (string)x["id"] == replayId); if (replay == null) return false;
        replay["plays"] = ((int?)replay["plays"] ?? 0) + 1;
        if (CPH.TryGetArg("userId", out string userId) && !string.IsNullOrWhiteSpace(userId))
        {
            var name = CPH.TryGetArg("userName", out string userName) ? userName : userId; var users = (JObject)(replay["users"] ?? new JObject()); replay["users"] = users;
            var user = (JObject)(users[userId] ?? new JObject { ["name"] = name, ["plays"] = 0 }); user["name"] = string.IsNullOrWhiteSpace(name) ? (string)user["name"] : name; user["plays"] = ((int?)user["plays"] ?? 0) + 1; users[userId] = user;
        }
        Save(data); return true;
    }

    private void ApplyPlayerSettings()
    {
        CPH.SetArgument("replayShowControls", CPH.GetGlobalVar<bool?>("rts.actionreplay.showControls", true) ?? false); CPH.SetArgument("replayShowProgress", CPH.GetGlobalVar<bool?>("rts.actionreplay.showProgress", true) ?? true); CPH.SetArgument("replayPlaybackSpeed", GetSettingDouble("rts.actionreplay.playbackSpeed", 1.0));
        CPH.SetArgument("replayFrameColor", CPH.GetGlobalVar<string>("rts.actionreplay.frameColor", true) ?? "#0384CB"); CPH.SetArgument("replayBorderColor", CPH.GetGlobalVar<string>("rts.actionreplay.borderColor", true) ?? "#FFFFFF"); CPH.SetArgument("replayBorderStyle", CPH.GetGlobalVar<string>("rts.actionreplay.borderStyle", true) ?? "Solid");
        CPH.SetArgument("replayPositions", CPH.GetGlobalVar<string>("rts.actionreplay.positions", true) ?? "{\"Full Screen\":{\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}"); CPH.SetArgument("replayStartPosition", CPH.GetGlobalVar<string>("rts.actionreplay.defaultStartPosition", true) ?? "Full Screen"); CPH.SetArgument("replayEndPosition", CPH.GetGlobalVar<string>("rts.actionreplay.defaultEndPosition", true) ?? "Full Screen");
        CPH.SetArgument("replayAnimationDuration", GetSettingDouble("rts.actionreplay.animationDuration", .5)); CPH.SetArgument("replayAnimationEasing", CPH.GetGlobalVar<string>("rts.actionreplay.animationEasing", true) ?? "ease-in-out");
        CPH.SetArgument("replayShowTitle", CPH.GetGlobalVar<bool?>("rts.actionreplay.showTitle", true) ?? true); CPH.SetArgument("replayShowBranding", CPH.GetGlobalVar<bool?>("rts.actionreplay.showBranding", true) ?? true); CPH.SetArgument("replayTitleSuffix", CPH.GetGlobalVar<string>("rts.actionreplay.titleSuffix", true) ?? " - Replay Capture"); CPH.SetArgument("replayTitleDecorationPosition", CPH.GetGlobalVar<string>("rts.actionreplay.titleDecorationPosition", true) ?? "Suffix"); CPH.SetArgument("replayTitleDecoration", CPH.GetGlobalVar<string>("rts.actionreplay.titleDecoration", true) ?? " - Replay Capture"); CPH.SetArgument("replayTitleStyle", CPH.GetGlobalVar<string>("rts.actionreplay.titleBarStyle", true) ?? "Broadcast"); CPH.SetArgument("replayTitlePosition", CPH.GetGlobalVar<string>("rts.actionreplay.titlePosition", true) ?? "Bottom");
        CPH.SetArgument("replayTitleAnimation", CPH.GetGlobalVar<string>("rts.actionreplay.titleAnimation", true) ?? "Slide up/down"); CPH.SetArgument("replayTitleDelay", GetSettingInt("rts.actionreplay.titleDelay", 0)); CPH.SetArgument("replayTitleDuration", GetSettingInt("rts.actionreplay.titleDuration", 5000)); CPH.SetArgument("replayTitleAnimationDuration", GetSettingInt("rts.actionreplay.titleAnimationDuration", 450));
        CPH.SetArgument("replayTitleFont", CPH.GetGlobalVar<string>("rts.actionreplay.titleFont", true) ?? "Inter"); CPH.SetArgument("replayTitleFontSize", GetSettingInt("rts.actionreplay.titleFontSize", 34)); CPH.SetArgument("replayTitleTextColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleTextColor", true) ?? "#FFFFFFFF"); CPH.SetArgument("replayTitleShadowColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleShadowColor", true) ?? "#FF000000");
        CPH.SetArgument("replayTitlePrimaryColor", CPH.GetGlobalVar<string>("rts.actionreplay.titlePrimaryColor", true) ?? "#FF0384CB"); CPH.SetArgument("replayTitleSecondaryColor", CPH.GetGlobalVar<string>("rts.actionreplay.titleSecondaryColor", true) ?? "#FF101416");
    }

    private void SendMessage(string type)
    {
        var key = "rts.actionreplay.message." + type; var text = CPH.GetGlobalVar<string>(key + ".text", true); if (string.IsNullOrWhiteSpace(text)) return; text = CPH.Parse(text); if (CPH.GetGlobalVar<bool?>(key + ".chat", true) ?? true) CPH.SendMessage(text);
        if (!(CPH.GetGlobalVar<bool?>(key + ".overlay", true) ?? false)) return;
        CPH.SetArgument("replayCommand", "message"); CPH.SetArgument("replayMessage", text); CPH.SetArgument("replayLogoUrl", CPH.GetGlobalVar<string>("rts.actionreplay.brandLogoUrl", true) ?? ""); CPH.SetArgument("replayMessageBoardColor", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.boardColor", true) ?? "#101416"); CPH.SetArgument("replayMessageStripeLight", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.stripeLight", true) ?? "#EEEEEE"); CPH.SetArgument("replayMessageStripeDark", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.stripeDark", true) ?? "#111111"); CPH.SetArgument("replayMessageAccent", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.accent", true) ?? "#0384CB");
        CPH.SetArgument("replayMessageTextColor", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.textColor", true) ?? "#0384CB"); CPH.SetArgument("replayMessageFont", CPH.GetGlobalVar<string>("rts.actionreplay.clapper.font", true) ?? "Arial, sans-serif"); CPH.SetArgument("replayMessageSize", GetSettingInt("rts.actionreplay.clapper.size", 100)); CPH.SetArgument("replayMessagePositionX", GetSettingInt("rts.actionreplay.clapper.positionX", 50)); CPH.SetArgument("replayMessagePositionY", GetSettingInt("rts.actionreplay.clapper.positionY", 50)); CPH.TriggerEvent(EventName, true);
    }

    private int GetSettingInt(string key, int fallback) { try { return Convert.ToInt32(CPH.GetGlobalVar<object>(key, true), System.Globalization.CultureInfo.InvariantCulture); } catch { return fallback; } }
    private double GetSettingDouble(string key, double fallback) { try { return Convert.ToDouble(CPH.GetGlobalVar<object>(key, true), System.Globalization.CultureInfo.InvariantCulture); } catch { return fallback; } }
    private JObject Load() => JObject.Parse(CPH.GetGlobalVar<string>(DataKey, true) ?? "{\"version\":1,\"replays\":[]}");
    private void Save(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
}