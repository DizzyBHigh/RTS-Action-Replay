using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

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

        ui.BeginSection("Title Settings", "Appearance");
        ui.BeginSection("Broadcast");
        ui.BeginRow();
        ui.AddColorPicker("Primary Colour", "Primary colour for Broadcast chevrons and accents.", "Appearance", "rts.actionreplay.broadcast.primaryColor", "#0384CBFF");
        ui.AddColorPicker("Secondary Colour", "Secondary colour for Broadcast chevrons and accents.", "Appearance", "rts.actionreplay.broadcast.secondaryColor", "#FFD400FF");
        ui.EndRow();
        ui.BeginRow();
        ui.AddNumericTextbox("Chevron Height", "Height of each Broadcast chevron in pixels. The chevron width is derived from this height.", "Appearance", "rts.actionreplay.broadcast.chevronHeight", 42, 1, 200);
        ui.AddToggleSwitch("Random", "Randomize each Broadcast chevron height between 1 and the configured height.", "Appearance", "rts.actionreplay.broadcast.randomHeight", false);
        ui.AddNumericTextbox("Chevron Spacing", "Visible gap between Broadcast chevrons in pixels. Zero means the chevrons touch with no dark gap.", "Appearance", "rts.actionreplay.broadcast.chevronSpacing", 0, 0, 200);
        ui.AddToggleSwitch("Random", "Randomize each Broadcast chevron gap between 0 and the configured spacing.", "Appearance", "rts.actionreplay.broadcast.randomSpacing", false);
        ui.EndRow();
        ui.AddNumericTextbox("Chevron Speed", "Broadcast chevron movement speed in pixels per second.", "Appearance", "rts.actionreplay.broadcast.chevronSpeed", 95, 10, 500);
        ui.BeginRow();
        ui.AddColorPicker("Title Prefix / Suffix Colour", "Colour of the Broadcast title decoration.", "Appearance", "rts.actionreplay.broadcast.decorationColor", "#0384CBFF");
        ui.AddColorPicker("Title Colour", "Colour of the Broadcast title text.", "Appearance", "rts.actionreplay.broadcast.titleColor", "#FFFFFFFF");
        ui.EndRow();
        ui.EndSection();

        ui.BeginSection("Cut");
        ui.BeginRow();
        ui.AddColorPicker("Primary Colour", "Primary colour for Cut blocks and accents.", "Appearance", "rts.actionreplay.cut.primaryColor", "#0384CBFF");
        ui.AddColorPicker("Secondary Colour", "Secondary colour for Cut blocks and accents.", "Appearance", "rts.actionreplay.cut.secondaryColor", "#FFD400FF");
        ui.EndRow();
        ui.BeginRow();
        ui.AddNumericTextbox("Block Width", "Cut block width in pixels.", "Appearance", "rts.actionreplay.cut.blockWidth", 170, 1, 1000);
        ui.AddToggleSwitch("Random", "Randomize each Cut block width between 1 and the configured width.", "Appearance", "rts.actionreplay.cut.randomWidth", true);
        ui.AddNumericTextbox("Bar Height", "Cut accent bar height in pixels.", "Appearance", "rts.actionreplay.cut.barHeight", 5, 1, 50);
        ui.EndRow();
        ui.BeginRow();
        ui.AddColorPicker("Title Prefix / Suffix Colour", "Colour of the Cut title decoration.", "Appearance", "rts.actionreplay.cut.decorationColor", "#0384CBFF");
        ui.AddColorPicker("Title Colour", "Colour of the Cut title text.", "Appearance", "rts.actionreplay.cut.titleColor", "#FFFFFFFF");
        ui.EndRow();
        ui.EndSection();
        ui.EndSection();
    }

    private void AddPositionSettings(RtsUI ui)
    {
        ui.BeginSection("Saved Positions", "Positions");
        ui.BeginRow(3, 2);
        ui.AddPositionEditor("Saved Positions", "Create and edit reusable player positions. Full Screen is built in and cannot be deleted.", "Positions", "rts.actionreplay.positions", "{\"Full Screen\":{\"name\":\"Full Screen\",\"tag\":\"full-screen\",\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}", "Edit Positions", null, "scale,x,y,rotateX,rotateY,rotateZ", null, null);
        ui.AddList("Position Tags", "", "Positions", BuildPositionTagList(CPH.GetGlobalVar<string>("rts.actionreplay.positions", true)));
        ui.EndRow();
        ui.EndSection();
        AddAnimationProfileSettings(ui);
    }

    private void AddAnimationProfileSettings(RtsUI ui)
    {
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
        ui.AddPositionSelector("Start Position", "Position used at the start of this playback animation.", "Positions", "rts.actionreplay.animation." + slug + ".startPosition", "rts.actionreplay.positions", "Full Screen");
        ui.AddPositionSelector("End Position", "Position used at the end of this playback animation.", "Positions", "rts.actionreplay.animation." + slug + ".endPosition", "rts.actionreplay.positions", "Full Screen");
        ui.EndRow();
        ui.BeginRow();
        ui.AddDecimalTextbox("Duration", "Animation duration in seconds.", "Positions", "rts.actionreplay.animation." + slug + ".duration", .5, .1, 10, .1);
        ui.AddDropdown("Easing", "Position animation easing.", "Positions", "rts.actionreplay.animation." + slug + ".easing", new[] { "linear", "ease-in", "ease-out", "ease-in-out", "ease" }, "ease-in-out");
        ui.EndRow();
        ui.EndSection();
    }

    private void EnsureAnimationProfiles()
    {
        SetDefault("rts.actionreplay.animation.default.startPosition", "Full Screen");
        SetDefault("rts.actionreplay.animation.default.endPosition", "Full Screen");
        SetDefault("rts.actionreplay.animation.default.duration", .5);
        SetDefault("rts.actionreplay.animation.default.easing", "ease-in-out");
        SetDefault("rts.actionreplay.animation.twitchClip.startPosition", "Mini Right Hidden");
        SetDefault("rts.actionreplay.animation.twitchClip.endPosition", "Mini Right Angled");
        SetDefault("rts.actionreplay.animation.twitchClip.duration", 1.0);
        SetDefault("rts.actionreplay.animation.twitchClip.easing", "ease-in-out");
        SetDefault("rts.actionreplay.animation.obsClip.startPosition", "Mini Right Off Screen");
        SetDefault("rts.actionreplay.animation.obsClip.endPosition", "Mini Right");
        SetDefault("rts.actionreplay.animation.obsClip.duration", 1.0);
        SetDefault("rts.actionreplay.animation.obsClip.easing", "ease-in-out");
        SetDefault("rts.actionreplay.animation.playlist.startPosition", "Center Hidden");
        SetDefault("rts.actionreplay.animation.playlist.endPosition", "Center Large");
        SetDefault("rts.actionreplay.animation.playlist.duration", 1.0);
        SetDefault("rts.actionreplay.animation.playlist.easing", "ease-in-out");
        SetDefault("rts.actionreplay.animation.recent.startPosition", "Mini Right Hidden");
        SetDefault("rts.actionreplay.animation.recent.endPosition", "Full Screen");
        SetDefault("rts.actionreplay.animation.recent.duration", 1.0);
        SetDefault("rts.actionreplay.animation.recent.easing", "ease-in-out");
    }

    private void SetDefault(string key, object value)
    {
        if (CPH.GetGlobalVar<object>(key, true) == null) CPH.SetGlobalVar(key, value, true);
    }

    private string[][] BuildPositionTagList(string raw)
    {
        var result = new List<string[]>();
        if (string.IsNullOrWhiteSpace(raw)) return result.ToArray();
        try
        {
            var positions = JObject.Parse(raw);
            foreach (var item in positions.Properties())
            {
                var tag = (string)item.Value["tag"];
                if (!string.IsNullOrWhiteSpace(tag)) result.Add(new[] { item.Name, tag });
            }
        }
        catch { }
        return result.ToArray();
    }

    private void AddMessageSettings(RtsUI ui)
    {
        ui.BeginSection("Messages", "Messages");
        ui.AddTextbox("Save Confirmation", "Chat message after a replay is saved.", "Messages", "rts.actionreplay.message.save", "Replay saved.", false);
        ui.AddTextbox("Playback Confirmation", "Chat message after a replay starts playing.", "Messages", "rts.actionreplay.message.play", "Playing replay.", false);
        ui.AddTextbox("Hide Confirmation", "Chat message after the replay player is hidden.", "Messages", "rts.actionreplay.message.hide", "Replay hidden.", false);
        ui.EndSection();
    }
}