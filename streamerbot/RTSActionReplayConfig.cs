using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string LegacyPlayerPositionsKey = "rts.actionreplay.positions";
    private const string LegacyPanelPositionsKey = "rts.actionreplay.panel.positions";
    private const string LegacyPanelWidthKey = "rts.actionreplay.panel.width";
    private const string LegacyPanelHeightKey = "rts.actionreplay.panel.height";

    public bool Execute() => Ensure();

    public bool Ensure()
    {
        EnsurePlayer();
        EnsureObject(PanelKey, CreatePanelDefaults());
        return true;
    }

    public bool Migrate()
    {
        MigratePlayerPositions();
        MigratePanel();
        return true;
    }

    public bool GetPlayer()
    {
        EnsurePlayer();
        CPH.SetArgument("replayPlayerConfig", Read(PlayerKey).ToString(Newtonsoft.Json.Formatting.None));
        return true;
    }

    public bool GetPlayerPositions()
    {
        EnsurePlayer();
        var player = Read(PlayerKey);
        CPH.SetArgument("replayPositions", ((JObject)player["positions"] ?? new JObject()).ToString(Newtonsoft.Json.Formatting.None));
        return true;
    }

    public bool GetPanel()
    {
        EnsureObject(PanelKey, CreatePanelDefaults());
        CPH.SetArgument("replayPanelConfig", Read(PanelKey).ToString(Newtonsoft.Json.Formatting.None));
        return true;
    }

    private void EnsurePlayer()
    {
        var current = Read(PlayerKey);
        if (current.Count > 0) return;

        var player = CreatePlayerDefaults();
        MigratePositionsInto(player);
        Save(PlayerKey, player);
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

    private void MigratePlayerPositions()
    {
        var player = Read(PlayerKey);
        if (player.Count == 0) player = CreatePlayerDefaults();
        MigratePositionsInto(player);
        Save(PlayerKey, player);
    }

    private void MigratePositionsInto(JObject player)
    {
        var raw = CPH.GetGlobalVar<string>(LegacyPlayerPositionsKey, true);
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

    private void MigratePanel()
    {
        var panel = Read(PanelKey);
        if (panel.Count == 0) panel = CreatePanelDefaults();
        MigratePanelPositions(panel);
        MigratePanelSize(panel);
        Save(PanelKey, panel);
    }

    private void MigratePanelPositions(JObject panel)
    {
        var raw = CPH.GetGlobalVar<string>(LegacyPanelPositionsKey, true);
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
        var width = CPH.GetGlobalVar<int?>(LegacyPanelWidthKey, true);
        var height = CPH.GetGlobalVar<int?>(LegacyPanelHeightKey, true);
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
