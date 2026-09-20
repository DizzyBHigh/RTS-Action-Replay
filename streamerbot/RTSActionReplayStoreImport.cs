using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string TitleKey = "rts.actionreplay.replayTitle";
    private const string FileTypesKey = "rts.actionreplay.replayFileTypes";
    private const string FolderKey = "rts.actionreplay.replayFolder";

    public bool AddReplay()
    {
        var input = Arg("rawInput").Trim();
        if (string.IsNullOrWhiteSpace(input)) { CPH.SendMessage("Please provide a replay filename."); return false; }
        var folder = ReplayFolder();
        if (string.IsNullOrWhiteSpace(folder)) { CPH.SendMessage("The Replay Folder is not configured."); return false; }
        var fileName = Path.GetFileName(input);
        if (!string.Equals(fileName, input, StringComparison.OrdinalIgnoreCase)) { CPH.SendMessage("Please provide a filename from the configured Replay Folder."); return false; }
        var path = Path.Combine(folder, fileName);
        if (!TryAddFile(path, out var result)) { CPH.SendMessage(result); return false; }
        CPH.SendMessage(result);
        return true;
    }

    public bool ScanReplays()
    {
        var folder = ReplayFolder();
        if (string.IsNullOrWhiteSpace(folder)) { CPH.SendMessage("The Replay Folder is not configured."); return false; }
        if (!Directory.Exists(folder)) { CPH.SendMessage("The configured Replay Folder does not exist."); return false; }

        var added = 0;
        var skipped = 0;
        foreach (var path in Directory.EnumerateFiles(folder))
        {
            if (!IsReplayFile(path)) continue;
            if (IsCataloged(path)) { skipped++; continue; }
            if (TryAddFile(path, out _)) added++;
        }
        CPH.SendMessage($"Replay scan complete: {added} added, {skipped} already in the Catalog.");
        return true;
    }

    private bool TryAddFile(string path, out string result)
    {
        result = "";
        if (!File.Exists(path) || !IsReplayFile(path)) { result = "Replay file was not found or its file type is not enabled."; return false; }

        var folder = ReplayFolder();
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) { result = "The replay must be inside the configured Replay Folder."; return false; }
        if (!Stable(path)) { result = "The replay file is still changing. Try again when it has finished saving."; return false; }
        if (IsCataloged(path)) { result = $"Replay is already in the Catalog: {Path.GetFileName(path)}"; return false; }

        var data = Load();
        var catalog = GetCatalog(data);
        var info = new FileInfo(path);
        var captured = info.LastWriteTime;
        CPH.SetArgument("replayName", Path.GetFileNameWithoutExtension(path));
        var title = CPH.Parse(CPH.GetGlobalVar<string>(TitleKey, true) ?? "%replayName%");
        if (string.IsNullOrWhiteSpace(title)) title = Path.GetFileNameWithoutExtension(path);

        var id = captured.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        catalog.Insert(0, new JObject {
            ["id"] = id, ["sourceType"] = "OBS", ["sourceId"] = id,
            ["file"] = Path.GetFileName(path), ["filePath"] = fullPath, ["title"] = title,
            ["customTitle"] = false, ["added"] = DateTime.Now.ToString("o"),
            ["captured"] = captured.ToString("o"), ["acquisitionMethod"] = "OBSReplayBufferImport",
            ["creator"] = new JObject { ["platform"] = "OBS", ["id"] = "", ["name"] = "Imported" },
            ["plays"] = 0, ["users"] = new JObject()
        });
        Save(data);
        result = $"Replay added to Catalog: {title}";
        CPH.LogInfo($"RTS Action Replay: imported {title} ({id})");
        return true;
    }

    private bool IsCataloged(string path)
    {
        var file = Path.GetFileName(path);
        return GetCatalog(Load()).OfType<JObject>().Any(x =>
            string.Equals((string)x["file"], file, StringComparison.OrdinalIgnoreCase) &&
            string.Equals((string)x["sourceType"] ?? "OBS", "OBS", StringComparison.OrdinalIgnoreCase));
    }

    private bool IsReplayFile(string path)
    {
        var extension = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(extension)) return false;
        var configured = CPH.GetGlobalVar<string>(FileTypesKey, true) ?? ".mp4, .mkv";
        return configured.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().StartsWith(".") ? x.Trim() : "." + x.Trim())
            .Any(x => extension.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    private bool Stable(string path)
    {
        try { var first = new FileInfo(path).Length; System.Threading.Thread.Sleep(250); return new FileInfo(path).Length == first; }
        catch { return false; }
    }

    private string ReplayFolder() => CPH.GetGlobalVar<string>(FolderKey, true) ?? "";
    private string Arg(string name) { CPH.TryGetArg(name, out string value); return value ?? ""; }
    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true);
        try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); }
        catch { return new JObject(); }
    }

    private void Save(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
    private JArray GetCatalog(JObject data)
    {
        var catalog = data["catalog"] as JArray;
        if (catalog != null) return catalog;
        catalog = new JArray();
        data["catalog"] = catalog;
        return catalog;
    }
}