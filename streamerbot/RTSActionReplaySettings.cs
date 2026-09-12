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

        BuildSettings(ui);
        ui.ShowUI();
        return true;
    }

    private void BuildSettings(RtsUI ui)
    {
        AddGeneralSettings(ui); AddBrandingSettings(ui); AddPlaylistSettings(ui); AddTwitchSettings(ui);
        CPH.ExecuteMethod("RTS - Action Replay - Core - Animation", "EnsureProfiles");
        AddPlayerSettings(ui); AddAppearanceSettings(ui); AddPositionSettings(ui); AddMessageSettings(ui);
    }

    private void AddGeneralSettings(RtsUI ui)
    {
        ui.AddThemeSelector("Settings Theme", "Choose the RtsUI theme.", "General", "rts.actionreplay.uiTheme", "Dark");
        ui.BeginSection("Replay Source", "General");
        ui.AddFolderPicker("Replay Folder", "Folder containing OBS Replay Buffer files. Twitch clips use a separate folder and are never written here.", "General", "rts.actionreplay.replayFolder", "");
        ui.AddTextbox("Replay File Types", "Accepted extensions, separated by commas. e.g .mp4, .mov", "General", "rts.actionreplay.replayFileTypes", ".mp4, .mkv", false);
        ui.AddTextbox("HTTP Mapping", "Streamer.bot HTTP path mapped to the OBS replay folder.", "General", "rts.actionreplay.httpMapping", "replays", false);
        ui.AddNumericTextbox("HTTP Port", "Streamer.bot HTTP Server port used to serve replay files.", "General", "rts.actionreplay.httpPort", 7474, 1, 65535);
        ui.EndSection();
    }

    private void AddBrandingSettings(RtsUI ui)
    {
        ui.BeginSection("Branding", "Branding"); ui.BeginSection("Brand Identity");
        ui.AddToggleSwitch("Show Replay Branding", "Display the branding on the top left of the replay screen.", "Branding", "rts.actionreplay.showBranding", true);
        ui.AddTextbox("Branding Logo URL", "HTTPS URL to a logo. Leave blank to use the fallback text.", "Branding", "rts.actionreplay.brandLogoUrl", "", false);
        ui.BeginRow(); ui.AddTextbox("Branding Fallback Text", "Text shown when no branding logo is defined.", "Branding", "rts.actionreplay.brandFallbackText", "RTS", false); ui.AddColorPicker("Branding Fallback Text Color", "Colour of the branding fallback text.", "Branding", "rts.actionreplay.brandFallbackTextColor", "#0384CBFF"); ui.EndRow();
        ui.BeginRow(); ui.AddTextbox("Branding Label", "Text displayed beside the logo or fallback text.", "Branding", "rts.actionreplay.brandLabel", "ACTION REPLAY", false); ui.AddColorPicker("Branding Label Color", "Colour of the branding label.", "Branding", "rts.actionreplay.brandLabelColor", "#FFFFFFFF"); ui.EndRow();
        ui.EndSection(); ui.BeginSection("Accent Colours"); ui.BeginRow();
        ui.AddColorPicker("Primary Colour", "Main Action Replay accent colour used throughout the player.", "Branding", "rts.actionreplay.titlePrimaryColor", "#0384CBFF");
        ui.AddColorPicker("Secondary Colour", "Secondary Action Replay accent colour used throughout the player.", "Branding", "rts.actionreplay.titleSecondaryColor", "#101416FF");
        ui.EndRow(); ui.EndSection(); ui.EndSection();
    }

    private void AddPlaylistSettings(RtsUI ui)
    {
        ui.BeginSection("Replay Defaults", "Playlist"); ui.AddTextbox("Replay Title Template", "Default title for new replays. Streamer.bot variables can be used.", "Playlist", "rts.actionreplay.replayTitle", "%replayName%", false); ui.AddTextbox("New Replay Display Title", "Title shown when a newly discovered replay is automatically played.", "Playlist", "rts.actionreplay.newReplayTitle", "New Replay", false); ui.EndSection();
        ui.BeginSection("Recent Clips", "Playlist"); ui.AddSlider("Maximum Recent Clips", "Maximum number of newest Catalog items shown as Recent Clips. Older Catalog items are not deleted.", "Playlist", "rts.actionreplay.maxHistory", 1, 100, 20); ui.BeginRow(); ui.AddToggleSwitch("Auto-add Saved Replays", "Add each newly saved OBS replay to the Catalog and Recent Clips.", "Playlist", "rts.actionreplay.autoAdd", true); ui.AddToggleSwitch("Auto-play Newest Replay", "Load and play a newly saved OBS replay automatically.", "Playlist", "rts.actionreplay.autoPlay", false); ui.EndRow(); ui.EndSection();
        ui.BeginSection("Live Playlist", "Playlist"); ui.AddToggleSwitch("Persist Playlist Across Restarts", "Keep the current Playlist when Streamer.bot restarts. When disabled, the queue is cleared on restart.", "Playlist", "rts.actionreplay.playlistPersist", false); ui.EndSection();
    }

    private void AddTwitchSettings(RtsUI ui)
    {
        ui.BeginSection("Twitch Clips", "Twitch");
        ui.AddDropdown("Twitch Clip Playback", "How Action Replay obtains Twitch media when a Twitch Catalog item is played. The Catalog always stores the Twitch Clip URL and ID. Both stores a local copy and plays the local copy.", "Twitch", "rts.actionreplay.twitch.playbackMode", new[] { "Twitch URL", "Download Locally", "Both" }, "Download Locally");
        ui.AddFolderPicker("Twitch Clip Folder", "Folder used only for downloaded Twitch Clips. It must be separate from the OBS Replay Folder so Twitch downloads cannot trigger the OBS replay watcher.", "Twitch", "rts.actionreplay.twitch.folder", "");
        ui.AddTextbox("Twitch HTTP Mapping", "Streamer.bot HTTP path mapped to the Twitch Clip Folder. Add this mapping separately in Streamer.bot's HTTP Server settings.", "Twitch", "rts.actionreplay.twitch.httpMapping", "twitch", false);
        ui.AddNumericTextbox("Clip Duration", "Default duration used by !twitchclip, in seconds. Twitch allows 5–60 seconds.", "Twitch", "rts.actionreplay.twitch.clipDuration", 30, 5, 60);
        ui.AddTitle("Twitch Clip URLs are always retained in the Catalog. Twitch URL playback uses a fresh Twitch media URL at playback time. Download Locally and Both use the separate Twitch Clip Folder served through the Twitch HTTP Mapping. The hourly SyncTwitchClips action follows the same storage rules but never plays newly discovered clips.", "Twitch"); ui.EndSection();
    }

    private void AddPlayerSettings(RtsUI ui)
    {
        ui.BeginSection("Playback", "Player"); ui.BeginRow(); ui.AddToggleSwitch("Show Controls", "Display the visual player status bar. It is not interactive.", "Player", "rts.actionreplay.showControls", false); ui.AddToggleSwitch("Show Progress Bar", "Display the non-interactive playback progress bar.", "Player", "rts.actionreplay.showProgress", true); ui.EndRow();
        ui.BeginRow(); ui.AddDecimalTextbox("Default Playback Speed", "Playback speed applied when a replay is loaded.", "Player", "rts.actionreplay.playbackSpeed", 1.0, 0.25, 2.0, 0.25); ui.AddDropdown("Show Visibility", "Choose when the playback speed indicator is shown.", "Player", "rts.actionreplay.playbackSpeedVisibility", new[] { "Always", "Only when greater or less than 1", "Never" }, "Only when greater or less than 1"); ui.EndRow(); ui.EndSection();
        ui.BeginSection("Replay Title", "Player"); ui.AddToggleSwitch("Show Replay Title", "Display the replay title on the video.", "Player", "rts.actionreplay.showTitle", true); ui.AddDropdown("Title Decoration Position", "Choose whether the title decoration appears before or after the replay title.", "Player", "rts.actionreplay.titleDecorationPosition", new[] { "Prefix", "Suffix" }, "Suffix"); ui.AddTextbox("Title Decoration", "Optional text added to the replay title on the player only.", "Player", "rts.actionreplay.titleDecoration", " - Replay Capture", false); ui.BeginRow(); ui.AddDropdown("Title Bar Style", "Choose the visual design used for the replay title.", "Player", "rts.actionreplay.titleBarStyle", new[] { "Broadcast", "Cinematic", "Cut", "Minimal" }, "Broadcast"); ui.AddDropdown("Title Position", "Place the title bar at the top or bottom of the video.", "Player", "rts.actionreplay.titlePosition", new[] { "Top", "Bottom" }, "Bottom"); ui.EndRow(); ui.AddDropdown("Title Appearance", "Choose how the title enters and exits.", "Player", "rts.actionreplay.titleAnimation", new[] { "Fade", "Left to right", "Right to left", "Slide up/down" }, "Slide up/down"); ui.BeginRow(); ui.AddNumericTextbox("Title Show Delay", "Delay before the replay title appears, in milliseconds.", "Player", "rts.actionreplay.titleDelay", 0, 0, 10000); ui.AddNumericTextbox("Title Display Duration", "How long the replay title remains visible, in milliseconds. Zero keeps it visible.", "Player", "rts.actionreplay.titleDuration", 5000, 0, 60000); ui.AddNumericTextbox("Title Animation Duration", "Duration of the title entrance and exit animation, in milliseconds.", "Player", "rts.actionreplay.titleAnimationDuration", 450, 0, 5000); ui.EndRow(); ui.EndSection();
        ui.BeginSection("Typography", "Player"); ui.BeginRow(); ui.AddGoogleFontSelector("Title Font", "Choose the Google Font used by all replay title variants.", "Player", "rts.actionreplay.titleFont", "Inter"); ui.AddNumericTextbox("Title Font Size", "Replay title font size in pixels.", "Player", "rts.actionreplay.titleFontSize", 34, 12, 96); ui.EndRow(); ui.BeginRow(); ui.AddColorPicker("Title Font Color", "Replay title text colour.", "Player", "rts.actionreplay.titleTextColor", "#FFFFFFFF"); ui.AddColorPicker("Title Shadow Color", "Replay title shadow colour.", "Player", "rts.actionreplay.titleShadowColor", "#000000FF"); ui.EndRow(); ui.EndSection();
    }

    private void AddAppearanceSettings(RtsUI ui)
    {
        ui.BeginSection("Player Frame", "Appearance"); ui.BeginRow(); ui.AddColorPicker("Frame Color", "Main player frame and progress colour.", "Appearance", "rts.actionreplay.frameColor", "#0384CBFF"); ui.AddToggleSwitch("Border Glow", "Add a branded glow around the player border.", "Appearance", "rts.actionreplay.borderGlow", true); ui.EndRow(); ui.BeginRow(); ui.AddSlider("Border Width", "Width of the player border in pixels.", "Appearance", "rts.actionreplay.borderWidth", 0, 12, 4); ui.AddSlider("Corner Radius", "Round the player corners in pixels.", "Appearance", "rts.actionreplay.cornerRadius", 0, 48, 0); ui.EndRow(); ui.EndSection();
        ui.BeginSection("Title Settings", "Appearance"); ui.BeginSection("Broadcast"); ui.BeginRow(); ui.AddColorPicker("Primary Chevron Colour", "Broadcast Primary colour.", "Appearance", "rts.actionreplay.broadcast.primaryColor", "#0384CBFF"); ui.AddColorPicker("Secondary Chevron Colour", "Broadcast Secondary colour.", "Appearance", "rts.actionreplay.broadcast.secondaryColor", "#FFD400FF"); ui.EndRow(); ui.BeginRow(); ui.AddNumericTextbox("Chevron Height", "Height of each Broadcast chevron in pixels. Maximum is limited to the Broadcast clipping area.", "Appearance", "rts.actionreplay.broadcast.chevronHeight", 42, 1, 89); ui.AddToggleSwitch("Random", "Randomize each Broadcast chevron height between 1 and the configured height.", "Appearance", "rts.actionreplay.broadcast.randomHeight", false); ui.AddNumericTextbox("Chevron Width", "Width of each Broadcast chevron in pixels.", "Appearance", "rts.actionreplay.broadcast.chevronWidth", 42, 1, 300); ui.AddToggleSwitch("Random", "Randomize each Broadcast chevron width between 1 and the configured width.", "Appearance", "rts.actionreplay.broadcast.randomWidth", false); ui.EndRow(); ui.BeginRow(); ui.AddNumericTextbox("Chevron Spacing", "Visible gap between Broadcast chevrons in pixels. Zero means the chevrons touch with no dark gap.", "Appearance", "rts.actionreplay.broadcast.chevronSpacing", 0, 0, 200); ui.AddToggleSwitch("Random", "Randomize each Broadcast chevron gap between 0 and the configured spacing.", "Appearance", "rts.actionreplay.broadcast.randomSpacing", false); ui.EndRow(); ui.AddNumericTextbox("Chevron Speed", "Broadcast chevron movement speed in pixels per second.", "Appearance", "rts.actionreplay.broadcast.chevronSpeed", 95, 10, 500); ui.BeginRow(); ui.AddColorPicker("Title Prefix / Suffix Colour", "Colour of the Broadcast title decoration.", "Appearance", "rts.actionreplay.broadcast.decorationColor", "#0384CBFF"); ui.AddColorPicker("Title Colour", "Colour of the Broadcast title text.", "Appearance", "rts.actionreplay.broadcast.titleColor", "#FFFFFFFF"); ui.EndRow(); ui.EndSection();
        ui.BeginSection("Cut"); ui.BeginRow(); ui.AddColorPicker("Primary Colour", "Cut Primary colour.", "Appearance", "rts.actionreplay.cut.primaryColor", "#0384CBFF"); ui.AddColorPicker("Secondary Colour", "Cut Secondary colour.", "Appearance", "rts.actionreplay.cut.secondaryColor", "#FFD400FF"); ui.EndRow(); ui.BeginRow(); ui.AddNumericTextbox("Block Width", "Cut block width in pixels.", "Appearance", "rts.actionreplay.cut.blockWidth", 170, 1, 1000); ui.AddToggleSwitch("Random", "Randomize each Cut block width between 1 and the configured width.", "Appearance", "rts.actionreplay.cut.randomWidth", true); ui.AddNumericTextbox("Bar Height", "Cut accent bar height in pixels.", "Appearance", "rts.actionreplay.cut.barHeight", 5, 1, 50); ui.EndRow(); ui.BeginRow(); ui.AddColorPicker("Title Prefix / Suffix Colour", "Colour of the Cut title decoration.", "Appearance", "rts.actionreplay.cut.decorationColor", "#0384CBFF"); ui.AddColorPicker("Title Colour", "Colour of the Cut title text.", "Appearance", "rts.actionreplay.cut.titleColor", "#FFFFFFFF"); ui.EndRow(); ui.EndSection(); ui.EndSection();
    }

    private void AddPositionSettings(RtsUI ui)
    {
        ui.BeginSection("Saved Positions", "Positions"); ui.BeginRow(3, 2); ui.AddPositionEditor("Saved Positions", "Create and edit reusable player positions. Full Screen is built in and cannot be deleted.", "Positions", "rts.actionreplay.positions", "{\"Full Screen\":{\"name\":\"Full Screen\",\"tag\":\"full-screen\",\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}", "Edit Positions", null, "scale,x,y,rotateX,rotateY,rotateZ", null, null); ui.AddList("Position Tags", "", "Positions", BuildPositionTagList(CPH.GetGlobalVar<string>("rts.actionreplay.positions", true))); ui.EndRow(); ui.EndSection();
        AddAnimationProfileSettings(ui);
    }

    private void AddAnimationProfileSettings(RtsUI ui)
    {
        ui.AddTitle("Animation Profiles", "Positions");
        ui.AddDropdown("Default Animation Profile", "Fallback profile used when no entry point has a specific profile configured.", "Positions", "rts.actionreplay.animation.selectedProfile", BuildAnimationProfileOptions(), "Default");
        ui.BeginSection("Entry Point Profiles", "Positions");
        ui.AddTitle("Choose which animation profile each replay entry point uses. Default is used unless you select another profile. Start animation only runs when the player is hidden; the selected profile also supplies the exit sequence when that playback session hides the player.", "Positions");
        ui.BeginRow();
        ui.AddDropdown("Create — OBS", "Animation profile used when a newly captured OBS replay is automatically played.", "Positions", "rts.actionreplay.animation.entry.obs", BuildAnimationProfileOptions(), "Default");
        ui.AddDropdown("Create — Twitch", "Animation profile used when a newly created Twitch Clip is automatically played.", "Positions", "rts.actionreplay.animation.entry.twitch", BuildAnimationProfileOptions(), "Default");
        ui.EndRow();
        ui.BeginRow();
        ui.AddDropdown("Play — Recent", "Animation profile used when a replay is started from Recent Clips.", "Positions", "rts.actionreplay.animation.entry.recent", BuildAnimationProfileOptions(), "Default");
        ui.AddDropdown("Play — Catalog", "Animation profile used when a replay is started from the Catalog.", "Positions", "rts.actionreplay.animation.entry.catalog", BuildAnimationProfileOptions(), "Default");
        ui.EndRow();
        ui.AddDropdown("Play — Playlist", "Animation profile used when starting a populated Playlist while the player is hidden.", "Positions", "rts.actionreplay.animation.entry.playlist", BuildAnimationProfileOptions(), "Default");
        ui.EndSection();
        ui.AddClickableButton("Add Profile", "Create a new animation profile.", "Add Profile", "blue", "Positions", delegate
        {
            if (CPH.ExecuteMethod("RTS - Action Replay - Core - Animation", "AddProfile"))
            {
                ui.RebuildUI(delegate(RtsUI rebuiltUi) { BuildSettings(rebuiltUi); });
            }
        });

        foreach (var item in ReadAnimationProfiles())
        {
            var id = (string)item["id"]; if (string.IsNullOrWhiteSpace(id)) continue;
            var name = id == "default" ? "Default" : CPH.GetGlobalVar<string>("rts.actionreplay.animation." + id + ".name", true) ?? (string)item["name"] ?? "New Profile";
            AddAnimationProfile(ui, name, id);
        }
    }

    private void AddAnimationProfile(RtsUI ui, string title, string profile)
    {
        ui.BeginSection(title, "Positions");
        if (profile == "default") ui.AddTitle("Default profile is permanent and cannot be renamed or deleted. Its animation sequences can be edited.", "Positions");
        else
        {
            ui.AddTextbox("Profile Name", "Display name for this animation profile.", "Positions", "rts.actionreplay.animation." + profile + ".name", title, false);
            ui.AddClickableButton("Remove Profile", "Delete this animation profile.", "Remove Profile", "red", "Positions", delegate
            {
                CPH.SetArgument("profileId", profile);
                if (CPH.ExecuteMethod("RTS - Action Replay - Core - Animation", "RemoveProfile")) ui.RebuildUI(delegate(RtsUI rebuiltUi) { BuildSettings(rebuiltUi); });
            });
        }
        AddAnimationSequence(ui, "Start Sequence", "The positions and transitions used when the replay starts.", "rts.actionreplay.animation." + profile + ".startSequence", GetStartDefaults(profile));
        AddAnimationSequence(ui, "End Sequence", "The positions and transitions used when the replay ends.", "rts.actionreplay.animation." + profile + ".endSequence", GetEndDefaults());
        ui.EndSection();
    }

    private string[] BuildAnimationProfileOptions()
    {
        var options = new List<string>();
        foreach (var item in ReadAnimationProfiles())
        {
            var id = (string)item["id"];
            var name = id == "default" ? "Default" : CPH.GetGlobalVar<string>("rts.actionreplay.animation." + id + ".name", true) ?? (string)item["name"];
            if (!string.IsNullOrWhiteSpace(name)) options.Add(name);
        }
        if (options.Count == 0) options.Add("Default");
        return options.ToArray();
    }

    private JArray ReadAnimationProfiles()
    {
        var raw = CPH.GetGlobalVar<string>("rts.actionreplay.animation.profiles", true);
        if (string.IsNullOrWhiteSpace(raw)) return new JArray(new JObject { ["id"] = "default", ["name"] = "Default" });
        try { return JArray.Parse(raw); } catch { return new JArray(new JObject { ["id"] = "default", ["name"] = "Default" }); }
    }

    private void AddAnimationSequence(RtsUI ui, string title, string description, string key, string defaultPosition)
    {
        ui.AddDynamicRows(title, description, "Positions", key, rows =>
        {
            rows.AddDropdown("Position", "position", BuildPositionOptions(), defaultPosition);
            rows.AddNumericTextbox("Duration", "duration", 1000, 0, 60000);
            rows.AddDropdown("Easing", "easing", new[] { "linear", "ease", "ease-in", "ease-out", "ease-in-out" }, "ease-in-out");
            rows.AddNumericTextbox("Delay", "delay", 0, 0, 60000);
        });
    }

    private string GetStartDefaults(string profile) => profile == "default" ? "Mini Hidden" : "Full Screen";
    private string GetEndDefaults() => "Mini Hidden";

    private string[] BuildPositionOptions()
    {
        var positions = ParsePositions(CPH.GetGlobalVar<string>("rts.actionreplay.positions", true)); var options = new List<string>();
        foreach (var item in positions) { var position = item.Value as JObject; if (position == null) continue; var name = (string)position["name"]; if (!string.IsNullOrWhiteSpace(name) && !options.Contains(name)) options.Add(name); }
        if (options.Count == 0) options.Add("Full Screen"); return options.ToArray();
    }

    private void AddMessageSettings(RtsUI ui)
    {
        ui.BeginSection("Clapperboard", "Messages");
        ui.BeginRow();
        ui.AddColorPicker("Board Color", "Clapperboard slate colour.", "Messages", "rts.actionreplay.clapper.boardColor", "#101416");
        ui.AddColorPicker("Stripe Light", "Clapperstick light stripe colour.", "Messages", "rts.actionreplay.clapper.stripeLight", "#EEEEEE");
        ui.AddColorPicker("Stripe Dark", "Clapperstick dark stripe colour.", "Messages", "rts.actionreplay.clapper.stripeDark", "#111111");
        ui.AddColorPicker("Accent Color", "Clapperboard accent colour.", "Messages", "rts.actionreplay.clapper.accent", "#0384CB");
        ui.AddColorPicker("Text Color", "Message text colour.", "Messages", "rts.actionreplay.clapper.textColor", "#0384CB");
        ui.EndRow();
        ui.AddGoogleFontSelector("Font", "Choose a Google Font.", "Messages", "rts.actionreplay.clapper.font", "Inter");
        ui.BeginRow();
        ui.AddSlider("Size (%)", "Overall clapperboard size.", "Messages", "rts.actionreplay.clapper.size", 0, 100, 50);
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

    private string[][] BuildPositionTagList(string json)
    {
        var positions = ParsePositions(json); var rows = new List<string[]>();
        foreach (var item in positions) { var position = item.Value as JObject; if (position == null) continue; rows.Add(new string[] { (string)position["tag"] ?? "", (string)position["name"] ?? "" }); }
        return rows.ToArray();
    }

    private JObject ParsePositions(string json) { if (string.IsNullOrWhiteSpace(json)) return new JObject(); try { return JObject.Parse(json); } catch { return new JObject(); } }
}