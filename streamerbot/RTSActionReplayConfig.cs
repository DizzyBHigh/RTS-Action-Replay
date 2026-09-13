using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string PanelKey = "rts.actionreplay.config.panel";

    public bool Execute() => Ensure();

    public bool Ensure()
    {
        EnsureObject(PlayerKey, CreatePlayerDefaults());
        EnsureObject(PanelKey, CreatePanelDefaults());
        return true;
    }

    public bool Migrate()
    {
        var player = CreatePlayerDefaults();
        var panel = CreatePanelDefaults();
        MigratePositions(player);
        MigratePanelPositions(panel);
        MigratePanelSize(panel);
        Save(PlayerKey, player);
        Save(PanelKey, panel);
        return true;
    }

    public bool GetPlayer()
    {
        CPH.SetArgument("replayPlayerConfig", Read(PlayerKey).ToString(Newtonsoft.Json.Formatting.None));
        return true;
    }

    public bool GetPanel()
    {
        CPH.SetArgument("replayPanelConfig", Read(PanelKey).ToString(Newtonsoft.Json.Formatting.None));
        return true;
    }

    private JObject CreatePlayerDefaults() => new JObject
    {
        ["version"] = 1,
        ["positions"] = new JObject
        {
            ["Full Screen"] = new JObject
            {
                ["name"] = "Full Screen", ["tag"] = "full-screen", ["scale"] = 100,
                ["x"] = 0, ["y"] = 0, ["rotateX"] = 0, ["rotateY"] = 0, ["rotateZ"] = 0
            }
        },
        ["animationProfiles"] = new JArray
        {
            new JObject { ["id"] = "default", ["name"] = "Default", ["startSequence"] = new JArray(), ["endSequence"] = new JArray() }
        },
        ["animation"] = new JObject
        {
            ["selectedProfile"] = "default",
            ["entryPoints"] = new JObject
            {
                ["obs"] = "default", ["twitch"] = "default", ["recent"] = "default",
                ["catalog"] = "default", ["playlist"] = "default"
            }
        }
    };

    private JObject CreatePanelDefaults() => new JObject
    {
        ["version"] = 1,
        ["width"] = 500,
        ["height"] = 700,
        ["positions"] = new JObject(),
        ["animationProfiles"] = new JArray
        {
            new JObject { ["id"] = "default", ["name"] = "Default", ["startSequence"] = new JArray(), ["endSequence"] = new JArray() }
        },
        ["animation"] = new JObject
        {
            ["entryPoints"] = new JObject
            {
                ["recent"] = "default", ["playlist"] = "default",
                ["creatorLeaderboard"] = "default", ["playbackLeaderboard"] = "default"
            }
        }
    };

    private void MigratePositions(JObject player)
    {
        var raw = CPH.GetGlobalVar<string>("rts.actionreplay.positions", true);
        if (string.IsNullOrWhiteSpace(raw)) return;
        try
        {
            var positions = JObject.Parse(raw);
            if (positions.Count > 0) player["positions"] = positions;
        }
        catch (Exception ex)
        {
            CPH.LogWarn("RTS Action Replay: player position JSON migration failed: " + ex.Message);
        }
    }

    private void MigratePanelPositions(JObject panel)
    {
        var raw = CPH.GetGlobalVar<string>("rts.actionreplay.panel.positions", true);
        if (string.IsNullOrWhiteSpace(raw)) return;
        try
        {
            var positions = JObject.Parse(raw);
            if (positions.Count > 0) panel["positions"] = positions;
        }
        catch (Exception ex)
        {
            CPH.LogWarn("RTS Action Replay: panel position JSON migration failed: " + ex.Message);
        }
    }

    private void MigratePanelSize(JObject panel)
    {
        var width = CPH.GetGlobalVar<int?>("rts.actionreplay.panel.width", true);
        var height = CPH.GetGlobalVar<int?>("rts.actionreplay.panel.height", true);
        if (width.HasValue) panel["width"] = width.Value;
        if (height.HasValue) panel["height"] = height.Value;
    }

    private JObject Read(string key)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(raw)) return new JObject();
        try { return JObject.Parse(raw); }
        catch { return new JObject(); }
    }

    private void EnsureObject(string key, JObject defaults)
    {
        var current = Read(key);
        if (current.Count == 0) Save(key, defaults);
    }

    private void Save(string key, JObject value)
    {
        CPH.SetGlobalVar(key, value.ToString(Newtonsoft.Json.Formatting.None), true);
    }
}
