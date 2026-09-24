using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string QueueKey = "rts.actionreplay.message.queue";
    private const string ActiveKey = "rts.actionreplay.message.active";
    private const string OverlayEvent = "RTS-Action Replay";
    private const string ResolverAction = "RTS - Action Replay - Core - Resolver";
    private const string MessageOperationKey = "rts.actionreplay.operation.message";

    public bool Execute() => IsOverlayCompletion() ? OverlayCompleted() : Enqueue();

    private bool IsOverlayCompletion()
    {
        var queueId = Arg("messageQueueId");
        if (!string.IsNullOrWhiteSpace(queueId)) return true;
        return string.Equals(Arg("messageComplete"), "true", StringComparison.OrdinalIgnoreCase);
    }

    public bool Reset() => ClearQueue();

    public bool SendOverlayMessage()
    {
        if (!CPH.TryGetArg("rawInput", out string message) || string.IsNullOrWhiteSpace(message)) return false;

        CPH.SetArgument("messageEvent", "Overlay Message");
        CPH.SetArgument("messageOverride", message.Trim());
        CPH.SetArgument("requesterId", "rts-overlay-message");
        CPH.SetArgument("requesterName", "Streamer");
        var sourcePlatform = Arg("commandSource");
        if (string.IsNullOrWhiteSpace(sourcePlatform)) sourcePlatform = SourcePlatform();
        CPH.SetArgument("requesterPlatform", sourcePlatform);
        CPH.SetArgument("requesterBroadcastId", "");
        return Enqueue();
    }

    public bool ClearQueue()
    {
        lock (typeof(CPHInline))
        {
            SaveQueue(new JArray());
            CPH.SetGlobalVar(ActiveKey, "", false);
        }

        CPH.LogInfo("RTS Action Replay: message queue cleared.");
        return true;
    }

    public bool Resume()
    {
        lock (typeof(CPHInline))
        {
            CPH.SetGlobalVar(ActiveKey, "", false);
        }

        CPH.LogInfo("RTS Action Replay: message queue resume requested.");
        ProcessQueue();
        return true;
    }

    public bool Enqueue()
    {
        CPH.LogInfo($"RTS Action Replay TRACE: Messaging.Enqueue entered; event={Arg("messageEvent")}; requester={Arg("requesterName")}; requesterPlatform={Arg("requesterPlatform")}.");
        var item = BuildItem();
        if (item == null)
        {
            CPH.LogWarn($"RTS Action Replay TRACE: Messaging.Enqueue BuildItem returned null; event={Arg("messageEvent")}.");
            return false;
        }
        CPH.LogInfo($"RTS Action Replay TRACE: Messaging.Enqueue built item; chat={(bool?)item["chat"] == true}; overlay={(bool?)item["overlay"] == true}; text={(string)item["message"] ?? ""}.");

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

        CPH.LogInfo($"RTS Action Replay TRACE: Messaging.Enqueue overlay queued; queueCount={LoadQueue().Count}; shouldProcess={shouldProcess}; active={CPH.GetGlobalVar<string>(ActiveKey, false) ?? "<none>"}.");
        if (shouldProcess) ProcessQueue();
        return true;
    }

    public bool TestMessage()
    {
        CPH.LogInfo("RTS Action Replay: TestMessage entered.");
        var testOperation = ReadTestOperation();
        if (testOperation == null)
        {
            CPH.LogWarn("RTS Action Replay: test message operation was not found.");
            return false;
        }

        ApplyTestOperation(testOperation);
        CPH.LogInfo($"RTS Action Replay: test operation read event={Arg("messageEvent")}, source={Arg("replaySourcePlatform")}.");

        var item = BuildItem();
        if (item == null)
        {
            CPH.LogWarn($"RTS Action Replay: test message BuildItem returned null event={Arg("messageEvent")}." );
            return false;
        }
        CPH.LogInfo($"RTS Action Replay: test message built chat={(bool?)item["chat"] == true}, overlay={(bool?)item["overlay"] == true}, text={(string)item["message"] ?? ""}.");
        if ((bool?)item["chat"] == true) SendChat(item);
        if ((bool?)item["overlay"] != true)
        {
            CPH.LogWarn("RTS Action Replay: test message overlay is disabled.");
            return true;
        }

        item["test"] = true;
        item["messagePresentationSource"] = testOperation["messagePresentationSource"]?.ToString() ?? "Current Settings";
        item["messageDesign"] = testOperation["messageDesign"]?.ToString() ?? "";
        item["messageBranding"] = testOperation["messageBranding"]?.ToString() ?? "";
        WriteMessageOperation(item);

        CPH.LogInfo("RTS Action Replay: test message operation handed to Resolver.");
        return CPH.ExecuteMethod(ResolverAction, "ResolveMessagePresentation");
    }

    private JObject ReadTestOperation()
    {
        var raw = CPH.GetGlobalVar<string>("rts.actionreplay.operation.message.test", false);
        if (string.IsNullOrWhiteSpace(raw)) return null;

        try
        {
            var operation = JObject.Parse(raw);
            CPH.UnsetGlobalVar("rts.actionreplay.operation.message.test", false);
            return operation;
        }
        catch
        {
            CPH.LogWarn("RTS Action Replay: invalid message test operation.");
            CPH.UnsetGlobalVar("rts.actionreplay.operation.message.test", false);
            return null;
        }
    }

    private void ApplyTestOperation(JObject operation)
    {
        var fields = new[]
        {
            "messageEvent", "replayId", "replayNumber", "replayTitle", "replayUserId",
            "replayUser", "replayPlatform", "replaySourcePlatform", "requesterId",
            "requesterName", "requesterPlatform", "requesterBroadcastId", "replaySource",
            "oldTitle", "newTitle", "oldRating", "averageRating", "replayRating",
            "clearedCount", "remainingCount", "messagePresentationSource", "messageDesign", "messageBranding"
        };

        foreach (var field in fields)
            CPH.SetArgument(field, operation[field]?.ToString() ?? "");
    }

    public bool OverlayCompleted()
    {
        var completedId = Arg("messageQueueId");
        CPH.LogInfo($"RTS Action Replay TRACE: Messaging.OverlayCompleted entered; queueId={completedId ?? "<none>"}; messageComplete={Arg("messageComplete")}.");
        if (string.IsNullOrWhiteSpace(completedId))
        {
            CPH.LogWarn("RTS Action Replay TRACE: Messaging.OverlayCompleted ignored - no messageQueueId.");
            return false;
        }

        var shouldProcess = false;
        lock (typeof(CPHInline))
        {
            var activeId = CPH.GetGlobalVar<string>(ActiveKey, false);
            CPH.LogInfo($"RTS Action Replay TRACE: Messaging.OverlayCompleted active={activeId ?? "<none>"}; completed={completedId}.");
            if (!string.Equals(activeId, completedId, StringComparison.OrdinalIgnoreCase))
            {
                CPH.LogWarn("RTS Action Replay TRACE: Messaging.OverlayCompleted ignored - queueId does not match active message.");
                return false;
            }

            var queue = LoadQueue();
            if (queue.Count > 0 && string.Equals((string)queue[0]?["id"], completedId, StringComparison.OrdinalIgnoreCase))
            {
                queue.RemoveAt(0);
                SaveQueue(queue);
            }

            CPH.SetGlobalVar(ActiveKey, "", false);
            shouldProcess = true;
        }

        CPH.LogInfo($"RTS Action Replay TRACE: Messaging.OverlayCompleted cleared active; shouldProcess={shouldProcess}; queueCount={LoadQueue().Count}.");
        if (shouldProcess) ProcessQueue();
        return true;
    }

    private void ProcessQueue()
    {
        CPH.LogInfo($"RTS Action Replay TRACE: Messaging.ProcessQueue entered; active={CPH.GetGlobalVar<string>(ActiveKey, false) ?? "<none>"}; queueCount={LoadQueue().Count}.");
        JObject item = null;

        lock (typeof(CPHInline))
        {
            var activeId = CPH.GetGlobalVar<string>(ActiveKey, false);
            if (!string.IsNullOrWhiteSpace(activeId))
            {
                CPH.LogInfo($"RTS Action Replay TRACE: Messaging.ProcessQueue blocked by active={activeId}.");
                return;
            }

            var queue = LoadQueue();
            if (queue.Count == 0)
            {
                CPH.LogInfo("RTS Action Replay TRACE: Messaging.ProcessQueue nothing queued.");
                return;
            }

            item = queue[0] as JObject;
            if (item == null)
            {
                CPH.LogWarn("RTS Action Replay TRACE: Messaging.ProcessQueue first queue item was not an object.");
                return;
            }

            CPH.SetGlobalVar(ActiveKey, (string)item["id"] ?? "", false);
            CPH.LogInfo($"RTS Action Replay TRACE: Messaging.ProcessQueue activated queueId={(string)item["id"] ?? "<none>"}; event={(string)item["event"] ?? "<none>"}; queueCount={queue.Count}.");
        }

        if ((bool?)item["overlay"] != true)
        {
            CPH.LogInfo($"RTS Action Replay TRACE: Messaging.ProcessQueue completing non-overlay item queueId={(string)item["id"] ?? "<none>"}.");
            CompleteWithoutOverlay((string)item["id"]);
            return;
        }

        CPH.LogInfo($"RTS Action Replay TRACE: Messaging.ProcessQueue calling TriggerOverlay queueId={(string)item["id"] ?? "<none>"}.");
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
        WriteMessageOperation(item);
        CPH.LogInfo($"RTS Action Replay TRACE: Messaging.TriggerOverlay operation written; queueId={(string)item["id"] ?? "<none>"}; event={(string)item["event"] ?? "<none>"}; text={(string)item["message"] ?? ""}.");
        var resolved = CPH.ExecuteMethod(ResolverAction, "ResolveMessagePresentation");
        CPH.LogInfo($"RTS Action Replay TRACE: Messaging.TriggerOverlay Resolver result={resolved}; queueId={(string)item["id"] ?? "<none>"}.");
    }

    private void WriteMessageOperation(JObject item)
    {
        var replay = item["replay"] as JObject ?? new JObject();
        var operation = new JObject
        {
            ["message"] = (string)item["message"] ?? "",
            ["messageQueueId"] = (bool?)item["test"] == true ? "" : (string)item["id"] ?? "",
            ["messageTest"] = (bool?)item["test"] == true,
            ["messagePosition"] = "Centered",
            ["messageSourcePlatform"] = (string)item["requester"]?["platform"] ?? "",
            ["replaySourcePlatform"] = (string)replay["sourcePlatform"] ?? "",
            ["messagePresentationSource"] = (string)item["messagePresentationSource"] ?? "Current Settings",
            ["messageDesign"] = (string)item["messageDesign"] ?? "",
            ["messageBranding"] = (string)item["messageBranding"] ?? ""
        };

        CPH.SetGlobalVar(
            MessageOperationKey,
            operation.ToString(Newtonsoft.Json.Formatting.None),
            false
        );
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
        var messageOverride = Arg("messageOverride");
        var isOverlayMessage = eventName.Equals("Overlay Message", StringComparison.OrdinalIgnoreCase);
        var chat = CPH.GetGlobalVar<bool?>(configKey + ".chat", true) ?? !isOverlayMessage;
        var defaultOverlay = eventName.Equals("Replay Created", StringComparison.OrdinalIgnoreCase) || isOverlayMessage;
        var configuredOverlay = CPH.GetGlobalVar<bool?>(configKey + ".overlay", true);
        var overlay = configuredOverlay ?? defaultOverlay;

        var values = new Dictionary<string, object>
        {
            ["replayNumber"] = Arg("replayNumber"),
            ["replayTitle"] = Arg("replayTitle"),
            ["replayRating"] = Arg("replayRating"),
            ["replayUser"] = Arg("replayUser"),
            ["replayPlatform"] = Arg("replayPlatform"),
            ["replaySourcePlatform"] = Arg("replaySourcePlatform"),
            ["requesterName"] = Arg("requesterName"),
            ["requesterPlatform"] = requesterPlatform,
            ["oldTitle"] = Arg("oldTitle"),
            ["newTitle"] = Arg("newTitle"),
            ["oldRating"] = Arg("oldRating"),
            ["averageRating"] = Arg("averageRating"),
            ["clearedCount"] = Arg("clearedCount"),
            ["remainingCount"] = Arg("remainingCount")
        };

        var message = !string.IsNullOrWhiteSpace(messageOverride)
            ? messageOverride
            : (string.IsNullOrWhiteSpace(textTemplate) ? "" : CPH.Parse(textTemplate, values));
        var presentation = "message";

        return new JObject
        {
            ["id"] = Guid.NewGuid().ToString("N"),
            ["event"] = eventName,
            ["message"] = message,
            ["chat"] = chat,
            ["overlay"] = overlay,
            ["presentation"] = presentation,
            ["test"] = false,
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
            case "replay renamed": return "Replay #%replayNumber% renamed from %oldTitle% to %newTitle%.";
            case "replay removed": return "Replay removed: %replayTitle%.";
            case "replay rated": return "Rated %replayTitle% %replayRating%/5 (average %averageRating%/5).";
            case "playlist cleared": return "Playlist cleared: %clearedCount% waiting item(s) removed.";
            case "playlist completely cleared": return "Playlist completely cleared: %clearedCount% item(s) removed.";
            default: return "";
        }
    }

    public bool FormatListEntry()
    {
        var format = CPH.GetGlobalVar<string>("rts.actionreplay.message.list.format", true);
        if (string.IsNullOrWhiteSpace(format)) format = "#%listNumber% %title% — %creator% | %rating%/5 | %platform% | %plays% plays";
        var maxLength = Math.Max(1, CPH.GetGlobalVar<int?>("rts.actionreplay.message.list.maxLength", true) ?? 500);
        var values = new Dictionary<string, object>
        {
            ["listNumber"] = Arg("listNumber"),
            ["title"] = Arg("title"),
            ["creator"] = Arg("creator"),
            ["rating"] = Arg("rating"),
            ["platform"] = Arg("platform"),
            ["plays"] = Arg("plays")
        };
        var message = CPH.Parse(format, values) ?? "";
        if (message.Length > maxLength) message = maxLength == 1 ? "…" : message.Substring(0, maxLength - 1) + "…";
        CPH.SetArgument("formattedListEntry", message);
        return true;
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
            case "playlist cleared": return "cleared";
            case "playlist completely cleared": return "clearedall";
            case "overlay message": return "custom";
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
