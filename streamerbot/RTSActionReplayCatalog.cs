using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string MaxHistoryKey = "rts.actionreplay.maxHistory";
    private const string UserStateKey = "rts.actionreplay.catalogState";
    private const string SearchQueueAction = "RTS - Action Replay - Core - Search Queue";
    private const string PanelAnimationAction = "RTS - Action Replay - Core - Animation";
    private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";

    public bool Execute() => ListCatalog();
    public bool ListCatalog() { var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); var amount = ParseAmount(ref parts); var search = string.Join(" ", parts).Trim(); return Queue(BuildState(string.IsNullOrWhiteSpace(search) ? "all" : "search", search, "catalog", amount)); }
    public bool ListRecent() => Queue(BuildState("recent", "", "recent", ParseAmount(Arg("rawInput"))));
    public bool ListLeaderboard() { var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); var amount = ParseAmount(ref parts); var period = string.Join(" ", parts).Trim(); return Queue(BuildState("leaderboard", period, "leaderboard", amount > 0 ? amount : 5)); }
    public bool ListCatalogDate() { var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); var amount = ParseAmount(ref parts); var period = string.Join(" ", parts).Trim(); if (string.IsNullOrWhiteSpace(period)) { SendCatalogMessage("Please provide a catalog date period."); return false; } return Queue(BuildState("date", period, "catalog", amount)); }
    public bool ListCatalogCreator() { var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); var amount = ParseAmount(ref parts); var creator = string.Join(" ", parts).Trim(); if (string.IsNullOrWhiteSpace(creator)) { SendCatalogMessage("Please provide a creator name."); return false; } return Queue(BuildState("creator", creator, "catalog", amount)); }
    public bool ListCatalogMostViews() => Queue(BuildState("all", "", "plays", ParseAmount(Arg("rawInput"))));
    public bool ListCatalogTopRated() => Queue(BuildState("all", "", "rating", ParseAmount(Arg("rawInput"))));
    public bool CatalogNext() => MovePage(1);
    public bool CatalogPrevious() => MovePage(-1);
    public bool CatalogFirst() => SetPage(1);
    public bool CatalogLast() { var state = LoadUserState(); var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); state["page"] = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount)); SaveUserState(state); return Queue(state); }
    public bool ShowUserSearch() => ShowUserSearchPage(0);
    public bool UserSearchNext() => ShowUserSearchPage(1);
    public bool UserSearchPrevious() => ShowUserSearchPage(-1);
    public bool ResolveSelection()
    {
        var selector = Arg("rawInput").Trim(); if (!int.TryParse(selector, out var index) || index < 1) return false;
        var state = LoadUserState(); if (string.Equals((string)state["filterType"], "leaderboard", StringComparison.OrdinalIgnoreCase)) return false;
        return ResolveStateSelection(state, index);
    }
    public bool ResolveSelectionForUser()
    {
        var selector = Arg("rawInput").Trim();
        var parts = selector.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var indexText = parts.Length == 1 ? parts[0] : parts.Length == 2 ? parts[1] : "";
        if (!int.TryParse(indexText, out var index) || index < 1) return false;
        var platform = Arg("catalogSelectionPlatform"); var userName = Arg("catalogSelectionUser"); if (!TryParseUserTarget(platform + ":" + userName, out platform, out userName)) return false;
        var userId = ResolveUserId(platform, userName); if (string.IsNullOrWhiteSpace(userId)) return false;
        var state = LoadUserSearchState(platform, userId, userName); if (state == null || string.Equals((string)state["filterType"], "leaderboard", StringComparison.OrdinalIgnoreCase)) return false;
        return ResolveStateSelection(state, index);
    }
    public bool RenderSearchRequest()
    {
        var json = Arg("replaySearchRequest"); if (string.IsNullOrWhiteSpace(json)) return false; JObject request; try { request = JObject.Parse(json); } catch { return false; }
        var isLeaderboard = string.Equals((string)request["filterType"], "leaderboard", StringComparison.OrdinalIgnoreCase); var results = Query(request); var amount = Math.Max(1, (int?)request["amount"] ?? MaxAmount()); var page = Math.Max(1, (int?)request["page"] ?? 1); var pages = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount)); page = Math.Min(page, pages); var start = (page - 1) * amount;
        if (isLeaderboard) { var entries = results.Skip(start).Take(amount).OfType<JObject>().Select((x, i) => new JObject { ["rank"] = start + i + 1, ["creator"] = (string)x["creator"] ?? "Unknown creator", ["count"] = (int?)x["count"] ?? 0 }).ToList(); CPH.SetArgument("replayLeaderboardEntries", new JArray(entries).ToString(Newtonsoft.Json.Formatting.None)); CPH.SetArgument("replayLeaderboardPeriod", LeaderboardPeriodLabel((string)request["filter"] ?? "")); }
        else { var entries = results.Skip(start).Take(amount).OfType<JObject>().Select((x, i) => Entry(x, start + i + 1)).ToList(); CPH.SetArgument("replaySearchEntries", new JArray(entries).ToString(Newtonsoft.Json.Formatting.None)); }
        CPH.SetArgument("replaySearchHeader", Header(request, page, pages, results.Count)); CPH.SetArgument("replaySearchRequester", (string)request["requesterName"] ?? ""); CPH.SetArgument("replaySearchRequesterPlatform", (string)request["platform"] ?? ""); CPH.SetArgument("replaySearchParameters", Parameters(request)); CPH.SetArgument("replaySearchRequestId", (string)request["requestId"] ?? ""); CPH.SetArgument("replaySearchDuration", CPH.GetGlobalVar<int?>("rts.actionreplay.searchPanel.duration", true) ?? 10000); CPH.SetArgument("replayPanelWidth", CPH.GetGlobalVar<int?>("rts.actionreplay.panel.width", true) ?? 500); CPH.SetArgument("replayPanelHeight", CPH.GetGlobalVar<int?>("rts.actionreplay.panel.height", true) ?? 700); CPH.SetArgument("panelType", isLeaderboard ? "creatorLeaderboard" : "recent"); CPH.ExecuteMethod(PanelAnimationAction, "ResolvePanelAnimation"); CPH.SetArgument("replayCommand", isLeaderboard ? "leaderboard-panel" : "search-panel"); CPH.TriggerEvent("RTS-Action Replay", true); return true;
    }
    public bool RateReplay()
    {
        var input = Arg("rawInput").Trim();
        if (!int.TryParse(input, out var rating) || rating < 1 || rating > 5)
        {
            SendCatalogMessage("Please provide a rating from 1 to 5 for the currently playing replay.");
            return false;
        }
        var userId = Arg("userId");
        if (string.IsNullOrWhiteSpace(userId)) { SendCatalogMessage("A user account is required to rate a replay."); return false; }
        CPH.SetArgument("catalogActiveReplayId", "");
        if (!CPH.ExecuteMethod(PlaylistAction, "ResolveActiveReplay"))
        {
            SendCatalogMessage("There is no replay currently playing.");
            return false;
        }
        var replayId = Arg("catalogActiveReplayId");
        if (string.IsNullOrWhiteSpace(replayId)) { SendCatalogMessage("There is no replay currently playing."); return false; }
        var data = Load(); var catalog = data["catalog"] as JArray ?? new JArray(); var replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], replayId, StringComparison.OrdinalIgnoreCase));
        if (replay == null) { SendCatalogMessage("The currently playing replay is no longer in the Catalog."); return false; }
        var ratings = replay["ratings"] as JObject ?? new JObject(); ratings[IdentityKey(CurrentPlatform(), userId)] = rating; replay["ratings"] = ratings; Save(data); SendCatalogMessage($"Rated {(string)replay["title"] ?? "Replay"} {rating}/5."); return true;
    }
    private bool ShowUserSearchPage(int delta)
    {
        if (!TryParseUserTarget(Arg("rawInput"), out var platform, out var userName)) return false;
        var userId = ResolveUserId(platform, userName); var state = string.IsNullOrWhiteSpace(userId) ? null : LoadUserSearchState(platform, userId, userName);
        var label = platform.ToLowerInvariant() + ":" + userName;
        if (state == null) { SendCatalogMessage(label + " has not made any searches"); return false; }
        if (delta != 0)
        {
            var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); var pages = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount));
            state["page"] = Math.Max(1, Math.Min(pages, ((int?)state["page"] ?? 1) + delta));
            SaveUserSearchState(platform, userId, state);
        }
        return Queue(state);
    }
    private bool ResolveStateSelection(JObject state, int index)
    {
        var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); var page = Math.Max(1, (int?)state["page"] ?? 1); var position = ((page - 1) * amount) + index - 1;
        if (position < 0 || position >= results.Count) return false;
        CPH.SetArgument("catalogSelectionReplayId", (string)results[position]["id"] ?? ""); return !string.IsNullOrWhiteSpace((string)results[position]["id"]);
    }
    private JObject BuildState(string type, string value, string sort, int amount = 0) { var platform = CurrentPlatform(); var userId = Arg("userId"); var state = new JObject { ["filterType"] = type, ["filter"] = value ?? "", ["sort"] = sort, ["amount"] = amount > 0 ? amount : MaxAmount(), ["page"] = 1, ["requesterId"] = userId, ["requesterName"] = Arg("userName"), ["platform"] = platform, ["userId"] = userId, ["identityKey"] = IdentityKey(platform, userId) }; SaveUserState(state); return state; }
    private bool MovePage(int delta) { var state = LoadUserState(); var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); var pages = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount)); state["page"] = Math.Max(1, Math.Min(pages, ((int?)state["page"] ?? 1) + delta)); SaveUserState(state); return Queue(state); }
    private bool SetPage(int page) { var state = LoadUserState(); var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); var pages = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount)); state["page"] = Math.Max(1, Math.Min(pages, page)); SaveUserState(state); return Queue(state); }
    private bool Queue(JObject state) { CPH.SetArgument("replaySearchRequest", state.ToString(Newtonsoft.Json.Formatting.None)); return CPH.ExecuteMethod(SearchQueueAction, "Enqueue"); }
    private JArray Query(JObject state) { var list = (JArray)Load()["catalog"] ?? new JArray(); var type = ((string)state["filterType"] ?? "all").ToLowerInvariant(); var filter = ((string)state["filter"] ?? "").Trim(); IEnumerable<JObject> query = list.OfType<JObject>(); if (type == "search") query = query.Where(x => SearchMatch(x, filter)); else if (type == "date") query = query.Where(x => DateMatch(x, filter)); else if (type == "creator") query = query.Where(x => CreatorMatch(x, filter)); if (type == "recent") query = query.OrderByDescending(x => ParseDate((string)x["captured"] ?? (string)x["added"])).Take(Math.Max(1, CPH.GetGlobalVar<int?>(MaxHistoryKey, true) ?? 20)); if (type == "leaderboard") query = BuildLeaderboard(query, filter); var sort = ((string)state["sort"] ?? "catalog").ToLowerInvariant(); if (sort == "plays") query = query.OrderByDescending(x => (int?)x["plays"] ?? 0); if (sort == "rating") query = query.Where(HasRatings).OrderByDescending(Rating).ThenByDescending(RatingCount); return new JArray(query); }
    private IEnumerable<JObject> BuildLeaderboard(IEnumerable<JObject> source, string period) { var now = DateTime.Now; var key = NormalizePeriod(period); if (!string.IsNullOrWhiteSpace(key) && key != "all" && key != "today" && key != "week" && key != "month" && key != "year" && !TryParseMonthYear(period, out _, out _)) return new List<JObject>(); int? year = null; int? month = null; if (TryParseMonthYear(period, out var requestedYear, out var requestedMonth)) { year = requestedYear; month = requestedMonth; } var filtered = source.Where(x => { var date = ParseDate((string)x["captured"] ?? (string)x["added"]); if (date == DateTime.MinValue) return false; if (year.HasValue) return date.Year == year.Value && date.Month == month.Value; if (key == "today") return date.Date == now.Date; if (key == "week") { var start = StartOfWeek(now); return date >= start && date < start.AddDays(7); } if (key == "month") return date.Year == now.Year && date.Month == now.Month; if (key == "year") return date.Year == now.Year; return true; }); return filtered.Select(x => x["creator"] as JObject).Where(x => x != null && !string.IsNullOrWhiteSpace((string)x["id"])).GroupBy(x => IdentityKey((string)x["platform"], (string)x["id"]), StringComparer.OrdinalIgnoreCase).Select(g => new JObject { ["id"] = g.Key, ["creator"] = (string)g.First()["name"] ?? g.Key, ["count"] = g.Count() }).OrderByDescending(x => (int)x["count"]).ThenBy(x => (string)x["creator"], StringComparer.OrdinalIgnoreCase); }
    private string NormalizePeriod(string period) => (period ?? "").ToLowerInvariant().Replace("_", "-").Replace(" ", "-").Trim();
    private bool TryParseMonthYear(string value, out int year, out int month) { year = 0; month = 0; if (string.IsNullOrWhiteSpace(value)) return false; var normalized = value.Trim().Replace("/", " ").Replace(".", " ").Replace("-", " "); if (DateTime.TryParseExact(normalized, new[] { "MMMM yyyy", "MMM yyyy", "MM yyyy", "M yyyy" }, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var date) || DateTime.TryParse("1 " + normalized, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out date)) { year = date.Year; month = date.Month; return true; } return false; }
    private bool SearchMatch(JObject x, string term) { if (string.IsNullOrWhiteSpace(term)) return true; var creator = x["creator"] as JObject; return Contains(x["title"], term) || Contains(x["file"], term) || Contains(creator?["name"], term); }
    private bool CreatorMatch(JObject x, string name) { var creator = x["creator"] as JObject; return string.Equals((string)creator?["name"], name, StringComparison.OrdinalIgnoreCase) || string.Equals((string)creator?["id"], name, StringComparison.OrdinalIgnoreCase); }
