using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
public class CPHInline
{
 const string D="rts.actionreplay.data",P="rts.actionreplay.config.presets",PL="rts.actionreplay.config.player",PA="rts.actionreplay.config.panel",M="rts.actionreplay.config.message",C="rts.actionreplay.config.clapper",A="rts.actionreplay.config.animation",S="rts.actionreplay.handoff.configurationSnapshot";
 public bool Execute()=>Import();
 public bool Export(){if(!CPH.ExecuteMethod("RTS - Action Replay - Core - Store","GetConfigurationSnapshot"))return false;var raw=CPH.GetGlobalVar<string>(S,false);if(string.IsNullOrWhiteSpace(raw))return false;var t=new System.Threading.Thread(()=>{try{var d=new SaveFileDialog{Title="Export RTS Action Replay Settings",Filter="RTS Action Replay Settings (*.json)|*.json|All files (*.*)|*.*",DefaultExt=".json",FileName="rts-action-replay-settings.json",OverwritePrompt=true,RestoreDirectory=true};if(d.ShowDialog()!=true)return;File.WriteAllText(d.FileName,raw);MessageBox.Show("Settings exported successfully.","RTS Action Replay",MessageBoxButton.OK,MessageBoxImage.Information);}catch(Exception e){CPH.LogError("RTS Action Replay export failed: "+e.Message);}});t.SetApartmentState(System.Threading.ApartmentState.STA);t.IsBackground=true;t.Start();return true;}
 public bool Import(){var t=new System.Threading.Thread(()=>ImportCore());t.SetApartmentState(System.Threading.ApartmentState.STA);t.IsBackground=true;t.Start();return true;} bool ImportCore(){var d=new OpenFileDialog{Title="Import RTS Action Replay Settings",Filter="RTS Action Replay Settings (*.json)|*.json|All files (*.*)|*.*"};if(d.ShowDialog()!=true)return false;try{var r=JObject.Parse(File.ReadAllText(d.FileName));if(r["player"]==null&&r["presets"]==null)throw new Exception("Invalid RTS Action Replay settings file.");HashSet<string> b=null,a=null;if((B("import.branding",true)||B("import.animation",true)||B("import.catalog",false))&&!ChooseProfiles(r,B("import.branding",true),B("import.animation",true),out b,out a))return false;if(MessageBox.Show("Import the selected settings from this file?","RTS Action Replay",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return false;var player=B("import.player",true)&&Put(PL,r["player"] as JObject);var panel=B("import.panel",true)&&Put(PA,r["panel"] as JObject);var message=B("import.message",true)&&Put(M,r["message"] as JObject);var branding=Preset("branding",r["presets"]?["branding"] as JArray,b,B("import.branding",true));var visual=Preset("visual",r["presets"]?["visual"] as JArray,null,B("import.visual",true));var title=Preset("title",r["presets"]?["title"] as JArray,null,B("import.title",true));var animation=B("import.animation",true)?Animations(r,a):0;var n=B("import.catalog",false)?Catalog(r["data"]?["catalog"] as JArray):new int[3];var globals=Globals(r["globals"] as JObject);CPH.ExecuteMethod("RTS - Action Replay - Core - Store","EnsureDefaults");CPH.ExecuteMethod("RTS - Action Replay - Core - Store","EnsureEntryPoints");ShowImportReport(player,panel,message,branding,animation,visual,title,n,globals);return true;}catch(Exception e){CPH.LogError("RTS Action Replay import failed: "+e.Message);MessageBox.Show("Import failed: "+e.Message,"RTS Action Replay",MessageBoxButton.OK,MessageBoxImage.Error);return false;}}

 bool ChooseProfiles(JObject r,bool branding,bool animation,out HashSet<string> b,out HashSet<string> a)
 {
  var selectedBranding=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  var selectedAnimation=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  var w=new Window{Title="Select Profiles to Import",Width=760,Height=820,MinWidth=650,MinHeight=600,WindowStartupLocation=WindowStartupLocation.CenterScreen,ResizeMode=ResizeMode.CanResize};
  var root=new DockPanel{Margin=new Thickness(8)};
  var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(8,12,8,8)};
  var cancel=new Button{Content="Cancel",Margin=new Thickness(0,0,8,0)};
  var ok=new Button{Content="Import Selected"};
  DockPanel.SetDock(footer,Dock.Bottom);footer.Children.Add(cancel);footer.Children.Add(ok);root.Children.Add(footer);

  var actions=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(8,4,8,10)};
  var all=new Button{Content="Select All",Margin=new Thickness(0,0,8,0)};
  var none=new Button{Content="Clear All"};
  actions.Children.Add(all);actions.Children.Add(none);DockPanel.SetDock(actions,Dock.Top);root.Children.Add(actions);

  var list=new StackPanel{Margin=new Thickness(8,0,8,8)};
  ApplyTheme(w,root);
  var checks=new List<RtsUICustomToggle>();
  var catalog=new RtsUICustomToggle{Content="Catalog",IsChecked=B("import.catalog",true),Margin=new Thickness(0,2,0,6)};
  var platforms=new List<RtsUICustomToggle>();
  var scope=new ComboBox{MinWidth=180,Tag="catalogScope"};
  scope.Items.Add("All");scope.Items.Add("Selected");scope.SelectedItem=Get("import.catalog.mode","All");
  foreach(var p in new[]{new[]{"Twitch","twitch"},new[]{"YouTube","youtube"},new[]{"Kick","kick"},new[]{"Local / OBS","local"}})
  {
   var cb=new RtsUICustomToggle{Content=p[0],IsChecked=B("import.catalog."+p[1],true),Tag=p[1],Margin=new Thickness(0,3,0,3)};
   platforms.Add(cb);
  }
  var duplicates=new ComboBox{MinWidth=180};
  duplicates.Items.Add("Skip");duplicates.Items.Add("Overwrite");duplicates.SelectedItem=Get("import.duplicates","Skip");

  var catalogBody=new StackPanel();
  catalogBody.Children.Add(catalog);
  catalogBody.Children.Add(new TextBlock{Text="Catalog Scope",Margin=new Thickness(0,2,0,2)});
  catalogBody.Children.Add(scope);
  catalogBody.Children.Add(new TextBlock{Text="Platforms (when Scope is Selected)",Margin=new Thickness(0,10,0,2)});
  var platformRow=new Grid{Margin=new Thickness(0,2,0,4)};
  for(int i=0;i<4;i++)platformRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  for(int i=0;i<platforms.Count;i++){Grid.SetColumn(platforms[i],i);platformRow.Children.Add(platforms[i]);}
  catalogBody.Children.Add(platformRow);
  catalogBody.Children.Add(new TextBlock{Text="Duplicate Entries",Margin=new Thickness(0,8,0,2)});
  catalogBody.Children.Add(duplicates);
  AddSection(list,"Catalog",catalogBody,w);

  if(branding)
  {
   var body=new StackPanel();
   foreach(var p in (r["presets"]?["branding"] as JArray??new JArray()).OfType<JObject>())
   {
    var id=(string)p["id"];if(!string.IsNullOrWhiteSpace(id))AddProfile(body,(string)p["name"]??id,id,false,checks,w);
   }
   AddSection(list,"Branding Profiles",body,w);
  }

  if(animation)
  {
   foreach(var component in new[]{"player","panel","message","clapperboard"})
   {
    var src=r[component] as JObject;var ps=src?["animationProfiles"] as JArray??new JArray();if(ps.Count==0)continue;
    var body=new StackPanel();
    foreach(var p in ps.OfType<JObject>())
    {
     var id=(string)p["id"];if(!string.IsNullOrWhiteSpace(id))AddProfile(body,(string)p["name"]??id,component+":"+id,true,checks,w);
    }
    AddSection(list,char.ToUpper(component[0])+component.Substring(1)+" Animation Profiles",body,w);
   }
  }

  all.Click+=delegate{
   catalog.IsChecked=true;
   foreach(var p in platforms)p.IsChecked=true;
   foreach(var c in checks)c.IsChecked=true;
  };
  none.Click+=delegate{
   catalog.IsChecked=false;
   foreach(var p in platforms)p.IsChecked=false;
   foreach(var c in checks)c.IsChecked=false;
  };
  cancel.Click+=delegate{w.DialogResult=false;w.Close();};
  ok.Click+=delegate{
   CPH.SetGlobalVar("rts.actionreplay.import.catalog",catalog.IsChecked==true,true);
   CPH.SetGlobalVar("rts.actionreplay.import.catalog.mode",(string)scope.SelectedItem??"All",true);
   foreach(var cb in platforms)CPH.SetGlobalVar("rts.actionreplay.import.catalog."+((string)cb.Tag),cb.IsChecked==true,true);
   CPH.SetGlobalVar("rts.actionreplay.import.duplicates",(string)duplicates.SelectedItem??"Skip",true);
   foreach(var c in checks)if(c.IsChecked==true){var tag=(string)c.Tag;if(tag.StartsWith("b:"))selectedBranding.Add(tag.Substring(2));else selectedAnimation.Add(tag.Substring(2));}
   w.DialogResult=true;w.Close();
  };

  var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Content=list};
  root.Children.Add(scroll);w.Content=root;ApplyTheme(w,root);
  var result=w.ShowDialog()==true;b=selectedBranding;a=selectedAnimation;return result;
 }

 void AddSection(StackPanel list,string title,Panel body,Window w)
 {
  var content=new StackPanel();
  var accent=w.Resources["duhBuhAccent"] as Brush;
  var sectionBg=w.Resources["duhBuhSectionBackground"] as Brush;
  var sectionBorder=w.Resources["duhBuhSectionBorder"] as Brush;
  var sectionText=w.Resources["duhBuhSectionText"] as Brush;
  content.Children.Add(new Border{Height=3,Background=accent,HorizontalAlignment=HorizontalAlignment.Stretch,Margin=new Thickness(0,0,0,8)});
  content.Children.Add(new TextBlock{Text=title,FontSize=14,FontWeight=FontWeights.SemiBold,Foreground=sectionText,Background=sectionBg,Padding=new Thickness(10,7,10,7),Margin=new Thickness(0,0,0,10),HorizontalAlignment=HorizontalAlignment.Stretch});
  content.Children.Add(body);
  var card=new Border{Background=sectionBg,BorderBrush=sectionBorder,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(10),Padding=new Thickness(14,8,14,10),Margin=new Thickness(0,0,0,14),Child=content};
  list.Children.Add(card);
 }

 void AddProfile(StackPanel body,string label,string id,bool isAnimation,List<RtsUICustomToggle> checks,Window w)
 {
  var row=new StackPanel{Margin=new Thickness(0,2,0,7)};
  var cb=new RtsUICustomToggle{Content=label,IsChecked=true,Tag=(isAnimation?"a:":"b:")+id,Margin=new Thickness(0,2,0,0)};
  row.Children.Add(cb);
  row.Children.Add(new TextBlock{Text="ID: "+id,Margin=new Thickness(37,0,0,0),FontSize=12,Foreground=w.Resources["duhBuhDescriptionText"] as Brush});
  body.Children.Add(row);checks.Add(cb);
 }
 void ApplyTheme(Window w,Panel root){var light=!string.Equals(CPH.GetGlobalVar<string>("rts.actionreplay.uiTheme",true),"Dark",StringComparison.OrdinalIgnoreCase);var bg=new SolidColorBrush((Color)ColorConverter.ConvertFromString(light?"#FFFFFF":"#1E1E1E"));var fg=new SolidColorBrush((Color)ColorConverter.ConvertFromString(light?"#111111":"#F2F2F2"));var sub=new SolidColorBrush((Color)ColorConverter.ConvertFromString(light?"#666666":"#B8B8B8"));w.Background=bg;root.Background=bg;RtsUITheme.Initialize();RtsUITheme.Apply(w,light);foreach(var x in Find(root)){if(x is TextBlock t)t.Foreground=t.FontWeight==FontWeights.SemiBold?fg:sub;if(x is RtsUICustomToggle toggle)toggle.Foreground=fg;}} IEnumerable<DependencyObject> Find(DependencyObject p){if(p==null)yield break;foreach(var c in LogicalTreeHelper.GetChildren(p)){if(c is DependencyObject d){yield return d;foreach(var x in Find(d))yield return x;}}}
 int Preset(string t,JArray src,HashSet<string> ids,bool enabled){if(!enabled||src==null)return 0;var c=Read(P);var a=c[t] as JArray??new JArray();var count=0;foreach(var p in src.OfType<JObject>()){var id=(string)p["id"];if(ids!=null&&!ids.Contains(id))continue;var old=a.OfType<JObject>().FirstOrDefault(x=>string.Equals((string)x["id"],id,StringComparison.OrdinalIgnoreCase));if(old!=null){if(Get("import.duplicates","Skip")=="Skip")continue;old.Replace(p.DeepClone());count++;}else{a.Add(p.DeepClone());count++;}}c[t]=a;Save(P,c);return count;}
 int Animations(JObject r,HashSet<string> wanted){var seq=r["animation"] as JObject;var count=0;foreach(var x in new[]{new[]{"player",PL},new[]{"panel",PA},new[]{"message",M},new[]{"clapperboard",C}}){var src=r[x[0]] as JObject;if(src==null)continue;var ids=wanted==null?new HashSet<string>():new HashSet<string>(wanted.Where(i=>i.StartsWith(x[0]+":",StringComparison.OrdinalIgnoreCase)).Select(i=>i.Substring(x[0].Length+1)),StringComparer.OrdinalIgnoreCase);var c=Read(x[1]);var a=c["animationProfiles"] as JArray??new JArray();var dest=Read(A);var ds=dest[x[0]] as JObject??new JObject();var sa=src["animationProfiles"] as JArray??new JArray();foreach(var p in sa.OfType<JObject>()){var id=(string)p["id"];if(wanted!=null&&!ids.Contains(id))continue;var old=a.OfType<JObject>().FirstOrDefault(z=>string.Equals((string)z["id"],id,StringComparison.OrdinalIgnoreCase));if(old!=null){if(Get("import.duplicates","Skip")=="Skip")continue;old.Replace(p.DeepClone());count++;}else{a.Add(p.DeepClone());count++;}var s=seq?[x[0]]?[id];if(s!=null&&(Get("import.duplicates","Skip")=="Overwrite"||ds[id]==null))ds[id]=s.DeepClone();}c["animationProfiles"]=a;Save(x[1],c);dest[x[0]]=ds;Save(A,dest);}return count;}

 int[] Catalog(JArray src){var n=new int[3];if(src==null)return n;var d=Read(D);var a=d["catalog"] as JArray??new JArray();foreach(var p in src.OfType<JObject>()){if(!Platform((string)p["sourceType"]))continue;var old=a.OfType<JObject>().FirstOrDefault(x=>Same(x,p));if(old!=null){if(Get("import.duplicates","Skip")=="Skip"){n[2]++;continue;}old.Replace(p.DeepClone());n[1]++;}else{a.Add(p.DeepClone());n[0]++;}}d["catalog"]=a;Save(D,d);return n;}
 bool Same(JObject a,JObject b){var t=(string)b["sourceType"]??"";var id=(string)b["sourceId"]??"";return id!=""&&t.Equals((string)a["sourceType"],StringComparison.OrdinalIgnoreCase)&&id.Equals((string)a["sourceId"],StringComparison.OrdinalIgnoreCase)||(t.Equals("OBS",StringComparison.OrdinalIgnoreCase)&&string.Equals((string)a["file"],(string)b["file"],StringComparison.OrdinalIgnoreCase));}
 bool Platform(string p){if(Get("import.catalog.mode","All")=="All")return true;p=(p??"").ToLowerInvariant();return B("import.catalog."+((p=="obs")?"local":p),false);}
 int Globals(JObject g){if(g==null)return 0;var count=0;foreach(var p in g.Properties()){var k=Key(p.Name);if(k!=null&&Allowed(p.Name)){CPH.SetGlobalVar(k,p.Value.Type==JTokenType.String?(object)(string)p.Value:p.Value.ToObject<object>(),true);count++;}}return count;}
 bool Allowed(string n){if(B("import.player",true)&&"uiTheme replayFolder replayFileTypes httpMapping httpPort replayTitle newReplayTitle autoAdd autoPlay twitchPlaybackMode twitchFolder twitchHttpMapping twitchClipDuration kickPlaybackMode kickFolder kickHttpMapping youtubeClipDuration showControls showProgress playbackSpeed playbackSpeedVisibility frameColorSource frameColor controlColorSource controlColor borderGlow borderWidth cornerRadius".Split(' ').Contains(n))return true;if(B("import.panel",true)&&"panelWidth panelHeight panelCornerRadius".Split(' ').Contains(n))return true;return B("import.message",true)&&n.StartsWith("message",StringComparison.OrdinalIgnoreCase);}
 string Key(string n){var m=new System.Collections.Generic.Dictionary<string,string>{{"uiTheme","rts.actionreplay.uiTheme"},{"replayFolder","rts.actionreplay.replayFolder"},{"replayFileTypes","rts.actionreplay.replayFileTypes"},{"httpMapping","rts.actionreplay.httpMapping"},{"httpPort","rts.actionreplay.httpPort"},{"replayTitle","rts.actionreplay.replayTitle"},{"newReplayTitle","rts.actionreplay.newReplayTitle"},{"autoAdd","rts.actionreplay.autoAdd"},{"autoPlay","rts.actionreplay.autoPlay"},{"twitchPlaybackMode","rts.actionreplay.twitch.playbackMode"},{"twitchFolder","rts.actionreplay.twitch.folder"},{"twitchHttpMapping","rts.actionreplay.twitch.httpMapping"},{"twitchClipDuration","rts.actionreplay.twitch.clipDuration"},{"kickPlaybackMode","rts.actionreplay.kick.playbackMode"},{"kickFolder","rts.actionreplay.kick.folder"},{"kickHttpMapping","rts.actionreplay.kick.httpMapping"},{"youtubeClipDuration","rts.actionreplay.youtube.clipDuration"},{"showControls","rts.actionreplay.showControls"},{"showProgress","rts.actionreplay.showProgress"},{"playbackSpeed","rts.actionreplay.playbackSpeed"},{"playbackSpeedVisibility","rts.actionreplay.playbackSpeedVisibility"},{"frameColorSource","rts.actionreplay.frameColorSource"},{"frameColor","rts.actionreplay.frameColor"},{"controlColorSource","rts.actionreplay.controlColorSource"},{"controlColor","rts.actionreplay.controlColor"},{"borderGlow","rts.actionreplay.borderGlow"},{"borderWidth","rts.actionreplay.borderWidth"},{"cornerRadius","rts.actionreplay.cornerRadius"},{"panelWidth","rts.actionreplay.panel.width"},{"panelHeight","rts.actionreplay.panel.height"},{"panelCornerRadius","rts.actionreplay.panel.cornerRadius"},{"messageMinWidth","rts.actionreplay.message.minWidth"},{"messageMinHeight","rts.actionreplay.message.minHeight"},{"messageCornerRadius","rts.actionreplay.message.cornerRadius"},{"messageDuration","rts.actionreplay.message.duration"},{"messageWidth","rts.actionreplay.message.width"},{"messageHeight","rts.actionreplay.message.height"},{"messageUseSourcePlatformBranding","rts.actionreplay.message.useSourcePlatformBranding"},{"messageRecentChat","rts.actionreplay.message.recent.chat"},{"messagePlaylistChat","rts.actionreplay.message.playlist.chat"},{"messageListFormat","rts.actionreplay.message.list.format"},{"messageListMaxLength","rts.actionreplay.message.list.maxLength"}};if(m.ContainsKey(n))return m[n];foreach(var x in new[]{"Created","Queued","Renamed","Removed","Rated","Cleared","Clearedall"})if(n.StartsWith("message"+x,StringComparison.OrdinalIgnoreCase)){var rest=n.Substring(7+x.Length);return "rts.actionreplay.message."+x.ToLowerInvariant()+"."+char.ToLowerInvariant(rest[0])+rest.Substring(1);}return null;}
 void ShowImportReport(bool player,bool panel,bool message,int branding,int animation,int visual,int title,int[] catalog,int globals){MessageBox.Show((player?"Player Settings: Imported":"Player Settings: Not selected")+"\n"+(panel?"Panel Settings: Imported":"Panel Settings: Not selected")+"\n"+(message?"Message Settings: Imported":"Message Settings: Not selected")+"\nBranding Profiles: "+branding+" imported\nAnimation Profiles: "+animation+" imported\nVisual / Design Presets: "+visual+" imported\nTitle Presets: "+title+" imported\nCatalog: "+catalog[0]+" added, "+catalog[1]+" overwritten, "+catalog[2]+" skipped\nOther configuration values: "+globals+" imported\n\nPlay history: Not modified","Import Complete",MessageBoxButton.OK,MessageBoxImage.Information);} string Get(string k,string d)=>CPH.GetGlobalVar<string>(k,true)??d;bool B(string k,bool d){var v=CPH.GetGlobalVar<bool?>(k,true);return v??d;}JObject Read(string k){var s=CPH.GetGlobalVar<string>(k,true);try{return string.IsNullOrWhiteSpace(s)?new JObject():JObject.Parse(s);}catch{return new JObject();}}bool Put(string k,JObject v){if(v==null)return false;Save(k,(JObject)v.DeepClone());return true;}void Save(string k,JObject v)=>CPH.SetGlobalVar(k,v.ToString(Newtonsoft.Json.Formatting.None),true);
}