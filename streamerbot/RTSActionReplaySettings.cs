// Streamer.bot C# action: open the Action Replay settings window.
// Requires RtsUI.dll 0.2.0 or newer as a custom assembly reference.
public class CPHInline
{
    public bool Execute()
    {
        var ui = new RtsUI("RTS Action Replay", "1.0.0",
            (key, persisted) => CPH.GetGlobalVar<bool?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<int?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<string>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<object>(key, persisted),
            (key, value, persisted) => CPH.SetGlobalVar(key, value, persisted),
            message => CPH.LogInfo(message));

        AddGeneralSettings(ui);
        AddBrandingSettings(ui);
        AddPlaylistSettings(ui);
        AddTwitchSettings(ui);
        AddPlayerSettings(ui);
        AddAppearanceSettings(ui);
        EnsureAnimationProfiles();
        AddPositionSettings(ui);
        AddMessageSettings(ui);

        ui.ShowUI();
        return true;
    }

    private void AddGeneralSettings(RtsUI ui)
    {
        ui.AddThemeSelector("Settings Theme", "Choose the RtsUI theme.", "General", "rts.actionreplay.uiTheme", "Dark");
        ui.BeginSection("Replay Source", "General");
        ui.AddFolderPicker("Replay Folder", "Folder containing OBS Replay Buffer files. Twitch clips use a separate folder and are never written here.", "General", "rts.actionreplay.replayFolder", "");
        ui.AddTextbox("Replay File Types", "Accepted extensions, separated by commas.", "General", "rts.actionreplay.replayFileTypes", ".mp4, .mkv", false);
        ui.AddTextbox("HTTP Mapping", "Streamer.bot HTTP path mapped to the OBS replay folder.", "General", "rts.actionreplay.httpMapping", "replays", false);
        ui.AddNumericTextbox("HTTP Port", "Streamer.bot HTTP Server port used to serve replay files.", "General", "rts.actionreplay.httpPort", 7474, 1, 65535);
        ui.EndSection();
    }

    private void AddBrandingSettings(RtsUI ui)
    {
        ui.BeginSection("Branding", "Branding");
        ui.BeginSection("Brand Identity");
        ui.AddToggleSwitch("Show Replay Branding", "Display the branding area on the replay player.", "Branding", "rts.actionreplay.showBranding", true);
        ui.AddTextbox("Branding Logo URL", "HTTPS URL to a logo. Leave blank to use the fallback text.", "Branding", "rts.actionreplay.brandLogoUrl", "", false);
        ui.BeginRow();
        ui.AddTextbox("Branding Fallback Text", "Text shown when no branding logo is defined.", "Branding", "rts.actionreplay.brandFallbackText", "RTS", false);
        ui.AddColorPicker("Branding Fallback Text Color", "Colour of the branding fallback text.", "Branding", "rts.actionreplay.brandFallbackTextColor", "#0384CBFF");
        ui.EndRow();
        ui.BeginRow();
        ui.AddTextbox("Branding Label", "Text displayed beside the logo or fallback text.", "Branding", "rts.actionreplay.brandLabel", "ACTION REPLAY", false);
        ui.AddColorPicker("Branding Label Color", "Colour of the branding label.", "Branding", "rts.actionreplay.brandLabelColor", "#FFFFFFFF");
        ui.EndRow();
        ui.EndSection();
        ui.BeginSection("Title Colours");
        ui.BeginRow();
        ui.AddColorPicker("Primary Colour", "Primary branding colour used by replay title accents.", "Branding", "rts.actionreplay.titlePrimaryColor", "#0384CBFF");
        ui.AddColorPicker("Secondary Colour", "Secondary branding colour paired with Primary Colour in replay title accents.", "Branding", "rts.actionreplay.titleSecondaryColor", "#101416FF");
        ui.EndRow();
        ui.EndSection();
        ui.EndSection();
    }

    private void AddPlaylistSettings(RtsUI ui)
    {
        ui.BeginSection("Replay Defaults", "Playlist");
        ui.AddTextbox("Replay Title Template", "Default title for new replays. Streamer.bot variables can be used.", "Playlist", "rts.actionreplay.replayTitle", "%replayName%", false);
        ui.AddTextbox("New Replay Display Title", "Title shown when a newly discovered replay is automatically played.", "Playlist", "rts.actionreplay.newReplayTitle", "New Replay", false);
        ui.EndSection();
        ui.BeginSection("Recent Clips", "Playlist");
        ui.AddSlider("Maximum Recent Clips", "Maximum number of newest Catalog items shown as Recent Clips. Older Catalog items are not deleted.", "Playlist", "rts.actionreplay.maxHistory", 1, 100, 20);
        ui.BeginRow();
        ui.AddToggleSwitch("Auto-add Saved Replays", "Add each newly saved OBS replay to the Catalog and Recent Clips.", "Playlist", "rts.actionreplay.autoAdd", true);
        ui.AddToggleSwitch("Auto-play Newest Replay", "Load and play a newly saved OBS replay automatically.", "Playlist", "rts.actionreplay.autoPlay", false);
        ui.EndRow();
        ui.EndSection();
    }

    private void AddTwitchSettings(RtsUI ui)
    {
        ui.BeginSection("Twitch Clips", "Twitch");
        ui.AddDropdown("Twitch Clip Playback", "How Action Replay obtains Twitch media when a Twitch Catalog item is played. The Catalog always stores the Twitch Clip URL and ID. Both stores a local copy and plays the local copy.", "Twitch", "rts.actionreplay.twitch.playbackMode", new[] { "Twitch URL", "Download Locally", "Both" }, "Download Locally");
        ui.AddFolderPicker("Twitch Clip Folder", "Folder used only for downloaded Twitch Clips. It must be separate from the OBS Replay Folder so Twitch downloads cannot trigger the OBS replay watcher.", "Twitch", "rts.actionreplay.twitch.folder", "");
        ui.AddTextbox("Twitch HTTP Mapping", "Streamer.bot HTTP path mapped to the Twitch Clip Folder. Add this mapping separately in Streamer.bot's HTTP Server settings.", "Twitch", "rts.actionreplay.twitch.httpMapping", "twitch", false);
        ui.AddNumericTextbox("Clip Duration", "Duration used by !twitchclip, in seconds. Twitch allows 5–60 seconds.", "Twitch", "rts.actionreplay.twitch.clipDuration", 30, 5, 60);
        ui.AddTitle("Twitch Clip URLs are always retained in the Catalog. Twitch URL playback uses a fresh Twitch media URL at playback time. Download Locally and Both use the separate Twitch Clip Folder served through the Twitch HTTP Mapping. The hourly SyncTwitchClips action follows the same storage rules but never plays newly discovered clips.", "Twitch");
        ui.EndSection();
    }

    private void AddPlayerSettings(RtsUI ui)
    {
        ui.BeginSection("Playback", "Player");
        ui.BeginRow();
        ui.AddToggleSwitch("Show Controls", "Display the visual player status bar. It is not interactive.", "Player", "rts.actionreplay.showControls", false);
        ui.AddToggleSwitch("Show Progress Bar", "Display the non-interactive playback progress bar.", "Player", "rts.actionreplay.showProgress", true);
        ui.EndRow();
        ui.BeginRow();
        ui.AddDecimalTextbox("Default Playback Speed", "Playback speed applied when a replay is loaded.", "Player", "rts.actionreplay.playbackSpeed", 1.0, 0.25, 2.0, 0.25);
        ui.AddDropdown("Show Visibility", "Choose when the playback speed indicator is shown.", "Player", "rts.actionreplay.playbackSpeedVisibility", new[] { "Always", "Only when greater or less than 1", "Never" }, "Only when greater or less than 1");
        ui.EndRow();
        ui.EndSection();

        ui.BeginSection("Replay Title", "Player");
        ui.AddToggleSwitch("Show Replay Title", "Display the replay title on the video.", "Player", "rts.actionreplay.showTitle", true);
        ui.AddDropdown("Title Decoration Position", "Choose whether the title decoration appears before or after the replay title.", "Player", "rts.actionreplay.titleDecorationPosition", new[] { "Prefix", "Suffix" }, "Suffix");
        ui.AddTextbox("Title Decoration", "Optional text added to the replay title on the player only.", "Player", "rts.actionreplay.titleDecoration", " - Replay Capture", false);
        ui.BeginRow();
        ui.AddDropdown("Title Bar Style", "Choose the visual design used for the replay title.", "Player", "rts.actionreplay.titleBarStyle", new[] { "Broadcast", "Cinematic", "Cut", "Minimal" }, "Broadcast");
        ui.AddDropdown("Title Position", "Place the title bar at the top or bottom of the video.", "Player", "rts.actionreplay.titlePosition", new[] { "Top", "Bottom" }, "Bottom");
        ui.EndRow();
        ui.AddDropdown("Title Appearance", "Choose how the title enters and exits.", "Player", "rts.actionreplay.titleAnimation", new[] { "Fade", "Left to right", "Right to left", "Slide up/down" }, "Slide up/down");
        ui.BeginRow();
        ui.AddNumericTextbox("Title Show Delay", "Delay before the replay title appears, in milliseconds.", "Player", "rts.actionreplay.titleDelay", 0, 0, 10000);
        ui.AddNumericTextbox("Title Display Duration", "How long the replay title remains visible, in milliseconds. Zero keeps it visible.", "Player", "rts.actionreplay.titleDuration", 5000, 0, 60000);
        ui.AddNumericTextbox("Title Animation Duration", "Duration of the title entrance and exit animation, in milliseconds.", "Player", "rts.actionreplay.titleAnimationDuration", 450, 0, 5000);
        ui.EndRow();
        ui.EndSection();

        ui.BeginSection("Typography", "Player");
        ui.BeginRow();
        ui.AddGoogleFontSelector("Title Font", "Choose the Google Font used by all replay title variants.", "Player", "rts.actionreplay.titleFont", "Inter");
        ui.AddNumericTextbox("Title Font Size", "Replay title font size in pixels.", "Player", "rts.actionreplay.titleFontSize", 34, 12, 96);
        ui.EndRow();
        ui.BeginRow();
        ui.AddColorPicker("Title Font Color", "Replay title text colour.", "Player", "rts.actionreplay.titleTextColor", "#FFFFFFFF");
        ui.AddColorPicker("Title Shadow Color", "Replay title shadow colour.", "Player", "rts.actionreplay.titleShadowColor", "#000000FF");
        ui.EndRow();
        ui.EndSection();
    }

    private void AddAppearanceSettings(RtsUI ui)
    {
        ui.BeginSection("Player Frame", "Appearance");
        ui.BeginRow();
        ui.AddColorPicker("Frame Color", "Main player frame and progress colour.", "Appearance", "rts.actionreplay.frameColor", "#0384CBFF");
        ui.AddSlider("Border Width", "Width of the player border in pixels.", "Appearance", "rts.actionreplay.borderWidth", 0, 12, 4);
        ui.AddSlider("Corner Radius", "Round the player corners in pixels.", "Appearance", "rts.actionreplay.cornerRadius", 0, 48, 0);
        ui.AddToggleSwitch("Border Glow", "Add a branded glow around the player border.", "Appearance", "rts.actionreplay.borderGlow", true);
        ui.EndRow();
        ui.EndSection();
    }

    private void AddPositionSettings(RtsUI ui)
    {
        ui.BeginSection("Saved Positions", "Positions");
        ui.BeginRow(3, 2);
        ui.AddPositionEditor("Saved Positions", "Create and edit reusable player positions. Full Screen is built in and cannot be deleted.", "Positions", "rts.actionreplay.positions", "{\"Full Screen\":{\"name\":\"Full Screen\",\"tag\":\"full-screen\",\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}", null, PreviewPosition);
        ui.AddList("Position Tags", "", "Positions", BuildPositionTagList(CPH.GetGlobalVar<string>("rts.actionreplay.positions", true)));
        ui.EndRow();

        AddAnimationProfileSettings(ui);
        ui.EndSection();
    }

    private void AddAnimationProfileSettings(RtsUI ui)
    {
        // Use the explicit overload so this section can establish a parent even if
        // the section stack has been cleared by another UI definition helper.
        // When Saved Positions is active this remains nested under it; otherwise it
        // safely becomes a Positions section and the profiles remain nested within it.
        ui.BeginSection("Animation Profiles", "Positions");
        AddAnimationProfile(ui, "Default", "default");
        AddAnimationProfile(ui, "Twitch Clip", "twitchClip");
        AddAnimationProfile(ui, "OBS Clip", "obsClip");
        AddAnimationProfile(ui, "Playlist", "playlist");
        AddAnimationProfile(ui, "Recent", "recent");
        ui.EndSection();
    }

    private void AddAnimationProfile(RtsUI ui, string name, string slug)
    {
        ui.BeginSection(name);
        ui.BeginRow();
        ui.AddPositionSelector("Start Position", "Position used when this source starts showing.", "Positions", "rts.actionreplay.animation." + slug + ".startPosition", "rts.actionreplay.positions", "Full Screen");
        ui.AddPositionSelector("End Position", "Position reached when this source finishes showing.", "Positions", "rts.actionreplay.animation." + slug + ".endPosition", "rts.actionreplay.positions", "Full Screen");
        ui.EndRow();
        ui.BeginRow();
        ui.AddNumericTextbox("Duration", "Duration of the movement between Start Position and End Position, in milliseconds.", "Positions", "rts.actionreplay.animation." + slug + ".duration", 500, 100, 20000);
        ui.AddDropdown("Easing", "CSS easing used for this source's player movement.", "Positions", "rts.actionreplay.animation." + slug + ".easing", new[] { "linear", "ease", "ease-in", "ease-out", "ease-in-out" }, "ease-in-out");
        ui.EndRow();
        ui.EndSection();
    }

    private void EnsureAnimationProfiles()
    {
        var start = CPH.GetGlobalVar<string>("rts.actionreplay.defaultStartPosition", true) ?? "Full Screen";
        var end = CPH.GetGlobalVar<string>("rts.actionreplay.defaultEndPosition", true) ?? "Full Screen";
        var duration = GetSettingDouble("rts.actionreplay.animationDuration", 500);
        var easing = CPH.GetGlobalVar<string>("rts.actionreplay.animationEasing", true) ?? "ease-in-out";
        var profiles = new[] { "default", "twitchClip", "obsClip", "playlist", "recent" };
        foreach (var slug in profiles)
        {
            SetIfMissing("rts.actionreplay.animation." + slug + ".startPosition", start);
            SetIfMissing("rts.actionreplay.animation." + slug + ".endPosition", end);
            SetIfMissing("rts.actionreplay.animation." + slug + ".duration", duration);
            SetIfMissing("rts.actionreplay.animation." + slug + ".easing", easing);
        }
    }

    private void SetIfMissing(string key, string value)
    {
        var existing = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(existing)) CPH.SetGlobalVar(key, value, true);
    }

    private void SetIfMissing(string key, double value)
    {
        var existing = CPH.GetGlobalVar<double?>(key, true);
        if (!existing.HasValue) CPH.SetGlobalVar(key, value, true);
    }

    private void AddMessageSettings(RtsUI ui)
    {
        ui.BeginSection("Clapperboard", "Messages");
        ui.BeginRow();
        ui.AddColorPicker("Board Color", "Clapperboard slate colour.", "Messages", "rts.actionreplay.clapper.boardColor", "#101416");
        ui.AddColorPicker("Accent Color", "Clapperboard accent colour.", "Messages", "rts.actionreplay.clapper.accent", "#0384CB");
        ui.EndRow();
        ui.BeginRow();
        ui.AddColorPicker("Stripe Light", "Clapperstick light stripe colour.", "Messages", "rts.actionreplay.clapper.stripeLight", "#EEEEEE");
        ui.AddColorPicker("Stripe Dark", "Clapperstick dark stripe colour.", "Messages", "rts.actionreplay.clapper.stripeDark", "#111111");
        ui.EndRow();
        ui.BeginRow();
        ui.AddGoogleFontSelector("Font", "Choose a Google Font.", "Messages", "rts.actionreplay.clapper.font", "Inter");
        ui.AddColorPicker("Text Color", "Message text colour.", "Messages", "rts.actionreplay.clapper.textColor", "#0384CB");
        ui.EndRow();
        ui.AddSlider("Size (%)", "Overall clapperboard size.", "Messages", "rts.actionreplay.clapper.size", 0, 100, 50);
        ui.BeginRow();
        ui.AddSlider("Position X (%)", "Horizontal clapperboard position.", "Messages", "rts.actionreplay.clapper.positionX", 0, 100, 50);
        ui.AddSlider("Position Y (%)", "Vertical clapperboard position.", "Messages", "rts.actionreplay.clapper.positionY", 0, 100, 50);
        ui.EndRow();
        ui.EndSection();

        ui.BeginSection("Message Outputs", "Messages");
        AddMessageOutput(ui, "Save Replay", "Replay saved: %replayTitle%.", "rts.actionreplay.message.save");
        AddMessageOutput(ui, "Name Replay", "Replay #%replayNumber% renamed to %replayTitle%.", "rts.actionreplay.message.name");
        AddMessageOutput(ui, "Play Replay", "Playing replay #%replayNumber%: %replayTitle%.", "rts.actionreplay.message.play");
        AddMessageOutput(ui, "Playlist", "%replayPlaylist%", "rts.actionreplay.message.playlist");
        AddMessageOutput(ui, "Creator Leaderboard", "%replayLeaderboard%", "rts.actionreplay.message.creatorLeaderboard");
        AddMessageOutput(ui, "Playback Leaderboard", "%replayLeaderboard%", "rts.actionreplay.message.playbackLeaderboard");
        ui.EndSection();
    }

    private void AddMessageOutput(RtsUI ui, string name, string message, string key)
    {
        ui.BeginRow();
        ui.AddTextbox(name + " Message", "Message sent when this command completes.", "Messages", key + ".text", message, false);
        ui.EndRow();
        ui.BeginRow();
        ui.AddToggleSwitch(name + " - Chat", "Send this message to Twitch chat.", "Messages", key + ".chat", true);
        ui.AddToggleSwitch(name + " - Overlay", "Send this message to the Action Replay overlay.", "Messages", key + ".overlay", false);
        ui.EndRow();
    }

    private void PreviewPosition(string positionName, string positionsJson)
    {
        // Preview callback is intentionally left as the extension-specific integration point.
        CPH.SetArgument("replayPreviewPosition", positionName ?? "");
        CPH.SetArgument("replayPreviewPositions", positionsJson ?? "");
    }

    private string NormalizePositionTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var result = new System.Text.StringBuilder();
        bool hyphen = false;
        foreach (char c in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) { result.Append(c); hyphen = false; }
            else if ((char.IsWhiteSpace(c) || c == '-') && result.Length > 0 && !hyphen) { result.Append('-'); hyphen = true; }
        }
        return result.ToString().Trim('-');
    }

    private string[][] BuildPositionTagList(string json)
    {
        var items = new System.Collections.Generic.List<string[]>();
        try
        {
            var map = Newtonsoft.Json.Linq.JObject.Parse(json ?? "{}");
            foreach (var item in map)
            {
                string name = item.Key;
                var position = item.Value as Newtonsoft.Json.Linq.JObject;
                string tag = position == null ? null : (string)position["tag"];
                string normalizedTag = string.IsNullOrWhiteSpace(tag) ? NormalizePositionTag(name) : NormalizePositionTag(tag);
                items.Add(new[] { name, "→  " + normalizedTag });
            }
        }
        catch { }
        if (items.Count == 0) items.Add(new[] { "Full Screen", "→  full-screen" });
        return items.ToArray();
    }

    private double GetSettingDouble(string key, double fallback)
    {
        var value = CPH.GetGlobalVar<double?>(key, true);
        return value ?? fallback;
    }
}