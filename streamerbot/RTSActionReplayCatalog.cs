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
        CPH.SetArgument("replaySearchHeader", Header(request, page, pages, results.Count)); CPH.SetArgument("replaySearchRequester", (string)request["requesterName"] ?? ""); CPH.SetArgument("replaySearchParameters", Parameters(request)); CPH.SetArgument("replaySearchRequestId", (string)request["requestId"] ?? ""); CPH.SetArgument("replaySearchDuration", CPH.GetGlobalVar<int?>("rts.actionreplay.searchPanel.duration", true) ?? 10000); CPH.SetArgument("replayPanelWidth", CPH.GetGlobalVar<int?>("rts.actionreplay.panel.width", true) ?? 500); CPH.SetArgument("replayPanelHeight", CPH.GetGlobalVar<int?>("rts.actionreplay.panel.height", true) ?? 700); CPH.SetArgument("panelType", isLeaderboard ? "creatorLeaderboard" : "recent"); CPH.ExecuteMethod(PanelAnimationAction, "ResolvePanelAnimation"); CPH.SetArgument("replayCommand", isLeaderboard ? "leaderboard-panel" : "search-panel"); CPH.TriggerEvent("RTS-Action Replay", true); return true;
    }
    public bool RateReplay() { var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); if (parts.Length < 2 || !int.TryParse(parts[0], out var index) || index < 1 || !int.TryParse(parts[1], out var rating) || rating < 1 || rating > 5) { SendCatalogMessage("Usage: !rate-replay <catalog number> <1-5>"); return false; } var state = LoadUserState(); var results = Query(state); if (index > results.Count) { SendCatalogMessage("That catalog entry does not exist."); return false; } var userId = Arg("userId"); if (string.IsNullOrWhiteSpace(userId)) { SendCatalogMessage("A user account is required to rate a replay."); return false; } var data = Load(); var replayId = results[index - 1]?["id"]?.ToString(); var catalog = data["catalog"] as JArray ?? new JArray(); var replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], replayId, StringComparison.OrdinalIgnoreCase)); if (replay == null) { SendCatalogMessage("That replay is no longer in the Catalog."); return false; } var ratings = replay["ratings"] as JObject ?? new JObject(); ratings[IdentityKey(CurrentPlatform(), userId)] = rating; replay["ratings"] = ratings; Save(data); SendCatalogMessage($"Rated {(string)replay["title"] ?? "Replay"} {rating}/5."); return true; }
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
    private bool DateMatch(JObject x, string period) { var raw = (string)x["captured"] ?? (string)x["added"]; if (!DateTime.TryParse(raw, out var date)) return false; var now = DateTime.Now; var key = period.ToLowerInvariant().Replace("_", "-").Replace(" ", "-").Trim(); if (key == "today") return date.Date == now.Date; if (key == "yesterday") return date.Date == now.Date.AddDays(-1); var week = StartOfWeek(now); if (key == "this-week") return date >= week && date < week.AddDays(7); if (key == "last-week") return date >= week.AddDays(-7) && date < week; if (key == "this-month") return date.Year == now.Year && date.Month == now.Month; var month = new DateTime(now.Year, now.Month, 1).AddMonths(-1); if (key == "last-month") return date.Year == month.Year && date.Month == month.Month; if (key == "this-year") return date.Year == now.Year; if (key == "last-year") return date.Year == now.Year - 1; if (DateTime.TryParse("1 " + period, CultureInfo.CurrentCulture, DateTimeStyles.None, out var requestedMonth)) return date.Year == requestedMonth.Year && date.Month == requestedMonth.Month; return false; }
    private DateTime StartOfWeek(DateTime date) => date.Date.AddDays(-(7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7);
    private DateTime ParseDate(string value) => DateTime.TryParse(value, out var date) ? date : DateTime.MinValue;
    private bool Contains(JToken token, string term) => (token?.ToString() ?? "").IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    private JObject Ratings(JObject x) => x["ratings"] as JObject ?? new JObject(); private bool HasRatings(JObject x) => Ratings(x).Properties().Any(); private double Rating(JObject x) => Ratings(x).Values().Select(v => (double)v.Value<int>()).DefaultIfEmpty().Average(); private int RatingCount(JObject x) => Ratings(x).Properties().Count();
    private JObject Entry(JObject x, int number) { var creator = x["creator"] as JObject; var ratings = Ratings(x); return new JObject { ["number"] = number, ["title"] = (string)x["title"] ?? "Untitled replay", ["creator"] = (string)creator?["name"] ?? "", ["plays"] = (int?)x["plays"] ?? 0, ["rating"] = ratings.Properties().Any() ? Math.Round(Rating(x), 1) : 0, ["ratingCount"] = ratings.Properties().Count() }; }
    private string Header(JObject request, int page, int pages, int total) { if (((string)request["filterType"] ?? "").Equals("leaderboard", StringComparison.OrdinalIgnoreCase)) return "CLIP CREATORS • " + LeaderboardPeriodLabel((string)request["filter"]) + " • " + page + "/" + pages + " • " + total; return $"{Parameters(request)} • {page}/{pages} • {total}"; }
    private string LeaderboardPeriodLabel(string period) { var key = NormalizePeriod(period); if (string.IsNullOrWhiteSpace(key) || key == "all") return "ALL TIME"; if (key == "today") return "TODAY"; if (key == "week") return "THIS WEEK"; if (key == "month") return "THIS MONTH"; if (key == "year") return "THIS YEAR"; if (TryParseMonthYear(period, out var year, out var month)) return new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.CurrentCulture).ToUpperInvariant(); return period.ToUpperInvariant(); }
    private string Parameters(JObject request) { var type = ((string)request["filterType"] ?? "all").ToLowerInvariant(); if (type == "recent") return "RECENT"; if (type == "search") return "SEARCH: " + ((string)request["filter"] ?? ""); if (type == "date") return "DATE: " + ((string)request["filter"] ?? ""); if (type == "creator") return "CREATOR: " + ((string)request["filter"] ?? ""); var sort = ((string)request["sort"] ?? "catalog").ToLowerInvariant(); return sort == "plays" ? "MOST VIEWS" : sort == "rating" ? "TOP RATED" : "CATALOG"; }
    private JObject LoadUserState()
    {
        var userId = Arg("userId"); var userName = Arg("userName"); var platform = CurrentPlatform(); if (string.IsNullOrWhiteSpace(userId)) return DefaultState(userName, platform, userId);
        var raw = GetUserVar(platform, userId, UserStateKey); try { var state = string.IsNullOrWhiteSpace(raw) ? DefaultState(userName, platform, userId) : JObject.Parse(raw); var changed = false; if (state["platform"] == null) { state["platform"] = "Twitch"; changed = true; } if (state["userId"] == null) { state["userId"] = userId; changed = true; } if (state["identityKey"] == null) { state["identityKey"] = IdentityKey((string)state["platform"], (string)state["userId"]); changed = true; } if (state["requesterId"] == null) { state["requesterId"] = userId; changed = true; } if (changed) SetUserVar(platform, userId, UserStateKey, state.ToString(Newtonsoft.Json.Formatting.None)); return state; } catch { return DefaultState(userName, platform, userId); }
    }
    private JObject LoadUserSearchState(string platform, string userId, string userName)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var raw = GetUserVar(platform, userId, UserStateKey);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try { return JObject.Parse(raw); } catch { return null; }
    }
    private void SaveUserSearchState(string platform, string userId, JObject state)
    {
        if (!string.IsNullOrWhiteSpace(userId)) SetUserVar(platform, userId, UserStateKey, state.ToString(Newtonsoft.Json.Formatting.None));
    }
    private string ResolveUserId(string platform, string userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return null;
        var catalog = (JArray)Load()["catalog"] ?? new JArray();
        var creator = catalog.OfType<JObject>().Select(x => x["creator"] as JObject).FirstOrDefault(x => x != null && string.Equals((string)x["platform"], platform, StringComparison.OrdinalIgnoreCase) && (string.Equals((string)x["name"], userName, StringComparison.OrdinalIgnoreCase) || string.Equals((string)x["id"], userName, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace((string)x["id"]));
        return (string)creator?["id"];
    }
    private bool TryParseUserTarget(string raw, out string platform, out string userName)
    {
        platform = CurrentPlatform(); userName = (raw ?? "").Trim(); if (string.IsNullOrWhiteSpace(userName)) return false;
        var separator = userName.IndexOf(':'); if (separator > 0) { var requestedPlatform = userName.Substring(0, separator); var requestedUser = userName.Substring(separator + 1).Trim(); if (requestedPlatform.Equals("twitch", StringComparison.OrdinalIgnoreCase) || requestedPlatform.Equals("kick", StringComparison.OrdinalIgnoreCase) || requestedPlatform.Equals("youtube", StringComparison.OrdinalIgnoreCase)) { platform = NormalizePlatform(requestedPlatform); userName = requestedUser; } }
        return !string.IsNullOrWhiteSpace(userName);
    }
    private JObject DefaultState(string userName, string platform, string userId) => new JObject { ["filterType"] = "all", ["filter"] = "", ["sort"] = "catalog", ["amount"] = MaxAmount(), ["page"] = 1, ["requesterName"] = userName, ["requesterId"] = userId, ["platform"] = platform, ["userId"] = userId, ["identityKey"] = IdentityKey(platform, userId) };
    private void SaveUserState(JObject state) { var userId = Arg("userId"); if (!string.IsNullOrWhiteSpace(userId)) SetUserVar(CurrentPlatform(), userId, UserStateKey, state.ToString(Newtonsoft.Json.Formatting.None)); }
    private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true); JObject data; try { data = string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { data = new JObject(); }
        var catalog = data["catalog"] as JArray ?? new JArray(); var changed = false;
        foreach (var item in catalog.OfType<JObject>())
        {
            var creator = item["creator"] as JObject; if (creator != null && creator["platform"] == null) { creator["platform"] = "Twitch"; changed = true; }
            var broadcaster = item["broadcaster"] as JObject; if (broadcaster != null && broadcaster["platform"] == null) { broadcaster["platform"] = "Twitch"; changed = true; }
            var ratings = item["ratings"] as JObject; if (ratings != null) { var migrated = new JObject(); foreach (var property in ratings.Properties()) { var key = property.Name.IndexOf(":", StringComparison.Ordinal) > 0 ? property.Name : IdentityKey("Twitch", property.Name); migrated[key] = property.Value; if (!string.Equals(key, property.Name, StringComparison.OrdinalIgnoreCase)) changed = true; } item["ratings"] = migrated; }
        }
        data["catalog"] = catalog; if (changed) Save(data); return data;
    }
    private void Save(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
    private string CurrentPlatform() => NormalizePlatform(Arg("userType"));
    private string NormalizePlatform(string userType) { if (string.Equals(userType, "YouTube", StringComparison.OrdinalIgnoreCase)) return "YouTube"; if (string.Equals(userType, "Kick", StringComparison.OrdinalIgnoreCase)) return "Kick"; return "Twitch"; }
    private string IdentityKey(string platform, string userId) => NormalizePlatform(platform).ToLowerInvariant() + ":" + (userId ?? "").Trim();
    private string GetUserVar(string platform, string userId, string key) { if (platform == "YouTube") return CPH.GetYouTubeUserVarById<string>(userId, key, true); if (platform == "Kick") return CPH.GetKickUserVarById<string>(userId, key, true); return CPH.GetTwitchUserVarById<string>(userId, key, true); }
    private void SetUserVar(string platform, string userId, string key, string value) { if (platform == "YouTube") CPH.SetYouTubeUserVarById(userId, key, value, true); else if (platform == "Kick") CPH.SetKickUserVarById(userId, key, value, true); else CPH.SetTwitchUserVarById(userId, key, value, true); }
    private void SendCatalogMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var platform = Arg("requesterPlatform"); if (string.IsNullOrWhiteSpace(platform)) platform = Arg("userType");
        if (string.Equals(platform, "Kick", StringComparison.OrdinalIgnoreCase)) { CPH.SendKickMessage(text); return; }
        if (string.Equals(platform, "YouTube", StringComparison.OrdinalIgnoreCase)) { var broadcastId = Arg("requesterBroadcastId"); if (!string.IsNullOrWhiteSpace(broadcastId)) { CPH.SendYouTubeMessage(text, true, true, broadcastId); return; } CPH.SendYouTubeMessageToLatestMonitored(text); return; }
        if (string.Equals(platform, "Twitch", StringComparison.OrdinalIgnoreCase)) { CPH.SendMessage(text); return; }
        CPH.LogWarn("RTS Action Replay: unable to route catalog chat response because the originating platform is unknown.");
    }
    private string Arg(string name) { CPH.TryGetArg(name, out string value); return value ?? ""; }
    private int MaxAmount() => Math.Max(1, CPH.GetGlobalVar<int?>(MaxHistoryKey, true) ?? 20);
    private int ParseAmount(string raw) { var parts = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); return ParseAmount(ref parts); }
    private int ParseAmount(ref string[] parts) { for (var i = 0; i < parts.Length - 1; i++) if (string.Equals(parts[i], "--amount", StringComparison.OrdinalIgnoreCase) && int.TryParse(parts[i + 1], out var amount) && amount > 0) { var list = parts.ToList(); list.RemoveRange(i, 2); parts = list.ToArray(); return amount; } return 0; }
}
