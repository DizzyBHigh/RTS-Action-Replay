using System;
using Newtonsoft.Json.Linq;

// Shared Visual and Branding presets plus entry-point resolution.
public class CPHInline
{
    private const string PlayerKey = "rts.actionreplay.config.player";
    private const string PanelKey = "rts.actionreplay.config.panel";
    private const string ClapperKey = "rts.actionreplay.config.clapper";
    private const string PresetsKey = "rts.actionreplay.config.presets";

    public bool Execute() => EnsureEntryPoints();

    public bool EnsureEntryPoints()
    {
        EnsureDefaults();
        var player = Read(PlayerKey, new JObject());
        var panel = Read(PanelKey, new JObject());
        var clapper = Read(ClapperKey, new JObject());
        MigratePlayer(player); MigratePanel(panel); MigrateClapper(clapper);
        Write(PlayerKey, player); Write(PanelKey, panel); Write(ClapperKey, clapper);
        return true;
    }

    public bool EnsureDefaults()
    {
        var presets = Read(PresetsKey, CreateDefaults());
        var defaults = CreateDefaults();
        if (!(presets["branding"] is JArray)) presets["branding"] = defaults["branding"];
        if (!(presets["visual"] is JArray)) presets["visual"] = defaults["visual"];
        EnsureLegacyBrandingPresets(presets);
        presets["version"] = 1;
        Write(PresetsKey, presets);
        return true;
    }

    public bool ResolveEntryPoint()
    {
        CPH.TryGetArg("presetComponent", out string component); CPH.TryGetArg("entryPoint", out string entryPoint);
        component = (component ?? "player").Trim().ToLowerInvariant();
        JObject resolved;
        if (component == "panel") resolved = Resolve(Read(PanelKey, new JObject()), entryPoint, new[] { "recent", "playlist", "creatorLeaderboard" });
        else if (component == "clapper") resolved = ResolveClapperEntry();
        else if (component == "player") resolved = Resolve(Read(PlayerKey, new JObject()), entryPoint, new[] { "obs", "twitch", "youtube", "kick", "recent", "catalog", "playlist" });
        else return false;
        CPH.SetArgument("animationProfile", (string)resolved["animationProfile"] ?? "default");
        CPH.SetArgument("visualPreset", (string)resolved["visualPreset"] ?? "broadcast");
        CPH.SetArgument("brandingPreset", (string)resolved["brandingPreset"] ?? "default");
        CPH.SetArgument("presetResolution", resolved.ToString(Newtonsoft.Json.Formatting.None));
        return true;
    }

    public bool ApplyVisualAndBranding()
    {
        var visualId = Arg("visualPreset", Arg("replayVisualPresetId", "broadcast"));
        var brandingId = Arg("brandingPreset", Arg("replayBrandingPresetId", "default"));
        var visual = Find(Visuals(), visualId) ?? Find(Visuals(), "broadcast");
        var brand = Find(Branding(), brandingId) ?? Find(Branding(), "default");
        if (visual == null || brand == null) return false;
        var payload = BuildPayload(visualId, visual, brand);
        CPH.SetArgument("replayVisualPresetId", (string)visual["id"]);
        CPH.SetArgument("replayBrandingPresetId", (string)brand["id"]);
        CPH.SetArgument("replayTitleStyle", (string)visual["id"]);
        CPH.SetArgument("replayTitleProfileId", (string)visual["id"]);
        CPH.SetArgument("replayTitleProfile", payload.ToString(Newtonsoft.Json.Formatting.None));
        ApplyArguments(payload);
        return true;
    }

    private JObject BuildPayload(string visualId, JObject visual, JObject brand) => new JObject
    {
        ["profile"] = visualId, ["style"] = visualId,
        ["showTitle"] = LegacyBool("rts.actionreplay.showTitle", true),
        ["decorationPosition"] = LegacyString("rts.actionreplay.titleDecorationPosition", "Suffix"),
        ["decoration"] = LegacyString("rts.actionreplay.titleDecoration", " - Replay Capture"),
        ["position"] = LegacyString("rts.actionreplay.titlePosition", "Bottom"),
        ["animation"] = LegacyString("rts.actionreplay.titleAnimation", "Slide up/down"),
        ["delay"] = LegacyInt("rts.actionreplay.titleDelay", 0), ["duration"] = LegacyInt("rts.actionreplay.titleDuration", 5000),
        ["animationDuration"] = LegacyInt("rts.actionreplay.titleAnimationDuration", 450),
        ["font"] = (string)brand["font"] ?? "Inter", ["fontSize"] = (int?)brand["fontSize"] ?? 34,
        ["textColor"] = (string)brand["textColor"] ?? "#FFFFFFFF", ["shadowColor"] = (string)brand["shadowColor"] ?? "#000000FF",
        ["primaryColor"] = (string)brand["primaryColor"] ?? "#0384CBFF", ["secondaryColor"] = (string)brand["secondaryColor"] ?? "#101416FF",
        ["broadcast"] = BuildBroadcast(visual, brand), ["cut"] = BuildCut(visual, brand)
    };

    private void ApplyArguments(JObject p)
    {
        CPH.SetArgument("replayShowTitle", (bool)p["showTitle"]); CPH.SetArgument("replayTitleDecorationPosition", (string)p["decorationPosition"]);
        CPH.SetArgument("replayTitleDecoration", (string)p["decoration"]); CPH.SetArgument("replayTitlePosition", (string)p["position"]);
        CPH.SetArgument("replayTitleAnimation", (string)p["animation"]); CPH.SetArgument("replayTitleDelay", (int)p["delay"]);
        CPH.SetArgument("replayTitleDuration", (int)p["duration"]); CPH.SetArgument("replayTitleAnimationDuration", (int)p["animationDuration"]);
        CPH.SetArgument("replayTitleFont", (string)p["font"]); CPH.SetArgument("replayTitleFontSize", (int)p["fontSize"]);
        CPH.SetArgument("replayTitleTextColor", (string)p["textColor"]); CPH.SetArgument("replayTitleShadowColor", (string)p["shadowColor"]);
        CPH.SetArgument("replayTitlePrimaryColor", (string)p["primaryColor"]); CPH.SetArgument("replayTitleSecondaryColor", (string)p["secondaryColor"]);
        SetProperties("replayBroadcast", p["broadcast"] as JObject); SetProperties("replayCut", p["cut"] as JObject);
    }

    private JObject BuildBroadcast(JObject v, JObject b) => new JObject
    {
        ["primaryColor"] = (string)b["primaryColor"] ?? "#101416FF", ["secondaryColor"] = (string)b["secondaryColor"] ?? "#101416FF",
        ["chevronHeight"] = (int?)v["chevronHeight"] ?? 42, ["randomHeight"] = (bool?)v["randomHeight"] ?? false,
        ["chevronWidth"] = (int?)v["chevronWidth"] ?? 42, ["randomWidth"] = (bool?)v["randomWidth"] ?? false,
        ["chevronSpacing"] = (int?)v["chevronSpacing"] ?? 0, ["randomSpacing"] = (bool?)v["randomSpacing"] ?? false,
        ["chevronSpeed"] = (int?)v["chevronSpeed"] ?? 95, ["decorationColor"] = (string)b["titlePrefixSuffixColor"] ?? "#0384CBFF",
        ["titleColor"] = (string)b["titleColor"] ?? "#FFFFFFFF"
    };

    private JObject BuildCut(JObject v, JObject b) => new JObject
    {
        ["primaryColor"] = (string)b["primaryColor"] ?? "#0384CBFF", ["secondaryColor"] = (string)b["secondaryColor"] ?? "#101416FF",
        ["blockWidth"] = (int?)v["blockWidth"] ?? 170, ["randomWidth"] = (bool?)v["randomWidth"] ?? true, ["barHeight"] = (int?)v["barHeight"] ?? 5,
        ["decorationColor"] = (string)b["titlePrefixSuffixColor"] ?? "#0384CBFF", ["titleColor"] = (string)b["titleColor"] ?? "#FFFFFFFF"
    };

    private void SetProperties(string prefix, JObject values) { foreach (var p in values?.Properties() ?? new JProperty[0]) CPH.SetArgument(prefix + Name(p.Name), Value(p.Value)); }
    private void MigratePlayer(JObject p) { var a=p["animation"] as JObject ?? new JObject(); var ae=a["entryPoints"] as JObject ?? new JObject(); var t=p["title"] as JObject ?? new JObject(); var v=Normalize((string)t["selectedProfile"] ?? CPH.GetGlobalVar<string>("rts.actionreplay.titleBarStyle",true)); foreach(var x in new[]{"obs","twitch","youtube","kick","recent","catalog","playlist"}) { var e=p["entryPoints"]?[x] as JObject ?? new JObject(); e["animationProfile"]=(string)e["animationProfile"]??(string)ae[x]??"default"; e["visualPreset"]=(string)e["visualPreset"]??v; e["brandingPreset"]=(string)e["brandingPreset"]??LegacyBrandingId(v); EnsureEntry(p,x,e); } }
    private void MigratePanel(JObject p) { var pr=p["preset"] as JObject??new JObject(); var fallback=(string)pr["fallback"]??"Broadcast"; var le=pr["entryPoints"] as JObject??new JObject(); var ae=(p["animation"] as JObject)?["entryPoints"] as JObject??new JObject(); foreach(var x in new[]{"recent","playlist","creatorLeaderboard"}) { var e=p["entryPoints"]?[x] as JObject??new JObject(); e["animationProfile"]=(string)e["animationProfile"]??(string)ae[x]??"default"; e["visualPreset"]=(string)e["visualPreset"]??Normalize((string)le[x]??fallback); e["brandingPreset"]=(string)e["brandingPreset"]??LegacyBrandingId((string)e["visualPreset"]); EnsureEntry(p,x,e); } }
    private void MigrateClapper(JObject c) { var e=c["entryPoint"] as JObject??new JObject(); e["animationProfile"]=(string)e["animationProfile"]??(string)(c["animation"] as JObject)?["selectedProfile"]??"default"; e["visualPreset"]=(string)e["visualPreset"]??"broadcast"; e["brandingPreset"]=(string)e["brandingPreset"]??"default"; c["entryPoint"]=e; }
    private static void EnsureEntry(JObject c,string n,JObject e){var a=c["entryPoints"] as JObject??new JObject();a[n]=e;c["entryPoints"]=a;}
    private JObject ResolveClapperEntry(){var c=Read(ClapperKey,new JObject());var e=c["entryPoint"] as JObject??new JObject();return new JObject{{"animationProfile",ResolveAnimation(c,e)},{"visualPreset",ResolveId(Visuals(),(string)e["visualPreset"])??"broadcast"},{"brandingPreset",ResolveId(Branding(),(string)e["brandingPreset"])??"default"}};}
    private JObject Resolve(JObject c,string ep,string[] valid){var a=c["entryPoints"] as JObject??new JObject();var n=string.IsNullOrWhiteSpace(ep)?valid[0]:ep.Trim().ToLowerInvariant();if(Array.IndexOf(valid,n)<0)n=valid[0];var e=a[n] as JObject??new JObject();return new JObject{{"animationProfile",ResolveAnimation(c,e)},{"visualPreset",ResolveId(Visuals(),(string)e["visualPreset"])??"broadcast"},{"brandingPreset",ResolveId(Branding(),(string)e["brandingPreset"])??"default"}};}
    private static string ResolveAnimation(JObject c,JObject e){return (string)e["animationProfile"]??(string)(c["animation"] as JObject)?["selectedProfile"]??"default";}
    public JArray Branding()=>Read(PresetsKey,CreateDefaults())["branding"] as JArray??new JArray(); public JArray Visuals()=>Read(PresetsKey,CreateDefaults())["visual"] as JArray??new JArray();
    private static JObject Find(JArray a,string id){foreach(var x in a??new JArray())if(string.Equals((string)x["id"],id,StringComparison.OrdinalIgnoreCase))return x as JObject;return null;}
    private static string ResolveId(JArray a,string v){var x=Find(a,v);return x==null?null:(string)x["id"];}
    private static string Normalize(string v){v=(v??"").Trim().ToLowerInvariant();return v=="cinematic"||v=="cut"||v=="minimal"?v:"broadcast";}
    private string Arg(string n,string f)=>CPH.TryGetArg(n,out string v)&&!string.IsNullOrWhiteSpace(v)?v.Trim():f;
    private JObject Read(string k,JObject f){var r=CPH.GetGlobalVar<string>(k,true);try{return string.IsNullOrWhiteSpace(r)?f:JObject.Parse(r);}catch{return f;}}
    private void Write(string k,JObject v)=>CPH.SetGlobalVar(k,v.ToString(Newtonsoft.Json.Formatting.None),true);
    private static JObject CreateDefaults()=>new JObject{{"version",1},{"branding",new JArray(new JObject{{"id","default"},{"name","Default"},{"primaryColor","#0384CBFF"},{"secondaryColor","#101416FF"},{"titleColor","#FFFFFFFF"},{"titlePrefixSuffixColor","#0384CBFF"},{"textColor","#FFFFFFFF"},{"shadowColor","#000000FF"},{"font","Inter"},{"fontSize",34},{"logo",""},{"fallbackText","RTS"},{"brandLabel","ACTION REPLAY"}})},{"visual",new JArray(new JObject{{"id","broadcast"},{"name","Broadcast"},{"chevronHeight",42},{"randomHeight",false},{"chevronWidth",42},{"randomWidth",false},{"chevronSpacing",0},{"randomSpacing",false},{"chevronSpeed",95}},new JObject{{"id","cinematic"},{"name","Cinematic"},{"fixed",true}},new JObject{{"id","cut"},{"name","Cut"},{"blockWidth",170},{"randomWidth",true},{"barHeight",5}},new JObject{{"id","minimal"},{"name","Minimal"},{"fixed",true}})}};
    private void EnsureLegacyBrandingPresets(JObject presets)
    {
        var branding = presets["branding"] as JArray ?? new JArray();
        EnsureLegacyBrand(branding, "legacy-broadcast", "Legacy Broadcast", "rts.actionreplay.broadcast");
        EnsureLegacyBrand(branding, "legacy-cut", "Legacy Cut", "rts.actionreplay.cut");
        presets["branding"] = branding;
    }
    private JObject EnsureLegacyBrand(JArray a, string id, string name, string prefix)
    {
        var existing = Find(a, id); if (existing != null) return existing;
        var b = Find(a, "default") ?? CreateDefaults()["branding"][0] as JObject; if (b == null) return null;
        var copy = (JObject)b.DeepClone(); copy["id"] = id; copy["name"] = name;
        copy["primaryColor"] = CPH.GetGlobalVar<string>(prefix + ".primaryColor", true) ?? (string)copy["primaryColor"];
        copy["secondaryColor"] = CPH.GetGlobalVar<string>(prefix + ".secondaryColor", true) ?? (string)copy["secondaryColor"];
        copy["titleColor"] = CPH.GetGlobalVar<string>(prefix + ".titleColor", true) ?? (string)copy["titleColor"];
        copy["titlePrefixSuffixColor"] = CPH.GetGlobalVar<string>(prefix + ".decorationColor", true) ?? (string)copy["titlePrefixSuffixColor"];
        a.Add(copy); return copy;
    }
    private string LegacyBrandingId(string visual) => visual == "cut" ? "legacy-cut" : visual == "broadcast" ? "legacy-broadcast" : "default";
    private string LegacyString(string k,string f)=>CPH.GetGlobalVar<string>(k,true)??f; private int LegacyInt(string k,int f)=>CPH.GetGlobalVar<int?>(k,true)??f; private bool LegacyBool(string k,bool f)=>CPH.GetGlobalVar<bool?>(k,true)??f;
    private static string Name(string k)=>char.ToUpperInvariant(k[0])+k.Substring(1); private static object Value(JToken v)=>v.Type==JTokenType.Boolean?(object)(bool)v:v.Type==JTokenType.Integer?(object)(int)v:v.ToString();
}
