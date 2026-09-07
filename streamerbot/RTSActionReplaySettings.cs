// Streamer.bot C# action: open the Action Replay settings window.
// Requires RtsUI.dll 0.2.0 or newer as a custom assembly reference.
public class CPHInline
{
    public bool Execute()
    {
        var ui = new RtsUI(
            "RTS Action Replay",
            "0.1.0",
            (key, persisted) => CPH.GetGlobalVar<bool?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<int?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<string>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<object>(key, persisted),
            (key, value, persisted) => CPH.SetGlobalVar(key, value, persisted),
            message => CPH.LogInfo(message));

        ui.AddThemeSelector("Settings Theme", "Choose the RtsUI theme.", "General", "rts.actionreplay.uiTheme", "Dark");
        ui.AddTitle("Replay Source", "General");
        ui.AddFolderPicker("Replay Folder", "Folder containing OBS Replay Buffer files.", "General", "rts.actionreplay.replayFolder", "");
        ui.AddTextbox("HTTP Mapping", "Streamer.bot HTTP path mapped to the replay folder, without leading or trailing slashes.", "General", "rts.actionreplay.httpMapping", "replays", false);
        ui.AddNumericTextbox("HTTP Port", "Streamer.bot HTTP Server port used to serve replay files.", "General", "rts.actionreplay.httpPort", 7474, 1, 65535);

        ui.AddTitle("Playlist", "Playlist");
        ui.AddSlider("Maximum History", "Maximum number of saved replays retained in the playlist.", "Playlist", "rts.actionreplay.maxHistory", 1, 100, 20);
        ui.AddToggleSwitch("Auto-add Saved Replays", "Add each newly saved OBS replay to the playlist.", "Playlist", "rts.actionreplay.autoAdd", true);
        ui.AddToggleSwitch("Auto-play Newest Replay", "Load and play a newly saved replay automatically.", "Playlist", "rts.actionreplay.autoPlay", false);

        ui.AddTitle("Player", "Player");
        ui.AddToggleSwitch("Show Controls", "Display player controls in the overlay.", "Player", "rts.actionreplay.showControls", false);
        ui.AddToggleSwitch("Show Progress Bar", "Display the playback progress bar.", "Player", "rts.actionreplay.showProgress", true);
        ui.AddDecimalTextbox("Default Playback Speed", "Playback speed used when a replay is loaded.", "Player", "rts.actionreplay.playbackSpeed", 1.0, 0.25, 2.0, 0.25);

        ui.AddTitle("Player Frame", "Appearance");
        ui.AddColorPicker("Frame Color", "Color of the player frame.", "Appearance", "rts.actionreplay.frameColor", "#FF0384CB");
        ui.AddColorPicker("Border Color", "Color of the player border.", "Appearance", "rts.actionreplay.borderColor", "#FFFFFFFF");
        ui.AddDropdown("Border Style", "Visual style of the player border.", "Appearance", "rts.actionreplay.borderStyle", new[] { "None", "Solid", "Dashed", "Double" }, "Solid");

        ui.AddTitle("3D Perspective", "Perspective");
        ui.AddDropdown("Perspective Preset", "Choose a predefined player perspective or Custom.", "Perspective", "rts.actionreplay.perspectivePreset", new[] { "Full Screen", "Slight Angle", "Left Angled", "Right Angled", "Side Facing", "Custom" }, "Full Screen");
        ui.AddSlider("Rotate X", "Custom X-axis rotation.", "Perspective", "rts.actionreplay.rotateX", -180, 180, 0);
        ui.AddSlider("Rotate Y", "Custom Y-axis rotation.", "Perspective", "rts.actionreplay.rotateY", -180, 180, 0);
        ui.AddSlider("Rotate Z", "Custom Z-axis rotation.", "Perspective", "rts.actionreplay.rotateZ", -180, 180, 0);
        ui.AddSlider("Scale (%)", "Custom player scale.", "Perspective", "rts.actionreplay.scale", 25, 200, 100);
        ui.AddSlider("Position X", "Custom horizontal position.", "Perspective", "rts.actionreplay.positionX", -100, 100, 0);
        ui.AddSlider("Position Y", "Custom vertical position.", "Perspective", "rts.actionreplay.positionY", -100, 100, 0);

        ui.ShowUI();
        return true;
    }
}
