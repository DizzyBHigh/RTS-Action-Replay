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
        ui.AddTextbox("Replay File Types", "Accepted extensions, separated by commas. e.g .mp4, .mov", "General", "rts.actionreplay.replayFileTypes", ".mp4, .mkv", false);
        ui.AddTextbox("HTTP Mapping", "Streamer.bot HTTP path mapped to the OBS replay folder.", "General", "rts.actionreplay.httpMapping", "replays", false);
        ui.AddNumericTextbox("HTTP Port", "Streamer.bot HTTP Server port used to serve replay files.", "General", "rts.actionreplay.httpPort", 7474, 1, 65535);
        ui.EndSection();
    }

    private void AddBrandingSettings(RtsUI ui)
    {
        ui.BeginSection("Branding", "Branding");
        ui.BeginSection("Brand Identity");
        ui.AddToggleSwitch("Show Replay Branding", "Display the branding on the top left of the replay screen.", "Branding", "rts.actionreplay.showBranding", true);
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
        ui.BeginSection("Accent Colours");
        ui.BeginRow();
        ui.AddColorPicker("Primary Colour", "Main Action Replay accent colour used throughout the player.", "Branding", "rts.actionreplay.titlePrimaryColor", "#0384CBFF");
        ui.AddColorPicker("Secondary Colour", "Secondary Action Replay accent colour used throughout the player.", "Branding", "rts.actionreplay.titleSecondaryColor", "#101416FF");
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
        ui.BeginSection("Live Playlist", "Playlist");
        ui.AddToggleSwitch("Persist Playlist Across Restarts", "Keep the current Playlist when Streamer.bot restarts. When disabled, the queue is cleared on restart.", "Playlist", "rts.actionreplay.playlistPersist", false);
        ui.EndSection();
    }
