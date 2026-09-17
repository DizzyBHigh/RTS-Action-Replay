using System;
using Newtonsoft.Json.Linq;

// Shared position storage and runtime handoff for replay positions.
public class CPHInline
{
    const string PresetsKey="rts.actionreplay.config.presets";
    const string PlayerKey="rts.actionreplay.config.player";
    const string PanelKey="rts.actionreplay.config.panel";
    const string ClapperKey="rts.actionreplay.clapper.positions";
    const string HandoffKey="rts.actionreplay.handoff.playerPositions";

    public bool EnsurePositions()
    {
        var presets=Read(PresetsKey);
        var positions=presets["positions"] as JObject ?? new JObject();
        positions["player"]=MigratePlayer(positions["player"] as JObject);
        positions["panel"]=MigratePanel(positions["panel"] as JObject);
        positions["clapperboard"]=MigrateClapper(positions["clapperboard"] as JObject);
        presets["positions"]=positions;
        Save(PresetsKey,presets);
        return true;
    }

    public bool GetPlayerPositions()
    {
        EnsurePositions();
        var presets=Read(PresetsKey);
        var positions=(presets["positions"] as JObject)?["player"] as JObject ?? DefaultPlayer();
        CPH.SetGlobalVar(HandoffKey,positions.ToString(Newtonsoft.Json.Formatting.None),false);
        return true;
    }

    public bool GetPanelPositions()
    {
        EnsurePositions();
        var presets=Read(PresetsKey);
        var positions=(presets["positions"] as JObject)?["panel"] as JObject ?? DefaultPanel();
        CPH.SetGlobalVar("rts.actionreplay.handoff.panelPositions",positions.ToString(Newtonsoft.Json.Formatting.None),false);
        return true;
    }

    public bool GetClapperboardPositions()
    {
        EnsurePositions();
        var presets=Read(PresetsKey);
        var positions=(presets["positions"] as JObject)?["clapperboard"] as JObject ?? DefaultClapper();
        CPH.SetGlobalVar("rts.actionreplay.handoff.clapperPositions",positions.ToString(Newtonsoft.Json.Formatting.None),false);
        return true;
    }

    JObject MigratePlayer(JObject current)=>current!=null&&current.Count>0?current:ReadNested(PlayerKey,"positions",DefaultPlayer());
    JObject MigratePanel(JObject current)=>current!=null&&current.Count>0?current:ReadNested(PanelKey,"positions",DefaultPanel());
    JObject MigrateClapper(JObject current)=>current!=null&&current.Count>0?current:ReadRaw(ClapperKey,DefaultClapper());

    JObject ReadNested(string key,string property,JObject fallback){var root=Read(key);var value=root[property] as JObject;return value!=null&&value.Count>0?value:fallback;}
    JObject ReadRaw(string key,JObject fallback){var raw=CPH.GetGlobalVar<string>(key,true);try{var value=string.IsNullOrWhiteSpace(raw)?null:JObject.Parse(raw);return value!=null&&value.Count>0?value:fallback;}catch{return fallback;}}
    JObject Read(string key){var raw=CPH.GetGlobalVar<string>(key,true);try{return string.IsNullOrWhiteSpace(raw)?new JObject():JObject.Parse(raw);}catch{return new JObject();}}
    void Save(string key,JObject value)=>CPH.SetGlobalVar(key,value.ToString(Newtonsoft.Json.Formatting.None),true);

    JObject DefaultPlayer()=>Position("Full Screen","full-screen",100);
    JObject DefaultPanel()=>Position("Centered","centered",100);
    JObject DefaultClapper()=>Position("Centered","centered",50);
    JObject Position(string name,string tag,int scale)=>new JObject{[name]=new JObject{["name"]=name,["tag"]=tag,["scale"]=scale,["scaleX"]=scale,["scaleY"]=scale,["x"]=0,["y"]=0,["z"]=0,["rotateX"]=0,["rotateY"]=0,["rotateZ"]=0,["fov"]=90}};
}
