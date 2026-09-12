using System;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string LegacyCatalogKey = "rts.actionreplay.catalog";
    private const string PlaylistAction = "RTS Action Replay Playlist";
    private const string AnimationAction = "RTS - Action Replay - Core - Animation";

    public bool Execute() => PlayRecent();

    public bool PlayRecent()
    {
        var data = Load();
        var catalog = (JArray)data["catalog"] ?? new JArray();
        var recentIds = (JArray)data["recentIds"] ?? new JArray();
        if (recentIds.Count == 0) { CPH.SendMessage("There are no recent replays."); return false; }

        var selector = "1";
        CPH.TryGetArg("rawInput", out string rawInput);
        if (!string.IsNullOrWhiteSpace(rawInput)) selector = rawInput.Trim();

        JObject replay = null;
        if (int.TryParse(selector, out var index) && index > 0 && index <= recentIds.Count)
        {
            var id = Convert.ToString(recentIds[index - 1]);
            replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals(Convert.ToString(x["id"]), id, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            replay = recentIds.Select(item => Convert.ToString(item))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => catalog.OfType<JObject>().FirstOrDefault(x => string.Equals(Convert.ToString(x["id"]), id, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault(x => x != null && string.Equals(Convert.ToString(x["title"]), selector, StringComparison.OrdinalIgnoreCase));
        }

        if (replay == null) { CPH.SendMessage("Recent replay not found."); return false; }
        CPH.SetArgument("replayId", Convert.ToString(replay["id"]));
        CPH.SetArgument("animationEntryPoint", "recent");
        if (!CPH.ExecuteMethod(AnimationAction, "ResolveEntryPointProfile")) return false;
        return CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay");
    }

    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true);
        JObject data;
        if (string.IsNullOrWhiteSpace(raw)) data = new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() };
        else { try { data = JObject.Parse(raw); } catch { data = new JObject { ["version"] = 2, ["catalog"] = new JArray(), ["recentIds"] = new JArray() }; } }

        var catalog = data["catalog"] as JArray;
        var legacy = data["replays"] as JArray;
        if (catalog == null) catalog = legacy ?? new JArray();
        else if (legacy != null && legacy.Count > 0) MergeCatalog(catalog, legacy);

        var external = CPH.GetGlobalVar<string>(LegacyCatalogKey, true);
        if (!string.IsNullOrWhiteSpace(external))
        {
            try { var externalData = JObject.Parse(external); var externalCatalog = externalData["catalog"] as JArray; if (externalCatalog != null) MergeCatalog(catalog, externalCatalog); } catch { }
        }
        data["version"] = 2; data["catalog"] = catalog; data["recentIds"] = data["recentIds"] as JArray ?? new JArray(); data.Remove("replays");
        return data;
    }

    private void MergeCatalog(JArray target, JArray source)
    {
        foreach (var token in source)
        {
            var item = token as JObject; if (item == null) continue;
            var id = (string)item["id"]; var type = (string)item["sourceType"] ?? "OBS"; var sourceId = (string)item["sourceId"];
            if (target.OfType<JObject>().Any(x => (!string.IsNullOrWhiteSpace(id) && string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrWhiteSpace(sourceId) && string.Equals((string)x["sourceType"] ?? "OBS", type, StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceId"], sourceId, StringComparison.OrdinalIgnoreCase)))) continue;
            var clone = (JObject)item.DeepClone(); if (string.IsNullOrWhiteSpace((string)clone["sourceType"])) clone["sourceType"] = "OBS"; if (string.IsNullOrWhiteSpace((string)clone["sourceId"])) clone["sourceId"] = (string)clone["id"] ?? ""; if (clone["plays"] == null) clone["plays"] = 0; if (clone["users"] == null) clone["users"] = new JObject(); target.Add(clone);
        }
    }
}