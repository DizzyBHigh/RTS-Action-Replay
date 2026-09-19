using System;
using Newtonsoft.Json.Linq;

// Shared position storage and runtime handoff for replay positions.
public class CPHInline
{
    const string PresetsKey="rts.actionreplay.config.presets";
    const string HandoffKey="rts.actionreplay.handoff.playerPositions";

    public bool EnsurePositions()
    {
        var presets=Read(PresetsKey);
        var positions=presets["positions"] as JObject ?? new JObject();
        positions["player"]=EnsureSet(positions["player"] as JObject,"Full Screen","full-screen",100);
        positions["panel"]=EnsureSet(positions["panel"] as JObject,"Centered","centered",100);
        positions["clapperboard"]=EnsureSet(positions["clapperboard"] as JObject,"Centered","centered",50);
        presets["positions"]=positions;
        Save(PresetsKey,presets);
        return true;
    }

    public bool GetPlayerPositions(){EnsurePositions();var presets=Read(PresetsKey);var positions=(presets["positions"] as JObject)?["player"] as JObject??new JObject();CPH.SetGlobalVar(HandoffKey,positions.ToString(Newtonsoft.Json.Formatting.None),false);return true;}
    public bool GetPanelPositions(){EnsurePositions();var presets=Read(PresetsKey);var positions=(presets["positions"] as JObject)?["panel"] as JObject??new JObject();CPH.SetGlobalVar("rts.actionreplay.handoff.panelPositions",positions.ToString(Newtonsoft.Json.Formatting.None),false);return true;}
    public bool GetClapperboardPositions(){EnsurePositions();var presets=Read(PresetsKey);var positions=(presets["positions"] as JObject)?["clapperboard"] as JObject??new JObject();CPH.SetGlobalVar("rts.actionreplay.handoff.clapperPositions",positions.ToString(Newtonsoft.Json.Formatting.None),false);return true;}

    JObject EnsureSet(JObject value,string name,string tag,int scale)
    {
        if(value!=null&&value.Count>0)return value;
        return Position(name,tag,scale);
    }
    JObject Read(string key){var raw=CPH.GetGlobalVar<string>(key,true);try{return string.IsNullOrWhiteSpace(raw)?new JObject():JObject.Parse(raw);}catch{return new JObject();}}
    void Save(string key,JObject value)=>CPH.SetGlobalVar(key,value.ToString(Newtonsoft.Json.Formatting.None),true);
    JObject Position(string name,string tag,int scale)=>new JObject{[name]=new JObject{["name"]=name,["tag"]=tag,["scale"]=scale,["scaleX"]=scale,["scaleY"]=scale,["x"]=0,["y"]=0,["z"]=0,["rotateX"]=0,["rotateY"]=0,["rotateZ"]=0,["fov"]=90}};
}
