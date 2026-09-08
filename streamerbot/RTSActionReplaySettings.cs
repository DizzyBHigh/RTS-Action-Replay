using System;

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
        ui.AddTextbox("Brand Logo URL", "HTTPS URL to the shared branding logo. Leave blank to use the default text branding.", "General", "rts.actionreplay.brandLogoUrl", "", false);

        ui.AddTitle("Playlist", "Playlist");
        ui.AddTextbox("Replay Title Template", "Default title for new replays. Streamer.bot variables can be used.", "Playlist", "rts.actionreplay.replayTitle", "%replayName%", false);
        ui.AddSlider("Maximum History", "Maximum number of saved replays retained.", "Playlist", "rts.actionreplay.maxHistory", 1, 100, 20);
        ui.AddToggleSwitch("Auto-add Saved Replays", "Add each newly saved OBS replay to the playlist.", "Playlist", "rts.actionreplay.autoAdd", true);
        ui.AddToggleSwitch("Auto-play Newest Replay", "Load and play a newly saved replay automatically.", "Playlist", "rts.actionreplay.autoPlay", false);

        ui.AddTitle("Player", "Player");
        ui.AddToggleSwitch("Show Branding", "Display the shared branding on the replay player.", "Player", "rts.actionreplay.showPlayerBranding", true);
        ui.AddToggleSwitch("Show Controls", "Display the visual player status bar. It is not interactive.", "Player", "rts.actionreplay.showControls", false);
        ui.AddToggleSwitch("Show Progress Bar", "Display the non-interactive playback progress bar.", "Player", "rts.actionreplay.showProgress", true);
        ui.AddDecimalTextbox("Default Playback Speed", "Playback speed applied when a replay is loaded. Values below 1x are slow motion.", "Player", "rts.actionreplay.playbackSpeed", 1.0, 0.25, 2.0, 0.25);
        ui.AddToggleSwitch("Show Replay Title", "Display the replay title on the player.", "Player", "rts.actionreplay.showTitle", true);
        ui.AddDecimalTextbox("Replay Title Display Duration", "Seconds the replay title remains visible. Set to 0 to keep it visible for the entire replay.", "Player", "rts.actionreplay.titleDuration", 5.0, 0.0, 60.0, 1.0);
        ui.AddGoogleFontSelector("Player Skin Font", "Font used by the replay title and playback-speed indicators.", "Player", "rts.actionreplay.playerFont", "Inter");
        ui.AddTextbox("Slow Motion Text", "Text shown during slow-motion playback.", "Player", "rts.actionreplay.slowMotionText", "Slow Motion", false);
        ui.AddToggleSwitch("Show Slow Motion Speed", "Append the current playback speed to the slow-motion text.", "Player", "rts.actionreplay.slowMotionShowSpeed", true);
        ui.AddToggleSwitch("Fade Slow Motion Indicator", "Fade the slow-motion indicator when it appears and disappears.", "Player", "rts.actionreplay.slowMotionFade", true);
        ui.AddDecimalTextbox("Slow Motion Fade Duration", "Seconds used for slow-motion indicator fading.", "Player", "rts.actionreplay.slowMotionFadeDuration", 0.25, 0.0, 2.0, 0.05);
        ui.AddToggleSwitch("Flash Slow Motion Indicator", "Flash the slow-motion indicator on and off repeatedly while slow motion is active.", "Player", "rts.actionreplay.slowMotionFlash", false);
        ui.AddDecimalTextbox("Slow Motion Flash Interval", "Seconds shown and hidden for each flash phase. 0.5 gives 0.5 seconds on, then 0.5 seconds off.", "Player", "rts.actionreplay.slowMotionFlashInterval", 0.5, 0.1, 5.0, 0.1);
        ui.AddTitle("Player Elements", "Player");
        ui.AddPositionEditor("Element Positions", "Set the screen-relative position and size of Brand, Title, and Play Speed Indicator.", "Player", "rts.actionreplay.playerElements", "{\"Brand\":{\"name\":\"Brand\",\"scale\":100,\"x\":0,\"y\":0},\"Title\":{\"name\":\"Title\",\"scale\":100,\"x\":0,\"y\":0},\"Play Speed Indicator\":{\"name\":\"Play Speed Indicator\",\"scale\":100,\"x\":0,\"y\":0}}", new[] { "Brand", "Title", "Play Speed Indicator" }, "scale,x,y", null, null);

        ui.AddTitle("Player Frame", "Appearance");
        ui.AddColorPicker("Frame Color", "Main player frame and progress colour.", "Appearance", "rts.actionreplay.frameColor", "#FF0384CB");
        ui.AddColorPicker("Border Color", "Outer player border colour.", "Appearance", "rts.actionreplay.borderColor", "#FFFFFFFF");
        ui.AddNumericTextbox("Border Width", "Width of the player border in pixels. Set to 0 for no visible border.", "Appearance", "rts.actionreplay.borderWidth", 2, 0, 20);
        ui.AddDropdown("Border Style", "Visual style of the player border.", "Appearance", "rts.actionreplay.borderStyle", new[] { "None", "Solid", "Dashed", "Double" }, "Solid");
        ui.AddToggleSwitch("Drop Shadow", "Add a drop shadow around the player frame.", "Appearance", "rts.actionreplay.dropShadow", false);
        ui.AddColorPicker("Shadow Color", "Base colour of the player drop shadow.", "Appearance", "rts.actionreplay.shadowColor", "#80000000");

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
        CPH.SetArgument("replayShowBranding", CPH.GetGlobalVar<bool?>("rts.actionreplay.showPlayerBranding", true) ?? true);
        CPH.SetArgument("replayLogoUrl", CPH.GetGlobalVar<string>("rts.actionreplay.brandLogoUrl", true) ?? "");
        CPH.SetArgument("replayShowTitle", CPH.GetGlobalVar<bool?>("rts.actionreplay.showTitle", true) ?? true);
        CPH.SetArgument("replayTitle", "Replay Title");
        CPH.SetArgument("replayPlayerElements", CPH.GetGlobalVar<string>("rts.actionreplay.playerElements", true) ?? "{\"Brand\":{\"name\":\"Brand\",\"scale\":100,\"x\":0,\"y\":0},\"Title\":{\"name\":\"Title\",\"scale\":100,\"x\":0,\"y\":0},\"Play Speed Indicator\":{\"name\":\"Play Speed Indicator\",\"scale\":100,\"x\":0,\"y\":0}}");
        CPH.SetArgument("replayPlayerFont", CPH.GetGlobalVar<string>("rts.actionreplay.playerFont", true) ?? "Inter");
        CPH.SetArgument("replayShowControls", CPH.GetGlobalVar<bool?>("rts.actionreplay.showControls", true) ?? false);
        CPH.SetArgument("replayShowProgress", CPH.GetGlobalVar<bool?>("rts.actionreplay.showProgress", true) ?? true);
        CPH.SetArgument("replayFrameColor", CPH.GetGlobalVar<string>("rts.actionreplay.frameColor", true) ?? "#0384CB");
        CPH.SetArgument("replayBorderColor", CPH.GetGlobalVar<string>("rts.actionreplay.borderColor", true) ?? "#FFFFFF");
        CPH.SetArgument("replayBorderWidth", GetSettingInt("rts.actionreplay.borderWidth", 2));
        CPH.SetArgument("replayBorderStyle", CPH.GetGlobalVar<string>("rts.actionreplay.borderStyle", true) ?? "Solid");
        CPH.SetArgument("replayDropShadow", CPH.GetGlobalVar<bool?>("rts.actionreplay.dropShadow", true) ?? false);
        CPH.SetArgument("replayShadowColor", CPH.GetGlobalVar<string>("rts.actionreplay.shadowColor", true) ?? "#80000000");
        CPH.SetArgument("replayPositions", positionsJson ?? "{\"Full Screen\":{\"name\":\"Full Screen\",\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}");
        CPH.TriggerEvent("RTS-Action Replay", true);
    }

    private void AddMessages(RtsUI ui)
    {
        ui.AddTitle("Clapperboard", "Clapperboard");
        ui.AddToggleSwitch("Show Branding", "Display the shared branding on the clapperboard.", "Clapperboard", "rts.actionreplay.showClapperBranding", true);
        ui.AddColorPicker("Board Color", "Clapperboard slate colour.", "Clapperboard", "rts.actionreplay.clapper.boardColor", "#101416");
        ui.AddColorPicker("Stripe Light", "Clapperstick light stripe colour.", "Clapperboard", "rts.actionreplay.clapper.stripeLight", "#EEEEEE");
        ui.AddColorPicker("Stripe Dark", "Clapperstick dark stripe colour.", "Clapperboard", "rts.actionreplay.clapper.stripeDark", "#111111");
        ui.AddColorPicker("Accent Color", "Clapperboard accent colour.", "Clapperboard", "rts.actionreplay.clapper.accent", "#0384CB");
        ui.AddColorPicker("Text Color", "Message text colour.", "Clapperboard", "rts.actionreplay.clapper.textColor", "#0384CB");
        ui.AddGoogleFontSelector("Font", "Choose a Google Font.", "Clapperboard", "rts.actionreplay.clapper.font", "Inter");
        ui.AddSlider("Size (%)", "Overall clapperboard size.", "Clapperboard", "rts.actionreplay.clapper.size", 0, 100, 50);
        ui.AddSlider("Position X (%)", "Horizontal clapperboard position.", "Clapperboard", "rts.actionreplay.clapper.positionX", 0, 100, 50);
        ui.AddSlider("Position Y (%)", "Vertical clapperboard position.", "Clapperboard", "rts.actionreplay.clapper.positionY", 0, 100, 50);
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

    private int GetSettingInt(string key, int fallback)
    {
        try { return Convert.ToInt32(CPH.GetGlobalVar<object>(key, true), System.Globalization.CultureInfo.InvariantCulture); }
        catch { return fallback; }
    }
}