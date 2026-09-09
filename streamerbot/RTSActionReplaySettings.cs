// Streamer.bot C# action: open the Action Replay settings window.
// Requires RtsUI.dll 0.2.0 or newer as a custom assembly reference.
public class CPHInline
{
    public bool Execute()
    {
        var ui = new RtsUI("RTS Action Replay", "0.1.0",
            (key, persisted) => CPH.GetGlobalVar<bool?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<int?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<string>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<object>(key, persisted),
            (key, value, persisted) => CPH.SetGlobalVar(key, value, persisted),
            message => CPH.LogInfo(message));

        ui.AddThemeSelector("Settings Theme", "Choose the RtsUI theme.", "General", "rts.actionreplay.uiTheme", "Dark");
        ui.AddTitle("Replay Source", "General");
        ui.AddFolderPicker("Replay Folder", "Folder containing OBS Replay Buffer files.", "General", "rts.actionreplay.replayFolder", "");
        ui.AddTextbox("Replay File Types", "Accepted extensions, separated by commas.", "General", "rts.actionreplay.replayFileTypes", ".mp4, .mkv", false);
        ui.AddTextbox("HTTP Mapping", "Streamer.bot HTTP path mapped to the replay folder.", "General", "rts.actionreplay.httpMapping", "replays", false);
        ui.AddNumericTextbox("HTTP Port", "Streamer.bot HTTP Server port used to serve replay files.", "General", "rts.actionreplay.httpPort", 7474, 1, 65535);

        ui.AddTitle("Playlist", "Playlist");
        ui.AddTextbox("Replay Title Template", "Default title for new replays. Streamer.bot variables can be used.", "Playlist", "rts.actionreplay.replayTitle", "%replayName%", false);
        ui.AddTextbox("New Replay Display Title", "Title shown when a newly discovered replay is automatically played.", "Playlist", "rts.actionreplay.newReplayTitle", "New Replay", false);
        ui.AddSlider("Maximum History", "Maximum number of saved replays retained.", "Playlist", "rts.actionreplay.maxHistory", 1, 100, 20);
        ui.AddToggleSwitch("Auto-add Saved Replays", "Add each newly saved OBS replay to the playlist.", "Playlist", "rts.actionreplay.autoAdd", true);
        ui.AddToggleSwitch("Auto-play Newest Replay", "Load and play a newly saved replay automatically.", "Playlist", "rts.actionreplay.autoPlay", false);

        ui.AddTitle("Player", "Player");
        ui.AddToggleSwitch("Show Controls", "Display the visual player status bar. It is not interactive.", "Player", "rts.actionreplay.showControls", false);
        ui.AddToggleSwitch("Show Progress Bar", "Display the non-interactive playback progress bar.", "Player", "rts.actionreplay.showProgress", true);
        ui.AddDecimalTextbox("Default Playback Speed", "Playback speed applied when a replay is loaded.", "Player", "rts.actionreplay.playbackSpeed", 1.0, 0.25, 2.0, 0.25);
        ui.AddToggleSwitch("Show Replay Title", "Display the replay title on the video.", "Player", "rts.actionreplay.showTitle", true);
        ui.AddToggleSwitch("Show Replay Branding", "Display RTS Action Replay branding with the title.", "Player", "rts.actionreplay.showBranding", true);
        ui.AddDropdown("Title Bar Style", "Choose the visual design used for the replay title.", "Player", "rts.actionreplay.titleBarStyle", new[] { "Broadcast", "Cinematic", "Cut", "Minimal" }, "Broadcast");
        ui.AddDropdown("Title Position", "Place the title bar at the top or bottom of the video.", "Player", "rts.actionreplay.titlePosition", new[] { "Top", "Bottom" }, "Bottom");
        ui.AddDropdown("Title Appearance", "Choose how the title enters and exits.", "Player", "rts.actionreplay.titleAnimation", new[] { "Fade", "Left to right", "Right to left", "Slide up/down" }, "Slide up/down");
        ui.AddDecimalTextbox("Title Display Duration", "How long the title remains visible, in seconds. Zero keeps it visible.", "Player", "rts.actionreplay.titleDuration", 5.0, 0.0, 30.0, 0.5);
        ui.AddDecimalTextbox("Title Animation Duration", "Animation duration in seconds.", "Player", "rts.actionreplay.titleAnimationDuration", 0.45, 0.1, 2.0, 0.05);
        ui.AddGoogleFontSelector("Title Font", "Choose the Google Font used by the replay title.", "Player", "rts.actionreplay.titleFont", "Inter");
        ui.AddNumericTextbox("Title Font Size", "Replay title font size in pixels.", "Player", "rts.actionreplay.titleFontSize", 34, 12, 96);
        ui.AddColorPicker("Title Text Color", "Replay title text colour.", "Player", "rts.actionreplay.titleTextColor", "#FFFFFFFF");
        ui.AddColorPicker("Title Shadow Color", "Replay title shadow colour.", "Player", "rts.actionreplay.titleShadowColor", "#FF000000");
        ui.AddColorPicker("Title Background Color", "Replay title background colour.", "Player", "rts.actionreplay.titleBackgroundColor", "#F005090C");
        ui.AddSlider("Title Background Opacity", "Opacity of the replay title background.", "Player", "rts.actionreplay.titleBackgroundOpacity", 0, 100, 94);
        ui.AddColorPicker("Title Accent Color", "Replay title accent and animated highlight colour.", "Player", "rts.actionreplay.titleAccentColor", "#FF0384CB");

        ui.AddTitle("Player Frame", "Appearance");
        ui.AddColorPicker("Frame Color", "Main player frame and progress colour.", "Appearance", "rts.actionreplay.frameColor", "#FF0384CB");
        ui.AddColorPicker("Border Color", "Outer player border colour.", "Appearance", "rts.actionreplay.borderColor", "#FFFFFFFF");
        ui.AddDropdown("Border Style", "Visual style of the player border.", "Appearance", "rts.actionreplay.borderStyle", new[] { "None", "Solid", "Dashed", "Double" }, "Solid");

        ui.AddTitle("Player Positions", "Positions");
        ui.AddPositionSelector("Default Start Position", "Position used when the player starts showing.", "Positions", "rts.actionreplay.defaultStartPosition", "rts.actionreplay.positions", "Full Screen");
        ui.AddPositionSelector("Default End Position", "Position used when the player has finished showing.", "Positions", "rts.actionreplay.defaultEndPosition", "rts.actionreplay.positions", "Full Screen");
        ui.AddPositionEditor("Saved Positions", "Create and edit reusable player positions. Full Screen is built in and cannot be deleted.", "Positions", "rts.actionreplay.positions", "{\"Full Screen\":{\"name\":\"Full Screen\",\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}");

        ui.AddTitle("Player Animation", "Animation");
        ui.AddDecimalTextbox("Animation Duration", "Duration used when moving between the Start and End positions.", "Animation", "rts.actionreplay.animationDuration", 0.5, 0.1, 5.0, 0.1);
        ui.AddDropdown("Animation Easing", "CSS easing used for player movement.", "Animation", "rts.actionreplay.animationEasing", new[] { "linear", "ease", "ease-in", "ease-out", "ease-in-out" }, "ease-in-out");

        AddMessages(ui);
        ui.ShowUI();
        return true;
    }

    private void AddMessages(RtsUI ui)
    {
        ui.AddTitle("Messages", "Messages");
        ui.AddTextbox("Brand Logo URL", "HTTPS URL to a PNG logo, or blank for RTS text.", "Messages", "rts.actionreplay.brandLogoUrl", "", false);
        ui.AddColorPicker("Board Color", "Clapperboard slate colour.", "Messages", "rts.actionreplay.clapper.boardColor", "#101416");
        ui.AddColorPicker("Stripe Light", "Clapperstick light stripe colour.", "Messages", "rts.actionreplay.clapper.stripeLight", "#EEEEEE");
        ui.AddColorPicker("Stripe Dark", "Clapperstick dark stripe colour.", "Messages", "rts.actionreplay.clapper.stripeDark", "#111111");
        ui.AddColorPicker("Accent Color", "Clapperboard accent colour.", "Messages", "rts.actionreplay.clapper.accent", "#0384CB");
        ui.AddColorPicker("Text Color", "Message text colour.", "Messages", "rts.actionreplay.clapper.textColor", "#0384CB");
        ui.AddGoogleFontSelector("Font", "Choose a Google Font.", "Messages", "rts.actionreplay.clapper.font", "Inter");
        ui.AddSlider("Size (%)", "Overall clapperboard size.", "Messages", "rts.actionreplay.clapper.size", 0, 100, 50);
        ui.AddSlider("Position X (%)", "Horizontal clapperboard position.", "Messages", "rts.actionreplay.clapper.positionX", 0, 100, 50);
        ui.AddSlider("Position Y (%)", "Vertical clapperboard position.", "Messages", "rts.actionreplay.clapper.positionY", 0, 100, 50);
        AddMessageSettings(ui, "Save Replay", "Replay saved: %replayTitle%.", "rts.actionreplay.message.save");
        AddMessageSettings(ui, "Name Replay", "Replay #%replayNumber% renamed to %replayTitle%.", "rts.actionreplay.message.name");
        AddMessageSettings(ui, "Play Replay", "Playing replay #%replayNumber%: %replayTitle%.", "rts.actionreplay.message.play");
        AddMessageSettings(ui, "Playlist", "%replayPlaylist%", "rts.actionreplay.message.playlist");
        AddMessageSettings(ui, "Creator Leaderboard", "%replayLeaderboard%", "rts.actionreplay.message.creatorLeaderboard");
        AddMessageSettings(ui, "Playback Leaderboard", "%replayLeaderboard%", "rts.actionreplay.message.playbackLeaderboard");
    }

    private void AddMessageSettings(RtsUI ui, string name, string message, string key)
    {
        ui.AddTextbox(name + " Message", "Message sent when this command completes.", "Messages", key + ".text", message, false);
        ui.AddToggleSwitch(name + " - Chat", "Send this message to Twitch chat.", "Messages", key + ".chat", true);
        ui.AddToggleSwitch(name + " - Overlay", "Send this message to the Action Replay overlay.", "Messages", key + ".overlay", false);
    }
}
