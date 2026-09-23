using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string ReplayFolderKey = "rts.actionreplay.replayFolder";
    private const string TwitchFolderKey = "rts.actionreplay.twitch.folder";
    private const string KickFolderKey = "rts.actionreplay.kick.folder";
    public bool Execute() => Purge();
    public bool Purge()
    {
        var data = Load();
        var catalog = data["catalog"] as JArray ?? new JArray();
        var kept = new JArray();
        var removed = 0;
        var removedItems = new List<string>();
        foreach (var item in catalog.OfType<JObject>())
        {
            if (HasLocalFile(item) || HasUrl(item)) kept.Add(item);
            else
            {
                removed++;
                var id = (string)item["id"] ?? "<missing>";
                var title = (string)item["title"] ?? "<untitled>";
                var source = (string)item["sourceType"] ?? "Unknown";
                removedItems.Add(source + ": " + title + " [" + id + "]");
                CPH.LogInfo($"RTS Action Replay: purge removing unavailable replay; id={id}; title={title}.");
            }
        }
        data["catalog"] = kept;
        Save(data);
        SendMessage($"Catalog purge complete: {removed} unavailable replay(s) removed, {kept.Count} kept.");
        SendRemovedItems(removedItems);
        return true;
    }
    private bool HasLocalFile(JObject item)
    {
        var path = (string)item["filePath"];
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) return true;
        var file = (string)item["file"];
        if (string.IsNullOrWhiteSpace(file)) return false;
        var folder = GetFolder((string)item["sourceType"]);
        if (string.IsNullOrWhiteSpace(folder)) return false;
        return File.Exists(Path.IsPathRooted(file) ? file : Path.Combine(folder, file));
    }
    private string GetFolder(string source)
    {
        if (string.Equals(source, "Twitch", StringComparison.OrdinalIgnoreCase)) return CPH.GetGlobalVar<string>(TwitchFolderKey, true) ?? "";
        if (string.Equals(source, "Kick", StringComparison.OrdinalIgnoreCase)) return CPH.GetGlobalVar<string>(KickFolderKey, true) ?? "";
        return CPH.GetGlobalVar<string>(ReplayFolderKey, true) ?? "";
    }
    private bool HasUrl(JObject item)
    {
        foreach (var url in UrlCandidates(item)) if (UrlExists(url)) return true;
        return false;
    }
    private IEnumerable<string> UrlCandidates(JObject item)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in new[] { "sourceUrl", "externalUrl", "embedUrl" })
        {
            var url = (string)item[field];
            if (!string.IsNullOrWhiteSpace(url) && seen.Add(url)) yield return url;
        }
        var source = (string)item["sourceType"];
        var id = (string)item["sourceId"];
        if (string.Equals(source, "YouTube", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(id))
        {
            var url = "https://youtu.be/" + Uri.EscapeDataString(id);
            if (seen.Add(url)) yield return url;
        }
        if (string.Equals(source, "Twitch", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(id))
        {
            var url = "https://clips.twitch.tv/" + Uri.EscapeDataString(id);
            if (seen.Add(url)) yield return url;
        }
    }
    private bool UrlExists(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) return false;
        try
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "HEAD";
            request.AllowAutoRedirect = true;
            request.Timeout = 3000;
            request.UserAgent = "RTS-Action-Replay";
            using (var response = (HttpWebResponse)request.GetResponse()) return Success(response.StatusCode);
        }
        catch (WebException ex)
        {
            var response = ex.Response as HttpWebResponse;
            if (response == null || ((int)response.StatusCode != 405 && (int)response.StatusCode != 501)) return false;
        }
        catch { return false; }
        try
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            request.AddRange(0, 0);
            request.AllowAutoRedirect = true;
            request.Timeout = 3000;
            request.UserAgent = "RTS-Action-Replay";
            using (var response = (HttpWebResponse)request.GetResponse()) return Success(response.StatusCode);
        }
        catch { return false; }
    }
    private bool Success(HttpStatusCode status) { var code = (int)status; return code >= 200 && code < 300; }
    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true);
        try { return string.IsNullOrWhiteSpace(raw) ? Defaults() : JObject.Parse(raw); }
        catch { return Defaults(); }
    }
    private JObject Defaults() => new JObject { ["version"] = "1.0", ["catalog"] = new JArray(), ["playHistory"] = new JArray() };
    private void Save(JObject data)
    {
        data["version"] = "1.0";
        data["catalog"] = data["catalog"] as JArray ?? new JArray();
        data["playHistory"] = data["playHistory"] as JArray ?? new JArray();
        CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
    }
    private void SendMessage(string text)
    {
        var platform = Arg("userType");
        if (string.Equals(platform, "Kick", StringComparison.OrdinalIgnoreCase)) { CPH.SendKickMessage(text); return; }
        if (string.Equals(platform, "YouTube", StringComparison.OrdinalIgnoreCase)) { var id = Arg("broadcast.id"); if (!string.IsNullOrWhiteSpace(id)) CPH.SendYouTubeMessage(text, true, true, id); else CPH.SendYouTubeMessageToLatestMonitored(text); return; }
        if (string.Equals(platform, "Twitch", StringComparison.OrdinalIgnoreCase)) { CPH.SendMessage(text); return; }
        CPH.LogWarn("RTS Action Replay: unable to route purge response because the originating platform is unknown.");
    }
    private void SendRemovedItems(List<string> items)
    {
        if (items.Count == 0) { SendMessage("Nothing was purged."); return; }
        var message = "Purged: ";
        foreach (var item in items)
        {
            var next = message == "Purged: " ? item : message + " | " + item;
            if (next.Length > 400) { SendMessage(message); message = "Purged: " + item; } else message = next;
        }
        if (message != "Purged: ") SendMessage(message);
    }
    private string Arg(string name) { CPH.TryGetArg(name, out string value); return value ?? ""; }
}
