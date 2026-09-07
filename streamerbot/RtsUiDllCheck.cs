using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

// Generic Streamer.bot C# action: verify/install/update the shared RTS UI DLL.
// The preceding action supplies rts.extensionName and rts.minimumRtsUiVersion.
// This action must NOT reference RtsUI.dll itself.
public class CPHInline
{
    private const string DllName = "RtsUI.dll";
    private const string ReleaseApiUrl = "https://api.github.com/repos/DizzyBHigh/RTS-UI-Dll/releases/latest";
    private const string DownloadUrl = "https://github.com/DizzyBHigh/RTS-UI-Dll/releases/latest/download/RtsUI.dll";

    public bool Execute()
    {
        string extensionName = CPH.GetArgument<string>("rts.extensionName");
        string minimumText = CPH.GetArgument<string>("rts.minimumRtsUiVersion");
        if (string.IsNullOrWhiteSpace(extensionName) || string.IsNullOrWhiteSpace(minimumText))
        {
            CPH.LogError("[RTS UI DLL Check] Missing rts.extensionName or rts.minimumRtsUiVersion action arguments.");
            return false;
        }

        try
        {
            Version minimum = ParseVersion(minimumText);
            string dllDirectory = Path.Combine(ResolveStreamerBotDirectory(), "dlls");
            string dllPath = Path.Combine(dllDirectory, DllName);
            Directory.CreateDirectory(dllDirectory);
            Version installed = GetVersion(dllPath);

            if (installed == null)
            {
                if (MessageBox.Show("The RTS UI library is required for the settings UI.\n\nRtsUI.dll is not installed. Download it now?", extensionName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return false;
                return DownloadAndInstall(dllPath, extensionName, minimum, null);
            }

            CPH.LogInfo($"[{extensionName}] Installed RtsUI.dll version: {installed}");
            if (installed < minimum)
            {
                if (MessageBox.Show($"This extension requires RtsUI.dll {minimum} or newer.\n\nInstalled version: {installed}\n\nDownload the required version now?", extensionName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return false;
                return DownloadAndInstall(dllPath, extensionName, minimum, null);
            }

            Version latest = GetLatestReleaseVersion(extensionName);
            if (latest != null && installed < latest && MessageBox.Show($"A newer RtsUI.dll is available.\n\nInstalled version: {installed}\nLatest version: {latest}\n\nDownload and install the update now?", extensionName, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                return DownloadAndInstall(dllPath, extensionName, minimum, latest);

            return true;
        }
        catch (Exception ex)
        {
            CPH.LogError($"[{extensionName}] RtsUI.dll check failed: {ex}");
            MessageBox.Show("The RTS UI library could not be checked.\n\n" + ex.Message, extensionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private bool DownloadAndInstall(string path, string extensionName, Version minimum, Version expected)
    {
        string temp = path + ".download";
        try
        {
            if (File.Exists(temp)) File.Delete(temp);
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "RTS-RtsUI-DLL-Check";
                client.DownloadFile(DownloadUrl, temp);
            }
            Version downloaded = GetVersion(temp);
            if (downloaded == null) throw new InvalidDataException("The downloaded file is not a valid RtsUI.dll assembly.");
            if (downloaded < minimum) throw new InvalidDataException($"The downloaded RtsUI.dll is version {downloaded}, but {minimum} is required.");
            if (expected != null && downloaded < expected) throw new InvalidDataException($"The downloaded RtsUI.dll is version {downloaded}, but the release reported {expected}.");
            File.Copy(temp, path, true);
            File.Delete(temp);
            CPH.LogInfo($"[{extensionName}] RtsUI.dll {downloaded} installed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            CPH.LogError($"[{extensionName}] Failed to install RtsUI.dll: {ex.Message}");
            MessageBox.Show("RtsUI.dll could not be installed.\n\n" + ex.Message + "\n\nThe existing DLL has been left untouched.", extensionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private static Version GetVersion(string path)
    {
        try { return File.Exists(path) ? AssemblyName.GetAssemblyName(path).Version : null; }
        catch { return null; }
    }

    private static Version GetLatestReleaseVersion(string extensionName)
    {
        try
        {
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "RTS-RtsUI-DLL-Check";
                Match match = Regex.Match(client.DownloadString(ReleaseApiUrl), @"\"tag_name\"\s*:\s*\"v?([0-9]+(?:\.[0-9]+){1,3})\"", RegexOptions.IgnoreCase);
                return match.Success ? ParseVersion(match.Groups[1].Value) : null;
            }
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"[{extensionName}] Could not check the latest RtsUI.dll release: {ex.Message}");
            return null;
        }
    }

    private static Version ParseVersion(string value)
    {
        string clean = value.Trim();
        if (clean.StartsWith("v", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(1);
        int dash = clean.IndexOf('-');
        if (dash >= 0) clean = clean.Substring(0, dash);
        return new Version(clean);
    }

    private static string ResolveStreamerBotDirectory()
    {
        string[] candidates = { AppDomain.CurrentDomain.BaseDirectory, Directory.GetCurrentDirectory(), Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) };
        foreach (string candidate in candidates)
            if (!string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate)) return candidate;
        return Directory.GetCurrentDirectory();
    }
}
