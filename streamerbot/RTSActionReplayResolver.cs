using System;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    const string PlayerKey="rts.actionreplay.config.player", PanelKey="rts.actionreplay.config.panel", MessageKey="rts.actionreplay.config.message", PresetsKey="rts.actionreplay.config.presets", AnimationKey="rts.actionreplay.config.animation";
    const string PlayerOperationKey="rts.actionreplay.operation.player", PanelOperationKey="rts.actionreplay.operation.panel", MessageOperationKey="rts.actionreplay.operation.message", EventName="RTS-Action Replay";

    private const string ClapperKey = "rts.actionreplay.config.clapper";
    private const string EntryPointHandoffKey = "rts.actionreplay.handoff.entryPoint";
    private const string ResolvedProfileHandoffKey = "rts.actionreplay.handoff.resolvedProfile";
    private const string PlayerPositionsHandoffKey = "rts.actionreplay.handoff.playerPositions";
    private const string ConfigurationSnapshotKey = "rts.actionreplay.handoff.configurationSnapshot";


    public bool Execute()=>EnsureProfiles();

    public bool SendConfigurationToOverlay()
    {
        CPH.LogInfo("RTS Action Replay: configuration test requested.");
        if (!CPH.ExecuteMethod("RTS - Action Replay - Core - Store", "GetConfigurationSnapshot"))
        {
            CPH.LogWarn("RTS Action Replay: Store configuration snapshot method failed.");
            return false;
        }

        var config = CPH.GetGlobalVar<string>(ConfigurationSnapshotKey, false);
        if (string.IsNullOrWhiteSpace(config))
        {
            CPH.LogWarn("RTS Action Replay: configuration snapshot was not produced.");
            return false;
        }

        CPH.SetArgument("replayConfig", config);
        CPH.SetArgument("replayCommand", "config-test");
        CPH.TriggerEvent(EventName, true);
        return true;
    }

    public bool ResolveClapperboardBranding()
    {
        var replayId = CPH.GetGlobalVar<string>("rts.actionreplay.handoff.replayId", false) ?? "";
        var catalogData = Read("rts.actionreplay.data");
        var catalog = catalogData["catalog"] as JArray ?? new JArray();
        JObject replay = null;
        foreach (var item in catalog)
        {
            var candidate = item as JObject;
            if (candidate != null && string.Equals((string)candidate["id"], replayId, StringComparison.OrdinalIgnoreCase))
            {
                replay = candidate;
                break;
            }
        }

        var creator = replay?["creator"] as JObject;
        var creatorPlatform = (string)creator?["platform"] ?? "";
        CPH.SetGlobalVar("rts.actionreplay.handoff.clapperCreatorPlatform", creatorPlatform, false);

        var clapper = Read(ClapperKey);
        var entry = clapper["entryPoint"] as JObject ?? new JObject();
        var brand = (string)entry["brandingPreset"] ?? "default";
        var useSource = CPH.GetGlobalVar<bool?>("rts.actionreplay.clapper.useSourcePlatformBranding", true) ?? false;
        if (useSource)
        {
            var source = CPH.TryGetArg("replaySourcePlatform",out string testSource)&&!string.IsNullOrWhiteSpace(testSource)?testSource:creatorPlatform;
            var sourceBrand = PlatformBranding(source);
            if (sourceBrand != null) brand = (string)sourceBrand["id"] ?? brand;
        }
        var presets = Read(PresetsKey);
        var branding = Find(presets["branding"] as JArray, brand) ?? Find(presets["branding"] as JArray, "default");
        if (branding == null) return false;
        CPH.SetArgument("replayMessageBoardColor", (string)branding["textColor"] ?? "#FFFFFFFF");
        CPH.SetArgument("replayMessageTextColor", (string)branding["titleColor"] ?? "#FFFFFFFF");
        CPH.SetArgument("replayMessageStripeLight", (string)branding["primaryColor"] ?? "#0384CBFF");
        CPH.SetArgument("replayMessageStripeDark", (string)branding["secondaryColor"] ?? "#101416FF");
        CPH.SetArgument("replayMessageAccent", (string)branding["shadowColor"] ?? "#000000FF");
        CPH.SetArgument("replayBrandPrimaryColor", (string)branding["primaryColor"] ?? "#0384CBFF");
        CPH.SetArgument("replayBrandSecondaryColor", (string)branding["secondaryColor"] ?? "#101416FF");
        CPH.SetArgument("replayBrandFallbackTextColor", (string)branding["primaryColor"] ?? "#0384CBFF");
        CPH.SetArgument("replayBrandLabelColor", (string)branding["textColor"] ?? "#FFFFFFFF");
        CPH.SetArgument("replayBrandLogoUrl", (string)branding["logo"] ?? "");
        CPH.SetArgument("replayBrandFallbackText", (string)branding["fallbackText"] ?? "RTS");
        CPH.SetArgument("replayBrandLabel", (string)branding["brandLabel"] ?? "ACTION REPLAY");
        CPH.SetArgument("replayMessageFont", (string)branding["font"] ?? "Inter");
        CPH.SetArgument("replayBrandingPresetId", (string)branding["id"] ?? "default");
        CPH.SetGlobalVar("rts.actionreplay.handoff.clapperBranding", branding.ToString(Newtonsoft.Json.Formatting.None), false);
        CPH.SetArgument("replayClapperDuration", CPH.GetGlobalVar<int?>("rts.actionreplay.clapper.duration", true) ?? 5000);
        return true;
    }

    public bool ResolvePlayerConfiguration()
    {
        var config=Read(PlayerKey); var entry=Entry(config,"play");
        var design=(string)entry["designPreset"]??(string)entry["visualPreset"]??"broadcast";
        var title=(string)entry["titlePreset"]??"default";
        var brand=(string)entry["brandingPreset"]??"default";
        var replayCreated=false; if(CPH.TryGetArg("replayCreated",out string created)) bool.TryParse(created,out replayCreated);
        var useSourcePlatformBranding=(bool?)entry["useSourcePlatformBranding"]==true;
        var changePlayerBrandingToClipSource=(bool?)entry["changePlayerBrandingToClipSource"]==true;
        if(changePlayerBrandingToClipSource){var source=CPH.TryGetArg("replaySourcePlatform",out string testSource)&&!string.IsNullOrWhiteSpace(testSource)?testSource:ResolveReplaySourcePlatform();var b=PlatformBranding(source);if(b!=null)brand=(string)b["id"]??brand;}
        else if(replayCreated&&useSourcePlatformBranding){CPH.TryGetArg("replaySource",out string source);var b=PlatformBranding(source??"");if(b!=null)brand=(string)b["id"]??brand;}
        CPH.SetArgument("animationProfile",(string)entry["animationProfile"]??"default");
        CPH.SetArgument("visualPreset",design); CPH.SetArgument("designPreset",design); CPH.SetArgument("titlePreset",title); CPH.SetArgument("brandingPreset",brand);
        CPH.SetArgument("useSourcePlatformBranding",useSourcePlatformBranding);
        CPH.SetArgument("changePlayerBrandingToClipSource",(bool?)entry["changePlayerBrandingToClipSource"]??false);
        return true;
    }

    public bool ResolvePlayer()
    {
        var op=Read(PlayerOperationKey); if(op==null)return false;
        ApplyObject(op); var config=Read(PlayerKey); var entry=Entry(config,"play");
        var animation=Animation(config,"player",(string)op["animationProfileId"]??(string)entry["animationProfile"]);
        var design=(string)op["designPresetId"]??(string)entry["designPreset"]??(string)entry["visualPreset"]??"broadcast";
        var title=(string)op["titlePresetId"]??(string)entry["titlePreset"]??"default";
        var brand=(string)entry["brandingPreset"]??"default";
        var replayCreated=(bool?)op["replayCreated"]==true;
        var useCreateBranding=replayCreated&&(bool?)entry["useSourcePlatformBranding"]==true;
        var useSourceBranding=(bool?)entry["changePlayerBrandingToClipSource"]==true;
        if(useSourceBranding){var source=(string)op["replaySourcePlatform"];if(string.IsNullOrWhiteSpace(source))source=ResolveReplaySourcePlatform((string)op["replayId"]);var b=PlatformBranding(source);if(b!=null)brand=(string)b["id"]??brand;}
        else if(useCreateBranding){var source=(string)op["replaySourcePlatform"]??(string)op["replaySource"]??"";var b=PlatformBranding(source);if(b!=null)brand=(string)b["id"]??brand;}
        ApplyPresentation(design,title,brand,animation,"player",useSourceBranding||useCreateBranding); CPH.LogInfo("RTS Action Replay TRACE: Player colours resolved; event arguments set for frame/control/branding."); CPH.TriggerEvent(EventName,true); CPH.UnsetGlobalVar(PlayerOperationKey,false); return true;
    }

    public bool ResolvePanel()
    {
        var op=Read(PanelOperationKey); if(op==null)return false;
        ApplyObject(op); var config=Read(PanelKey); var type=(string)op["panelType"]??"recent"; var entry=Entry(config,type);
        var animation=Animation(config,"panel",(string)entry["animationProfile"]); var brand=(string)entry["brandingPreset"]??"default"; var useSource=(bool?)config["useSourcePlatformBranding"]==true;
        if(useSource){var source=(string)op["requesterPlatform"]??(string)op["platform"]??"";if(string.IsNullOrWhiteSpace(source))CPH.TryGetArg("userType",out source);var b=PlatformBranding(source);if(b!=null)brand=(string)b["id"]??brand;}
        ApplyPresentation((string)entry["designPreset"]??(string)entry["visualPreset"]??"broadcast",(string)entry["titlePreset"]??"default",brand,animation,"panel",useSource);
        if((bool?)op["triggerEvent"]!=false) CPH.TriggerEvent(EventName,true); CPH.UnsetGlobalVar(PanelOperationKey,false); return true;
    }

    string ResolveReplaySourcePlatform(string replayId=null)
    {
        if(string.IsNullOrWhiteSpace(replayId)) CPH.TryGetArg("replayId",out replayId);
        if(string.IsNullOrWhiteSpace(replayId)) return "";
        var catalog=Read("rts.actionreplay.data")["catalog"] as JArray??new JArray();
        foreach(var item in catalog)
        {
            var replay=item as JObject;
            if(replay!=null&&string.Equals((string)replay["id"],replayId,StringComparison.OrdinalIgnoreCase))
                return (string)(replay["creator"] as JObject)?["platform"]??"";
        }
        return "";
    }

    JObject Entry(JObject config,string id)
    {
        var entries=config["entryPoints"] as JObject??new JObject(); return entries[id] as JObject??entries["recent"] as JObject??new JObject();
    }


    private JObject ReadOperation(string key)
    {
        var raw = CPH.GetGlobalVar<string>(key, false);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try { return JObject.Parse(raw); } catch { return null; }
    }

    public bool ResolveMessagePresentation()
    {
        try
        {
            CPH.LogInfo("RTS Action Replay TRACE: ResolveMessagePresentation entered.");

            var op = ReadOperation(MessageOperationKey);
            if (op == null || op.Count == 0)
            {
                CPH.LogWarn("RTS Action Replay TRACE: message operation was empty.");
                return false;
            }
            CPH.LogInfo("RTS Action Replay TRACE: message operation read.");

            var message = ReadConfig(MessageKey, CreateMessageDefaults());
            CPH.LogInfo("RTS Action Replay TRACE: message configuration read.");
        var entry = message["entryPoint"] as JObject ?? new JObject();
        var design = (string)entry["designPreset"] ?? "broadcast";
        var brand = (string)entry["brandingPreset"] ?? "default";
        var source = (string)op["messageSourcePlatform"] ?? "";

        if (CPH.GetGlobalVar<bool?>("rts.actionreplay.message.useSourcePlatformBranding", true) == true)
        {
            var sourceBrand = PlatformBranding(source);
            if (sourceBrand != null) brand = (string)sourceBrand["id"] ?? brand;
        }

        CPH.LogInfo("RTS Action Replay TRACE: resolving message animation.");
        var animation = Animation(message, "message", (string)entry["animationProfile"] ?? "default");
        CPH.LogInfo("RTS Action Replay TRACE: message animation resolved.");
        ApplyPresentation(design, "default", brand, animation, "message", false);
        CPH.LogInfo("RTS Action Replay TRACE: message presentation applied.");

        CPH.SetArgument("replayMessage", (string)op["message"] ?? "");
        CPH.SetArgument("messageQueueId", (string)op["messageQueueId"] ?? "");
        CPH.SetArgument("messageTest", (bool?)op["messageTest"] ?? false);
        CPH.SetArgument("replayCommand", "message");
        CPH.SetArgument("replayMessagePosition", (string)op["messagePosition"] ?? "Centered");

        CPH.LogInfo($"RTS Action Replay: message presentation resolved for source={source}, test={(bool?)op["messageTest"] == true}.");
        CPH.TriggerEvent(EventName, true);
        CPH.UnsetGlobalVar(MessageOperationKey, false);
        return true;
        }
        catch (Exception ex)
        {
            CPH.LogError($"RTS Action Replay: ResolveMessagePresentation failed: {ex}");
            return false;
        }
    }

    JObject Animation(JObject config,string target,string profileId)
    {
        profileId=string.IsNullOrWhiteSpace(profileId)?"default":profileId;
        var store=Read(AnimationKey); var item=(store[target] as JObject)?[profileId] as JObject??new JObject();
        var positions=((Read(PresetsKey)["positions"] as JObject)?[target] as JObject)??new JObject();
        var start=StoredSequence(item["startSequence"] as JArray,positions,target=="panel"||target=="message"?"Centered":"Full Screen");
        var end=StoredSequence(item["endSequence"] as JArray,positions,target=="panel"||target=="message"?"Centered":"Full Screen");
        return new JObject{["id"]=profileId,["name"]=profileId,["start"]=start.Count>0?start:DefaultSequence(target),["end"]=end.Count>0?end:DefaultSequence(target),["positions"]=positions};
    }

    JArray StoredSequence(JArray source,JObject positions,string fallback)
    {
        var result=new JArray(); foreach(var token in source??new JArray()){var row=token as JObject??new JObject();var name=(string)row["position"]??fallback;var p=positions[name] as JObject;result.Add(new JObject{["position"]=(string)p?["tag"]??name,["duration"]=(int?)row["duration"]??0,["delay"]=(int?)row["delay"]??0,["easing"]=(string)row["easing"]??"ease-in-out"});} return result;
    }

    JArray DefaultSequence(string target)=>new JArray(new JObject{["position"]=target=="panel"||target=="message"?"centered":"full-screen",["duration"]=0,["delay"]=0,["easing"]="ease-in-out"});

    void ApplyPresentation(string designId,string titleId,string brandId,JObject animation,string target,bool useSourceBranding)
    {
        var presets=Read(PresetsKey);var d=Find(presets["visual"] as JArray,designId)??Find(presets["visual"] as JArray,"broadcast");
        var t=Find(presets["title"] as JArray,titleId)??Find(presets["title"] as JArray,"default");
        var b=Find(presets["branding"] as JArray,brandId)??Find(presets["branding"] as JArray,"default");if(d==null||t==null||b==null)return;
        if(target=="player"&&useSourceBranding){d=(JObject)d.DeepClone();d["backgroundSource"]="Branding Secondary";}
        var design=(string)d["design"]??(string)d["id"]??"broadcast";var positions=((JObject)animation["positions"]??new JObject()).ToString(Newtonsoft.Json.Formatting.None);
        if(target=="panel")
        {
            CPH.SetArgument("replayPanelPositions",positions);CPH.SetArgument("replayPanelAnimation",animation.ToString(Newtonsoft.Json.Formatting.None));
            CPH.SetArgument("replayPanelWidth",CPH.GetGlobalVar<int?>("rts.actionreplay.panel.width",true)??500);CPH.SetArgument("replayPanelHeight",CPH.GetGlobalVar<int?>("rts.actionreplay.panel.height",true)??700);CPH.SetArgument("replayPanelCornerRadius",CPH.GetGlobalVar<int?>("rts.actionreplay.panel.cornerRadius",true)??0);
        }
        else if(target=="message")
        {
            CPH.SetArgument("replayMessagePositions",positions);CPH.SetArgument("replayMessageAnimation",animation.ToString(Newtonsoft.Json.Formatting.None));
            var messageWidth=CPH.GetGlobalVar<int?>("rts.actionreplay.message.width",true)??500;var messageHeight=CPH.GetGlobalVar<int?>("rts.actionreplay.message.height",true)??120;
            CPH.LogInfo($"RTS Action Replay TRACE: message dimensions global width={messageWidth}, height={messageHeight}.");
            CPH.SetArgument("replayMessageWidth",messageWidth);CPH.SetArgument("replayMessageHeight",messageHeight);
            CPH.LogInfo($"RTS Action Replay TRACE: message arguments width={CPH.GetArgument<int?>("replayMessageWidth")}, height={CPH.GetArgument<int?>("replayMessageHeight")}.");
            CPH.SetArgument("replayMessageCornerRadius",CPH.GetGlobalVar<int?>("rts.actionreplay.message.cornerRadius",true)??0);CPH.SetArgument("replayMessageDuration",CPH.GetGlobalVar<int?>("rts.actionreplay.message.duration",true)??5000);
        }
        else
        {
            CPH.SetArgument("replayAnimationProfile",animation.ToString(Newtonsoft.Json.Formatting.None));CPH.SetArgument("replayAnimationProfileId",(string)animation["id"]);CPH.SetArgument("replayPlayerPositions",positions);CPH.SetArgument("replayPositions",positions);
            var start=animation["start"] as JArray??new JArray();var end=animation["end"] as JArray??new JArray();CPH.SetArgument("replayStartPosition",(string)start[0]?["position"]??"full-screen");CPH.SetArgument("replayEndPosition",(string)end[end.Count-1]?["position"]??"full-screen");
        }
        CPH.SetArgument("replayPanelPreset",design);CPH.SetArgument("replayPanelPrimaryColor",PanelPrimaryColor(d,b));CPH.SetArgument("replayPanelSecondaryColor",PanelSecondaryColor(d,b));CPH.SetArgument("replayPanelTitleFont",(string)b["font"]??"Inter");CPH.SetArgument("replayPanelTitleSize",(int?)b["fontSize"]??34);CPH.SetArgument("replayPanelTitleColor",(string)b["textColor"]??"#FFFFFFFF");CPH.SetArgument("replayPanelListColor",(string)b["textColor"]??"#FFFFFFFF");CPH.SetArgument("replayPanelListShadowColor",(string)b["shadowColor"]??"#000000FF");
        CPH.SetArgument("replayTitleDecorationPosition",(string)t["decorationPosition"]??"Prefix");CPH.SetArgument("replayTitleDecoration",(string)t["decoration"]??"Action Replay -");CPH.SetArgument("replayTitlePosition",(string)t["position"]??"Bottom");CPH.SetArgument("replayTitleAnimation",(string)t["animation"]??"Left to right");CPH.SetArgument("replayTitleDelay",(int?)t["delay"]??2000);CPH.SetArgument("replayTitleDuration",(int?)t["duration"]??10000);CPH.SetArgument("replayTitleAnimationDuration",(int?)t["animationDuration"]??1000);
        CPH.SetArgument("replayTitleFont",(string)b["font"]??"Inter");CPH.SetArgument("replayTitleFontSize",(int?)b["fontSize"]??34);CPH.SetArgument("replayTitleTextColor",(string)b["textColor"]??"#FFFFFFFF");CPH.SetArgument("replayTitleShadowColor",(string)b["shadowColor"]??"#000000FF");CPH.SetArgument("replayTitlePrimaryColor",(string)b["primaryColor"]??"#0384CBFF");CPH.SetArgument("replayBrandPrimaryColor",(string)b["primaryColor"]??"#0384CBFF");CPH.SetArgument("replayTitleSecondaryColor",(string)b["secondaryColor"]??"#101416FF");CPH.SetArgument("replayBrandLogoUrl",(string)b["logo"]??"");CPH.SetArgument("replayBrandFallbackText",(string)b["fallbackText"]??"RTS");CPH.SetArgument("replayBrandLabel",(string)b["brandLabel"]??"ACTION REPLAY");CPH.SetArgument("replayBrandFallbackTextColor",(string)b["primaryColor"]??"#0384CBFF");CPH.SetArgument("replayBrandLabelColor",(string)b["textColor"]??"#FFFFFFFF");
        Props("replayBroadcast",Broadcast(d,b));Props("replayCut",Cut(d,b));CPH.SetArgument("replayPanelBackgroundColor",PanelBackgroundColor(d,b));CPH.SetArgument("replayDesignPresetId",(string)d["id"]??"broadcast");CPH.SetArgument("replayTitlePresetId",(string)t["id"]??"default");CPH.SetArgument("replayBrandingPresetId",(string)b["id"]??"default");
        if(target=="player")
        {
            var source=CPH.GetGlobalVar<string>("rts.actionreplay.frameColorSource",true)??"Custom";var frame=CPH.GetGlobalVar<string>("rts.actionreplay.frameColor",true)??"#0384CBFF";
            if(useSourceBranding||!string.Equals(source,"Custom",StringComparison.OrdinalIgnoreCase))frame=(string)b[string.Equals(source,"Branding Secondary",StringComparison.OrdinalIgnoreCase)&&!useSourceBranding?"secondaryColor":"primaryColor"]??frame;
            CPH.SetArgument("replayFrameColor",frame);var controlSource=CPH.GetGlobalVar<string>("rts.actionreplay.controlColorSource",true)??"Branding Primary";var control=CPH.GetGlobalVar<string>("rts.actionreplay.controlColor",true)??"#0384CBFF";
            if(useSourceBranding||!string.Equals(controlSource,"Custom",StringComparison.OrdinalIgnoreCase))control=(string)b[string.Equals(controlSource,"Branding Secondary",StringComparison.OrdinalIgnoreCase)&&!useSourceBranding?"secondaryColor":"primaryColor"]??control;
            CPH.SetArgument("replayControlColor",control);CPH.SetArgument("replayBorderGlow",CPH.GetGlobalVar<bool?>("rts.actionreplay.borderGlow",true)??true);CPH.SetArgument("replayBorderWidth",CPH.GetGlobalVar<int?>("rts.actionreplay.borderWidth",true)??4);CPH.SetArgument("replayCornerRadius",CPH.GetGlobalVar<int?>("rts.actionreplay.cornerRadius",true)??0);
        }
    }

    string PanelPrimaryColor(JObject d,JObject b){var source=((string)d["backgroundSource"]??"").Trim();if((string.Equals((string)d["id"],"cut",StringComparison.OrdinalIgnoreCase)||string.Equals((string)d["id"],"broadcast",StringComparison.OrdinalIgnoreCase))&&string.Equals(source,"RTS Dark Blue",StringComparison.OrdinalIgnoreCase))return "#0384CBFF";return (string)b["primaryColor"]??"#0384CBFF";}
    string PanelSecondaryColor(JObject d,JObject b){var source=((string)d["backgroundSource"]??"").Trim();if((string.Equals((string)d["id"],"cut",StringComparison.OrdinalIgnoreCase)||string.Equals((string)d["id"],"broadcast",StringComparison.OrdinalIgnoreCase))&&string.Equals(source,"RTS Dark Blue",StringComparison.OrdinalIgnoreCase))return "#101416FF";return (string)b["secondaryColor"]??"#101416FF";}
    string PanelBackgroundColor(JObject d,JObject b){var id=(string)d["id"]??"";if(string.Equals(id,"cut",StringComparison.OrdinalIgnoreCase)||string.Equals(id,"broadcast",StringComparison.OrdinalIgnoreCase))return string.Equals(((string)d["backgroundSource"]??"").Trim(),"RTS Dark Blue",StringComparison.OrdinalIgnoreCase)?"#101416FF":string.Equals(((string)d["backgroundSource"]??"").Trim(),"Branding Secondary",StringComparison.OrdinalIgnoreCase)?((string)b["secondaryColor"]??"#101416FF"):((string)d["backgroundColor"]??"#101416FF");return (string)b["secondaryColor"]??"#101416FF";}
    string CutBackground(JObject d,JObject b){var source=((string)d["backgroundSource"]??"Custom").Trim();if(string.Equals(source,"RTS Dark Blue",StringComparison.OrdinalIgnoreCase))return "#101416FF";if(string.Equals(source,"Branding Secondary",StringComparison.OrdinalIgnoreCase))return (string)b["secondaryColor"]??"#101416FF";return (string)d["backgroundColor"]??"#101416FF";}
    JObject Broadcast(JObject d,JObject b)=>new JObject{["primaryColor"]=PanelPrimaryColor(d,b),["secondaryColor"]=PanelSecondaryColor(d,b),["chevronHeight"]=(int?)d["chevronHeight"]??42,["randomHeight"]=(bool?)d["randomHeight"]??false,["chevronWidth"]=(int?)d["chevronWidth"]??42,["randomWidth"]=(bool?)d["randomWidth"]??false,["chevronSpacing"]=(int?)d["chevronSpacing"]??0,["randomSpacing"]=(bool?)d["randomSpacing"]??false,["chevronSpeed"]=(int?)d["chevronSpeed"]??95,["decorationColor"]=(string)b["titlePrefixSuffixColor"]??"#0384CBFF",["titleColor"]=(string)b["titleColor"]??"#FFFFFFFF"};
    JObject Cut(JObject d,JObject b)=>new JObject{["primaryColor"]=(string)b["primaryColor"]??"#0384CBFF",["secondaryColor"]=(string)b["secondaryColor"]??"#101416FF",["backgroundColor"]=CutBackground(d,b),["blockWidth"]=(int?)d["blockWidth"]??170,["randomWidth"]=(bool?)d["randomWidth"]??true,["barHeight"]=(int?)d["barHeight"]??5,["decorationColor"]=(string)b["titlePrefixSuffixColor"]??"#0384CBFF",["titleColor"]=(string)b["titleColor"]??"#FFFFFFFF"};

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

    public bool EnsureProfiles()
    {
        EnsurePlayerProfiles();
        EnsurePanelProfiles();
        EnsureMessageProfiles();
        EnsureClapperProfiles();
        return true;
    }

    public bool EnsureMessageProfiles()
    {
        var message = ReadConfig(MessageKey, CreateMessageDefaults());
        var profiles = NormalizeProfiles(message["animationProfiles"] as JArray);
        EnsureSequenceStore("message", profiles);
        var animation = message["animation"] as JObject ?? new JObject();
        animation["selectedProfile"] = ResolveProfileId(profiles, (string)animation["selectedProfile"]) ?? "default";
        message["animationProfiles"] = profiles;
        message["animation"] = animation;
        if (!(message["entryPoint"] is JObject)) message["entryPoint"] = CreateMessageEntryPoint();
        SaveConfig(MessageKey, message);
        return true;
    }

    public bool AddMessageProfile()
    {
        var message = ReadConfig(MessageKey, CreateMessageDefaults());
        var profiles = NormalizeProfiles(message["animationProfiles"] as JArray);
        var name = CPH.TryGetArg("messageProfileName", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "New Message Profile";
        var id = Guid.NewGuid().ToString("N");
        profiles.Add(CreateMessageProfile(id, name));
        message["animationProfiles"] = profiles;
        SaveConfig(MessageKey, message);
        EnsureSequenceStore("message", profiles);
        return true;
    }

    public bool RemoveMessageProfile()
    {
        if (!CPH.TryGetArg("messageProfileId", out string id) || string.IsNullOrWhiteSpace(id) || id.Trim() == "default") return false;
        id=id.Trim();
        var message=ReadConfig(MessageKey,CreateMessageDefaults());
        var profiles=NormalizeProfiles(message["animationProfiles"] as JArray);
        for(var i=profiles.Count-1;i>=0;i--) if(string.Equals((string)profiles[i]["id"],id,StringComparison.Ordinal)) profiles.RemoveAt(i);
        var animation=message["animation"] as JObject??new JObject();
        animation["selectedProfile"]=ResolveProfileId(profiles,(string)animation["selectedProfile"])??"default";
        message["animationProfiles"]=profiles;
        message["animation"]=animation;
        SaveConfig(MessageKey,message);
        RemoveProfileSequences("message",id);
        return true;
    }

    public bool ResolveMessageAnimation()
    {
        var message=ReadConfig(MessageKey,CreateMessageDefaults());
        var profiles=NormalizeProfiles(message["animationProfiles"] as JArray);
        var animation=message["animation"] as JObject??new JObject();
        var selected=ResolveProfileId(profiles,(string)animation["selectedProfile"])??"default";
        var store=ReadConfig(AnimationKey,new JObject());
        var target=store["message"] as JObject;
        var sequence=target?[selected] as JObject??new JObject();
        var start=sequence["startSequence"] as JArray??DefaultPanelStart();
        var end=sequence["endSequence"] as JArray??DefaultPanelEnd();
        CPH.SetArgument("replayMessageAnimation",new JObject{
            ["id"]=selected,
            ["name"]=(string)profiles.OfType<JObject>().FirstOrDefault(x=>string.Equals((string)x["id"],selected,StringComparison.Ordinal))?["name"]??"Default",
            ["start"]=start,
            ["end"]=end
        }.ToString(Newtonsoft.Json.Formatting.None));
        return true;
    }

    public bool EnsureClapperProfiles()
    {
        var clapper = ReadConfig(ClapperKey, CreateClapperDefaults());
        var profiles = NormalizeProfiles(clapper["animationProfiles"] as JArray);
        EnsureSequenceStore("clapperboard", profiles);
        var animation = clapper["animation"] as JObject ?? new JObject();
        animation["selectedProfile"] = ResolveProfileId(profiles, (string)animation["selectedProfile"]) ?? "default";
        clapper["animationProfiles"] = profiles;
        clapper["animation"] = animation;
        SaveConfig(ClapperKey, clapper);
        return true;
    }

    private void EnsurePlayerProfiles()
    {
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        NormalizePositionConfig(player, false);
        var profiles = NormalizeProfiles(player["animationProfiles"] as JArray);
        EnsureSequenceStore("player", profiles);
        player["animationProfiles"] = profiles;
        var animation = player["animation"] as JObject ?? new JObject();
        animation["selectedProfile"] = ResolveProfileId(profiles, (string)animation["selectedProfile"]) ?? "default";
        animation["entryPoints"] = NormalizeEntryPoints(animation["entryPoints"] as JObject, profiles, new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" });
        player["animation"] = animation;
        SaveConfig(PlayerKey, player);
    }

    private void EnsurePanelProfiles()
    {
        var panel = ReadConfig(PanelKey, CreatePanelDefaults());
        NormalizePositionConfig(panel, true);
        var profiles = NormalizeProfiles(panel["animationProfiles"] as JArray);
        EnsureSequenceStore("panel", profiles);
        panel["animationProfiles"] = profiles;
        var animation = panel["animation"] as JObject ?? new JObject();
        animation["entryPoints"] = NormalizeEntryPoints(animation["entryPoints"] as JObject, profiles, new[] { "recent", "playlist", "creatorLeaderboard" });
        panel["animation"] = animation;
        SaveConfig(PanelKey, panel);
    }

    public bool AddProfile()
    {
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        var profiles = NormalizeProfiles(player["animationProfiles"] as JArray);
        var name = CPH.TryGetArg("profileName", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "New Profile";
        var id = Guid.NewGuid().ToString("N");
        profiles.Add(CreateProfile(id, name));
        player["animationProfiles"] = profiles;
        SaveConfig(PlayerKey, player);
        EnsureSequenceStore("player", profiles);
        return true;
    }

    public bool RemoveProfile()
    {
        if (!CPH.TryGetArg("profileId", out string id) || string.IsNullOrWhiteSpace(id) || id.Trim() == "default") return false;
        id = id.Trim();
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        var profiles = NormalizeProfiles(player["animationProfiles"] as JArray);
        for (var i = profiles.Count - 1; i >= 0; i--)
            if (string.Equals((string)profiles[i]["id"], id, StringComparison.Ordinal)) profiles.RemoveAt(i);
        var animation = player["animation"] as JObject ?? new JObject();
        animation["selectedProfile"] = ResolveProfileId(profiles, (string)animation["selectedProfile"]) ?? "default";
        animation["entryPoints"] = ResetRemovedEntries(animation["entryPoints"] as JObject, id);
        player["animationProfiles"] = profiles;
        player["animation"] = animation;
        SaveConfig(PlayerKey, player);
        RemoveProfileSequences("player", id);
        return true;
    }

    public bool AddPanelProfile()
    {
        var panel = ReadConfig(PanelKey, CreatePanelDefaults());
        var profiles = NormalizeProfiles(panel["animationProfiles"] as JArray);
        var name = CPH.TryGetArg("panelProfileName", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "New Panel Profile";
        var id = Guid.NewGuid().ToString("N");
        profiles.Add(CreatePanelProfile(id, name));
        panel["animationProfiles"] = profiles;
        SaveConfig(PanelKey, panel);
        EnsureSequenceStore("panel", profiles);
        return true;
    }

    public bool RemovePanelProfile()
    {
        if (!CPH.TryGetArg("panelProfileId", out string id) || string.IsNullOrWhiteSpace(id) || id.Trim() == "default") return false;
        id = id.Trim();
        var panel = ReadConfig(PanelKey, CreatePanelDefaults());
        var profiles = NormalizeProfiles(panel["animationProfiles"] as JArray);
        for (var i = profiles.Count - 1; i >= 0; i--)
            if (string.Equals((string)profiles[i]["id"], id, StringComparison.Ordinal)) profiles.RemoveAt(i);
        var animation = panel["animation"] as JObject ?? new JObject();
        animation["entryPoints"] = ResetRemovedEntries(animation["entryPoints"] as JObject, id);
        panel["animationProfiles"] = profiles;
        panel["animation"] = animation;
        SaveConfig(PanelKey, panel);
        RemoveProfileSequences("panel", id);
        return true;
    }

    public bool AddClapperProfile()
    {
        var clapper = ReadConfig(ClapperKey, CreateClapperDefaults());
        var profiles = NormalizeProfiles(clapper["animationProfiles"] as JArray);
        var name = CPH.TryGetArg("clapperProfileName", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "New Clapperboard Profile";
        var id = Guid.NewGuid().ToString("N");
        profiles.Add(CreateClapperProfile(id, name));
        clapper["animationProfiles"] = profiles;
        SaveConfig(ClapperKey, clapper);
        EnsureSequenceStore("clapperboard", profiles);
        return true;
    }

    public bool RemoveClapperProfile()
    {
        if (!CPH.TryGetArg("clapperProfileId", out string id) || string.IsNullOrWhiteSpace(id) || id.Trim() == "default") return false;
        id = id.Trim();
        var clapper = ReadConfig(ClapperKey, CreateClapperDefaults());
        var profiles = NormalizeProfiles(clapper["animationProfiles"] as JArray);
        for (var i = profiles.Count - 1; i >= 0; i--)
            if (string.Equals((string)profiles[i]["id"], id, StringComparison.Ordinal)) profiles.RemoveAt(i);
        var animation = clapper["animation"] as JObject ?? new JObject();
        animation["selectedProfile"] = ResolveProfileId(profiles, (string)animation["selectedProfile"]) ?? "default";
        clapper["animationProfiles"] = profiles;
        clapper["animation"] = animation;
        SaveConfig(ClapperKey, clapper);
        RemoveProfileSequences("clapperboard", id);
        return true;
    }

    public bool ResolveClapperAnimation()
    {
        var clapper = ReadConfig(ClapperKey, CreateClapperDefaults());
        var profiles = NormalizeProfiles(clapper["animationProfiles"] as JArray);
        var animation = clapper["animation"] as JObject ?? new JObject();
        var selected = ResolveProfileId(profiles, (string)animation["selectedProfile"]) ?? "default";
        var store = ReadConfig(AnimationKey, new JObject());
        var target = store["clapperboard"] as JObject;
        var sequence = target?[selected] as JObject ?? new JObject();
        var start = sequence["startSequence"] as JArray ?? DefaultClapperStart();
        var end = sequence["endSequence"] as JArray ?? DefaultClapperEnd();
        CPH.SetArgument("replayClapperAnimation", new JObject
        {
            ["id"] = selected,
            ["name"] = (string)profiles.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["id"], selected, StringComparison.Ordinal))?["name"] ?? "Default",
            ["start"] = start,
            ["end"] = end
        }.ToString(Newtonsoft.Json.Formatting.None));
        return true;
    }

    public bool ResolveEntryPointProfile()
    {
        var entryPoint = CPH.TryGetArg("animationEntryPoint", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : CPH.GetGlobalVar<string>(EntryPointHandoffKey, false);
        CPH.UnsetGlobalVar(EntryPointHandoffKey, false);
        if (string.IsNullOrWhiteSpace(entryPoint)) return false;
        var player = ReadConfig(PlayerKey, CreatePlayerDefaults());
        var profiles = NormalizeProfiles(player["animationProfiles"] as JArray);
        var animation = player["animation"] as JObject ?? new JObject();
        var entries = animation["entryPoints"] as JObject ?? new JObject();
        var profile = ResolveProfileId(profiles, (string)entries[entryPoint.ToLowerInvariant()]) ?? "default";
        CPH.SetArgument("replayTitleEntryPoint", entryPoint.ToLowerInvariant());
        CPH.SetGlobalVar(ResolvedProfileHandoffKey, profile, false);
        CPH.SetArgument("replayAnimationProfileId", profile);
        return true;
    }

    private void NormalizePositionConfig(JObject config, bool panel)
    {
        var positions = config["positions"] as JObject ?? new JObject();
        var builtIn = panel
            ? new JObject { ["name"] = "Centered", ["tag"] = "centered", ["scale"] = 100, ["scaleX"] = 100, ["scaleY"] = 100, ["x"] = 0, ["y"] = 0, ["z"] = 0, ["rotateX"] = 0, ["rotateY"] = 0, ["rotateZ"] = 0, ["fov"] = 90 }
            : new JObject { ["name"] = "Full Screen", ["tag"] = "full-screen", ["scale"] = 100, ["scaleX"] = 100, ["scaleY"] = 100, ["x"] = 0, ["y"] = 0, ["z"] = 0, ["rotateX"] = 0, ["rotateY"] = 0, ["rotateZ"] = 0, ["fov"] = 90 };
        if (!positions.ContainsKey(panel ? "Centered" : "Full Screen"))
            positions[panel ? "Centered" : "Full Screen"] = builtIn;
        config["positions"] = positions;
    }

    private JArray NormalizeProfiles(JArray source)
    {
        var result = new JArray();
        JObject defaultProfile = null;
        foreach (var token in source ?? new JArray())
        {
            var id = (string)token["id"];
            if (string.IsNullOrWhiteSpace(id) || FindProfile(result, id) != null) continue;
            var item = new JObject { ["id"] = id, ["name"] = id == "default" ? "Default" : (string)token["name"] ?? "New Profile" };
            if (id == "default") defaultProfile = item; else result.Add(item);
        }
        result.Insert(0, defaultProfile ?? CreateProfile("default", "Default"));
        return result;
    }

    private JObject NormalizeEntryPoints(JObject source, JArray profiles, string[] names)
    {
        var result = new JObject();
        foreach (var name in names) result[name] = ResolveProfileId(profiles, (string)source?[name]) ?? "default";
        return result;
    }

    private JObject ResetRemovedEntries(JObject source, string id)
    {
        var result = source ?? new JObject();
        foreach (var property in result.Properties()) if (string.Equals((string)property.Value, id, StringComparison.Ordinal)) property.Value = "default";
        return result;
    }

    private string ResolveProfileId(JArray profiles, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        foreach (var item in profiles ?? new JArray())
        {
            if (string.Equals((string)item["id"], value, StringComparison.Ordinal)) return value;
            if (string.Equals((string)item["name"], value, StringComparison.OrdinalIgnoreCase)) return (string)item["id"];
        }
        return null;
    }

    private JObject FindProfile(JArray profiles, string id)
    {
        foreach (var item in profiles ?? new JArray()) if (string.Equals((string)item["id"], id, StringComparison.Ordinal)) return (JObject)item;
        return null;
    }

    private JObject CreateProfile(string id, string name) => new JObject { ["id"] = id, ["name"] = name };
    private JObject CreatePanelProfile(string id, string name) => new JObject { ["id"] = id, ["name"] = name };
    private JObject CreateMessageProfile(string id, string name) => new JObject { ["id"] = id, ["name"] = name };
    private JObject CreateClapperProfile(string id, string name) => new JObject { ["id"] = id, ["name"] = name };

    private JObject CreatePlayerDefaults() => new JObject { ["positions"] = new JObject(), ["animationProfiles"] = new JArray(CreateProfile("default", "Default")), ["animation"] = new JObject { ["selectedProfile"] = "default", ["entryPoints"] = new JObject { ["obs"] = "default", ["twitch"] = "default", ["youtube"] = "default", ["kick"] = "default", ["recent"] = "default", ["catalog"] = "default", ["playlist"] = "default" } }, ["entryPoints"] = new JObject { ["obs"] = new JObject { ["animationProfile"] = "default", ["designPreset"] = "broadcast", ["titlePreset"] = "default", ["brandingPreset"] = "default" }, ["twitch"] = new JObject { ["animationProfile"] = "default", ["designPreset"] = "broadcast", ["titlePreset"] = "default", ["brandingPreset"] = "default" }, ["youtube"] = new JObject { ["animationProfile"] = "default", ["designPreset"] = "broadcast", ["titlePreset"] = "default", ["brandingPreset"] = "default" }, ["kick"] = new JObject { ["animationProfile"] = "default", ["designPreset"] = "broadcast", ["titlePreset"] = "default", ["brandingPreset"] = "default" }, ["play"] = new JObject { ["animationProfile"] = "default", ["designPreset"] = "broadcast", ["titlePreset"] = "default", ["brandingPreset"] = "default", ["useSourcePlatformBranding"] = false, ["changePlayerBrandingToClipSource"] = false } } };
    private JObject CreatePanelDefaults() => new JObject { ["width"] = 500, ["height"] = 700, ["useSourcePlatformBranding"] = false, ["positions"] = new JObject(), ["animationProfiles"] = new JArray(CreatePanelProfile("default", "Default")), ["animation"] = new JObject { ["entryPoints"] = new JObject { ["recent"] = "default", ["playlist"] = "default", ["creatorLeaderboard"] = "default" } }, ["entryPoints"] = new JObject { ["recent"] = new JObject { ["animationProfile"] = "default", ["designPreset"] = "broadcast", ["titlePreset"] = "default", ["brandingPreset"] = "default" }, ["playlist"] = new JObject { ["animationProfile"] = "default", ["designPreset"] = "broadcast", ["titlePreset"] = "default", ["brandingPreset"] = "default" }, ["creatorLeaderboard"] = new JObject { ["animationProfile"] = "default", ["designPreset"] = "broadcast", ["titlePreset"] = "default", ["brandingPreset"] = "default" } } };
    private JObject CreateMessageDefaults() => new JObject { ["width"] = 500, ["height"] = 120, ["cornerRadius"] = 0, ["positions"] = new JObject(), ["animationProfiles"] = new JArray(CreateMessageProfile("default", "Default")), ["animation"] = new JObject { ["selectedProfile"] = "default" }, ["entryPoint"] = CreateMessageEntryPoint() };
    private JObject CreateMessageEntryPoint() => new JObject { ["animationProfile"] = "default", ["designPreset"] = "broadcast", ["brandingPreset"] = "default" };
    private JObject CreateClapperDefaults() => new JObject { ["animationProfiles"] = new JArray(CreateClapperProfile("default", "Default")), ["animation"] = new JObject { ["selectedProfile"] = "default" }, ["entryPoint"] = new JObject { ["animationProfile"] = "default", ["brandingPreset"] = "default" } };

    private JObject GetProfileSequences(string target, string id)
    {
        var store = ReadConfig(AnimationKey, new JObject());
        var targetObject = store[target] as JObject;
        var profile = targetObject?[id] as JObject;
        return profile ?? new JObject();
    }

    private void EnsureSequenceStore(string target, JArray profiles)
    {
        var store = ReadConfig(AnimationKey, new JObject());
        var targetObject = store[target] as JObject ?? new JObject();
        var changed = false;
        foreach (var token in profiles ?? new JArray())
        {
            var id = (string)token["id"];
            if (string.IsNullOrWhiteSpace(id) || targetObject[id] is JObject) continue;
            targetObject[id] = new JObject
            {
                ["startSequence"] = ProfileDefaultSequence(target),
                ["endSequence"] = ProfileDefaultEndSequence(target)
            };
            changed = true;
        }
        store[target] = targetObject;
        if (changed) SaveConfig(AnimationKey, store);
    }

    private void RemoveProfileSequences(string target, string id)
    {
        var store = ReadConfig(AnimationKey, new JObject());
        var targetObject = store[target] as JObject;
        if (targetObject == null || targetObject[id] == null) return;
        targetObject.Remove(id);
        store[target] = targetObject;
        SaveConfig(AnimationKey, store);
    }

    private JArray ProfileDefaultSequence(string target)
    {
        return target == "player" ? DefaultPlayerStart() : target == "panel" || target == "message" ? DefaultPanelStart() : DefaultClapperStart();
    }

    private JArray ProfileDefaultEndSequence(string target)
    {
        return target == "player" ? DefaultPlayerEnd() : target == "panel" || target == "message" ? DefaultPanelEnd() : DefaultClapperEnd();
    }

    private JArray DefaultPlayerStart() => new JArray(new JObject { ["position"] = "Full Screen", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });
    private JArray DefaultPlayerEnd() => new JArray(new JObject { ["position"] = "Full Screen", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });
    private JArray DefaultPanelStart() => new JArray(new JObject { ["position"] = "Centered", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });
    private JArray DefaultPanelEnd() => new JArray(new JObject { ["position"] = "Centered", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });
    private JArray DefaultClapperStart() => new JArray(new JObject { ["position"] = "Centered", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });
    private JArray DefaultClapperEnd() => new JArray(new JObject { ["position"] = "Centered", ["duration"] = 0, ["delay"] = 0, ["easing"] = "ease-in-out" });

    private JObject ReadConfig(string key, JObject defaults)
    {
        var raw = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(raw)) return defaults;
        try { return JObject.Parse(raw); } catch { return defaults; }
    }

    private void SaveConfig(string key, JObject value) => CPH.SetGlobalVar(key, value.ToString(Newtonsoft.Json.Formatting.None), true);

    const string HandoffKey="rts.actionreplay.handoff.playerPositions";

    public bool EnsurePositions()
    {
        var presets=ReadPositionStore(PresetsKey);
        var positions=presets["positions"] as JObject ?? new JObject();
        positions["player"]=EnsurePositionSet(positions["player"] as JObject,"Full Screen","full-screen",100);
        positions["panel"]=EnsurePositionSet(positions["panel"] as JObject,"Centered","centered",100);
        positions["clapperboard"]=EnsurePositionSet(positions["clapperboard"] as JObject,"Centered","centered",50);
        positions["message"]=EnsurePositionSet(positions["message"] as JObject,"Centered","centered",100);
        presets["positions"]=positions;
        SavePositionStore(PresetsKey,presets);
        return true;
    }

    public bool GetPlayerPositions(){EnsurePositions();var presets=ReadPositionStore(PresetsKey);var positions=(presets["positions"] as JObject)?["player"] as JObject??new JObject();CPH.SetGlobalVar(HandoffKey,positions.ToString(Newtonsoft.Json.Formatting.None),false);return true;}
    public bool GetPanelPositions(){EnsurePositions();var presets=ReadPositionStore(PresetsKey);var positions=(presets["positions"] as JObject)?["panel"] as JObject??new JObject();CPH.SetGlobalVar("rts.actionreplay.handoff.panelPositions",positions.ToString(Newtonsoft.Json.Formatting.None),false);return true;}
    public bool GetClapperboardPositions(){EnsurePositions();var presets=ReadPositionStore(PresetsKey);var positions=(presets["positions"] as JObject)?["clapperboard"] as JObject??new JObject();CPH.SetGlobalVar("rts.actionreplay.handoff.clapperPositions",positions.ToString(Newtonsoft.Json.Formatting.None),false);return true;}

    public bool ResetMessagePositions()
    {
        var presets = ReadPositionStore(PresetsKey);
        var positions = presets["positions"] as JObject ?? new JObject();
        positions.Remove("message");
        presets["positions"] = positions;
        presets.Remove("messagePositionCoordinates");
        SavePositionStore(PresetsKey, presets);
        CPH.LogInfo("RTS Action Replay: message positions reset; the position editor will recreate a fresh set.");
        return true;
    }

    JObject EnsurePositionSet(JObject value,string name,string tag,int scale)
    {
        if(value!=null&&value.Count>0)return value;
        return CreatePositionSet(name,tag,scale);
    }
    JObject ReadPositionStore(string key){var raw=CPH.GetGlobalVar<string>(key,true);try{return string.IsNullOrWhiteSpace(raw)?new JObject():JObject.Parse(raw);}catch{return new JObject();}}
    void SavePositionStore(string key,JObject value)=>CPH.SetGlobalVar(key,value.ToString(Newtonsoft.Json.Formatting.None),true);
    JObject CreatePositionSet(string name,string tag,int scale)=>new JObject{[name]=new JObject{["name"]=name,["tag"]=tag,["scale"]=scale,["scaleX"]=scale,["scaleY"]=scale,["x"]=0,["y"]=0,["z"]=0,["rotateX"]=0,["rotateY"]=0,["rotateZ"]=0,["fov"]=90}};

    public bool ApplyPanelPreset()
    {
        var panelType = CPH.TryGetArg("panelType", out string requested) && !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : "recent";
        CPH.SetGlobalVar(PanelOperationKey, new JObject { ["panelType"] = panelType, ["triggerEvent"] = false }.ToString(Newtonsoft.Json.Formatting.None), false);
        return ResolvePanel();
    }

    public void PreviewPanelPreset()
    {
        ApplyPanelPreset();
        var panel = Read(PanelKey);
        CPH.SetArgument("replayCommand", "panel-position-preview");
        CPH.SetArgument("replayPanelPosition", "Centered");
        CPH.SetArgument("replayPanelPositions", (panel["positions"] as JObject ?? new JObject()).ToString(Newtonsoft.Json.Formatting.None));
        CPH.SetArgument("replayPanelWidth", (int?)panel["width"] ?? 500);
        CPH.SetArgument("replayPanelHeight", (int?)panel["height"] ?? 700);
        CPH.TriggerEvent(EventName, true);
    }

}
