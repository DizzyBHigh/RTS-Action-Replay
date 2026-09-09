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
        AddPlayerSettings(ui);
        AddAppearanceSettings(ui);
        AddPositionSettings(ui);
        AddAnimationSettings(ui);
        AddMessageSettings(ui);

        ui.ShowUI();
        return true;
    }

    private void AddGeneralSettings(RtsUI ui)
    {
        ui.AddThemeSelector("Settings Theme", "Choose the RtsUI theme.", "General", "rts.actionreplay.uiTheme", "Dark");
        ui.BeginSection("Replay Source", "General");
        ui.AddFolderPicker("Replay Folder", "Folder containing OBS Replay Buffer files.", "General", "rts.actionreplay.replayFolder", "");
        ui.AddTextbox("Replay File Types", "Accepted extensions, separated by commas.", "General", "rts.actionreplay.replayFileTypes", ".mp4, .mkv", false);
        ui.AddTextbox("HTTP Mapping", "Streamer.bot HTTP path mapped to the replay folder.", "General", "rts.actionreplay.httpMapping", "replays", false);
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
        ui.BeginSection("Automatic Playlist", "Playlist");
        ui.AddSlider("Maximum History", "Maximum number of saved replays retained.", "Playlist", "rts.actionreplay.maxHistory", 1, 100, 20);
        ui.BeginRow();
        ui.AddToggleSwitch("Auto-add Saved Replays", "Add each newly saved OBS replay to the playlist.", "Playlist", "rts.actionreplay.autoAdd", true);
        ui.AddToggleSwitch("Auto-play Newest Replay", "Load and play a newly saved replay automatically.", "Playlist", "rts.actionreplay.autoPlay", false);
        ui.EndRow();
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
        ui.BeginSection("Default Positions", "Positions");
        ui.BeginRow();
        ui.AddPositionSelector("Default Start Position", "Position used when the player starts showing.", "Positions", "rts.actionreplay.defaultStartPosition", "rts.actionreplay.positions", "Full Screen");
        ui.AddPositionSelector("Default End Position", "Position used when the player has finished showing.", "Positions", "rts.actionreplay.defaultEndPosition", "rts.actionreplay.positions", "Full Screen");
        ui.EndRow();
        ui.EndSection();
        ui.BeginSection("Saved Positions", "Positions");
        ui.AddPositionEditor("Saved Positions", "Create and edit reusable player positions. Full Screen is built in and cannot be deleted.", "Positions", "rts.actionreplay.positions", "{\"Full Screen\":{\"name\":\"Full Screen\",\"tag\":\"full-screen\",\"scale\":100,\"x\":0,\"y\":0,\"rotateX\":0,\"rotateY\":0,\"rotateZ\":0}}", null, PreviewPosition);
        ui.AddTitle(BuildPositionTagList(CPH.GetGlobalVar<string>("rts.actionreplay.positions", true)), "Positions");
        ui.EndSection();
    }

    private void AddAnimationSettings(RtsUI ui)
    {
        ui.BeginSection("Player Animation", "Animation");
        ui.BeginRow();
        ui.AddNumericTextbox("Animation Duration", "Duration used when moving between the Start and End positions, in milliseconds.", "Animation", "rts.actionreplay.animationDuration", 0, 100, 20000);
        ui.AddDropdown("Animation Easing", "CSS easing used for player movement.", "Animation", "rts.actionreplay.animationEasing", new[] { "linear", "ease", "ease-in", "ease-out", "ease-in-out" }, "ease-in-out");
        ui.EndRow();
        ui.EndSection();
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
        if (string.IsNullOrWhiteSpace(positionName))
        {
            CPH.SetArgument("replayCommand", "hide");
            CPH.TriggerEvent("RTS-Action Replay", true);
            return;
        }
        CPH.SetArgument("replayCommand", "move");
        CPH.SetArgument("replayPosition", positionName);
        CPH.SetArgument("replayPositions", positionsJson ?? "{}");
        CPH.SetArgument("replayAnimationDuration", GetSettingDouble("rts.actionreplay.animationDuration", 0.0));
        CPH.SetArgument("replayAnimationEasing", CPH.GetGlobalVar<string>("rts.actionreplay.animationEasing", true) ?? "ease-in-out");
        CPH.TriggerEvent("RTS-Action Replay", true);
    }

    private double GetSettingDouble(string key, double fallback)
    {
        try
        {
            object value = CPH.GetGlobalVar<object>(key, true);
            if (value == null) return fallback;
            return System.Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch { return fallback; }
    }

    private string BuildPositionTagList(string json)
    {
        var lines = new System.Collections.Generic.List<string> { "Position Tags" };
        try
        {
            var map = Newtonsoft.Json.Linq.JObject.Parse(json ?? "{}");
            foreach (var item in map)
            {
                string name = item.Key;
                var position = item.Value as Newtonsoft.Json.Linq.JObject;
                string tag = position == null ? null : (string)position["tag"];
                lines.Add(name + "  —  " + (string.IsNullOrWhiteSpace(tag) ? NormalizePositionTag(name) : NormalizePositionTag(tag)));
            }
        }
        catch { }
        if (lines.Count == 1) lines.Add("Full Screen  —  full-screen");
        return string.Join("\n", lines.ToArray());
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
}
