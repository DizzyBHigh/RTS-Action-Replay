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
        ui.BeginSection("Information Panels", "Information Panels");
        ui.BeginRow(2, 2);
        ui.AddNumericTextbox("Panel Width", "Information panel width in 1920×1080 output pixels.", "Information Panels", "rts.actionreplay.panel.width", 500, 100, 1920);
        ui.AddNumericTextbox("Panel Height", "Information panel height in 1920×1080 output pixels.", "Information Panels", "rts.actionreplay.panel.height", 700, 100, 1080);
        ui.EndRow();
        ui.BeginRow(2, 2);
        ui.AddPositionEditor("Panel Positions", "Create and edit reusable information-panel positions. The preview represents the configured panel size on the 640×360 editor canvas.", "Information Panels", "rts.actionreplay.panel.positions", "{\"Center\":{\"name\":\"Center\",\"tag\":\"center\",\"scale\":100,\"x\":0,\"y\":0,\"rotateZ\":0},\"Top\":{\"name\":\"Top\",\"tag\":\"top\",\"scale\":100,\"x\":0,\"y\":32,\"rotateZ\":0},\"Bottom\":{\"name\":\"Bottom\",\"tag\":\"bottom\",\"scale\":100,\"x\":0,\"y\":-32,\"rotateZ\":0},\"Top Left\":{\"name\":\"Top Left\",\"tag\":\"top-left\",\"scale\":100,\"x\":-36,\"y\":28,\"rotateZ\":0},\"Top Right\":{\"name\":\"Top Right\",\"tag\":\"top-right\",\"scale\":100,\"x\":36,\"y\":28,\"rotateZ\":0},\"Bottom Left\":{\"name\":\"Bottom Left\",\"tag\":\"bottom-left\",\"scale\":100,\"x\":-36,\"y\":-28,\"rotateZ\":0},\"Bottom Right\":{\"name\":\"Bottom Right\",\"tag\":\"bottom-right\",\"scale\":100,\"x\":36,\"y\":-28,\"rotateZ\":0}}", "Edit Panel Positions", null, "scale,x,y,rotateX,rotateY,rotateZ", null, null, BuildPanelPreviewSizes());
        ui.AddList("Panel Position Tags", "", "Information Panels", BuildPositionTagList(CPH.GetGlobalVar<string>("rts.actionreplay.panel.positions", true)));
        ui.EndRow(); ui.AddPositionSelector("Panel Position", "Position used by all information panels: Recent Replays, Playlist/Replay Queue, Creator Leaderboard and Playback Leaderboard.", "Information Panels", "rts.actionreplay.panel.position", "rts.actionreplay.panel.positions", "Center");
        AddPanelAnimationSettings(ui);
        ui.EndSection();
        AddAnimationProfileSettings(ui);
    }

    private Dictionary<string, RtsUIPreviewSize> BuildPanelPreviewSizes()
    {
        var width = CPH.GetGlobalVar<int?>("rts.actionreplay.panel.width", true) ?? 500;
        var height = CPH.GetGlobalVar<int?>("rts.actionreplay.panel.height", true) ?? 700;
        var previewSize = new RtsUIPreviewSize(width * 640.0 / 1920.0, height * 360.0 / 1080.0);
        return new Dictionary<string, RtsUIPreviewSize>
        {
            ["Center"] = previewSize,
            ["Top"] = previewSize,
            ["Bottom"] = previewSize,
            ["Top Left"] = previewSize,
            ["Top Right"] = previewSize,
            ["Bottom Left"] = previewSize,
            ["Bottom Right"] = previewSize
        };
    }

    private void AddPanelAnimationSettings(RtsUI ui)
    {
        ui.BeginSection("Panel Animation Profiles", "Information Panels");
        ui.AddTitle("Information panels have their own animation profiles. These profiles never change the replay video animation profiles.", "Information Panels");
        ui.BeginRow();
        ui.AddDropdown("Recent Replays Profile", "Animation profile used when the Recent Replays panel appears and disappears.", "Information Panels", "rts.actionreplay.panel.animation.entry.recent", BuildPanelAnimationProfileOptions(), "Default");
        ui.AddDropdown("Playlist Profile", "Animation profile used by the Playlist / Replay Queue panel.", "Information Panels", "rts.actionreplay.panel.animation.entry.playlist", BuildPanelAnimationProfileOptions(), "Default");
        ui.EndRow();
        ui.BeginRow();
        ui.AddDropdown("Creator Leaderboard Profile", "Animation profile used when the Creator Leaderboard panel appears and disappears.", "Information Panels", "rts.actionreplay.panel.animation.entry.creatorLeaderboard", BuildPanelAnimationProfileOptions(), "Default");
        ui.AddDropdown("Playback Leaderboard Profile", "Animation profile used when the Playback Leaderboard panel appears and disappears.", "Information Panels", "rts.actionreplay.panel.animation.entry.playbackLeaderboard", BuildPanelAnimationProfileOptions(), "Default");
        ui.EndRow();
        ui.AddClickableButton("Add Panel Animation Profile", "Create a new information-panel animation profile.", "Add Panel Profile", "blue", "Information Panels", delegate
        {
            if (CPH.ExecuteMethod("RTS - Action Replay - Core - Animation", "AddPanelProfile")) ui.RebuildUI(delegate(RtsUI rebuiltUi) { BuildSettings(rebuiltUi); });
        });
        foreach (var item in ReadPanelAnimationProfiles())
        {
            var id = (string)item["id"]; if (string.IsNullOrWhiteSpace(id)) continue;
            var name = id == "default" ? "Default" : CPH.GetGlobalVar<string>("rts.actionreplay.panel.animation." + id + ".name", true) ?? (string)item["name"] ?? "New Panel Profile";
            AddPanelAnimationProfile(ui, name, id);
        }
        ui.EndSection();
    }

    private void AddPanelAnimationProfile(RtsUI ui, string title, string profile)
    {
        ui.BeginSection(title, "Information Panels");
        if (profile == "default") ui.AddTitle("Default panel profile is permanent. Its animation sequences can be edited.", "Information Panels");
        else
        {
            ui.AddTextbox("Profile Name", "Display name for this information-panel animation profile.", "Information Panels", "rts.actionreplay.panel.animation." + profile + ".name", title, false);
            ui.AddClickableButton("Remove Profile", "Delete this information-panel animation profile.", "Remove Panel Profile", "red", "Information Panels", delegate
            {
                CPH.SetArgument("panelProfileId", profile);
                if (CPH.ExecuteMethod("RTS - Action Replay - Core - Animation", "RemovePanelProfile")) ui.RebuildUI(delegate(RtsUI rebuiltUi) { BuildSettings(rebuiltUi); });
            });
        }
        AddPanelAnimationSequence(ui, "Start Sequence", "The positions and transitions used when the panel appears.", "rts.actionreplay.panel.animation." + profile + ".startSequence", "Hidden Left");
        AddPanelAnimationSequence(ui, "End Sequence", "The positions and transitions used when the panel disappears.", "rts.actionreplay.panel.animation." + profile + ".endSequence", "__PANEL_POSITION__");
        ui.EndSection();
    }

    private string[] BuildPanelAnimationProfileOptions()
    {
        var options = new List<string>(); foreach (var item in ReadPanelAnimationProfiles()) { var name = (string)item["name"]; if (!string.IsNullOrWhiteSpace(name)) options.Add(name); }
        if (options.Count == 0) options.Add("Default"); return options.ToArray();
    }

    private JArray ReadPanelAnimationProfiles()
    {
        var raw = CPH.GetGlobalVar<string>("rts.actionreplay.panel.animation.profiles", true);
        if (string.IsNullOrWhiteSpace(raw)) return new JArray(new JObject { ["id"] = "default", ["name"] = "Default" });
        try { return JArray.Parse(raw); } catch { return new JArray(new JObject { ["id"] = "default", ["name"] = "Default" }); }
    }

    private void AddPanelAnimationSequence(RtsUI ui, string title, string description, string key, string defaultPosition)
    {
        ui.AddDynamicRows(title, description, "Information Panels", key, rows =>
        {
            rows.AddDropdown("Position", "position", BuildPanelAnimationPositionOptions(), defaultPosition);
            rows.AddNumericTextbox("Duration", "duration", 600, 0, 60000);
            rows.AddDropdown("Easing", "easing", new[] { "linear", "ease", "ease-in", "ease-out", "ease-in-out" }, "ease-in-out");
            rows.AddNumericTextbox("Delay", "delay", 0, 0, 60000);
        });
    }

    private string[] BuildPanelAnimationPositionOptions()
    {
        var options = new List<string> { "Hidden Left", "Hidden Right", "Hidden Top", "Hidden Bottom" };
        var positions = ParsePositions(CPH.GetGlobalVar<string>("rts.actionreplay.panel.positions", true));
        foreach (var item in positions) { var position = item.Value as JObject; if (position == null) continue; var name = (string)position["name"]; if (!string.IsNullOrWhiteSpace(name) && !options.Contains(name)) options.Add(name); }
        if (options.Count == 4) options.Add("Center"); return options.ToArray();
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
            if (CPH.ExecuteMethod("RTS - Action Replay - Core - Animation", "AddProfile")) ui.RebuildUI(delegate(RtsUI rebuiltUi) { BuildSettings(rebuiltUi); });
        });
        foreach (var item in ReadAnimationProfiles())
        {
            var id = (string)item["id"]; if (string.IsNullOrWhiteSpace(id)) continue;
            var name = id == "default" ? "Default" : CPH.GetGlobalVar<string>("rts.actionreplay.animation." + id + ".name", true) ?? (string)item["name"] ?? "New Profile";
            AddAnimationProfile(ui, name, id);
        }
    }

    private string[] BuildAnimationProfileOptions()
    {
        var options = new List<string>(); foreach (var item in ReadAnimationProfiles()) { var id = (string)item["id"]; var name = id == "default" ? "Default" : CPH.GetGlobalVar<string>("rts.actionreplay.animation." + id + ".name", true) ?? (string)item["name"]; if (!string.IsNullOrWhiteSpace(name)) options.Add(name); }
        if (options.Count == 0) options.Add("Default"); return options.ToArray();
    }

    private JArray ReadAnimationProfiles()
    {
        var raw = CPH.GetGlobalVar<string>("rts.actionreplay.animation.profiles", true);
        if (string.IsNullOrWhiteSpace(raw)) return new JArray(new JObject { ["id"] = "default", ["name"] = "Default" });
        try { return JArray.Parse(raw); } catch { return new JArray(new JObject { ["id"] = "default", ["name"] = "Default" }); }
    }

    private void AddAnimationProfile(RtsUI ui, string title, string profile)
    {
        ui.BeginSection(title, "Positions");
        if (profile == "default") ui.AddTitle("Default profile is permanent. Its animation sequences can be edited.", "Positions");
        else
        {
            ui.AddTextbox("Profile Name", "Display name for this animation profile.", "Positions", "rts.actionreplay.animation." + profile + ".name", title, false);
            ui.AddClickableButton("Remove Profile", "Delete this animation profile.", "Remove Profile", "red", "Positions", delegate
            {
                CPH.SetArgument("profileId", profile);
                if (CPH.ExecuteMethod("RTS - Action Replay - Core - Animation", "RemoveProfile")) ui.RebuildUI(delegate(RtsUI rebuiltUi) { BuildSettings(rebuiltUi); });
            });
        }
        AddAnimationSequence(ui, "Start Sequence", "The positions and transitions used when the replay starts.", "rts.actionreplay.animation." + profile + ".startSequence", "Full Screen");
        AddAnimationSequence(ui, "End Sequence", "The positions and transitions used when the replay ends.", "rts.actionreplay.animation." + profile + ".endSequence", "Full Screen");
        ui.EndSection();
    }

    private void AddAnimationSequence(RtsUI ui, string title, string description, string key, string defaultPosition)
    {
        ui.AddDynamicRows(title, description, "Positions", key, rows =>
        {
            rows.AddDropdown("Position", "position", BuildAnimationPositionOptions(), defaultPosition);
            rows.AddNumericTextbox("Duration", "duration", 600, 0, 60000);
            rows.AddDropdown("Easing", "easing", new[] { "linear", "ease", "ease-in", "ease-out", "ease-in-out" }, "ease-in-out");
            rows.AddNumericTextbox("Delay", "delay", 0, 0, 60000);
        });
    }

    private string[] BuildAnimationPositionOptions()
    {
        var options = new List<string> { "Hidden Left", "Hidden Right", "Hidden Top", "Hidden Bottom" };
        var positions = ParsePositions(CPH.GetGlobalVar<string>("rts.actionreplay.positions", true));
        foreach (var item in positions) { var position = item.Value as JObject; if (position == null) continue; var name = (string)position["name"]; if (!string.IsNullOrWhiteSpace(name) && !options.Contains(name)) options.Add(name); }
        if (options.Count == 4) options.Add("Full Screen"); return options.ToArray();
    }

    private static Dictionary<string, JObject> ParsePositions(string json)
    {
        var result = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            var root = JObject.Parse(json);
            foreach (var item in root) if (item.Value is JObject position) result[item.Key] = position;
        }
        catch { }
        return result;
    }

    private static string BuildPositionTagList(string json)
    {
        var positions = ParsePositions(json); var lines = new List<string>();
        foreach (var item in positions)
        {
            var position = item.Value; var name = (string)position["name"] ?? item.Key; var tag = (string)position["tag"] ?? "";
            if (!string.IsNullOrWhiteSpace(name)) lines.Add(name + " = " + tag);
        }
        return string.Join("\n", lines);
    }

    private static void AddMessageSettings(RtsUI ui)
    {
        ui.BeginSection("Messages", "Messages");
        ui.AddTitle("Action Replay messages are configured by the core message action.", "Messages");
        ui.EndSection();
    }
}
