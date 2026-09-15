// RTS Action Replay platform identity normalization.
// Streamer.bot userType is normalized to a stable platform name.

using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    public bool Execute() => ResolveIdentity();

    public bool ResolveIdentity()
    {
        var platform = NormalizePlatform(Get("userType"));
        var id = Get("userId");
        var name = Get("userName");
        if (string.IsNullOrWhiteSpace(id)) return false;

        CPH.SetArgument("replayPlatform", platform);
        CPH.SetArgument("replayUserId", id);
        CPH.SetArgument("replayUserName", name);
        CPH.SetArgument("replayIdentityKey", IdentityKey(platform, id));
        CPH.SetArgument("replayIdentity", new JObject
        {
            ["platform"] = platform,
            ["id"] = id,
            ["name"] = name
        }.ToString(Newtonsoft.Json.Formatting.None));
        return true;
    }

    public static string NormalizePlatform(string userType)
    {
        if (string.Equals(userType, "YouTube", StringComparison.OrdinalIgnoreCase)) return "YouTube";
        if (string.Equals(userType, "Kick", StringComparison.OrdinalIgnoreCase)) return "Kick";
        return "Twitch";
    }

    public static string IdentityKey(string platform, string id) =>
        NormalizePlatform(platform).ToLowerInvariant() + ":" + (id ?? "").Trim();

    private string Get(string name)
    {
        CPH.TryGetArg(name, out string value);
        return value ?? "";
    }
}
