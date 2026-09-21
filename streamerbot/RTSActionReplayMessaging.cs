using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string QueueKey = "rts.actionreplay.message.queue";
    private const string ActiveKey = "rts.actionreplay.message.active";
    private const string OverlayEvent = "RTS-Action Replay";
    private const string ClapperPlaybackKey = "rts.actionreplay.handoff.clapperPlayback";

    public bool Execute() => Enqueue();

    public bool Enqueue()
    {
        var item = BuildItem();
        if (item == null) return false;

        SendChat(item);
        item["chatSent"] = true;

        if ((bool?)item["overlay"] != true) return true;

        var shouldProcess = false;
        lock (typeof(CPHInline))
        {
            var queue = LoadQueue();
            queue.Add(item);
            SaveQueue(queue);
            shouldProcess = string.IsNullOrWhiteSpace(CPH.GetGlobalVar<string>(ActiveKey, false));
        }

        if (shouldProcess) ProcessQueue();
        return true;
    }

    public bool OverlayCompleted()
    {
        var completedId = Arg("messageQueueId");
        if (string.IsNullOrWhiteSpace(completedId)) return false;

        var shouldProcess = false;
        var startPlayback = false;
        var replayId = "";
        lock (typeof(CPHInline))
        {
            var activeId = CPH.GetGlobalVar<string>(ActiveKey, false);
            if (!string.Equals(activeId, completedId, StringComparison.OrdinalIgnoreCase)) return false;

            var queue = LoadQueue();
            var completed = queue.Count > 0 && string.Equals((string)queue[0]?["id"], completedId, StringComparison.OrdinalIgnoreCase)
                ? queue[0] as JObject
                : null;
            startPlayback = (bool?)completed?["startPlaybackAfterClapperboard"] == true;
            replayId = (string)completed?["replay"]?["id"] ?? "";

            if (completed != null)
            {
                queue.RemoveAt(0);
                SaveQueue(queue);
            }

            CPH.SetGlobalVar(ActiveKey, "", false);
            shouldProcess = true;
        }

        if (startPlayback && !string.IsNullOrWhiteSpace(replayId))
        {
            CPH.SetArgument("replayId", replayId);
            CPH.ExecuteMethod("RTS - Action Replay - Core - Playlist", "StartAfterClapperboard");
        }

        if (shouldProcess) ProcessQueue();
        return true;
    }

    private void ProcessQueue()
    {
        JObject item = null;

        lock (typeof(CPHInline))
        {
            var activeId = CPH.GetGlobalVar<string>(ActiveKey, false);
            if (!string.IsNullOrWhiteSpace(activeId)) return;

            var queue = LoadQueue();
            if (queue.Count == 0) return;

            item = queue[0] as JObject;
            if (item == null) return;

            CPH.SetGlobalVar(ActiveKey, (string)item["id"] ?? "", false);
        }

        if ((bool?)item["overlay"] != true)
        {
            CompleteWithoutOverlay((string)item["id"]);
            return;
        }

        TriggerOverlay(item);
    }

    private void CompleteWithoutOverlay(string id)
    {
        lock (typeof(CPHInline))
        {
            var queue = LoadQueue();
            if (queue.Count > 0 && string.Equals((string)queue[0]?["id"], id, StringComparison.OrdinalIgnoreCase))
            {
                queue.RemoveAt(0);
                SaveQueue(queue);
            }
            CPH.SetGlobalVar(ActiveKey, "", false);
        }

        ProcessQueue();
    }

    private void SendChat(JObject item)
    {
        if ((bool?)item["chat"] != true) return;

        var platform = (string)item["requester"]?["platform"] ?? "";
        var broadcastId = (string)item["requester"]?["broadcastId"] ?? "";
        var text = (string)item["message"] ?? "";
        if (string.IsNullOrWhiteSpace(text)) return;

        if (platform.Equals("Kick", StringComparison.OrdinalIgnoreCase))
        {
            CPH.SendKickMessage(text);
            return;
        }

        if (platform.Equals("YouTube", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(broadcastId)) CPH.SendYouTubeMessage(text, true, true, broadcastId);
            else CPH.SendYouTubeMessageToLatestMonitored(text);
            return;
        }

        if (platform.Equals("Twitch", StringComparison.OrdinalIgnoreCase))
        {
            CPH.SendMessage(text);
            return;
        }

        CPH.LogWarn("RTS Action Replay: message chat response has no known originating platform.");
    }

    private void TriggerOverlay(JObject item)
    {
        CPH.SetArgument("messageQueueId", (string)item["id"] ?? "");
        CPH.SetArgument("replayMessage", (string)item["message"] ?? "");
        CPH.SetArgument("replayMessageSourcePlatform", (string)item["replay"]?["sourcePlatform"] ?? "");
        CPH.SetArgument("replaySource", (string)item["replay"]?["sourcePlatform"] ?? "");

        if (string.Equals((string)item["presentation"], "clapperboard", StringComparison.OrdinalIgnoreCase))
        {
            CPH.SetArgument("replayCommand", "clapperboard");
            CPH.SetArgument("replayLogoUrl", CPH.GetGlobalVar<string>("rts.actionreplay.brandLogoUrl", true) ?? "");
            CPH.SetArgument("replayClapperPosition", "Centered");
            CPH.ExecuteMethod("RTS - Action Replay - Core - Resolver", "GetClapperboardPositions");
            CPH.SetArgument("replayClapperPositions", CPH.GetGlobalVar<string>("rts.actionreplay.handoff.clapperPositions", false) ?? "{}");
            CPH.ExecuteMethod("RTS - Action Replay - Core - Resolver", "ResolveClapperboardBranding");
            CPH.ExecuteMethod("RTS - Action Replay - Core - Resolver", "ResolveClapperAnimation");
        }
        else
        {
            CPH.SetArgument("replayCommand", "message");
            CPH.SetArgument("replayMessagePosition", "Centered");
            CPH.ExecuteMethod("RTS - Action Replay - Core - Resolver", "ResolveMessagePresentation");
        }

        CPH.TriggerEvent(OverlayEvent, true);
    }

    private JObject BuildItem()
    {
        var eventName = Arg("messageEvent");
        if (string.IsNullOrWhiteSpace(eventName)) return null;

        var requesterPlatform = Arg("requesterPlatform");
        if (string.IsNullOrWhiteSpace(requesterPlatform)) requesterPlatform = SourcePlatform();

        var configKey = "rts.actionreplay.message." + EventKey(eventName);
        var textTemplate = CPH.GetGlobalVar<string>(configKey + ".text", true);
        if (string.IsNullOrWhiteSpace(textTemplate)) textTemplate = DefaultMessage(eventName);
        var chat = CPH.GetGlobalVar<bool?>(configKey + ".chat", true) ?? true;
        var defaultOverlay = eventName.Equals("Replay Created", StringComparison.OrdinalIgnoreCase);
        var configuredOverlay = CPH.GetGlobalVar<bool?>(configKey + ".overlay", true);\n        var overlay = eventName.Equals("Replay Created", StringComparison.OrdinalIgnoreCase)\n            ? ((CPH.GetGlobalVar<bool?>("rts.actionreplay.clapper.showOnNewClip", true) ?? true) && (configuredOverlay ?? defaultOverlay))\n            : (configuredOverlay ?? defaultOverlay);

        var values = new Dictionary<string, object>
        {
            ["replayId"] = Arg("replayId"),
            ["replayNumber"] = Arg("replayNumber"),
            ["replayTitle"] = Arg("replayTitle"),
            ["replayRating"] = Arg("replayRating"),
            ["replayUser"] = Arg("replayUser"),
            ["replayUserId"] = Arg("replayUserId"),
            ["replayPlatform"] = Arg("replayPlatform"),
            ["replaySourcePlatform"] = Arg("replaySourcePlatform"),
            ["requesterId"] = Arg("requesterId"),
            ["requesterName"] = Arg("requesterName"),
            ["requesterPlatform"] = requesterPlatform,
            ["requesterBroadcastId"] = Arg("requesterBroadcastId")
        };

        var message = string.IsNullOrWhiteSpace(textTemplate) ? "" : CPH.Parse(textTemplate, values);
        var startPlaybackAfterClapperboard = false;
        if (eventName.Equals("Replay Created", StringComparison.OrdinalIgnoreCase) && string.Equals(CPH.GetGlobalVar<string>(ClapperPlaybackKey, false), Arg("replayId"), StringComparison.OrdinalIgnoreCase))
        {
            startPlaybackAfterClapperboard = true;
        }
        var presentation = eventName.Equals("Replay Created", StringComparison.OrdinalIgnoreCase) ? "clapperboard" : "message";

        return new JObject
        {
            ["id"] = Guid.NewGuid().ToString("N"),
            ["event"] = eventName,
            ["message"] = message,
            ["chat"] = chat,
            ["overlay"] = overlay,
            ["presentation"] = presentation,
            ["startPlaybackAfterClapperboard"] = startPlaybackAfterClapperboard,
            ["requester"] = new JObject
            {
                ["id"] = Arg("requesterId"),
                ["name"] = Arg("requesterName"),
                ["platform"] = requesterPlatform,
                ["broadcastId"] = Arg("requesterBroadcastId")
            },
            ["replay"] = new JObject
            {
                ["id"] = Arg("replayId"),
                ["number"] = Arg("replayNumber"),
                ["title"] = Arg("replayTitle"),
                ["creatorId"] = Arg("replayUserId"),
                ["creatorName"] = Arg("replayUser"),
                ["creatorPlatform"] = Arg("replayPlatform"),
                ["sourcePlatform"] = Arg("replaySourcePlatform"),
                ["rating"] = Arg("replayRating")
            }
        };
    }

    private string DefaultMessage(string eventName)
    {
        switch ((eventName ?? "").Trim().ToLowerInvariant())
        {
            case "replay created": return "Replay saved: %replayTitle%.";
            case "replay queued": return "Replay queued: %replayTitle%.";
            case "replay renamed": return "Replay #%replayNumber% renamed to %replayTitle%.";
            case "replay removed": return "Replay removed: %replayTitle%.";
            case "replay rated": return "Rated %replayTitle% %replayRating%/5.";
            default: return "";
        }
    }

    private string SourcePlatform()
    {
        try
        {
            var source = CPH.GetSource().ToString();
            if (source.Equals("Kick", StringComparison.OrdinalIgnoreCase)) return "Kick";
            if (source.Equals("YouTube", StringComparison.OrdinalIgnoreCase)) return "YouTube";
            if (source.Equals("Twitch", StringComparison.OrdinalIgnoreCase)) return "Twitch";
        }
        catch { }
        return "";
    }

    private string EventKey(string eventName)
    {
        switch ((eventName ?? "").Trim().ToLowerInvariant())
        {
            case "replay created": return "created";
            case "replay queued": return "queued";
            case "replay renamed": return "renamed";
            case "replay removed": return "removed";
            case "replay rated": return "rated";
            default: return "";
        }
    }

    private JArray LoadQueue()
    {
        var raw = CPH.GetGlobalVar<string>(QueueKey, false);
        try { return string.IsNullOrWhiteSpace(raw) ? new JArray() : JArray.Parse(raw); }
        catch { return new JArray(); }
    }

    private void SaveQueue(JArray queue) =>
        CPH.SetGlobalVar(QueueKey, queue.ToString(Newtonsoft.Json.Formatting.None), false);

    private string Arg(string name)
    {
        CPH.TryGetArg(name, out string value);
        return value ?? "";
    }
}
