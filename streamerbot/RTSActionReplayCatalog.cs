using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string MaxHistoryKey = "rts.actionreplay.maxHistory";
    private const string UserStateKey = "rts.actionreplay.catalogState";
    private const string ActiveReplayKey = "rts.actionreplay.playlistActiveReplayId";
    private const string PlaylistKey = "rts.actionreplay.playlist";
    private const string ActiveKey = "rts.actionreplay.playlistActive";
    private const string SearchQueueAction = "RTS - Action Replay - Core - Search Queue";
    private const string ResolverAction = "RTS - Action Replay - Core - Resolver";
    private const string PanelOperationKey = "rts.actionreplay.operation.panel";
    private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";
    private static readonly object AvatarCacheLock = new object();
    private static readonly Dictionary<string, string> AvatarCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public bool Execute() => string.Equals(Arg("replayAvatarRequestId"), "", StringComparison.Ordinal) ? ListCatalog() : ResolveAvatar();
    public bool ListCatalog() { var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); var amount = ParseAmount(ref parts); var search = string.Join(" ", parts).Trim(); return Queue(BuildState(string.IsNullOrWhiteSpace(search) ? "all" : "search", search, "catalog", amount)); }
    public bool ListRecent()
    {
        var state = BuildState("recent", "", "recent", ParseAmount(Arg("rawInput")));
        if (CPH.GetGlobalVar<bool?>("rts.actionreplay.message.recent.chat", true) ?? true)
            SendRecentChat(new JArray(Query(state).Take(Math.Max(1, (int?)state["amount"] ?? MaxAmount()))));
        return Queue(state);
    }
    public bool ListLastPlayed() => Queue(BuildState("lastplayed", "", "history", ParseAmount(Arg("rawInput"))));
    public bool PlayHistory() { var input = Arg("rawInput").Trim(); if (!int.TryParse(input, out var index) || index < 1) { SendCatalogMessage("Please provide a history item number."); return false; } var history = (Load()["playHistory"] as JArray) ?? new JArray(); if (index > history.Count) { SendCatalogMessage($"History item #{index} does not exist."); return false; } var replayId = (string)(history[index - 1] as JObject)?["replayId"]; if (string.IsNullOrWhiteSpace(replayId)) { SendCatalogMessage($"History item #{index} is unavailable."); return false; } CPH.SetArgument("replayId", replayId); return CPH.ExecuteMethod(PlaylistAction, "EnqueueCurrentReplay"); }
    public bool ResolveAvatar() { var requestId = Arg("replayAvatarRequestId"); var platform = NormalizePlatform(Arg("replayAvatarPlatform")); var userId = Arg("replayAvatarUserId"); var userName = Arg("replayAvatarUserName"); var avatar = ResolveAvatarUrl(platform, userId, userName); CPH.SetArgument("replayCommand", "avatar-response"); CPH.SetArgument("replayAvatarRequestId", requestId); CPH.SetArgument("replayAvatarUserId", userId); CPH.SetArgument("replayAvatarUrl", avatar ?? ""); CPH.TriggerEvent("RTS-Action Replay", true); return true; }
    public bool ListLeaderboard() { var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); var amount = ParseAmount(ref parts); var period = string.Join(" ", parts).Trim(); return Queue(BuildState("leaderboard", period, "leaderboard", amount > 0 ? amount : 5)); }
    public bool ListCatalogDate() { var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); var amount = ParseAmount(ref parts); var period = string.Join(" ", parts).Trim(); if (string.IsNullOrWhiteSpace(period)) { SendCatalogMessage("Please provide a catalog date period."); return false; } return Queue(BuildState("date", period, "catalog", amount)); }
    public bool ListCatalogCreator() { var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); var amount = ParseAmount(ref parts); var creator = string.Join(" ", parts).Trim(); if (string.IsNullOrWhiteSpace(creator)) { SendCatalogMessage("Please provide a creator name."); return false; } return Queue(BuildState("creator", creator, "catalog", amount)); }
    public bool ListCatalogMostViews() => Queue(BuildState("all", "", "plays", ParseAmount(Arg("rawInput"))));
    public bool ListCatalogTopRated() { var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); var amount = ParseAmount(ref parts); var rating = ""; if (parts.Length > 0) rating = parts[0]; return Queue(BuildState("all", rating, "rating", amount)); }
    public bool ShowSearchPage() => Queue(LoadUserState());
    public bool CatalogNext() => MovePage(1);
    public bool CatalogPrevious() => MovePage(-1);
    public bool CatalogFirst() => SetPage(1);
    public bool CatalogLast() { var state = LoadUserState(); var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); state["page"] = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount)); SaveUserState(state); return Queue(state); }
    public bool ShowUserSearch() => ShowUserSearchPage(0);
    public bool UserSearchNext() => ShowUserSearchPage(1);
    public bool UserSearchPrevious() => ShowUserSearchPage(-1);
    public bool ResolveSelection() { var selector = Arg("rawInput").Trim(); if (!int.TryParse(selector, out var index) || index < 1) return false; var state = LoadUserState(); if (string.Equals((string)state["filterType"], "leaderboard", StringComparison.OrdinalIgnoreCase)) return false; return ResolveStateSelection(state, index); }

    public bool Purge()
    {
        var data = Load();
        var catalog = data["catalog"] as JArray ?? new JArray();
        var kept = new JArray();
        var removed = 0;
        var removedItems = new List<string>();
        foreach (var item in catalog.OfType<JObject>())
        {
            // Kick/KickBot clips are cloud-hosted and may take time to become downloadable.
            // Do not treat a temporarily unavailable Kick URL as proof that the Catalog item is dead.
            if (string.Equals((string)item["sourceType"], "Kick", StringComparison.OrdinalIgnoreCase))
            {
                kept.Add(item);
                continue;
            }
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
        SendCatalogMessage($"Catalog purge complete: {removed} unavailable replay(s) removed, {kept.Count} kept.");
        SendPurgeRemovedItems(removedItems);
        return true;
    }

    private bool HasLocalFile(JObject item)
    {
        var path = (string)item["filePath"];
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) return true;
        var file = (string)item["file"];
        if (string.IsNullOrWhiteSpace(file)) return false;
        var folder = GetPurgeFolder((string)item["sourceType"]);
        if (string.IsNullOrWhiteSpace(folder)) return false;
        return File.Exists(Path.IsPathRooted(file) ? file : Path.Combine(folder, file));
    }

    private string GetPurgeFolder(string source)
    {
        if (string.Equals(source, "Twitch", StringComparison.OrdinalIgnoreCase)) return CPH.GetGlobalVar<string>("rts.actionreplay.twitch.folder", true) ?? "";
        if (string.Equals(source, "Kick", StringComparison.OrdinalIgnoreCase)) return CPH.GetGlobalVar<string>("rts.actionreplay.kick.folder", true) ?? "";
        return CPH.GetGlobalVar<string>("rts.actionreplay.replayFolder", true) ?? "";
    }

    private bool HasUrl(JObject item)
    {
        foreach (var url in PurgeUrlCandidates(item)) if (PurgeUrlExists(url)) return true;
        return false;
    }

    private IEnumerable<string> PurgeUrlCandidates(JObject item)
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

    private bool PurgeUrlExists(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) return false;
        try
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "HEAD"; request.AllowAutoRedirect = true; request.Timeout = 3000; request.UserAgent = "RTS-Action-Replay";
            using (var response = (HttpWebResponse)request.GetResponse()) return PurgeHttpSuccess(response.StatusCode);
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
            request.Method = "GET"; request.AddRange(0, 0); request.AllowAutoRedirect = true; request.Timeout = 3000; request.UserAgent = "RTS-Action-Replay";
            using (var response = (HttpWebResponse)request.GetResponse()) return PurgeHttpSuccess(response.StatusCode);
        }
        catch { return false; }
    }

    private bool PurgeHttpSuccess(HttpStatusCode status) { var code = (int)status; return code >= 200 && code < 300; }

    private void SendPurgeRemovedItems(List<string> items)
    {
        if (items.Count == 0) { SendCatalogMessage("Nothing was purged."); return; }
        var message = "Purged: ";
        foreach (var item in items)
        {
            var next = message == "Purged: " ? item : message + " | " + item;
            if (next.Length > 400) { SendCatalogMessage(message); message = "Purged: " + item; } else message = next;
        }
        if (message != "Purged: ") SendCatalogMessage(message);
    }

    public bool DeleteCatalogItem()
    {
        CPH.SetArgument("catalogSelectionReplayId", "");
        if (!ResolveSelection())
        {
            SendCatalogMessage("Please provide a valid Catalog result number.");
            return false;
        }

        var replayId = Arg("catalogSelectionReplayId");
        var data = Load();
        var catalog = data["catalog"] as JArray ?? new JArray();
        var replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], replayId, StringComparison.OrdinalIgnoreCase));
        if (replay == null)
        {
            SendCatalogMessage("The selected replay is no longer in the Catalog.");
            return false;
        }

        var activeReplayId = CPH.GetGlobalVar<string>(ActiveReplayKey, false) ?? "";
        if (string.Equals(activeReplayId, replayId, StringComparison.OrdinalIgnoreCase))
        {
            SendCatalogMessage("The selected replay is currently playing and cannot be deleted.");
            return false;
        }

        var queueRaw = CPH.GetGlobalVar<string>(PlaylistKey, false);
        if (!string.IsNullOrWhiteSpace(queueRaw))
        {
            try
            {
                var queue = JArray.Parse(queueRaw);
                if (queue.OfType<JObject>().Any(x => string.Equals((string)x["replayId"], replayId, StringComparison.OrdinalIgnoreCase)))
                {
                    SendCatalogMessage("The selected replay is in the Playlist and cannot be deleted.");
                    return false;
                }
            }
            catch
            {
                CPH.LogWarn("RTS Action Replay: unable to inspect Playlist while deleting Catalog item.");
                SendCatalogMessage("The selected replay could not be safely deleted.");
                return false;
            }
        }

        var creator = replay["creator"] as JObject;
        var title = (string)replay["title"] ?? "Replay";
        catalog.Remove(replay);

        var history = data["playHistory"] as JArray ?? new JArray();
        for (var i = history.Count - 1; i >= 0; i--)
        {
            if (string.Equals((string)history[i]?["replayId"], replayId, StringComparison.OrdinalIgnoreCase))
                history.RemoveAt(i);
        }

        data["catalog"] = catalog;
        data["playHistory"] = history;
        Save(data);

        CPH.SetArgument("messageEvent", "Replay Deleted");
        CPH.SetArgument("replayId", replayId);
        CPH.SetArgument("replayNumber", "");
        CPH.SetArgument("replayTitle", title);
        CPH.SetArgument("replayRating", "");
        CPH.SetArgument("replayUserId", (string)creator?["id"] ?? "");
        CPH.SetArgument("replayUser", (string)creator?["name"] ?? "");
        CPH.SetArgument("replayPlatform", (string)creator?["platform"] ?? "");
        CPH.SetArgument("replaySourcePlatform", (string)replay["sourceType"] ?? "OBS");
        CPH.SetArgument("requesterId", Arg("userId"));
        CPH.SetArgument("requesterName", Arg("userName"));
        CPH.SetArgument("requesterPlatform", CurrentPlatform());
        CPH.SetArgument("requesterBroadcastId", Arg("broadcast.id"));
        CPH.ExecuteMethod("RTS - Action Replay - Core - Messaging", "Enqueue");

        CPH.LogInfo($"RTS Action Replay: Catalog replay deleted; id={replayId}; title={title}.");
        return true;
    }
    public bool ResolveSelectionForUser() { var selector = Arg("rawInput").Trim(); var parts = selector.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); var indexText = parts.Length == 1 ? parts[0] : parts.Length == 2 ? parts[1] : ""; if (!int.TryParse(indexText, out var index) || index < 1) return false; var platform = Arg("catalogSelectionPlatform"); var userName = Arg("catalogSelectionUser"); if (!TryParseUserTarget(platform + ":" + userName, out platform, out userName)) return false; var userId = ResolveUserId(platform, userName); if (string.IsNullOrWhiteSpace(userId)) return false; var state = LoadUserSearchState(platform, userId, userName); if (state == null || string.Equals((string)state["filterType"], "leaderboard", StringComparison.OrdinalIgnoreCase)) return false; return ResolveStateSelection(state, index); }
    private void SendRecentChat(JArray results)
    {
        for (var i = 0; i < results.Count; i++)
        {
            var replay = results[i] as JObject;
            if (replay == null) continue;
            var creator = replay["creator"] as JObject;
            SendListEntry(i + 1, (string)replay["title"] ?? "Untitled replay", (string)creator?["name"] ?? "", HasRatings(replay) ? Math.Round(Rating(replay), 1) : 0, (string)creator?["platform"] ?? (string)replay["sourceType"] ?? "", (int?)replay["plays"] ?? 0);
        }
    }

    private void SendListEntry(int number, string title, string creator, double rating, string platform, int plays)
    {
        CPH.SetArgument("listNumber", number);
        CPH.SetArgument("title", title);
        CPH.SetArgument("creator", creator);
        CPH.SetArgument("rating", rating);
        CPH.SetArgument("platform", platform);
        CPH.SetArgument("plays", plays);
        if (!CPH.ExecuteMethod("RTS - Action Replay - Core - Messaging", "FormatListEntry")) return;
        if (!CPH.TryGetArg("formattedListEntry", out string message) || string.IsNullOrWhiteSpace(message)) return;
        SendCatalogMessage(message);
    }

    public bool RenderSearchRequest() {
        var json = Arg("replaySearchRequest"); if (string.IsNullOrWhiteSpace(json)) return false;
        JObject request; try { request = JObject.Parse(json); } catch { return false; }
        var isLeaderboard = string.Equals((string)request["filterType"], "leaderboard", StringComparison.OrdinalIgnoreCase);
        var results = Query(request); var amount = Math.Max(1, (int?)request["amount"] ?? MaxAmount());
        var page = Math.Max(1, (int?)request["page"] ?? 1); var pages = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount));
        page = Math.Min(page, pages); var start = (page - 1) * amount;
        var showChat = CPH.GetGlobalVar<bool?>("rts.actionreplay.search.chat", true) ?? false;
        var showPanel = CPH.GetGlobalVar<bool?>("rts.actionreplay.search.panel", true) ?? true;
        if (!showChat && !showPanel) return true;
        if (showChat) RenderSearchChat(request, results, page, pages, amount, start);
        if (!showPanel) return true;
        var operation = (JObject)request.DeepClone();
        operation["replaySearchHeader"] = Header(request, page, pages, results.Count);
        operation["replaySearchRequester"] = (string)request["requesterName"] ?? "";
        operation["replaySearchRequesterPlatform"] = (string)request["platform"] ?? "";
        operation["replaySearchParameters"] = Parameters(request);
        operation["replaySearchMode"] = string.Equals((string)request["filterType"], "lastplayed", StringComparison.OrdinalIgnoreCase) ? "lastPlayed" : "";
        operation["replaySearchRequestId"] = (string)request["requestId"] ?? "";
        operation["replaySearchDuration"] = CPH.GetGlobalVar<int?>("rts.actionreplay.searchPanel.duration", true) ?? 10000;
        operation["replayPanelWidth"] = CPH.GetGlobalVar<int?>("rts.actionreplay.panel.width", true) ?? 500;
        operation["replayPanelHeight"] = CPH.GetGlobalVar<int?>("rts.actionreplay.panel.height", true) ?? 700;
        operation["panelType"] = isLeaderboard ? "creatorLeaderboard" : "recent";
        operation["replayCommand"] = isLeaderboard ? "leaderboard-panel" : "search-panel";
        if (isLeaderboard) {
            var entries = results.Skip(start).Take(amount).OfType<JObject>().Select((x, i) => new JObject { ["rank"] = start + i + 1, ["creator"] = (string)x["creator"] ?? "Unknown creator", ["count"] = (int?)x["count"] ?? 0 }).ToList();
            operation["replayLeaderboardEntries"] = new JArray(entries).ToString(Newtonsoft.Json.Formatting.None);
            operation["replayLeaderboardPeriod"] = LeaderboardPeriodLabel((string)request["filter"] ?? "");
        } else {
            var entries = results.Skip(start).Take(amount).OfType<JObject>().Select((x, i) => Entry(x, start + i + 1)).ToList();
            operation["replaySearchEntries"] = new JArray(entries).ToString(Newtonsoft.Json.Formatting.None);
        }
        CPH.SetGlobalVar(PanelOperationKey, operation.ToString(Newtonsoft.Json.Formatting.None), false);
        return CPH.ExecuteMethod(ResolverAction, "ResolvePanel");
    }

    private void RenderSearchChat(JObject request, JArray results, int page, int pages, int amount, int start)
    {
        SendCatalogMessage($"{Header(request, page, pages, results.Count)}");
        var entries = results.Skip(start).Take(amount).OfType<JObject>().ToList();
        if (entries.Count == 0)
        {
            SendCatalogMessage("No results on this search page.");
            return;
        }
        for (var i = 0; i < entries.Count; i++)
        {
            var replay = entries[i];
            var creatorObject = replay["creator"] as JObject;
            var creator = (string)creatorObject?["name"] ?? (string)replay["creator"] ?? "";
            var platform = (string)creatorObject?["platform"] ?? (string)replay["lastPlayedPlatform"] ?? (string)replay["sourceType"] ?? "";
            var title = (string)replay["title"] ?? "Untitled replay";
            var rating = HasRatings(replay) ? Math.Round(Rating(replay), 1) : 0;
            var plays = (int?)replay["plays"] ?? 0;
            SendListEntry(start + i + 1, title, creator, rating, platform, plays);
        }
    }

    public bool RecordPlayed() { var replayId = Arg("historyReplayId"); if (string.IsNullOrWhiteSpace(replayId)) return false; var data = Load(); var catalog = data["catalog"] as JArray ?? new JArray(); var replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], replayId, StringComparison.OrdinalIgnoreCase)); if (replay == null) return false; replay["plays"] = Math.Max(0, (int?)replay["plays"] ?? 0) + 1; var history = data["playHistory"] as JArray ?? new JArray(); var existing = history.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["replayId"], replayId, StringComparison.OrdinalIgnoreCase)); var count = existing == null ? 1 : Math.Max(1, (int?)existing["count"] ?? 1) + 1; if (existing != null) history.Remove(existing); var requesterId = ""; var activeId = CPH.GetGlobalVar<string>(ActiveKey, false); var queueRaw = CPH.GetGlobalVar<string>(PlaylistKey, false); if (!string.IsNullOrWhiteSpace(queueRaw)) { try { var queue = JArray.Parse(queueRaw); var active = queue.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["entryId"], activeId, StringComparison.OrdinalIgnoreCase)); requesterId = (string)active?["requesterId"] ?? ""; } catch { } } history.Insert(0, new JObject { ["replayId"] = replayId, ["title"] = Arg("historyReplayTitle"), ["creator"] = Arg("historyReplayCreator"), ["count"] = count, ["lastPlayedBy"] = Arg("historyReplayRequester"), ["lastPlayedPlatform"] = Arg("historyReplayPlatform"), ["lastPlayedUserId"] = requesterId, ["lastPlayed"] = DateTime.Now.ToString("o") }); while (history.Count > MaxAmount()) history.RemoveAt(history.Count - 1); data["playHistory"] = history; Save(data); return true; }
    public bool RateReplay()
    {
        var input = Arg("rawInput").Trim();
        if (!int.TryParse(input, out var rating) || rating < 1 || rating > 5) { SendCatalogMessage("Please provide a rating from 1 to 5 for the currently playing replay."); return false; }
        var userId = Arg("userId");
        if (string.IsNullOrWhiteSpace(userId)) { SendCatalogMessage("A user account is required to rate a replay."); return false; }
        var replayId = CPH.GetGlobalVar<string>(ActiveReplayKey, false);
        if (string.IsNullOrWhiteSpace(replayId)) { SendCatalogMessage("There is no replay currently playing."); return false; }
        var data = Load();
        var catalog = data["catalog"] as JArray ?? new JArray();
        var replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], replayId, StringComparison.OrdinalIgnoreCase));
        if (replay == null) { SendCatalogMessage("The currently playing replay is no longer in the Catalog."); return false; }

        var ratings = replay["ratings"] as JObject ?? new JObject();
        var requesterPlatform = CurrentPlatform();
        var identityKey = IdentityKey(requesterPlatform, userId);
        var oldRating = ratings[identityKey] == null ? 0d : ratings[identityKey].Value<double>();
        ratings[identityKey] = rating;
        replay["ratings"] = ratings;
        Save(data);
        var averageRating = Math.Round(ratings.Properties().Select(x => x.Value.Value<double>()).DefaultIfEmpty().Average(), 1);

        var creator = replay["creator"] as JObject;
        CPH.SetArgument("messageEvent", "Replay Rated");
        CPH.SetArgument("replayId", replayId);
        CPH.SetArgument("replayNumber", "");
        CPH.SetArgument("replayTitle", (string)replay["title"] ?? "Replay");
        CPH.SetArgument("replayRating", rating);
        CPH.SetArgument("oldRating", oldRating);
        CPH.SetArgument("averageRating", averageRating);
        CPH.SetArgument("replayUserId", (string)creator?["id"] ?? "");
        CPH.SetArgument("replayUser", (string)creator?["name"] ?? "");
        CPH.SetArgument("replayPlatform", (string)creator?["platform"] ?? "");
        CPH.SetArgument("replaySourcePlatform", (string)replay["sourceType"] ?? "OBS");
        CPH.SetArgument("requesterId", userId);
        CPH.SetArgument("requesterName", Arg("userName"));
        CPH.SetArgument("requesterPlatform", requesterPlatform);
        CPH.SetArgument("requesterBroadcastId", Arg("broadcast.id"));
        CPH.ExecuteMethod("RTS - Action Replay - Core - Messaging", "Enqueue");
        return true;
    }

    private string ResolveAvatarUrl(string platform, string userId, string userName)
    {
        if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(userName)) return "";
        var cacheKey = NormalizePlatform(platform) + ":" + (string.IsNullOrWhiteSpace(userId) ? userName : userId);
        lock (AvatarCacheLock)
        {
            if (AvatarCache.TryGetValue(cacheKey, out var cached) && !string.IsNullOrWhiteSpace(cached)) return cached;
        }
        var avatar = "";
        try
        {
            if (platform == "Twitch" && !string.IsNullOrWhiteSpace(userId)) avatar = CPH.TwitchGetExtendedUserInfoById(userId)?.ProfileImageUrl ?? "";
            if (platform == "Kick" && !string.IsNullOrWhiteSpace(userName))
            {
                var json = DownloadString("https://kick.com/api/v2/channels/" + Uri.EscapeDataString(userName));
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var item = JObject.Parse(json);
                    avatar = (string)item["user"]?["profile_pic"] ?? (string)item["profile_pic"] ?? "";
                }
            }
        }
        catch (Exception ex) { CPH.LogWarn("RTS Action Replay: avatar lookup failed: " + ex.Message); }
        if (!string.IsNullOrWhiteSpace(avatar))
        {
            lock (AvatarCacheLock) AvatarCache[cacheKey] = avatar;
        }
        return avatar;
    }
    private string DownloadString(string url) { try { using (var client = new WebClient()) { client.Headers[HttpRequestHeader.Accept] = "application/json"; client.Headers[HttpRequestHeader.UserAgent] = "RTS-Action-Replay"; return client.DownloadString(url); } } catch (Exception ex) { CPH.LogWarn("RTS Action Replay: avatar request failed: " + ex.Message); return ""; } }
    private bool ShowUserSearchPage(int delta) { if (!TryParseUserTarget(Arg("rawInput"), out var platform, out var userName)) return false; var userId = ResolveUserId(platform, userName); var state = string.IsNullOrWhiteSpace(userId) ? null : LoadUserSearchState(platform, userId, userName); var label = platform.ToLowerInvariant() + ":" + userName; if (state == null) { SendCatalogMessage(label + " has not made any searches"); return false; } if (delta != 0) { var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); var pages = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount)); state["page"] = Math.Max(1, Math.Min(pages, ((int?)state["page"] ?? 1) + delta)); SaveUserSearchState(platform, userId, state); } return Queue(state); }
    private bool ResolveStateSelection(JObject state, int index) { var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); var page = Math.Max(1, (int?)state["page"] ?? 1); var position = ((page - 1) * amount) + index - 1; if (position < 0 || position >= results.Count) return false; CPH.SetArgument("catalogSelectionReplayId", (string)results[position]["replayId"] ?? (string)results[position]["id"] ?? ""); return !string.IsNullOrWhiteSpace((string)results[position]["replayId"] ?? (string)results[position]["id"]); }
    private JObject BuildState(string type, string value, string sort, int amount = 0) { var platform = CurrentPlatform(); var userId = Arg("userId"); var state = new JObject { ["filterType"] = type, ["filter"] = value ?? "", ["sort"] = sort, ["amount"] = amount > 0 ? amount : MaxAmount(), ["page"] = 1, ["requesterId"] = userId, ["requesterName"] = Arg("userName"), ["platform"] = platform, ["userId"] = userId, ["identityKey"] = IdentityKey(platform, userId) }; SaveUserState(state); return state; }
    private bool MovePage(int delta) { var state = LoadUserState(); var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); var pages = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount)); state["page"] = Math.Max(1, Math.Min(pages, ((int?)state["page"] ?? 1) + delta)); SaveUserState(state); return Queue(state); }
    private bool SetPage(int page) { var state = LoadUserState(); var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); var pages = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount)); state["page"] = Math.Max(1, Math.Min(pages, page)); SaveUserState(state); return Queue(state); }
    private bool Queue(JObject state) { CPH.SetArgument("replaySearchRequest", state.ToString(Newtonsoft.Json.Formatting.None)); return CPH.ExecuteMethod(SearchQueueAction, "Enqueue"); }
    private JArray Query(JObject state) { var data = Load(); var list = (JArray)data["catalog"] ?? new JArray(); var type = ((string)state["filterType"] ?? "all").ToLowerInvariant(); var filter = ((string)state["filter"] ?? "").Trim(); IEnumerable<JObject> query = list.OfType<JObject>(); if (type == "lastplayed") query = ((JArray)data["playHistory"] ?? new JArray()).OfType<JObject>(); else if (type == "search") query = query.Where(x => SearchMatch(x, filter)); else if (type == "date") query = query.Where(x => DateMatch(x, filter)); else if (type == "creator") query = query.Where(x => CreatorMatch(x, filter)); if (type == "recent") query = query.OrderByDescending(x => ParseDate((string)x["captured"] ?? (string)x["added"])).Take(Math.Max(1, CPH.GetGlobalVar<int?>(MaxHistoryKey, true) ?? 20)); if (type == "leaderboard") query = BuildLeaderboard(query, filter); var sort = ((string)state["sort"] ?? "catalog").ToLowerInvariant(); if (type != "lastplayed" && sort == "plays") query = query.Where(x => (int?)x["plays"] > 0).OrderByDescending(x => (int?)x["plays"] ?? 0); if (type != "lastplayed" && sort == "rating") { if (int.TryParse(filter, out var ratingBand) && ratingBand >= 0 && ratingBand <= 5) { if (ratingBand == 0) query = query.Where(x => !HasRatings(x)); else query = query.Where(x => HasRatings(x) && Rating(x) >= ratingBand && Rating(x) < (ratingBand == 5 ? 6 : ratingBand + 1)); } else { query = query.Where(HasRatings); } query = query.OrderByDescending(Rating).ThenByDescending(RatingCount); } return new JArray(query); }
    private IEnumerable<JObject> BuildLeaderboard(IEnumerable<JObject> source, string period) { var now = DateTime.Now; var key = NormalizePeriod(period); if (!string.IsNullOrWhiteSpace(key) && key != "all" && key != "today" && key != "week" && key != "month" && key != "year" && !TryParseMonthYear(period, out _, out _)) return new List<JObject>(); int? year = null; int? month = null; if (TryParseMonthYear(period, out var requestedYear, out var requestedMonth)) { year = requestedYear; month = requestedMonth; } var filtered = source.Where(x => { var date = ParseDate((string)x["captured"] ?? (string)x["added"]); if (date == DateTime.MinValue) return false; if (year.HasValue) return date.Year == year.Value && date.Month == month.Value; if (key == "today") return date.Date == now.Date; if (key == "week") { var start = StartOfWeek(now); return date >= start && date < start.AddDays(7); } if (key == "month") return date.Year == now.Year && date.Month == now.Month; if (key == "year") return date.Year == now.Year; return true; }); return filtered.Select(x => x["creator"] as JObject).Where(x => x != null && !string.IsNullOrWhiteSpace((string)x["id"])).GroupBy(x => IdentityKey((string)x["platform"], (string)x["id"]), StringComparer.OrdinalIgnoreCase).Select(g => new JObject { ["id"] = g.Key, ["creator"] = (string)g.First()["name"] ?? g.Key, ["count"] = g.Count() }).OrderByDescending(x => (int)x["count"]).ThenBy(x => (string)x["creator"], StringComparer.OrdinalIgnoreCase); }
    private string NormalizePeriod(string period) => (period ?? "").ToLowerInvariant().Replace("_", "-").Replace(" ", "-").Trim();
    private bool TryParseMonthYear(string value, out int year, out int month) { year = 0; month = 0; if (string.IsNullOrWhiteSpace(value)) return false; var normalized = value.Trim().Replace("/", " ").Replace(".", " ").Replace("-", " "); if (DateTime.TryParseExact(normalized, new[] { "MMMM yyyy", "MMM yyyy", "MM yyyy", "M yyyy" }, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var date) || DateTime.TryParse("1 " + normalized, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out date)) { year = date.Year; month = date.Month; return true; } return false; }
    private bool SearchMatch(JObject x, string term) { if (string.IsNullOrWhiteSpace(term)) return true; var creator = x["creator"] as JObject; return Contains(x["title"], term) || Contains(x["file"], term) || Contains(creator?["name"], term); }
    private bool CreatorMatch(JObject x, string name) { var creator = x["creator"] as JObject; return string.Equals((string)creator?["name"], name, StringComparison.OrdinalIgnoreCase) || string.Equals((string)creator?["id"], name, StringComparison.OrdinalIgnoreCase); }
    private bool DateMatch(JObject x, string period) { var raw = (string)x["captured"] ?? (string)x["added"]; if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var captured)) return false; var date = captured.ToLocalTime().Date; var now = DateTime.Now; var key = period.ToLowerInvariant().Replace("_", "-").Replace(" ", "-").Trim(); if (key == "today") return date == now.Date; if (key == "yesterday") return date == now.Date.AddDays(-1); var week = StartOfWeek(now); if (key == "this-week") return date >= week && date < week.AddDays(7); if (key == "last-week") return date >= week.AddDays(-7) && date < week; if (key == "this-month") return date.Year == now.Year && date.Month == now.Month; var month = new DateTime(now.Year, now.Month, 1).AddMonths(-1); if (key == "last-month") return date.Year == month.Year && date.Month == month.Month; if (key == "this-year") return date.Year == now.Year; if (key == "last-year") return date.Year == now.Year - 1; if (DateTime.TryParse("1 " + period, CultureInfo.CurrentCulture, DateTimeStyles.None, out var requestedMonth)) return date.Year == requestedMonth.Year && date.Month == requestedMonth.Month; return false; }
    private DateTime StartOfWeek(DateTime date) => date.Date.AddDays(-(7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7);
    private DateTime ParseDate(string value) => DateTime.TryParse(value, out var date) ? date : DateTime.MinValue;
    private bool Contains(JToken token, string term) => (token?.ToString() ?? "").IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    private JObject Ratings(JObject x) => x["ratings"] as JObject ?? new JObject(); private bool HasRatings(JObject x) => Ratings(x).Properties().Any(); private double Rating(JObject x) => Ratings(x).Values().Select(v => (double)v.Value<int>()).DefaultIfEmpty().Average(); private int RatingCount(JObject x) => Ratings(x).Properties().Count();
    private JObject Entry(JObject x, int number) { var creator = x["creator"] as JObject; var ratings = Ratings(x); var entry = new JObject { ["number"] = number, ["title"] = (string)x["title"] ?? "Untitled replay", ["creator"] = (string)creator?["name"] ?? (string)x["creator"] ?? "", ["creatorId"] = (string)creator?["id"] ?? "", ["creatorPlatform"] = (string)creator?["platform"] ?? "", ["plays"] = (int?)x["plays"] ?? 0, ["rating"] = ratings.Properties().Any() ? Math.Round(Rating(x), 1) : 0, ["ratingCount"] = ratings.Properties().Count() }; if (x["count"] != null) { entry["historyCount"] = (int?)x["count"] ?? 1; entry["lastPlayedBy"] = (string)x["lastPlayedBy"] ?? ""; entry["lastPlayedPlatform"] = (string)x["lastPlayedPlatform"] ?? ""; entry["lastPlayedUserId"] = (string)x["lastPlayedUserId"] ?? ""; } return entry; }
    private string Header(JObject request, int page, int pages, int total) { if (((string)request["filterType"] ?? "").Equals("leaderboard", StringComparison.OrdinalIgnoreCase)) return "CLIP CREATORS • " + LeaderboardPeriodLabel((string)request["filter"]) + " • " + page + "/" + pages + " • " + total; return $"{Parameters(request)} • {page}/{pages} • {total}"; }
    private string LeaderboardPeriodLabel(string period) { var key = NormalizePeriod(period); if (string.IsNullOrWhiteSpace(key) || key == "all") return "ALL TIME"; if (key == "today") return "TODAY"; if (key == "week") return "THIS WEEK"; if (key == "month") return "THIS MONTH"; if (key == "year") return "THIS YEAR"; if (TryParseMonthYear(period, out var year, out var month)) return new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.CurrentCulture).ToUpperInvariant(); return period.ToUpperInvariant(); }
    private string Parameters(JObject request) { var type = ((string)request["filterType"] ?? "all").ToLowerInvariant(); if (type == "recent") return "RECENT"; if (type == "lastplayed") return "LAST PLAYED"; if (type == "search") return "SEARCH: " + ((string)request["filter"] ?? ""); if (type == "date") return "DATE: " + ((string)request["filter"] ?? ""); if (type == "creator") return "CREATOR: " + ((string)request["filter"] ?? ""); var sort = ((string)request["sort"] ?? "catalog").ToLowerInvariant(); return sort == "plays" ? "MOST VIEWS" : sort == "rating" ? "TOP RATED" : "CATALOG"; }
    private JObject LoadUserState() { var userId = Arg("userId"); var userName = Arg("userName"); var platform = CurrentPlatform(); if (string.IsNullOrWhiteSpace(userId)) return DefaultState(userName, platform, userId); var raw = GetUserVar(platform, userId, UserStateKey); try { var state = string.IsNullOrWhiteSpace(raw) ? DefaultState(userName, platform, userId) : JObject.Parse(raw); var changed = false; if (state["platform"] == null) { state["platform"] = "Twitch"; changed = true; } if (state["userId"] == null) { state["userId"] = userId; changed = true; } if (state["identityKey"] == null) { state["identityKey"] = IdentityKey((string)state["platform"], (string)state["userId"]); changed = true; } if (state["requesterId"] == null) { state["requesterId"] = userId; changed = true; } if (changed) SetUserVar(platform, userId, UserStateKey, state.ToString(Newtonsoft.Json.Formatting.None)); return state; } catch { return DefaultState(userName, platform, userId); } }
    private JObject LoadUserSearchState(string platform, string userId, string userName) { if (string.IsNullOrWhiteSpace(userId)) return null; var raw = GetUserVar(platform, userId, UserStateKey); if (string.IsNullOrWhiteSpace(raw)) return null; try { return JObject.Parse(raw); } catch { return null; } }
    private void SaveUserSearchState(string platform, string userId, JObject state) { if (!string.IsNullOrWhiteSpace(userId)) SetUserVar(platform, userId, UserStateKey, state.ToString(Newtonsoft.Json.Formatting.None)); }
    private string ResolveUserId(string platform, string userName) { if (string.IsNullOrWhiteSpace(userName)) return null; var catalog = (JArray)Load()["catalog"] ?? new JArray(); var creator = catalog.OfType<JObject>().Select(x => x["creator"] as JObject).FirstOrDefault(x => x != null && string.Equals((string)x["platform"], platform, StringComparison.OrdinalIgnoreCase) && (string.Equals((string)x["name"], userName, StringComparison.OrdinalIgnoreCase) || string.Equals((string)x["id"], userName, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace((string)x["id"])); return (string)creator?["id"]; }
    private bool TryParseUserTarget(string raw, out string platform, out string userName) { platform = CurrentPlatform(); userName = (raw ?? "").Trim(); if (string.IsNullOrWhiteSpace(userName)) return false; var separator = userName.IndexOf(':'); if (separator > 0) { var requestedPlatform = userName.Substring(0, separator); var requestedUser = userName.Substring(separator + 1).Trim(); if (requestedPlatform.Equals("twitch", StringComparison.OrdinalIgnoreCase) || requestedPlatform.Equals("kick", StringComparison.OrdinalIgnoreCase) || requestedPlatform.Equals("youtube", StringComparison.OrdinalIgnoreCase)) { platform = NormalizePlatform(requestedPlatform); userName = requestedUser; } } return !string.IsNullOrWhiteSpace(userName); }
    private JObject DefaultState(string userName, string platform, string userId) => new JObject { ["filterType"] = "all", ["filter"] = "", ["sort"] = "catalog", ["amount"] = MaxAmount(), ["page"] = 1, ["requesterName"] = userName, ["requesterId"] = userId, ["platform"] = platform, ["userId"] = userId, ["identityKey"] = IdentityKey(platform, userId) };
    private void SaveUserState(JObject state) { var userId = Arg("userId"); if (!string.IsNullOrWhiteSpace(userId)) SetUserVar(CurrentPlatform(), userId, UserStateKey, state.ToString(Newtonsoft.Json.Formatting.None)); }
    private JObject Load() { var raw = CPH.GetGlobalVar<string>(DataKey, true); try { return string.IsNullOrWhiteSpace(raw) ? CreateDataDefaults() : JObject.Parse(raw); } catch { return CreateDataDefaults(); } }
    private JObject CreateDataDefaults() { return new JObject { ["version"] = "1.0", ["catalog"] = new JArray(), ["playHistory"] = new JArray() }; }
    private void Save(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
    private string CurrentPlatform() => NormalizePlatform(Arg("userType"));
    private string NormalizePlatform(string userType) { if (string.Equals(userType, "YouTube", StringComparison.OrdinalIgnoreCase)) return "YouTube"; if (string.Equals(userType, "Kick", StringComparison.OrdinalIgnoreCase)) return "Kick"; return "Twitch"; }
    private string IdentityKey(string platform, string userId) => NormalizePlatform(platform).ToLowerInvariant() + ":" + (userId ?? "").Trim();
    private string GetUserVar(string platform, string userId, string key) { if (platform == "YouTube") return CPH.GetYouTubeUserVarById<string>(userId, key, true); if (platform == "Kick") return CPH.GetKickUserVarById<string>(userId, key, true); return CPH.GetTwitchUserVarById<string>(userId, key, true); }
    private void SetUserVar(string platform, string userId, string key, string value) { if (platform == "YouTube") CPH.SetYouTubeUserVarById(userId, key, value, true); else if (platform == "Kick") CPH.SetKickUserVarById(userId, key, value, true); else CPH.SetTwitchUserVarById(userId, key, value, true); }
    private void SendCatalogMessage(string text) { if (string.IsNullOrWhiteSpace(text)) return; var platform = Arg("requesterPlatform"); if (string.IsNullOrWhiteSpace(platform)) platform = Arg("userType"); if (string.Equals(platform, "Kick", StringComparison.OrdinalIgnoreCase)) { CPH.SendKickMessage(text); return; } if (string.Equals(platform, "YouTube", StringComparison.OrdinalIgnoreCase)) { var broadcastId = Arg("requesterBroadcastId"); if (!string.IsNullOrWhiteSpace(broadcastId)) { CPH.SendYouTubeMessage(text, true, true, broadcastId); return; } CPH.SendYouTubeMessageToLatestMonitored(text); return; } if (string.Equals(platform, "Twitch", StringComparison.OrdinalIgnoreCase)) { CPH.SendMessage(text); return; } CPH.LogWarn("RTS Action Replay: unable to route catalog chat response because the originating platform is unknown."); }
    private string Arg(string name) { CPH.TryGetArg(name, out string value); return value ?? ""; }
    private int MaxAmount() => Math.Max(1, CPH.GetGlobalVar<int?>(MaxHistoryKey, true) ?? 20);
    private int ParseAmount(string raw) { var parts = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); return ParseAmount(ref parts); }
    private int ParseAmount(ref string[] parts) { for (var i = 0; i < parts.Length - 1; i++) if (string.Equals(parts[i], "--amount", StringComparison.OrdinalIgnoreCase) && int.TryParse(parts[i + 1], out var amount) && amount > 0) { var list = parts.ToList(); list.RemoveRange(i, 2); parts = list.ToArray(); return amount; } return 0; }
}
