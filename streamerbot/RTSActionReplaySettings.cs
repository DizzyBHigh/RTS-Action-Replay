using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

// Single Streamer.bot Settings action: verify/update RtsUI.dll, then open Action Replay settings.
public class CPHInline
{
    private const string Name = "RTS Action Replay";
    private const string MinUi = "0.1.0";
    private const string Dll = "RtsUI.dll";
    private const string ReleaseApi = "https://api.github.com/repos/DizzyBHigh/RTS-UI-Dll/releases/latest";
    private const string Download = "https://github.com/DizzyBHigh/RTS-UI-Dll/releases/latest/download/RtsUI.dll";

    public bool Execute()
    {
        string dllPath = Path.Combine(ResolveBotDirectory(), "dlls", Dll);
        if (!EnsureDll(dllPath, new Version(MinUi))) return false;

        var ui = new RtsUI(Name, "0.1.0",
            (key, persisted) => CPH.GetGlobalVar<bool?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<int?>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<string>(key, persisted),
            (key, persisted) => CPH.GetGlobalVar<object>(key, persisted),
            (key, value, persisted) => CPH.SetGlobalVar(key, value, persisted),
            message => CPH.LogInfo(message));

        AddSettings(ui);
        ui.ShowUI();
        return true;
    }

    private void AddSettings(RtsUI ui)
    {
        ui.AddThemeSelector("Settings Theme", "Choose the RtsUI theme.", "General", "rts.actionreplay.uiTheme", "Dark");
        ui.AddTitle("Replay Source", "General");
        ui.AddTextbox("Replay Folder", "Folder containing OBS Replay Buffer files.", "General", "rts.actionreplay.replayFolder", "", false);
        ui.AddTextbox("HTTP Mapping", "Streamer.bot HTTP path mapped to the replay folder, without leading or trailing slashes.", "General", "rts.actionreplay.httpMapping", "replays", false);
        ui.AddSlider("HTTP Port", "Streamer.bot HTTP Server port used to serve replay files.", "General", "rts.actionreplay.httpPort", 1, 65535, 7474);

        ui.AddTitle("Playlist", "Playlist");
        ui.AddSlider("Maximum History", "Maximum number of saved replays retained in the playlist.", "Playlist", "rts.actionreplay.maxHistory", 1, 100, 20);
        ui.AddToggleSwitch("Auto-add Saved Replays", "Add each newly saved OBS replay to the playlist.", "Playlist", "rts.actionreplay.autoAdd", true);
        ui.AddToggleSwitch("Auto-play Newest Replay", "Load and play a newly saved replay automatically.", "Playlist", "rts.actionreplay.autoPlay", false);

        ui.AddTitle("Player", "Player");
        ui.AddThemeSelector("Player Theme", "Select the player theme.", "Player", "rts.actionreplay.playerTheme", "Dark");
        ui.AddToggleSwitch("Show Controls", "Display player controls in the overlay.", "Player", "rts.actionreplay.showControls", false);
        ui.AddToggleSwitch("Show Progress Bar", "Display the playback progress bar.", "Player", "rts.actionreplay.showProgress", true);
        ui.AddDropdown("Default Playback Speed", "Playback speed used when a replay is loaded.", "Player", "rts.actionreplay.playbackSpeed", new[] { "0.25", "0.5", "0.75", "1.0", "1.25", "1.5", "2.0" }, "1.0");

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
    }

    private bool EnsureDll(string path, Version minimum)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Version installed = VersionOf(path);
            if (installed == null || installed < minimum)
            {
                string prompt = installed == null
                    ? "RtsUI.dll is required for the settings UI. Download it now?"
                    : $"This extension requires RtsUI.dll {minimum} or newer. Installed: {installed}. Download it now?";
                if (MessageBox.Show(prompt, Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
                return DownloadDll(path, minimum);
            }
            Version latest = LatestVersion();
            if (latest != null && installed < latest && MessageBox.Show($"A newer RtsUI.dll is available ({latest}). Update now?", Name, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                return DownloadDll(path, latest);
            return true;
        }
        catch (Exception ex)
        {
            CPH.LogError($"[{Name}] RtsUI.dll check failed: {ex}");
            MessageBox.Show(ex.Message, Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private bool DownloadDll(string path, Version minimum)
    {
        string temp = path + ".download";
        try
        {
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "RTS-Action-Replay";
                client.DownloadFile(Download, temp);
            }
            Version downloaded = VersionOf(temp);
            if (downloaded == null || downloaded < minimum) throw new InvalidDataException("Downloaded RtsUI.dll is invalid or too old.");
            File.Copy(temp, path, true);
            File.Delete(temp);
            CPH.LogInfo($"[{Name}] RtsUI.dll {downloaded} installed.");
            return true;
        }
        catch (Exception ex)
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            CPH.LogError($"[{Name}] RtsUI.dll installation failed: {ex.Message}");
            MessageBox.Show("RtsUI.dll could not be installed.\n\n" + ex.Message, Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private static Version VersionOf(string path) { try { return File.Exists(path) ? AssemblyName.GetAssemblyName(path).Version : null; } catch { return null; } }
    private static Version LatestVersion()
    {
        try
        {
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "RTS-Action-Replay";
                Match m = Regex.Match(client.DownloadString(ReleaseApi), @"\"tag_name\"\s*:\s*\"v?([0-9]+(?:\.[0-9]+){1,3})\"", RegexOptions.IgnoreCase);
                return m.Success ? new Version(m.Groups[1].Value) : null;
            }
        }
        catch (Exception ex) { CPH.LogInfo($"[{Name}] Could not check latest RtsUI.dll: {ex.Message}"); return null; }
    }

    private static string ResolveBotDirectory()
    {
        string[] paths = { AppDomain.CurrentDomain.BaseDirectory, Directory.GetCurrentDirectory(), Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) };
        foreach (string path in paths) if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path)) return path;
        return Directory.GetCurrentDirectory();
    }
}
