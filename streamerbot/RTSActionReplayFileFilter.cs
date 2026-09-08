using System;
using System.IO;

public class CPHInline
{
    private const string FileTypesKey = "rts.actionreplay.replayFileTypes";

    public bool Execute()
    {
        string fileName;
        if (!CPH.TryGetArg("fileName", out fileName) || string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        var configured = CPH.GetGlobalVar<string>(FileTypesKey, true);
        if (string.IsNullOrWhiteSpace(configured))
            configured = ".mp4, .mkv";

        var types = configured.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < types.Length; i++)
        {
            var allowed = types[i].Trim();
            if (allowed.Length == 0) continue;
            if (!allowed.StartsWith(".")) allowed = "." + allowed;
            if (string.Equals(extension, allowed, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
