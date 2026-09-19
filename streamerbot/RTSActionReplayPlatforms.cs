using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Twitch.Common.Models.Api;

public class CPHInline
{
private const string DataKey = "rts.actionreplay.data";

private const string LegacyCatalogKey = "rts.actionreplay.catalog";

private const string MaxRecentKey = "rts.actionreplay.maxHistory";

private const string PlaybackModeKey = "rts.actionreplay.twitch.playbackMode";

private const string TwitchFolderKey = "rts.actionreplay.twitch.folder";

private const string PlaylistAction = "RTS - Action Replay - Core - Playlist";

private const string ResolverAction = "RTS - Action Replay - Core - Resolver";

private const string ReplayIdHandoffKey = "rts.actionreplay.handoff.replayId";

private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";

private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";

public bool Execute() => CreateTwitchClip();

public bool CreateTwitchClip()
    {
        CPH.TryGetArg("rawInput", out string rawInput);

public bool SyncTwitchClips()
    {
        var data = Load();

private JObject AddTwitchClip(ClipData clip, bool playAfterAdd)
    {
        var data = Load();

private void EnsureLocalCopyIfConfigured(JObject data, JObject item, string clipId)
    {
        if (!ModeNeedsLocalCopy(GetPlaybackMode())) return;

private bool BroadcastReplay(JObject item)
    {
        CPH.SetGlobalVar(ReplayIdHandoffKey, (string)item["id"] ?? "", false);

private string ResolvePlaybackUrl(JObject item)
    {
        var mode = GetPlaybackMode();

private string GetTwitchMediaUrl(string clipId)
    {
        if (string.IsNullOrWhiteSpace(clipId)) return null;

private string DownloadClip(string clipId)
    {
        var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true);

private JObject Load()
    {
        var raw = CPH.GetGlobalVar<string>(DataKey, true);

private void Save(JObject data) { data["version"] = 2; data["catalog"] = data["catalog"] as JArray ?? new JArray(); data["recentIds"] = data["recentIds"] as JArray ?? new JArray(); data.Remove("replays"); CPH.SetGlobalVar(DataKey, data.ToString(Newtonsoft.Json.Formatting.None), true); CPH.SetGlobalVar("rts.actionreplay.recentIds", ((JArray)data["recentIds"]).ToString(Newtonsoft.Json.Formatting.None), true); }

private void MergeCatalog(JArray target, JArray source) { foreach (var token in source) { var item = token as JObject; if (item == null) continue; var id = (string)item["id"]; var type = (string)item["sourceType"] ?? "OBS"; var sourceId = (string)item["sourceId"]; if (target.OfType<JObject>().Any(x => (!string.IsNullOrWhiteSpace(id) && string.Equals((string)x["id"], id, StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrWhiteSpace(sourceId) && string.Equals((string)x["sourceType"] ?? "OBS", type, StringComparison.OrdinalIgnoreCase) && string.Equals((string)x["sourceId"], sourceId, StringComparison.OrdinalIgnoreCase)))) continue; var clone = (JObject)item.DeepClone(); if (string.IsNullOrWhiteSpace((string)clone["sourceType"])) clone["sourceType"] = "OBS"; if (string.IsNullOrWhiteSpace((string)clone["sourceId"])) clone["sourceId"] = (string)clone["id"] ?? ""; if (clone["plays"] == null) clone["plays"] = 0; if (clone["users"] == null) clone["users"] = new JObject(); target.Add(clone); }

private JObject FindTwitchClip(JArray catalog, string clipId) { if (catalog == null) return null;

private void AddRecent(JArray recent, string id) { for (var i = recent.Count - 1; i >= 0; i--) if (string.Equals((string)recent[i], id, StringComparison.OrdinalIgnoreCase)) recent.RemoveAt(i); recent.Insert(0, id); }

private void TrimRecent(JArray recent) { var max = CPH.GetGlobalVar<int?>(MaxRecentKey, true) ?? 20;

private bool ModeNeedsLocalCopy(string mode) => string.Equals(mode, "Download Locally", StringComparison.OrdinalIgnoreCase) || string.Equals(mode, "Both", StringComparison.OrdinalIgnoreCase);

private string GetPlaybackMode() { var mode = CPH.GetGlobalVar<string>(PlaybackModeKey, true);

private string BuildTwitchHttpUrl(string fileName) { var mapping = CPH.GetGlobalVar<string>("rts.actionreplay.twitch.httpMapping", true) ?? "twitch";

private bool PathsEqual(string a, string b) { try { return string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

private string Sanitize(string value) { var invalid = Path.GetInvalidFileNameChars();

private int GetSettingInt(string key, int fallback) { try { object value = CPH.GetGlobalVar<object>(key, true);

private const string YouTubeBroadcastIdKey = "rts.actionreplay.youtube.broadcastId";

private const string YouTubeStartTimeKey = "rts.actionreplay.youtube.actualStartTime";

public bool BroadcastStarted()
    {
        var broadcastId = Arg("broadcast.id").Trim();

public bool CreateYouTubeClip()
    {
        var duration = Math.Max(5, Math.Min(60, GetSettingInt("rts.actionreplay.youtube.clipDuration", 30)));

private bool TryGetStartTime(string videoId, out long startTime)
    {
        startTime = 0;
        var storedId = GetGlobalString(YouTubeBroadcastIdKey);
        var actualStart = GetGlobalLong(YouTubeStartTimeKey);
        if (!string.Equals(storedId, videoId, StringComparison.OrdinalIgnoreCase) || actualStart <= 0) return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        startTime = now - actualStart;
        return startTime >= 0;
    }

private string GetGlobalString(string key) { try { return CPH.GetGlobalVar<string>(key, true) ?? "";

private long GetGlobalLong(string key) { try { return CPH.GetGlobalVar<long?>(key, true) ?? 0L;

private string NormalizePlatform(string userType) => string.Equals(userType, "YouTube", StringComparison.OrdinalIgnoreCase) ? "YouTube" : string.Equals(userType, "Kick", StringComparison.OrdinalIgnoreCase) ? "Kick" : "Twitch";

private string Arg(string name) { CPH.TryGetArg(name, out string value);

private void SendOriginMessage(string message)
    {
        var platform = Arg("userType");

private const string PendingKey = "rts.actionreplay.kick.pending";

public bool RequestKickBotClip()
    {
        var message = Arg("text");

public bool CaptureKickBotClip()
    {
        var message = Arg("text");

public bool CaptureKickClip()
    {
        var message = Arg("text");

private JObject GetNativeKickClipMetadata(string clipId)
    {
        var json = DownloadString("https://kick.com/api/v2/clips/" + CPH.UrlEncode(clipId) + "/play");

private string NormalizeCreateClipMessage(string message)
    {
        if (Regex.IsMatch(message ?? "", @"^!create-clip(?:\s|$)", RegexOptions.IgnoreCase)) return message;

private bool RequestKickBotClipInternal(string message)
    {
        var duration = ParseDuration(message);

private int ParseDuration(string message)
    {
        var match = Regex.Match(message ?? "", @"^!create-clip(?:\s+(\d+))?", RegexOptions.IgnoreCase);

private string ParseTitle(string message)
    {
        var match = Regex.Match(message ?? "", @"^!create-clip(?:\s+\d+)?(?:\s+(.*))?$", RegexOptions.IgnoreCase);

private JObject LoadPending()
    {
        var raw = CPH.GetGlobalVar<string>(PendingKey, false);

private void ClearPending() => CPH.UnsetGlobalVar(PendingKey, false);

private string ExtractKickBotUrl(string text)
    {
        var match = Regex.Match(text ?? "", @"https?://(?:www\.)?kickbot\.com/clip/[A-Za-z0-9]+", RegexOptions.IgnoreCase);

private string ExtractKickBotId(string url)
    {
        var match = Regex.Match(url ?? "", @"kickbot\.com/clip/([A-Za-z0-9]+)", RegexOptions.IgnoreCase);

private string ExtractKickClipUrl(string text)
    {
        var match = Regex.Match(text ?? "", @"https?://(?:www\.)?kick\.com/[A-Za-z0-9_-]+/clips/clip_[A-Za-z0-9_-]+", RegexOptions.IgnoreCase);

private string ExtractKickClipId(string value)
    {
        var match = Regex.Match(value ?? "", @"/clips/(clip_[A-Za-z0-9_-]+)", RegexOptions.IgnoreCase);

private string DownloadString(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

private JObject Find(JArray catalog, string clipId)
    {
        if (catalog == null) return null;

private bool EnsureLocalCopy(JObject item, string clipId)
    {
        var current = (string)item["filePath"];

private string Download(string clipId)
    {
        var folder = CPH.GetGlobalVar<string>(TwitchFolderKey, true);
}
