// Streamer.bot C# action: open the Action Replay settings window.
// Requires RtsUI.dll 0.2.0 or newer as a custom assembly reference.
public class CPHInline
{
    public bool Execute()
    {
        string pendingPositions = null;
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
        ui.AddSlider("Maximum History", "Maximum number of saved replays retained.", "Playlist", "rts.actionreplay.maxHistory", 1, 100, 20);
        ui.AddToggleSwitch("Auto-add Saved Replays", "Add each newly saved OBS replay to the playlist.", "Playlist", "rts.actionreplay.autoAdd", true);
        ui.AddToggleSwitch("Auto-play Newest Replay", "Load and play a newly saved replay automatically.", "Playlist", "rts.actionreplay.autoPlay", false);

        ui.AddTitle("Player", "Player");
        ui.AddToggleSwitch("Show Controls", "Display the visual player status bar. It is not interactive.", "Player", "rts.actionreplay.showControls", false);
        ui.AddToggleSwitch("Show Progress Bar", "Display the non-interactive playback progress bar.", "Player", "rts.actionreplay.showProgress", true);
        ui.AddDecimalTextbox("Default Playback Speed", "Playback speed applied when a replay is loaded.", "Player", "rts.actionreplay.playbackSpeed", 1.0, 0.25, 2.0, 0.25);

        ui.AddTitle("Player Frame", "Appearance");
        ui.AddColorPicker("Frame Color", "Main player frame and progress colour.", "Appearance", "rts.actionreplay.frameColor", "#FF0384CB");
        ui.AddColorPicker("Border Color", "Outer player border colour.", "Appearance", "rts.actionreplay.borderColor", "#FFFFFFFF");
        ui.AddDropdown("Border Style", "Visual style of the player border.", "Appearance", "rts.actionreplay.borderStyle", new[] { "None", "Solid", "Dashed", "Double" }, "Solid");

        ui.AddTitle("Player Positions", "Positions");
        ui.AddPositionSelector("Default Start Position", "Position used when the player starts showing.", "Positions", "rts.actionreplay.defaultStartPosition", "rts.actionreplay.positions", "Full Screen");
        ui.AddPositionSelector("Default End Position", "Position used when the player has finished showing.", "Positions", "rts.actionreplay.defaultEndPosition", "rts.actionreplay.positions", "Full Screen");
        ui.AddPositionEditor("Saved Positions", "Create and edit reusable player positions. Full Screen is built in and cannot be deleted.", "Positions", "rts.actionreplay.positions", "{\"Full Screen\":{\"name\":\"Full Screen\",\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}", saved => pendingPositions = saved, PreviewPosition);

        ui.AddTitle("Player Animation", "Animation");
        ui.AddDecimalTextbox("Animation Duration", "Duration used when moving between the Start and End positions.", "Animation", "rts.actionreplay.animationDuration", 0.5, 0.1, 5.0, 0.1);
        ui.AddDropdown("Animation Easing", "CSS easing used for player movement.", "Animation", "rts.actionreplay.animationEasing", new[] { "linear", "ease", "ease-in", "ease-out", "ease-in-out" }, "ease-in-out");

        AddMessages(ui);
        ui.ShowUI();
        if (pendingPositions != null)
        {
            CPH.SetGlobalVar("rts.actionreplay.positions", pendingPositions, true);
            CPH.LogInfo("[RTS Action Replay] Persisted player positions after settings window closed.");
        }
        return true;
    }

    private void PreviewPosition(string position, string positionsJson)
    {
        if (string.IsNullOrWhiteSpace(position))
        {
            CPH.SetArgument("replayCommand", "previewHide");
            CPH.TriggerEvent("RTS-Action Replay", true);
            return;
        }
        CPH.SetArgument("replayCommand", "preview");
        CPH.SetArgument("replayPosition", position);
        CPH.SetArgument("replayShowControls", CPH.GetGlobalVar<bool?>("rts.actionreplay.showControls", true) ?? false);
        CPH.SetArgument("replayShowProgress", CPH.GetGlobalVar<bool?>("rts.actionreplay.showProgress", true) ?? true);
        CPH.SetArgument("replayFrameColor", CPH.GetGlobalVar<string>("rts.actionreplay.frameColor", true) ?? "#0384CB");
        CPH.SetArgument("replayBorderColor", CPH.GetGlobalVar<string>("rts.actionreplay.borderColor", true) ?? "#FFFFFF");
        CPH.SetArgument("replayBorderStyle", CPH.GetGlobalVar<string>("rts.actionreplay.borderStyle", true) ?? "Solid");
        CPH.SetArgument("replayPositions", positionsJson ?? "{\"Full Screen\":{\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}");
        CPH.TriggerEvent("RTS-Action Replay", true);
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
