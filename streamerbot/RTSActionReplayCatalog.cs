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
    public bool ListCatalogDate() { var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); var amount = ParseAmount(ref parts); var period = string.Join(" ", parts).Trim(); if (string.IsNullOrWhiteSpace(period)) { CPH.SendMessage("Please provide a catalog date period."); return false; } return Queue(BuildState("date", period, "catalog", amount)); }
    public bool ListCatalogCreator() { var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); var amount = ParseAmount(ref parts); var creator = string.Join(" ", parts).Trim(); if (string.IsNullOrWhiteSpace(creator)) { CPH.SendMessage("Please provide a creator name."); return false; } return Queue(BuildState("creator", creator, "catalog", amount)); }
    public bool ListCatalogMostViews() => Queue(BuildState("all", "", "plays", ParseAmount(Arg("rawInput"))));
    public bool ListCatalogTopRated() => Queue(BuildState("all", "", "rating", ParseAmount(Arg("rawInput"))));
    public bool CatalogNext() => MovePage(1);
    public bool CatalogPrevious() => MovePage(-1);
    public bool CatalogFirst() => SetPage(1);
    public bool CatalogLast() { var state = LoadUserState(); var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); state["page"] = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount)); SaveUserState(state); return Queue(state); }
    public bool RenderSearchRequest()
    {
        var json = Arg("replaySearchRequest"); if (string.IsNullOrWhiteSpace(json)) return false; JObject request; try { request = JObject.Parse(json); } catch { return false; }
        var results = Query(request); var amount = Math.Max(1, (int?)request["amount"] ?? MaxAmount()); var page = Math.Max(1, (int?)request["page"] ?? 1); var pages = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount)); page = Math.Min(page, pages); var start = (page - 1) * amount;
        var entries = results.Skip(start).Take(amount).OfType<JObject>().Select((x, i) => Entry(x, start + i + 1)).ToList(); CPH.SetArgument("replaySearchEntries", new JArray(entries).ToString(Newtonsoft.Json.Formatting.None)); CPH.SetArgument("replaySearchHeader", Header(request, page, pages, results.Count)); CPH.SetArgument("replaySearchRequester", (string)request["requesterName"] ?? ""); CPH.SetArgument("replaySearchParameters", Parameters(request)); CPH.SetArgument("replaySearchRequestId", (string)request["requestId"] ?? ""); CPH.SetArgument("replaySearchDuration", CPH.GetGlobalVar<int?>("rts.actionreplay.searchPanel.duration", true) ?? 10000); CPH.SetArgument("replayPanelWidth", CPH.GetGlobalVar<int?>("rts.actionreplay.panel.width", true) ?? 500); CPH.SetArgument("replayPanelHeight", CPH.GetGlobalVar<int?>("rts.actionreplay.panel.height", true) ?? 700); CPH.SetArgument("panelType", "recent"); CPH.ExecuteMethod(PanelAnimationAction, "ResolvePanelAnimation"); CPH.SetArgument("replayCommand", "search-panel"); CPH.TriggerEvent("RTS-Action Replay", true); return true;
    }
    public bool RateReplay()
    {
        var parts = Arg("rawInput").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); if (parts.Length < 2 || !int.TryParse(parts[0], out var index) || index < 1 || !int.TryParse(parts[1], out var rating) || rating < 1 || rating > 5) { CPH.SendMessage("Usage: !rate-replay <catalog number> <1-5>"); return false; }
        var state = LoadUserState(); var results = Query(state); if (index > results.Count) { CPH.SendMessage("That catalog entry does not exist."); return false; } var userId = Arg("userId"); if (string.IsNullOrWhiteSpace(userId)) { CPH.SendMessage("A user account is required to rate a replay."); return false; }
        var data = Load(); var replayId = results[index - 1]?["id"]?.ToString(); var catalog = data["catalog"] as JArray ?? new JArray(); var replay = catalog.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], replayId, StringComparison.OrdinalIgnoreCase)); if (replay == null) { CPH.SendMessage("That replay is no longer in the Catalog."); return false; } var ratings = replay["ratings"] as JObject ?? new JObject(); ratings[userId] = rating; replay["ratings"] = ratings; Save(data); CPH.SendMessage($"Rated {(string)replay["title"] ?? "Replay"} {rating}/5."); return true;
    }
    private JObject BuildState(string type, string value, string sort, int amount = 0) { var state = new JObject { ["filterType"] = type, ["filter"] = value ?? "", ["sort"] = sort, ["amount"] = amount > 0 ? amount : MaxAmount(), ["page"] = 1, ["requesterId"] = Arg("userId"), ["requesterName"] = Arg("userName") }; SaveUserState(state); return state; }
    private bool MovePage(int delta) { var state = LoadUserState(); var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); var pages = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount)); state["page"] = Math.Max(1, Math.Min(pages, ((int?)state["page"] ?? 1) + delta)); SaveUserState(state); return Queue(state); }
    private bool SetPage(int page) { var state = LoadUserState(); var results = Query(state); var amount = Math.Max(1, (int?)state["amount"] ?? MaxAmount()); var pages = Math.Max(1, (int)Math.Ceiling(results.Count / (double)amount)); state["page"] = Math.Max(1, Math.Min(pages, page)); SaveUserState(state); return Queue(state); }
    private bool Queue(JObject state) { CPH.SetArgument("replaySearchRequest", state.ToString(Newtonsoft.Json.Formatting.None)); return CPH.ExecuteMethod(SearchQueueAction, "Enqueue"); }
    private JArray Query(JObject state)
    {
        var list = (JArray)Load()["catalog"] ?? new JArray(); var type = ((string)state["filterType"] ?? "all").ToLowerInvariant(); var filter = ((string)state["filter"] ?? "").Trim(); IEnumerable<JObject> query = list.OfType<JObject>(); if (type == "search") query = query.Where(x => SearchMatch(x, filter)); else if (type == "date") query = query.Where(x => DateMatch(x, filter)); else if (type == "creator") query = query.Where(x => CreatorMatch(x, filter));
        if (type == "recent") query = query.OrderByDescending(x => ParseDate((string)x["captured"] ?? (string)x["added"])).Take(Math.Max(1, CPH.GetGlobalVar<int?>(MaxHistoryKey, true) ?? 20));
        var sort = ((string)state["sort"] ?? "catalog").ToLowerInvariant(); if (sort == "plays") query = query.OrderByDescending(x => (int?)x["plays"] ?? 0); if (sort == "rating") query = query.Where(HasRatings).OrderByDescending(Rating).ThenByDescending(RatingCount); return new JArray(query);
    }
    private bool SearchMatch(JObject x, string term) { if (string.IsNullOrWhiteSpace(term)) return true; var creator = x["creator"] as JObject; return Contains(x["title"], term) || Contains(x["file"], term) || Contains(creator?["name"], term); }
    private bool CreatorMatch(JObject x, string name) { var creator = x["creator"] as JObject; return string.Equals((string)creator?["name"], name, StringComparison.OrdinalIgnoreCase) || string.Equals((string)creator?["id"], name, StringComparison.OrdinalIgnoreCase); }
    private bool DateMatch(JObject x, string period)
    {
        var raw = (string)x["captured"] ?? (string)x["added"]; if (!DateTime.TryParse(raw, out var date)) return false; var now = DateTime.Now; var key = period.ToLowerInvariant().Replace("_", "-").Replace(" ", "-").Trim(); if (key == "today") return date.Date == now.Date; if (key == "yesterday") return date.Date == now.Date.AddDays(-1); var week = StartOfWeek(now); if (key == "this-week") return date >= week && date < week.AddDays(7); if (key == "last-week") return date >= week.AddDays(-7) && date < week; if (key == "this-month") return date.Year == now.Year && date.Month == now.Month; var month = new DateTime(now.Year, now.Month, 1).AddMonths(-1); if (key == "last-month") return date.Year == month.Year && date.Month == month.Month; if (key == "this-year") return date.Year == now.Year; if (key == "last-year") return date.Year == now.Year - 1; if (DateTime.TryParse("1 " + period, CultureInfo.CurrentCulture, DateTimeStyles.None, out var requestedMonth)) return date.Year == requestedMonth.Year && date.Month == requestedMonth.Month; return false;
    }
    private DateTime StartOfWeek(DateTime date) => date.Date.AddDays(-(7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7);
    private DateTime ParseDate(string value) { return DateTime.TryParse(value, out var date) ? date : DateTime.MinValue; }
    private bool Contains(JToken token, string term) => (token?.ToString() ?? "").IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    private JObject Ratings(JObject x) => x["ratings"] as JObject ?? new JObject(); private bool HasRatings(JObject x) => Ratings(x).Properties().Any(); private double Rating(JObject x) => Ratings(x).Values().Select(v => (double)v.Value<int>()).DefaultIfEmpty().Average(); private int RatingCount(JObject x) => Ratings(x).Properties().Count();
    private JObject Entry(JObject x, int number) { var creator = x["creator"] as JObject; var ratings = Ratings(x); return new JObject { ["number"] = number, ["title"] = (string)x["title"] ?? "Untitled replay", ["creator"] = (string)creator?["name"] ?? "", ["plays"] = (int?)x["plays"] ?? 0, ["rating"] = ratings.Properties().Any() ? Math.Round(Rating(x), 1) : 0, ["ratingCount"] = ratings.Properties().Count() }; }
    private string Header(JObject request, int page, int pages, int total) => $"{Parameters(request)} • {page}/{pages} • {total}";
    private string Parameters(JObject request) { var type = ((string)request["filterType"] ?? "all").ToLowerInvariant(); if (type == "recent") return "RECENT"; if (type == "search") return "SEARCH: " + ((string)request["filter"] ?? ""); if (type == "date") return "DATE: " + ((string)request["filter"] ?? ""); if (type == "creator") return "CREATOR: " + ((string)request["filter"] ?? ""); var sort = ((string)request["sort"] ?? "catalog").ToLowerInvariant(); return sort == "plays" ? "MOST VIEWS" : sort == "rating" ? "TOP RATED" : "CATALOG"; }
    private JObject LoadUserState()
    {
        var userId = Arg("userId"); var userName = Arg("userName"); if (string.IsNullOrWhiteSpace(userId)) return DefaultState(userName); var raw = CPH.GetTwitchUserVarById<string>(userId, UserStateKey, true); try { return string.IsNullOrWhiteSpace(raw) ? DefaultState(userName) : JObject.Parse(raw); } catch { return DefaultState(userName); }
    }
    private JObject DefaultState(string userName) => new JObject { ["filterType"] = "all", ["filter"] = "", ["sort"] = "catalog", ["amount"] = MaxAmount(), ["page"] = 1, ["requesterName"] = userName };
    private void SaveUserState(JObject state) { var userId = Arg("userId"); if (!string.IsNullOrWhiteSpace(userId)) CPH.SetTwitchUserVarById(userId, UserStateKey, state.ToString(Newtonsoft.Json.Formatting.None), true); }
    private JObject Load() { var raw = CPH.GetGlobalVar<string>(DataKey, true); try { return string.IsNullOrWhiteSpace(raw) ? new JObject() : JObject.Parse(raw); } catch { return new JObject(); }
    }
    private void Save(JObject data) => CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true);
    private string Arg(string name) { CPH.TryGetArg(name, out string value); return value ?? ""; }
    private int MaxAmount() => Math.Max(1, CPH.GetGlobalVar<int?>(MaxHistoryKey, true) ?? 20);
    private int ParseAmount(string raw) { var parts = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); return ParseAmount(ref parts); }
    private int ParseAmount(ref string[] parts) { for (var i = 0; i < parts.Length - 1; i++) if (string.Equals(parts[i], "--amount", StringComparison.OrdinalIgnoreCase) && int.TryParse(parts[i + 1], out var amount) && amount > 0) { var list = parts.ToList(); list.RemoveRange(i, 2); parts = list.ToArray(); return amount; } return 0; }
}