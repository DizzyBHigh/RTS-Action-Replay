using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    const string PlayerKey="rts.actionreplay.config.player", PanelKey="rts.actionreplay.config.panel", PresetsKey="rts.actionreplay.config.presets", AnimationKey="rts.actionreplay.config.animation";
    const string PlayerOperationKey="rts.actionreplay.operation.player", PanelOperationKey="rts.actionreplay.operation.panel", EventName="RTS-Action Replay";

    public bool Execute()=>false;

    public bool ResolvePlayer()
    {
        var op=Read(PlayerOperationKey); if(op==null)return false;
        ApplyObject(op); var config=Read(PlayerKey); var entry=Entry(config,"play");
        var animation=Animation(config,"player",(string)op["animationProfileId"]??(string)entry["animationProfile"]);
        var design=(string)op["designPresetId"]??(string)entry["designPreset"]??(string)entry["visualPreset"]??"broadcast";
        var title=(string)op["titlePresetId"]??(string)entry["titlePreset"]??"default";
        var brand=(string)op["brandingPresetId"]??(string)entry["brandingPreset"]??"default";
        if((bool?)entry["useSourcePlatformBranding"]==true){var b=PlatformBranding((string)op["replaySource"]??"");if(b!=null)brand=(string)b["id"]??brand;}
        ApplyPresentation(design,title,brand,animation,false); CPH.TriggerEvent(EventName,true); CPH.UnsetGlobalVar(PlayerOperationKey,false); return true;
    }

    public bool ResolvePanel()
    {
        var op=Read(PanelOperationKey); if(op==null)return false;
        ApplyObject(op); var config=Read(PanelKey); var type=(string)op["panelType"]??"recent"; var entry=Entry(config,type);
        var animation=Animation(config,"panel",(string)entry["animationProfile"]);
        ApplyPresentation((string)entry["designPreset"]??(string)entry["visualPreset"]??"broadcast",(string)entry["titlePreset"]??"default",(string)entry["brandingPreset"]??"default",animation,true);
        CPH.TriggerEvent(EventName,true); CPH.UnsetGlobalVar(PanelOperationKey,false); return true;
    }

    JObject Entry(JObject config,string id)
    {
        var entries=config["entryPoints"] as JObject??new JObject(); return entries[id] as JObject??entries["recent"] as JObject??new JObject();
    }

    JObject Animation(JObject config,string target,string profileId)
    {
        profileId=string.IsNullOrWhiteSpace(profileId)?"default":profileId;
        var store=Read(AnimationKey); var item=(store[target] as JObject)?[profileId] as JObject??new JObject();
        var positions=((Read(PresetsKey)["positions"] as JObject)?[target] as JObject)??new JObject();
        var start=StoredSequence(item["startSequence"] as JArray,positions,target=="panel"?"Centered":"Full Screen");
        var end=StoredSequence(item["endSequence"] as JArray,positions,target=="panel"?"Centered":"Full Screen");
        return new JObject{["id"]=profileId,["name"]=profileId,["start"]=start.Count>0?start:DefaultSequence(target),["end"]=end.Count>0?end:DefaultSequence(target),["positions"]=positions};
    }

    JArray StoredSequence(JArray source,JObject positions,string fallback)
    {
        var result=new JArray(); foreach(var token in source??new JArray()){var row=token as JObject??new JObject();var name=(string)row["position"]??fallback;var p=positions[name] as JObject;result.Add(new JObject{["position"]=(string)p?["tag"]??name,["duration"]=(int?)row["duration"]??0,["delay"]=(int?)row["delay"]??0,["easing"]=(string)row["easing"]??"ease-in-out"});} return result;
    }

    JArray DefaultSequence(string target)=>new JArray(new JObject{["position"]=target=="panel"?"centered":"full-screen",["duration"]=0,["delay"]=0,["easing"]="ease-in-out"});

    void ApplyPresentation(string designId,string titleId,string brandId,JObject animation,bool panel)
    {
        var presets=Read(PresetsKey);var d=Find(presets["visual"] as JArray,designId)??Find(presets["visual"] as JArray,"broadcast");
        var t=Find(presets["title"] as JArray,titleId)??Find(presets["title"] as JArray,"default");
        var b=Find(presets["branding"] as JArray,brandId)??Find(presets["branding"] as JArray,"default");if(d==null||t==null||b==null)return;
        var design=(string)d["design"]??(string)d["id"]??"broadcast";var positions=((JObject)animation["positions"]??new JObject()).ToString(Newtonsoft.Json.Formatting.None);
        if(panel){CPH.SetArgument("replayPanelPositions",positions);CPH.SetArgument("replayPanelAnimation",animation.ToString(Newtonsoft.Json.Formatting.None));}
        else{CPH.SetArgument("replayAnimationProfile",animation.ToString(Newtonsoft.Json.Formatting.None));CPH.SetArgument("replayAnimationProfileId",(string)animation["id"]);CPH.SetArgument("replayPlayerPositions",positions);CPH.SetArgument("replayPositions",positions);var start=animation["start"] as JArray??new JArray();var end=animation["end"] as JArray??new JArray();CPH.SetArgument("replayStartPosition",(string)start[0]?["position"]??"full-screen");CPH.SetArgument("replayEndPosition",(string)end[end.Count-1]?["position"]??"full-screen");}
        CPH.SetArgument("replayPanelPreset",design);CPH.SetArgument("replayPanelPrimaryColor",(string)b["primaryColor"]??"#0384CBFF");CPH.SetArgument("replayPanelSecondaryColor",(string)b["secondaryColor"]??"#101416FF");CPH.SetArgument("replayPanelTitleFont",(string)b["font"]??"Inter");CPH.SetArgument("replayPanelTitleSize",(int?)b["fontSize"]??34);CPH.SetArgument("replayPanelTitleColor",(string)b["textColor"]??"#FFFFFFFF");CPH.SetArgument("replayPanelListColor",(string)b["textColor"]??"#FFFFFFFF");
        CPH.SetArgument("replayShowTitle",(bool?)t["showTitle"]??true);CPH.SetArgument("replayTitleDecorationPosition",(string)t["decorationPosition"]??"Prefix");CPH.SetArgument("replayTitleDecoration",(string)t["decoration"]??"Action Replay -");CPH.SetArgument("replayTitlePosition",(string)t["position"]??"Bottom");CPH.SetArgument("replayTitleAnimation",(string)t["animation"]??"Left to right");CPH.SetArgument("replayTitleDelay",(int?)t["delay"]??2000);CPH.SetArgument("replayTitleDuration",(int?)t["duration"]??10000);CPH.SetArgument("replayTitleAnimationDuration",(int?)t["animationDuration"]??1000);
        CPH.SetArgument("replayTitleFont",(string)b["font"]??"Inter");CPH.SetArgument("replayTitleFontSize",(int?)b["fontSize"]??34);CPH.SetArgument("replayTitleTextColor",(string)b["textColor"]??"#FFFFFFFF");CPH.SetArgument("replayTitleShadowColor",(string)b["shadowColor"]??"#000000FF");CPH.SetArgument("replayTitlePrimaryColor",(string)b["primaryColor"]??"#0384CBFF");CPH.SetArgument("replayTitleSecondaryColor",(string)b["secondaryColor"]??"#101416FF");CPH.SetArgument("replayBrandLogoUrl",(string)b["logo"]??"");CPH.SetArgument("replayBrandFallbackText",(string)b["fallbackText"]??"RTS");CPH.SetArgument("replayBrandLabel",(string)b["brandLabel"]??"ACTION REPLAY");CPH.SetArgument("replayBrandFallbackTextColor",(string)b["primaryColor"]??"#0384CBFF");CPH.SetArgument("replayBrandLabelColor",(string)b["textColor"]??"#FFFFFFFF");
        Props("replayBroadcast",Broadcast(d,b));Props("replayCut",Cut(d,b));CPH.SetArgument("replayDesignPresetId",(string)d["id"]??"broadcast");CPH.SetArgument("replayTitlePresetId",(string)t["id"]??"default");CPH.SetArgument("replayBrandingPresetId",(string)b["id"]??"default");
        if(!panel){var source=CPH.GetGlobalVar<string>("rts.actionreplay.frameColorSource",true)??"Custom";var frame=CPH.GetGlobalVar<string>("rts.actionreplay.frameColor",true)??"#0384CBFF";if(!string.Equals(source,"Custom",StringComparison.OrdinalIgnoreCase))frame=(string)b[string.Equals(source,"Branding Secondary",StringComparison.OrdinalIgnoreCase)?"secondaryColor":"primaryColor"]??frame;CPH.SetArgument("replayFrameColor",frame);CPH.SetArgument("replayBorderGlow",CPH.GetGlobalVar<bool?>("rts.actionreplay.borderGlow",true)??true);CPH.SetArgument("replayBorderWidth",CPH.GetGlobalVar<int?>("rts.actionreplay.borderWidth",true)??4);CPH.SetArgument("replayCornerRadius",CPH.GetGlobalVar<int?>("rts.actionreplay.cornerRadius",true)??0);}
    }

    JObject Broadcast(JObject d,JObject b)=>new JObject{["primaryColor"]=(string)b["primaryColor"]??"#0384CBFF",["secondaryColor"]=(string)b["secondaryColor"]??"#101416FF",["chevronHeight"]=(int?)d["chevronHeight"]??42,["randomHeight"]=(bool?)d["randomHeight"]??false,["chevronWidth"]=(int?)d["chevronWidth"]??42,["randomWidth"]=(bool?)d["randomWidth"]??false,["chevronSpacing"]=(int?)d["chevronSpacing"]??0,["randomSpacing"]=(bool?)d["randomSpacing"]??false,["chevronSpeed"]=(int?)d["chevronSpeed"]??95,["decorationColor"]=(string)b["titlePrefixSuffixColor"]??"#0384CBFF",["titleColor"]=(string)b["titleColor"]??"#FFFFFFFF"};
    JObject Cut(JObject d,JObject b)=>new JObject{["primaryColor"]=(string)b["primaryColor"]??"#0384CBFF",["secondaryColor"]=(string)b["secondaryColor"]??"#101416FF",["blockWidth"]=(int?)d["blockWidth"]??170,["randomWidth"]=(bool?)d["randomWidth"]??true,["barHeight"]=(int?)d["barHeight"]??5,["decorationColor"]=(string)b["titlePrefixSuffixColor"]??"#0384CBFF",["titleColor"]=(string)b["titleColor"]??"#FFFFFFFF"};

    void Props(string prefix,JObject value)
    {
        foreach(var property in value?.Properties() ?? new JProperty[0])
        {
            var name=property.Name.Length==0 ? "" : char.ToUpperInvariant(property.Name[0])+property.Name.Substring(1);
            var v=property.Value;
            object o=v.Type==JTokenType.Boolean ? (object)(bool)v : v.Type==JTokenType.Integer ? (object)(int)v : v.ToString();
            CPH.SetArgument(prefix+name,o);
        }
    }

    JObject PlatformBranding(string platform){foreach(var x in Read(PresetsKey)["branding"] as JArray??new JArray()){var b=x as JObject;if(b!=null&&string.Equals((string)b["platform"],platform,StringComparison.OrdinalIgnoreCase))return b;}return null;}
    void ApplyObject(JObject value){foreach(var p in value.Properties()){var v=p.Value;object o=v.Type==JTokenType.Boolean?(object)(bool)v:v.Type==JTokenType.Integer?(object)(int)v:v.Type==JTokenType.Float?(object)(double)v:v.ToString();CPH.SetArgument(p.Name,o);}}
    JObject Find(JArray values,string id){foreach(var x in values??new JArray())if(string.Equals((string)x["id"],id,StringComparison.OrdinalIgnoreCase))return x as JObject;return null;}
    JObject Read(string key){var raw=key==PlayerOperationKey||key==PanelOperationKey?CPH.GetGlobalVar<string>(key,false):CPH.GetGlobalVar<string>(key,true);try{return string.IsNullOrWhiteSpace(raw)?new JObject():JObject.Parse(raw);}catch{return new JObject();}}
}
