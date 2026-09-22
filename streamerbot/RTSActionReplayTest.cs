using System;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string DataKey = "rts.actionreplay.data";
    private const string TestOriginKey = "rts.actionreplay.test.origin";
    private const string ReplayOriginKey = "rts.actionreplay.test.replayOrigin";
    private const string MessageTypeKey = "rts.actionreplay.test.messageType";
    private const string EventName = "RTS-Action Replay";
    private const string MessagingAction = "RTS - Action Replay - Core - Messaging";
    private const string ResolverAction = "RTS - Action Replay - Core - Resolver";
    private const string PlaybackAction = "RTS - Action Replay - Core - Playback";

    public bool Execute() => TestMessage();

    public bool TestMessage()
    {
        var replay = FirstReplay();
        if (replay == null) return false;
        var creator = replay["creator"] as JObject;
        SetReplayArgs(replay);
        CPH.SetArgument("messageEvent", CPH.GetGlobalVar<string>(MessageTypeKey, true) ?? "Replay Created");
        CPH.SetArgument("oldTitle", (string)replay["title"] ?? "Test Replay");
        CPH.SetArgument("newTitle", ((string)replay["title"] ?? "Test Replay") + " (Test)");
        var rating = Rating(replay);
        CPH.SetArgument("oldRating", rating);
        CPH.SetArgument("averageRating", rating);
        CPH.SetArgument("replayRating", rating);
        CPH.SetArgument("clearedCount", 3);
        CPH.SetArgument("remainingCount", 0);

        var testEvent = CPH.GetGlobalVar<string>(MessageTypeKey, true) ?? "Replay Created";
        CPH.SetGlobalVar(
            "rts.actionreplay.operation.message.test",
            new JObject
            {
                ["messageEvent"] = testEvent,
                ["replayId"] = (string)replay["id"] ?? "",
                ["replayNumber"] = ReplayNumber((string)replay["id"] ?? ""),
                ["replayTitle"] = (string)replay["title"] ?? "Test Replay",
                ["replayUserId"] = (string)creator?[ "id" ] ?? "",
                ["replayUser"] = (string)creator?[ "name" ] ?? "Unknown Creator",
                ["replayPlatform"] = (string)creator?[ "platform" ] ?? "",
                ["replaySourcePlatform"] = ReplayOrigin(),
                ["requesterId"] = "rts-test-user",
                ["requesterName"] = "Test User",
                ["requesterPlatform"] = TestOrigin(),
                ["requesterBroadcastId"] = "",
                ["replaySource"] = ReplayOrigin(),
                ["oldTitle"] = (string)replay["title"] ?? "Test Replay",
                ["newTitle"] = ((string)replay["title"] ?? "Test Replay") + " (Test)",
                ["oldRating"] = rating,
                ["averageRating"] = rating,
                ["replayRating"] = rating,
                ["clearedCount"] = 3,
                ["remainingCount"] = 0
            }.ToString(Newtonsoft.Json.Formatting.None),
            false
        );

        CPH.LogInfo($"RTS Action Replay: test message handoff written event={testEvent}, replay={replay["id"]}.");
        return CPH.ExecuteMethod(MessagingAction, "TestMessage");
    }

    public bool TestPanel()
    {
        var replay = FirstReplay();
        if (replay == null) return false;
        SetReplayArgs(replay);
        var creator = replay["creator"] as JObject;
        var entry = new JArray(new JObject {
            ["number"] = 1,
            ["title"] = (string)replay["title"] ?? "Test Replay",
            ["creator"] = (string)creator?["name"] ?? "Unknown Creator",
            ["creatorPlatform"] = (string)creator?["platform"] ?? ReplayOrigin(),
            ["plays"] = (int?)replay["plays"] ?? 0,
            ["rating"] = Rating(replay),
            ["ratingCount"] = (replay["ratings"] as JObject)?.Count ?? 0
        });
        CPH.SetArgument("replaySearchEntries", entry.ToString(Newtonsoft.Json.Formatting.None));
        CPH.SetArgument("replaySearchParameters", "TEST");
        CPH.SetArgument("replaySearchHeader", "Page 1 of 1 • 1 Clips");
        CPH.SetArgument("replaySearchRequester", "Test User");
        CPH.SetArgument("replaySearchRequesterPlatform", TestOrigin());
        CPH.SetArgument("replaySearchDuration", 10000);
        CPH.SetArgument("replaySearchRequestId", "rts-test-panel-" + Guid.NewGuid().ToString("N"));
        CPH.SetArgument("replayTest", true);
        CPH.SetArgument("replayPanelPosition", "Centered");
        CPH.SetGlobalVar("rts.actionreplay.operation.panel", new JObject {
            ["panelType"] = "recent", ["requesterPlatform"] = TestOrigin(), ["triggerEvent"] = false
        }.ToString(Newtonsoft.Json.Formatting.None), false);
        if (!CPH.ExecuteMethod(ResolverAction, "ResolvePanel")) return false;
        CPH.SetArgument("replayCommand", "search-panel");
        CPH.TriggerEvent(EventName, true);
        return true;
    }

    public bool TestVideo()
    {
        var replay = FirstReplay();
        if (replay == null) return false;
        SetReplayArgs(replay);
        if (!CPH.ExecuteMethod(PlaybackAction, "PrepareTestVideo")) return false;
        return CPH.ExecuteMethod(ResolverAction, "ResolvePlayer");
    }

    public bool TestClapperboard()
    {
        var replay = FirstReplay();
        if (replay == null) return false;
        SetReplayArgs(replay);
        CPH.SetGlobalVar("rts.actionreplay.handoff.replayId", (string)replay["id"] ?? "", false);
        CPH.SetArgument("replayCommand", "clapperboard");
        CPH.SetArgument("replayMessage", (string)replay["title"] ?? "Test Replay");
        var creator = replay["creator"] as JObject;
        CPH.SetArgument("replayDirector", (string)creator?["name"] ?? "—");
        CPH.SetArgument("replayCreatorPlatform", ReplayOrigin());
        if (!CPH.ExecuteMethod(ResolverAction, "ResolveClapperboardBranding")) return false;
        CPH.SetArgument("replayBrandLogoUrl", Arg("replayBrandLogoUrl"));
        CPH.SetArgument("replayBrandFallbackText", Arg("replayBrandFallbackText", "RTS"));
        CPH.SetArgument("replayBrandLabel", Arg("replayBrandLabel", "ACTION REPLAY"));
        CPH.SetArgument("replayClapperPosition", "Centered");
        CPH.ExecuteMethod(ResolverAction, "GetClapperboardPositions");
        CPH.SetArgument("replayClapperPositions", CPH.GetGlobalVar<string>("rts.actionreplay.handoff.clapperPositions", false) ?? "{}");
        CPH.ExecuteMethod(ResolverAction, "ResolveClapperAnimation");
        CPH.SetGlobalVar("rts.actionreplay.handoff.replayId", "", false);
        CPH.TriggerEvent(EventName, true);
        return true;
    }

    private void SetReplayArgs(JObject replay)
    {
        var creator = replay["creator"] as JObject;
        CPH.SetArgument("replayId", (string)replay["id"] ?? "");
        CPH.SetArgument("replayNumber", ReplayNumber((string)replay["id"] ?? ""));
        CPH.SetArgument("replayTitle", (string)replay["title"] ?? "Test Replay");
        CPH.SetArgument("replayUserId", (string)creator?["id"] ?? "");
        CPH.SetArgument("replayUser", (string)creator?["name"] ?? "Unknown Creator");
        CPH.SetArgument("replayPlatform", (string)creator?["platform"] ?? "");
        CPH.SetArgument("replaySourcePlatform", ReplayOrigin());
        CPH.SetArgument("requesterId", "rts-test-user");
        CPH.SetArgument("requesterName", "Test User");
        CPH.SetArgument("requesterPlatform", TestOrigin());
        CPH.SetArgument("requesterBroadcastId", "");
        CPH.SetArgument("replaySource", ReplayOrigin());
    }

    private JObject FirstReplay()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true);
        try { var catalog = JObject.Parse(raw ?? "{}")["catalog"] as JArray; return catalog?.FirstOrDefault() as JObject; }
        catch { return null; }
    }

    private double Rating(JObject replay)
    {
        var ratings = replay?["ratings"] as JObject;
        if (ratings == null || !ratings.Properties().Any()) return 0;
        return Math.Round(ratings.Properties().Average(x => x.Value.Value<double>()), 1);
    }

    private int ReplayNumber(string id)
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true);
        try
        {
            var catalog = JObject.Parse(raw ?? "{}")["catalog"] as JArray ?? new JArray();
            for (var i = 0; i < catalog.Count; i++) if (string.Equals((string)catalog[i]?["id"], id, StringComparison.OrdinalIgnoreCase)) return i + 1;
        }
        catch { }
        return 1;
    }

    private string TestOrigin() => CPH.GetGlobalVar<string>(TestOriginKey, true) ?? "Twitch";
    private string ReplayOrigin() => CPH.GetGlobalVar<string>(ReplayOriginKey, true) ?? "Twitch";
    private string Arg(string name, string fallback = "") => CPH.TryGetArg(name, out string value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
}
