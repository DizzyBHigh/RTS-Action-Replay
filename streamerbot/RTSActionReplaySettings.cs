using System;
public class CPHInline
{
    public bool Execute()
    {
        var ui = new RtsUI("RTS Action Replay", "1.0.0",
            (key, persisted) => CPH.GetGlobalVar<bool?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<int?>(key, persisted),
            (key, persisted) => ReadUiValue(key),
            (key, persisted) => (object)CPH.GetGlobalVar<string>(key, persisted),
            (key, value, persisted) => SaveUiValue(key, value, persisted),
            message => CPH.LogInfo(message));
        BuildSettings(ui);
        ui.ShowUI();
        return true;
    }

    private void BuildSettings(RtsUI ui)
    {
        AddGeneralSettings(ui); AddBrandingSettings(ui); AddPlaylistSettings(ui); AddTwitchSettings(ui); AddYouTubeSettings(ui);
        AddPlayerSettings(ui); AddMessageSettings(ui);
    }

    private void AddGeneralSettings(RtsUI ui)
    {
        ui.AddThemeSelector("Settings Theme", "Choose the RtsUI theme.", "General", "rts.actionreplay.uiTheme", "Dark");
        ui.BeginSection("Replay Source", "General"); ui.AddFolderPicker("Replay Folder", "Folder containing local OBS Replay Buffer files. Downloaded Twitch and Kick clips use their own separate folders and are never stored here.", "General", "rts.actionreplay.replayFolder", ""); ui.AddTextbox("Replay File Types", "File extensions accepted when scanning the Replay Folder. Separate multiple extensions with commas, for example .mp4, .mkv.", "General", "rts.actionreplay.replayFileTypes", ".mp4, .mkv", false); ui.AddTextbox("HTTP Mapping", "URL path used by Streamer.bot's HTTP server to serve files from the Replay Folder. For example, replays creates the /replays/ path.", "General", "rts.actionreplay.httpMapping", "replays", false); ui.AddNumericTextbox("HTTP Port", "Port used by Streamer.bot's HTTP server to serve replay media. This must match the HTTP server configuration in Streamer.bot.", "General", "rts.actionreplay.httpPort", 7474, 1, 65535); ui.EndSection();
    }

    private void AddPlaylistSettings(RtsUI ui)
    {
        ui.BeginSection("Replay Defaults", "Playlist"); ui.AddTextbox("Replay Title Template", "Template used to generate the title of newly saved replays. Streamer.bot variables can be used.", "Playlist", "rts.actionreplay.replayTitle", "%replayName%", false); ui.AddTextbox("New Replay Display Title", "Temporary title displayed when a newly saved replay is automatically played. This does not rename the Catalog item.", "Playlist", "rts.actionreplay.newReplayTitle", "New Replay", false); ui.EndSection(); ui.BeginSection("Recent Clips", "Playlist"); ui.AddSlider("Maximum Recent Clips", "Maximum number of entries retained in the Recent Clips and Last Played lists. Older entries are removed from these lists but remain in the Catalog.", "Playlist", "rts.actionreplay.maxHistory", 1, 100, 20); ui.BeginRow(); ui.AddToggleSwitch("Auto-add Saved Replays", "Automatically add each newly saved OBS replay to the Catalog and Recent Clips list.", "Playlist", "rts.actionreplay.autoAdd", true); ui.AddToggleSwitch("Auto-play Newest Replay", "Automatically load and play a newly saved OBS replay.", "Playlist", "rts.actionreplay.autoPlay", false); ui.EndRow(); ui.EndSection(); ui.BeginSection("Live Playlist", "Playlist"); ui.AddToggleSwitch("Persist Playlist Across Restarts", "Keep the current Playlist when Streamer.bot restarts. When disabled, the queue is cleared on restart.", "Playlist", "rts.actionreplay.playlistPersist", false); ui.EndSection();
    }

    private void AddTwitchSettings(RtsUI ui)
    {
        ui.BeginSection("Twitch Clips", "Twitch"); ui.AddDropdown("Twitch Clip Playback", "Choose how Twitch clips are made available for playback: use the Twitch URL, download the clip locally, or support both methods.", "Twitch", "rts.actionreplay.twitch.playbackMode", new[] { "Twitch URL", "Download Locally", "Both" }, "Download Locally"); ui.AddFolderPicker("Twitch Clip Folder", "Folder used only for downloaded Twitch Clips. It must be separate from the OBS Replay Folder.", "Twitch", "rts.actionreplay.twitch.folder", ""); ui.AddTextbox("Twitch HTTP Mapping", "URL path used by Streamer.bot's HTTP server to serve downloaded Twitch clips. This mapping must point to the Twitch Clip Folder.", "Twitch", "rts.actionreplay.twitch.httpMapping", "twitch", false); ui.AddNumericTextbox("Clip Duration", "Default duration used by !twitchclip, in seconds.", "Twitch", "rts.actionreplay.twitch.clipDuration", 30, 5, 60); ui.EndSection();
        ui.BeginSection("Kick Clips", "Kick"); ui.AddDropdown("Kick Clip Playback", "Choose how Kick clips are made available for playback: use the Kick URL, download the clip locally, or support both methods.", "Kick", "rts.actionreplay.kick.playbackMode", new[] { "Kick URL", "Download Locally", "Both" }, "Kick URL"); ui.AddFolderPicker("Kick Clip Folder", "Folder used only for downloaded Kick clips. It must be separate from the OBS Replay Folder.", "Kick", "rts.actionreplay.kick.folder", ""); ui.AddTextbox("Kick HTTP Mapping", "URL path used by Streamer.bot's HTTP server to serve downloaded Kick clips. This mapping must point to the Kick Clip Folder.", "Kick", "rts.actionreplay.kick.httpMapping", "kick", false); ui.EndSection();
    }

    private void AddYouTubeSettings(RtsUI ui)
    {
        ui.BeginSection("YouTube Clips", "YouTube"); ui.AddNumericTextbox("Clip Duration", "Default YouTube clip duration used by !Create-clip when no duration is supplied, in seconds.", "YouTube", "rts.actionreplay.youtube.clipDuration", 30, 5, 60); ui.EndSection();
    }

    private void AddPlayerSettings(RtsUI ui)
    {
        ui.BeginSection("Playback", "Player");
        ui.BeginRow();
        ui.AddToggleSwitch("Show Controls", "Display the visual player status bar. It is not interactive.", "Player", "rts.actionreplay.showControls", false);
        ui.AddToggleSwitch("Show Progress Bar", "Display the non-interactive playback progress bar.", "Player", "rts.actionreplay.showProgress", true);
        ui.EndRow();
        ui.BeginRow();
        ui.AddDecimalTextbox("Default Playback Speed", "Playback speed applied when a replay is loaded. 1.0 is normal speed.", "Player", "rts.actionreplay.playbackSpeed", 1.0, 0.25, 2.0, 0.25);
        ui.AddDropdown("Show Visibility", "Choose when the playback speed indicator is shown.", "Player", "rts.actionreplay.playbackSpeedVisibility", new[] { "Always", "Only when greater or less than 1", "Never" }, "Only when greater or less than 1");
        ui.EndRow();
        ui.EndSection();
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
        ui.AddGoogleFontSelector("Font", "Google Font used for text displayed on the clapperboard.", "Messages", "rts.actionreplay.clapper.font", "Inter");
        ui.EndSection();

        ui.BeginSection("Message Outputs", "Messages");
        AddMessageOutput(ui, "Save Replay", "Replay saved: %replayTitle%.", "rts.actionreplay.message.save", "Message sent when a replay is successfully saved.");
        AddMessageOutput(ui, "Name Replay", "Replay #%replayNumber% renamed to %replayTitle%.", "rts.actionreplay.message.name", "Message sent when a replay is successfully renamed.");
        AddMessageOutput(ui, "Play Replay", "Playing replay #%replayNumber%: %replayTitle%.", "rts.actionreplay.message.play", "Message sent when a replay starts playing.");
        AddMessageOutput(ui, "Recent", "%replayRecent%", "rts.actionreplay.message.recent", "Message sent when the Recent Clips command completes.");
        AddMessageOutput(ui, "Playlist", "%replayPlaylist%", "rts.actionreplay.message.playlist", "Message sent when a Playlist command completes.");
        ui.EndSection();
    }

    private static string ReadUiValue(string key) => CPH.GetGlobalVar<string>(key, true);

    private static void SaveUiValue(string key, object value, bool persisted)
        => CPH.SetGlobalVar(key, value, persisted);

    private static void AddMessageOutput(RtsUI ui, string name, string message, string key, string messageHelp) { ui.BeginRow(); ui.AddTextbox(name + " Message", messageHelp, "Messages", key + ".text", message, false); ui.EndRow(); ui.BeginRow(); ui.AddToggleSwitch(name + " - Chat", "Send this message to the requesting platform's chat.", "Messages", key + ".chat", true); ui.AddToggleSwitch(name + " - Overlay", "Send this message to the Action Replay overlay.", "Messages", key + ".overlay", false); ui.EndRow(); }
}
