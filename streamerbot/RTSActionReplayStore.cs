using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string TitleKey = "rts.actionreplay.replayTitle";
    private const string PendingKey = "rts.actionreplay.pendingSaves";
    private const string FileTypesKey = "rts.actionreplay.replayFileTypes";
    private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";
    private const string PlaylistKey = "rts.actionreplay.playlist";
    private const string ReplayIdHandoffKey = "rts.actionreplay.handoff.replayId";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string ConfigurationSnapshotKey = "rts.actionreplay.handoff.configurationSnapshot";

    public bool Execute() => Initialize();
    public bool Initialize() { var data = Load(); if (data.Count == 0) { data = CreateDataDefaults(); Save(data); } else { if (!(data["catalog"] is JArray)) data["catalog"] = new JArray(); if (!(data["playHistory"] is JArray)) data["playHistory"] = new JArray(); data["version"] = "1.0"; Save(data); } return true; }
    public bool EnsureData() => Initialize();
    private JObject CreateDataDefaults() => new JObject { ["version"] = "1.0", ["catalog"] = new JArray(), ["playHistory"] = new JArray() };
    public bool Ensure() { EnsurePlayer(); EnsureObject(PanelKey, CreatePanelDefaults()); EnsureObject(MessageKey, CreateMessageDefaults()); return true; }
    public bool GetPlayer() { EnsurePlayer(); CPH.SetArgument("replayPlayerConfig", Read(PlayerKey).ToString(Newtonsoft.Json.Formatting.None)); return true; }
    public bool GetPlayerPositions() { EnsurePlayer(); var player = Read(PlayerKey); CPH.SetArgument("replayPositions", ((JObject)player["positions"] ?? new JObject()).ToString(Newtonsoft.Json.Formatting.None)); return true; }
    public bool SavePlayerPositions() { EnsurePlayer(); string positionsJson; if (!CPH.TryGetArg("replayPositions", out positionsJson) || string.IsNullOrWhiteSpace(positionsJson)) return false; try { var positions = JObject.Parse(positionsJson); var player = Read(PlayerKey); player["positions"] = positions; SaveConfig(PlayerKey, player); return true; } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: player position JSON save failed: " + ex.Message); return false; } }
    private void SavePlayerPositionsFromEditor(string json) { if (string.IsNullOrWhiteSpace(json)) return; try { var positions = JObject.Parse(json); var player = Read(PlayerKey); if (player.Count == 0) player = CreatePlayerDefaults(); player["positions"] = positions; SaveConfig(PlayerKey, player); } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: player position editor save failed: " + ex.Message); } }
    private JObject GetPlayerPositionsObject() { EnsurePlayer(); var player = Read(PlayerKey); return (JObject)player["positions"] ?? new JObject(); }
    public bool GetPanel() { EnsureObject(PanelKey, CreatePanelDefaults()); CPH.SetArgument("replayPanelConfig", Read(PanelKey).ToString(Newtonsoft.Json.Formatting.None)); return true; }
    private void EnsurePlayer() { var current = Read(PlayerKey); if (current.Count > 0) return; SaveConfig(PlayerKey, CreatePlayerDefaults()); }
    private JObject CreatePlayerDefaults() => new JObject { ["positions"] = new JObject(), ["animationProfiles"] = new JArray { new JObject { ["id"] = "default", ["name"] = "Default" } }, ["animation"] = new JObject { ["selectedProfile"] = "default", ["entryPoints"] = new JObject { ["obs"] = "default", ["twitch"] = "default", ["youtube"] = "default", ["kick"] = "default", ["recent"] = "default", ["catalog"] = "default", ["playlist"] = "default" } }, ["entryPoints"] = CreatePlayerEntryPoints() };
    private JObject CreatePanelDefaults() => new JObject { ["width"] = 500, ["height"] = 700, ["cornerRadius"] = 0, ["positions"] = new JObject(), ["animationProfiles"] = new JArray { new JObject { ["id"] = "default", ["name"] = "Default" } }, ["animation"] = new JObject { ["entryPoints"] = new JObject { ["recent"] = "default", ["playlist"] = "default", ["creatorLeaderboard"] = "default" } }, ["entryPoints"] = CreatePanelEntryPoints() };
    private JObject CreateMessageDefaults() => new JObject { ["minWidth"] = 500, ["minHeight"] = 120, ["cornerRadius"] = 0, ["positions"] = new JObject(), ["animationProfiles"] = new JArray { new JObject { ["id"] = "default", ["name"] = "Default" } }, ["animation"] = new JObject { ["selectedProfile"] = "default" }, ["entryPoint"] = CreateMessageEntryPoint() };
    private JObject CreateClapperDefaults() => new JObject { ["animationProfiles"] = new JArray { new JObject { ["id"] = "default", ["name"] = "Default" } }, ["animation"] = new JObject { ["selectedProfile"] = "default" }, ["entryPoint"] = CreateClapperEntryPoint() };
    private JObject Read(string key) { var raw = CPH.GetGlobalVar<string>(key, true); if (string.IsNullOrWhiteSpace(raw)) return new JObject(); try { return JObject.Parse(raw); } catch { return new JObject(); } }
    private void EnsureObject(string key, JObject defaults) { var current = Read(key); if (current.Count == 0) SaveConfig(key, defaults); }
    private void SaveConfig(string key, JObject value) => CPH.SetGlobalVar(key, value.ToString(Newtonsoft.Json.Formatting.None), true);

    public bool AddReplay()
    {
        if (!(CPH.GetGlobalVar<bool?>("rts.actionreplay.autoAdd", true) ?? true)) return true; string path; if (!CPH.TryGetArg("fullPath", out path) || string.IsNullOrWhiteSpace(path) || !File.Exists(path) || !IsReplayFile(path)) return false; var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true); if (!string.IsNullOrWhiteSpace(folder) && !Path.GetFullPath(path).StartsWith(Path.GetFullPath(folder).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) return false; if (Path.GetExtension(path).Equals(".tmp", StringComparison.OrdinalIgnoreCase) || !Stable(path)) return false; var data = Load(); var catalog = GetCatalog(data); var file = Path.GetFileName(path); if (catalog.OfType<JObject>().Any(x => string.Equals((string)x["file"], file, StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceType"] ?? "OBS", "OBS", StringComparison.OrdinalIgnoreCase))) return true; var now = DateTime.Now; var id = now.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6); var creatorId = Get("userId"); var creatorName = Get("userName"); var creatorPlatform = NormalizePlatform(Get("userType")); ApplyPendingCreator(ref creatorPlatform, ref creatorId, ref creatorName); CPH.SetArgument("replayId", id); CPH.SetArgument("replayFile", file); CPH.SetArgument("replayPath", path); CPH.SetArgument("replayName", Path.GetFileNameWithoutExtension(path)); CPH.SetArgument("replayNumber", 1); CPH.SetArgument("replayDate", now.ToString("yyyy-MM-dd")); CPH.SetArgument("replayTime", now.ToString("HH:mm:ss")); CPH.SetArgument("replayPlays", 0); CPH.SetArgument("replayUser", creatorName); CPH.SetArgument("replayUserId", creatorId); CPH.SetArgument("replayPlatform", creatorPlatform); CPH.SetArgument("replayUserPlays", 0); CPH.SetArgument("replayTitle", ""); var title = CPH.Parse(CPH.GetGlobalVar<string>(TitleKey, true) ?? "%replayName%"); if (string.IsNullOrWhiteSpace(title)) title = Path.GetFileNameWithoutExtension(path); var replay = new JObject { ["id"] = id, ["sourceType"] = "OBS", ["sourceId"] = id, ["file"] = file, ["filePath"] = path, ["title"] = title, ["customTitle"] = false, ["added"] = now.ToString("o"), ["captured"] = now.ToString("o"), ["acquisitionMethod"] = "OBSReplayBuffer", ["creator"] = new JObject { ["platform"] = creatorPlatform, ["id"] = creatorId, ["name"] = creatorName }, ["plays"] = 0, ["users"] = new JObject() }; catalog.Insert(0, replay); Save(data); CPH.LogInfo($"RTS Action Replay: added {title} ({id})"); CPH.SetArgument("replayTitle", title); CPH.SetArgument("animationEntryPoint", "obs"); CPH.SetArgument("replayAutoPlay", CPH.GetGlobalVar<bool?>("rts.actionreplay.autoPlay", true) ?? false); var autoPlay = CPH.GetGlobalVar<bool?>("rts.actionreplay.autoPlay", true) ?? false; CPH.SetArgument("userId", creatorId ?? ""); CPH.SetArgument("userName", creatorName ?? ""); CPH.SetArgument("userType", creatorPlatform ?? ""); CPH.SetArgument("broadcast.id", Get("broadcast.id")); var replayCreatedOverlay = CPH.GetGlobalVar<bool?>("rts.actionreplay.message.created.overlay", true) ?? true; var useClapperboard = CPH.GetGlobalVar<bool?>("rts.actionreplay.clapper.useOnReplayCreated", true) ?? true; CPH.SetArgument("replayCreated", replayCreatedOverlay); CPH.SetArgument("replayUseClapperboard", replayCreatedOverlay && useClapperboard); CPH.SetArgument("replayAutoPlay", autoPlay); if (!CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay")) return false; if (replayCreatedOverlay && !useClapperboard) EnqueueReplayCreated(replay, creatorId, creatorName, creatorPlatform); return true;
    }
    private void EnqueueReplayCreated(JObject replay, string creatorId, string creatorName, string creatorPlatform)
    {
        CPH.SetArgument("messageEvent", "Replay Created");
        CPH.SetArgument("replayId", (string)replay["id"] ?? "");
        CPH.SetArgument("replayNumber", 1);
        CPH.SetArgument("replayTitle", (string)replay["title"] ?? "");
        CPH.SetArgument("replayUserId", creatorId ?? "");
        CPH.SetArgument("replayUser", creatorName ?? "");
        CPH.SetArgument("replayPlatform", creatorPlatform ?? "");
        CPH.SetArgument("replaySourcePlatform", (string)replay["sourceType"] ?? "OBS");
        CPH.SetArgument("requesterId", Get("userId"));
        CPH.SetArgument("requesterName", Get("userName"));
        CPH.SetArgument("requesterPlatform", Get("userType"));
        CPH.SetArgument("requesterBroadcastId", Get("broadcast.id"));
        CPH.ExecuteMethod("RTS - Action Replay - Core - Messaging", "Enqueue");
    }

    public bool AddExistingReplay()
    {
        var input = Get("rawInput").Trim();
        if (string.IsNullOrWhiteSpace(input)) { CPH.SendMessage("Please provide a replay filename."); return false; }

        var file = input;
        var title = "";
        if (file.StartsWith("\"") && file.Length > 1)
        {
            var endQuote = file.IndexOf("\"", 1);
            if (endQuote < 0) { CPH.SendMessage("Please close the quoted replay filename."); return false; }
            title = file.Substring(endQuote + 1).Trim();
            file = file.Substring(1, endQuote - 1);
        }
        else
        {
            var parts = file.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            file = parts[0];
            if (parts.Length > 1) title = parts[1].Trim();
        }

        if (string.IsNullOrWhiteSpace(file)) { CPH.SendMessage("Please provide a replay filename."); return false; }
        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true) ?? "";
        if (string.IsNullOrWhiteSpace(folder)) { CPH.SendMessage("The Replay Folder is not configured."); return false; }
        var fileName = Path.GetFileName(file);
        if (!string.Equals(fileName, file, StringComparison.OrdinalIgnoreCase)) { CPH.SendMessage("Please provide a filename from the configured Replay Folder."); return false; }

        var path = Path.Combine(folder, fileName);
        if (!TryAddExistingFile(path, title, out var result)) { CPH.SendMessage(result); return false; }
        CPH.SendMessage(result);
        return true;
    }

    public bool ScanReplays()
    {
        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true) ?? "";
        if (string.IsNullOrWhiteSpace(folder)) { CPH.SendMessage("The Replay Folder is not configured."); return false; }
        if (!Directory.Exists(folder)) { CPH.SendMessage("The configured Replay Folder does not exist."); return false; }

        var added = 0;
        var skipped = 0;
        foreach (var path in Directory.EnumerateFiles(folder))
        {
            if (!IsReplayFile(path)) continue;
            if (IsCataloged(path)) { skipped++; continue; }
            if (TryAddExistingFile(path, "", out _)) added++;
        }
        CPH.SendMessage($"Replay scan complete: {added} added, {skipped} already in the Catalog.");
        return true;
    }

    private bool TryAddExistingFile(string path, string customTitle, out string result)
    {
        result = "";
        if (!File.Exists(path) || !IsReplayFile(path)) { result = "Replay file was not found or its file type is not enabled."; return false; }

        var folder = CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true) ?? "";
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
        var title = string.IsNullOrWhiteSpace(customTitle) ? CPH.Parse(CPH.GetGlobalVar<string>(TitleKey, true) ?? "%replayName%") : customTitle;
        if (string.IsNullOrWhiteSpace(title)) title = Path.GetFileNameWithoutExtension(path);

        var id = captured.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        catalog.Insert(0, new JObject {
            ["id"] = id, ["sourceType"] = "OBS", ["sourceId"] = id,
            ["file"] = Path.GetFileName(path), ["filePath"] = fullPath, ["title"] = title,
            ["customTitle"] = !string.IsNullOrWhiteSpace(customTitle), ["added"] = DateTime.Now.ToString("o"),
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

    public bool NameReplay() { string indexInput; string rawInput; if (!CPH.TryGetArg("input0", out indexInput) || !CPH.TryGetArg("rawInput", out rawInput)) return false; if (!int.TryParse(indexInput, out var index)) { CPH.SendMessage("Please provide a valid replay number."); return false; } var title = (rawInput ?? "").Trim(); if (title.StartsWith(indexInput + " ", StringComparison.OrdinalIgnoreCase)) title = title.Substring(indexInput.Length).Trim(); if (title.Length == 0) { CPH.SendMessage("Please provide a replay title."); return false; } var data = Load(); var list = GetCatalog(data); if (index < 1 || index > list.Count) { CPH.SendMessage($"Replay #{index} does not exist."); return false; } var target = (JObject)list[index - 1]; for (var i = 0; i < list.Count; i++) { var other = (JObject)list[i]; if (ReferenceEquals(other, target) || !((bool?)other["customTitle"] ?? false)) continue; if (string.Equals((string)other["title"], title, StringComparison.OrdinalIgnoreCase)) { CPH.SendMessage("That title already exists."); return false; } } target["title"] = title; target["customTitle"] = true; Save(data); CPH.SetArgument("replayNumber", index); CPH.SetArgument("replayTitle", title); EnqueueReplayRenamed(target, index, title); return true; }
    private void EnqueueReplayRenamed(JObject replay, int number, string title)
    {
        var creator = replay["creator"] as JObject;
        CPH.SetArgument("messageEvent", "Replay Renamed");
        CPH.SetArgument("replayId", (string)replay["id"] ?? "");
        CPH.SetArgument("replayNumber", number);
        CPH.SetArgument("replayTitle", title);
        CPH.SetArgument("replayUserId", (string)creator?["id"] ?? "");
        CPH.SetArgument("replayUser", (string)creator?["name"] ?? "");
        CPH.SetArgument("replayPlatform", (string)creator?["platform"] ?? "");
        CPH.SetArgument("replaySourcePlatform", (string)replay["sourceType"] ?? "OBS");
        CPH.SetArgument("requesterId", Get("userId"));
        CPH.SetArgument("requesterName", Get("userName"));
        CPH.SetArgument("requesterPlatform", NormalizePlatform(Get("userType")));
        CPH.SetArgument("requesterBroadcastId", Get("broadcast.id"));
        CPH.ExecuteMethod("RTS - Action Replay - Core - Messaging", "Enqueue");
    }

    public bool ListPlaylist() { var list = GetCatalog(Load()); var message = list.Count == 0 ? "The replay playlist is empty." : string.Join(" | ", list.OfType<JObject>().Select((x, i) => "#" + (i + 1) + " " + (string)x["title"])); CPH.SetArgument("replayPlaylist", message); SendStoreMessage("playlist"); return true; }
    private void SendStoreMessage(string type) { var key = "rts.actionreplay.message." + type; var text = CPH.GetGlobalVar<string>(key + ".text", true); if (string.IsNullOrWhiteSpace(text)) return; text = CPH.Parse(text); if (CPH.GetGlobalVar<bool?>(key + ".chat", true) ?? true) SendStoreOriginMessage(text); }
    private void SendStoreOriginMessage(string text) { var platform = Get("userType"); if (platform.Equals("Kick", StringComparison.OrdinalIgnoreCase)) { CPH.SendKickMessage(text); return; } if (platform.Equals("YouTube", StringComparison.OrdinalIgnoreCase)) { var broadcastId = Get("broadcast.id"); if (!string.IsNullOrWhiteSpace(broadcastId)) CPH.SendYouTubeMessage(text, true, true, broadcastId); else CPH.SendYouTubeMessageToLatestMonitored(text); return; } if (platform.Equals("Twitch", StringComparison.OrdinalIgnoreCase)) { CPH.SendMessage(text); return; } CPH.LogWarn("RTS Action Replay: unable to route store chat response because the originating platform is unknown."); }
    public bool ShowClapperboard()
    {
        CPH.LogInfo("RTS Action Replay: legacy ShowClapperboard action ignored; Replay Created clapperboard is owned by Playlist.");
        return true;
    }

    private void SetMessageStyleArguments() { CPH.ExecuteMethod("RTS - Action Replay - Core - Resolver", "ResolveMessagePresentation"); }
    private bool IsReplayFile(string path) { var extension = Path.GetExtension(path); if (string.IsNullOrWhiteSpace(extension)) return false; var configured = CPH.GetGlobalVar<string>(FileTypesKey, true) ?? ".mp4, .mkv"; foreach (var raw in configured.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)) { var type = raw.Trim(); if (!type.StartsWith(".")) type = "." + type; if (extension.Equals(type, StringComparison.OrdinalIgnoreCase)) return true; } return false; }
    private bool Stable(string path) { try { long previous = -1; var stableReads = 0; for (var attempt = 0; attempt < 40; attempt++) { var length = new FileInfo(path).Length; if (length == previous) { stableReads++; if (stableReads >= 2) return true; } else { previous = length; stableReads = 0; } System.Threading.Thread.Sleep(250); } return false; } catch { return false; } }
    private bool PlaylistHasItems() { var persist = CPH.GetGlobalVar<bool?>("rts.actionreplay.playlistPersist", true) ?? false; var raw = CPH.GetGlobalVar<string>(PlaylistKey, persist); if (string.IsNullOrWhiteSpace(raw)) return false; try { return JArray.Parse(raw).Count > 0; } catch { return false; } }
    private string Get(string name) { CPH.TryGetArg(name, out string value); return value ?? ""; }
    private string NormalizePlatform(string userType) { if (string.Equals(userType, "YouTube", StringComparison.OrdinalIgnoreCase)) return "YouTube"; if (string.Equals(userType, "Kick", StringComparison.OrdinalIgnoreCase)) return "Kick"; return "Twitch"; }
    private void ApplyPendingCreator(ref string platform, ref string id, ref string name) { var raw = CPH.GetGlobalVar<string>(PendingKey, false); if (string.IsNullOrWhiteSpace(raw)) return; try { var pending = JObject.Parse(raw); if (!string.IsNullOrWhiteSpace((string)pending["platform"])) platform = NormalizePlatform((string)pending["platform"]); if (!string.IsNullOrWhiteSpace((string)pending["id"])) id = (string)pending["id"]; if (!string.IsNullOrWhiteSpace((string)pending["name"])) name = (string)pending["name"]; } catch { } }
    private JObject Load() { var raw = CPH.GetGlobalVar<string>(DataKey, true); try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { return new JObject(); } }
    private void Save(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
    private void Save(string key, JObject value) => CPH.SetGlobalVar(key, value.ToString(Newtonsoft.Json.Formatting.None), true);

    private JArray GetCatalog(JObject data) { var catalog = data["catalog"] as JArray; if (catalog != null) return catalog; catalog = new JArray(); data["catalog"] = catalog; return catalog; }


    // Consolidated preset/config persistence from RTSActionReplayPresetStore.
    private JObject Read(string key, JObject fallback) { var raw = CPH.GetGlobalVar<string>(key, true); try { return string.IsNullOrWhiteSpace(raw) ? fallback : JObject.Parse(raw); } catch { return fallback; } }

    const string MessageKey = "rts.actionreplay.config.message";
    const string ClapperKey = "rts.actionreplay.config.clapper";

    const string PresetsKey = "rts.actionreplay.config.presets";

    public bool EnsureDefaults() { var p = Read(PresetsKey, Defaults()); var d = Defaults(); if (!(p["branding"] is JArray)) p["branding"] = d["branding"]; if (!(p["visual"] is JArray)) p["visual"] = d["visual"]; if (!(p["title"] is JArray)) p["title"] = TitleDefaults(); EnsureBranding(p); EnsureVisuals(p); EnsureTitlePresets(p); Save(PresetsKey, p); CPH.ExecuteMethod("RTS - Action Replay - Core - Resolver", "EnsurePositions"); return true; }

    public bool GetConfigurationSnapshot()
    {
        EnsureData();
        EnsureDefaults();
        EnsureEntryPoints();

        var config = new JObject
        {
            ["data"] = Load(),
            ["player"] = Read(PlayerKey, CreatePlayerDefaults()),
            ["panel"] = Read(PanelKey, CreatePanelDefaults()),
            ["clapperboard"] = Read(ClapperKey, CreateClapperDefaults()),
            ["message"] = Read(MessageKey, CreateMessageDefaults()),
            ["presets"] = Read(PresetsKey, Defaults()),
            ["animation"] = Read("rts.actionreplay.config.animation", new JObject()),
            ["globals"] = new JObject()
        };

        var globals = (JObject)config["globals"];
        AddSnapshotGlobal(globals, "uiTheme", "rts.actionreplay.uiTheme");
        AddSnapshotGlobal(globals, "replayFolder", "rts.actionreplay.replayFolder");
        AddSnapshotGlobal(globals, "replayFileTypes", "rts.actionreplay.replayFileTypes");
        AddSnapshotGlobal(globals, "httpMapping", "rts.actionreplay.httpMapping");
        AddSnapshotGlobal(globals, "httpPort", "rts.actionreplay.httpPort");
        AddSnapshotGlobal(globals, "replayTitle", "rts.actionreplay.replayTitle");
        AddSnapshotGlobal(globals, "newReplayTitle", "rts.actionreplay.newReplayTitle");
        AddSnapshotGlobal(globals, "maxHistory", "rts.actionreplay.maxHistory");
        AddSnapshotGlobal(globals, "autoAdd", "rts.actionreplay.autoAdd");
        AddSnapshotGlobal(globals, "autoPlay", "rts.actionreplay.autoPlay");
        AddSnapshotGlobal(globals, "clapperUseSourcePlatformBranding", "rts.actionreplay.clapper.useSourcePlatformBranding");
        AddSnapshotGlobal(globals, "playlistPersist", "rts.actionreplay.playlistPersist");
        AddSnapshotGlobal(globals, "twitchPlaybackMode", "rts.actionreplay.twitch.playbackMode");
        AddSnapshotGlobal(globals, "twitchFolder", "rts.actionreplay.twitch.folder");
        AddSnapshotGlobal(globals, "twitchHttpMapping", "rts.actionreplay.twitch.httpMapping");
        AddSnapshotGlobal(globals, "twitchClipDuration", "rts.actionreplay.twitch.clipDuration");
        AddSnapshotGlobal(globals, "kickPlaybackMode", "rts.actionreplay.kick.playbackMode");
        AddSnapshotGlobal(globals, "kickFolder", "rts.actionreplay.kick.folder");
        AddSnapshotGlobal(globals, "kickHttpMapping", "rts.actionreplay.kick.httpMapping");
        AddSnapshotGlobal(globals, "youtubeClipDuration", "rts.actionreplay.youtube.clipDuration");
        AddSnapshotGlobal(globals, "showControls", "rts.actionreplay.showControls");
        AddSnapshotGlobal(globals, "showProgress", "rts.actionreplay.showProgress");
        AddSnapshotGlobal(globals, "playbackSpeed", "rts.actionreplay.playbackSpeed");
        AddSnapshotGlobal(globals, "playbackSpeedVisibility", "rts.actionreplay.playbackSpeedVisibility");
        AddSnapshotGlobal(globals, "frameColorSource", "rts.actionreplay.frameColorSource");
        AddSnapshotGlobal(globals, "frameColor", "rts.actionreplay.frameColor");
        AddSnapshotGlobal(globals, "controlColorSource", "rts.actionreplay.controlColorSource");
        AddSnapshotGlobal(globals, "controlColor", "rts.actionreplay.controlColor");
        AddSnapshotGlobal(globals, "borderGlow", "rts.actionreplay.borderGlow");
        AddSnapshotGlobal(globals, "borderWidth", "rts.actionreplay.borderWidth");
        AddSnapshotGlobal(globals, "cornerRadius", "rts.actionreplay.cornerRadius");
        AddSnapshotGlobal(globals, "panelWidth", "rts.actionreplay.panel.width");
        AddSnapshotGlobal(globals, "panelHeight", "rts.actionreplay.panel.height");
        AddSnapshotGlobal(globals, "panelCornerRadius", "rts.actionreplay.panel.cornerRadius");
        AddSnapshotGlobal(globals, "messageMinWidth", "rts.actionreplay.message.minWidth");
        AddSnapshotGlobal(globals, "messageMinHeight", "rts.actionreplay.message.minHeight");
        AddSnapshotGlobal(globals, "messageCornerRadius", "rts.actionreplay.message.cornerRadius");
        AddSnapshotGlobal(globals, "messageDuration", "rts.actionreplay.message.duration");
        AddSnapshotGlobal(globals, "clapperDuration", "rts.actionreplay.clapper.duration");
        AddSnapshotGlobal(globals, "brandLogoUrl", "rts.actionreplay.brandLogoUrl");

        foreach (var prefix in new[] { "save", "name", "play", "recent", "playlist" })
        {
            AddSnapshotGlobal(globals, "message" + Cap(prefix) + "Text", "rts.actionreplay.message." + prefix + ".text");
            AddSnapshotGlobal(globals, "message" + Cap(prefix) + "Chat", "rts.actionreplay.message." + prefix + ".chat");
            AddSnapshotGlobal(globals, "message" + Cap(prefix) + "Overlay", "rts.actionreplay.message." + prefix + ".overlay");
        }

        CPH.SetGlobalVar(ConfigurationSnapshotKey, config.ToString(Newtonsoft.Json.Formatting.None), false);
        CPH.LogInfo("RTS Action Replay: configuration snapshot prepared.");
        return true;
    }

    private string Cap(string value) => char.ToUpperInvariant(value[0]) + value.Substring(1);

    private JToken ReadStringObject(string key)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(raw)) return new JObject();
        try { return JObject.Parse(raw); } catch { return new JObject(); }
    }

    private void AddSnapshotGlobal(JObject target, string name, string key)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        if (raw == null) return;
        if (bool.TryParse(raw, out var b)) target[name] = b;
        else if (int.TryParse(raw, out var i)) target[name] = i;
        else if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)) target[name] = d;
        else target[name] = raw;
    }

    public bool EnsureEntryPoints() { EnsureDefaults(); var player = Read(PlayerKey, CreatePlayerDefaults()); var panel = Read(PanelKey, CreatePanelDefaults()); var clapper = Read(ClapperKey, CreateClapperDefaults()); if (!(player["entryPoints"] is JObject)) player["entryPoints"] = CreatePlayerEntryPoints(); if (!(panel["entryPoints"] is JObject)) panel["entryPoints"] = CreatePanelEntryPoints(); if (!(clapper["entryPoint"] is JObject)) clapper["entryPoint"] = CreateClapperEntryPoint(); var message = Read(MessageKey, CreateMessageDefaults()); if (!(message["entryPoint"] is JObject)) message["entryPoint"] = CreateMessageEntryPoint(); Save(PlayerKey, player); Save(PanelKey, panel); Save(ClapperKey, clapper); Save(MessageKey, message); return true; }

    private JObject CreatePlayerEntryPoints() { return new JObject { ["obs"] = CreateEntryPoint(), ["twitch"] = CreateEntryPoint(), ["youtube"] = CreateEntryPoint(), ["kick"] = CreateEntryPoint(), ["play"] = new JObject { ["animationProfile"] = "default", ["designPreset"] = "broadcast", ["titlePreset"] = "default", ["brandingPreset"] = "default", ["useSourcePlatformBranding"] = false } }; }
    private JObject CreatePanelEntryPoints() { return new JObject { ["recent"] = CreateEntryPoint(), ["playlist"] = CreateEntryPoint(), ["creatorLeaderboard"] = CreateEntryPoint() }; }
    private JObject CreateEntryPoint() { return new JObject { ["animationProfile"] = "default", ["designPreset"] = "broadcast", ["titlePreset"] = "default", ["brandingPreset"] = "default" }; }
    private JObject CreateMessageEntryPoint() { return new JObject { ["animationProfile"] = "default", ["designPreset"] = "broadcast", ["brandingPreset"] = "default" }; }
    private JObject CreateClapperEntryPoint() { return new JObject { ["animationProfile"] = "default", ["brandingPreset"] = "default" }; }

    public JArray Branding() => Read(PresetsKey, Defaults())["branding"] as JArray ?? new JArray();

    public JArray Visuals() => Read(PresetsKey, Defaults())["visual"] as JArray ?? new JArray();

    public JArray Titles() => Read(PresetsKey, Defaults())["title"] as JArray ?? new JArray();

    static JObject Find(JArray a, string id) { foreach (var x in a ?? new JArray()) if (string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase)) return x as JObject; return null; }

    static string ResolveId(JArray a, string id) { var x = Find(a, id); return x == null ? null : (string)x["id"]; }






    JObject Resolve(JObject c, string ep, string[] valid) { var a = c["entryPoints"] as JObject ?? new JObject(); var id = string.IsNullOrWhiteSpace(ep) ? valid[0] : ep.Trim().ToLowerInvariant(); if (Array.IndexOf(valid, id) < 0) id = valid[0]; var e = a[id] as JObject ?? new JObject(); return new JObject { { "animationProfile", (string)e["animationProfile"] ?? "default" }, { "designPreset", ResolveId(Visuals(), (string)e["designPreset"] ?? (string)e["visualPreset"] ?? "broadcast") ?? "broadcast" }, { "titlePreset", ResolveId(Titles(), (string)e["titlePreset"] ?? "default") ?? "default" }, { "brandingPreset", ResolveId(Branding(), (string)e["brandingPreset"] ?? "default") ?? "default" }, { "useSourcePlatformBranding", (bool?)e["useSourcePlatformBranding"] ?? false } }; }





    JObject Defaults() { return new JObject { { "branding", new JArray( new JObject { { "id", "default" }, { "name", "Default" }, { "platform", "" }, { "primaryColor", "#0384CBFF" }, { "secondaryColor", "#101416FF" }, { "titleColor", "#FFFFFFFF" }, { "titlePrefixSuffixColor", "#0384CBFF" }, { "textColor", "#FFFFFFFF" }, { "shadowColor", "#000000FF" }, { "font", "Inter" }, { "fontSize", 34 }, { "logo", "" }, { "fallbackText", "RTS" }, { "brandLabel", "ACTION REPLAY" } }, new JObject { { "id", "rts" }, { "name", "RTS" }, { "platform", "" }, { "primaryColor", "#0384CBFF" }, { "secondaryColor", "#FFD400FF" }, { "titleColor", "#FFD400FF" }, { "titlePrefixSuffixColor", "#0384CBFF" }, { "textColor", "#FFFFFFFF" }, { "shadowColor", "#000000FF" }, { "font", "Inter" }, { "fontSize", 34 }, { "logo", "" }, { "fallbackText", "RTS" }, { "brandLabel", "ACTION REPLAY" } }, new JObject { { "id", "twitch" }, { "name", "Twitch" }, { "platform", "Twitch" }, { "primaryColor", "#9146FFFF" }, { "secondaryColor", "#FFFFFFFF" }, { "titleColor", "#FFFFFFFF" }, { "titlePrefixSuffixColor", "#FFFFFFFF" }, { "textColor", "#FFFFFFFF" }, { "shadowColor", "#000000FF" }, { "font", "Inter" }, { "fontSize", 34 }, { "logo", "https://www.freepnglogos.com/uploads/twitch-logo-vector-png-2.png" }, { "fallbackText", "Twitch" }, { "brandLabel", "" } }, new JObject { { "id", "youtube" }, { "name", "YouTube" }, { "platform", "YouTube" }, { "primaryColor", "#D4101DFF" }, { "secondaryColor", "#FFFFFFFF" }, { "titleColor", "#FFFFFFFF" }, { "titlePrefixSuffixColor", "#FFFFFFFF" }, { "textColor", "#FFFFFFFF" }, { "shadowColor", "#FFFFFFFF" }, { "font", "Oswald" }, { "fontSize", 34 }, { "logo", "https://www.freepnglogos.com/uploads/youtube-logo-png/youtube-transparent-youtube-icon-29.png" }, { "fallbackText", "YouTube" }, { "brandLabel", "" } }, new JObject { { "id", "kick" }, { "name", "Kick" }, { "platform", "Kick" }, { "primaryColor", "#53FC18FF" }, { "secondaryColor", "#FFFFFFFF" }, { "titleColor", "#FFFFFFFF" }, { "titlePrefixSuffixColor", "#FFFFFFFF" }, { "textColor", "#000000FF" }, { "shadowColor", "#000000FF" }, { "font", "Inter" }, { "fontSize", 34 }, { "logo", "https://static.kick.com/kick-logo.svg" }, { "fallbackText", "Kick" }, { "brandLabel", "" } } )}, { "visual", new JArray( new JObject { { "id", "broadcast" }, { "name", "Broadcast" }, { "backgroundSource", "RTS Dark Blue" }, { "backgroundColor", "#101416FF" }, { "chevronHeight", 42 }, { "randomHeight", false }, { "chevronWidth", 42 }, { "randomWidth", false }, { "chevronSpacing", 0 }, { "randomSpacing", false }, { "chevronSpeed", 95 }, { "design", "broadcast" }, }, new JObject { { "id", "cinematic" }, { "name", "Cinematic" }, { "fixed", true }, { "design", "cinematic" }, }, new JObject { { "id", "cut" }, { "name", "Cut" }, { "backgroundSource", "RTS Dark Blue" }, { "backgroundColor", "#101416FF" }, { "blockWidth", 170 }, { "randomWidth", true }, { "barHeight", 5 }, { "design", "cut" }, }, new JObject { { "id", "minimal" }, { "name", "Minimal" }, { "fixed", true }, { "design", "minimal" }, } )} }; }

    JArray TitleDefaults() { var d = new JObject { { "id", "default" }, { "name", "Default" }, { "decorationPosition", "Prefix" }, { "decoration", "Action Replay -" }, { "position", "Bottom" }, { "animation", "Left to right" }, { "delay", 2000 }, { "duration", 10000 }, { "animationDuration", 1000 } }; return new JArray(d); }


    void EnsureBranding(JObject p) { var a = p["branding"] as JArray ?? new JArray(); foreach (var x in a) if (x["platform"] == null) x["platform"] = ""; p["branding"] = a; }

    void EnsureVisuals(JObject p) { var a = p["visual"] as JArray ?? new JArray(); var d = Defaults()["visual"] as JArray; foreach (var id in new[] { "broadcast", "cinematic", "cut", "minimal" }) { var x = Find(a, id); var def = Find(d, id); if (x == null) { a.Add(def.DeepClone()); continue; } if (x["design"] == null) x["design"] = (string)def["design"]; if (x["name"] == null) x["name"] = (string)def["name"]; foreach (var f in new[] { "chevronHeight", "randomHeight", "chevronWidth", "randomWidth", "chevronSpacing", "randomSpacing", "chevronSpeed", "blockWidth", "barHeight", "backgroundColor" }) if (x[f] == null && def[f] != null) x[f] = def[f]; if (id == "cut" && x["backgroundSource"] == null) x["backgroundSource"] = x["backgroundColor"] != null ? "Custom" : "RTS Dark Blue"; if (id == "broadcast" && x["backgroundSource"] == null) x["backgroundSource"] = "RTS Dark Blue"; } p["visual"] = a; }

    void EnsureTitlePresets(JObject p) { var a = p["title"] as JArray ?? new JArray(); if (a.Count == 0) a = TitleDefaults(); foreach (var x in a) { ((JObject)x).Remove("showTitle"); if (x["name"] == null) x["name"] = (string)x["id"] ?? "Title Preset"; if (x["decorationPosition"] == null) x["decorationPosition"] = "Prefix"; if (x["position"] == null) x["position"] = "Bottom"; if (x["animation"] == null) x["animation"] = "Left to right"; if (x["delay"] == null) x["delay"] = 2000; if (x["duration"] == null) x["duration"] = 10000; if (x["animationDuration"] == null) x["animationDuration"] = 1000; } p["title"] = a; }
}
