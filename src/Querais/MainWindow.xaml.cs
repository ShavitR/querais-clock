using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms=System.Windows.Forms;
namespace Querais;
public partial class MainWindow : Window {
 readonly Preferences pref=Preferences.Load();
 readonly Grid root=new(),stage=new();
 readonly StackPanel nav=new(){Orientation=Orientation.Horizontal},rail=new(){Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
 readonly WrapPanel variants=new(){Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,MaxWidth=900};
 readonly TextBlock name=new(),description=new(),date=new(),zone=new(),status=new();
 readonly ClockSurface face=new(); SculptureView sculpture;
 readonly Border header=new(),bottom=new(),collectionHead=new();
 readonly Button focusExit=new(){Content="Exit focus  ·  Esc",HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,8,8,0),Opacity=.40,Visibility=Visibility.Collapsed};
 readonly Grid taskArea=new(); readonly StackPanel taskControls=new(){Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
 readonly StackPanel alarmList=new(),lapList=new();
 readonly DispatcherTimer pulse=new(){Interval=TimeSpan.FromMilliseconds(50)};
 readonly DispatcherTimer frames=new(){Interval=TimeSpan.FromMilliseconds(33)};
 readonly Stopwatch stopwatch=new(); readonly List<TimeSpan> laps=new();
 readonly Forms.NotifyIcon tray;
 readonly Dictionary<string,Button> tabs=new();
 Popup? popup; string mode="Clock"; bool focus,compact,exiting; double normalWidth=1180,normalHeight=890;
 DateTimeOffset? timerDeadline; TimeSpan timerRemaining; bool timerStarted;bool timerInputDirty;
 Button? timerStart,lapButton,stopStart; TextBox? timerInput; DateTime lastAlarmCheck=DateTime.MinValue;DateTime? ringingUntil;string ringingText="";readonly StackPanel notification=new();
 readonly Stopwatch renderWatch=Stopwatch.StartNew();long lastFrame;int faceStamp=-1;
 [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attr,ref int value,int size);
 Brush R(string key)=>(Brush)Application.Current.Resources[key];
 TextBlock Label(string text,double size=13,string brush="Fg") {var t=new TextBlock{Text=text,FontSize=size,VerticalAlignment=VerticalAlignment.Center};t.SetResourceReference(TextBlock.ForegroundProperty,brush);return t;}
 Button Btn(string text,Action action,string tip=""){var b=new Button{Content=text,ToolTip=string.IsNullOrEmpty(tip)?text:tip};System.Windows.Automation.AutomationProperties.SetName(b,tip.Length>0?tip:text);b.RenderTransform=new ScaleTransform(1,1);b.Click+=(s,e)=>{if(!pref.ReducedMotion&&SystemParameters.ClientAreaAnimation){var tap=new DoubleAnimation(1,.975,TimeSpan.FromMilliseconds(65)){AutoReverse=true};((ScaleTransform)b.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty,tap);((ScaleTransform)b.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty,tap);}action();};return b;}
 Button IconButton(string glyph,Action action,string tip){var b=Btn("",action,tip);b.Content=new TextBlock{Text=glyph,FontFamily=new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),FontSize=16};b.Padding=new Thickness(11,10,11,10);return b;}
 void Row(Grid grid,UIElement element,int row){Grid.SetRow(element,row);grid.Children.Add(element);}
 public MainWindow(){
  InitializeComponent();Width=Math.Clamp(pref.Width,520,1600);Height=Math.Clamp(pref.Height,530,1200);Topmost=pref.Topmost;timerRemaining=TimeSpan.FromSeconds(Math.Clamp(pref.TimerSeconds,1,359999));
  pref.Type=Math.Clamp(pref.Type,0,2);pref.Finish=Math.Clamp(pref.Finish,0,2);pref.AnalogFace=Math.Clamp(pref.AnalogFace,0,1);pref.DigitalFace=Math.Clamp(pref.DigitalFace,0,2);pref.Zoom=Math.Clamp(pref.Zoom,.65,1.3);
  face.Pref=pref;sculpture=new SculptureView(pref);sculpture.Changed+=()=>{RefreshCollection();};
  SetResourceReference(BackgroundProperty,"Bg");SetResourceReference(ForegroundProperty,"Fg");UseLayoutRounding=true;SnapsToDevicePixels=true;
  BuildShell();Theme();RefreshCollection();ShowMode("Clock");
  tray=new Forms.NotifyIcon{Icon=System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!),Text="Querais · Clock collection",Visible=true};
  var menu=new Forms.ContextMenuStrip();menu.Items.Add("Show Querais",null,(s,e)=>Dispatcher.Invoke(Restore));menu.Items.Add("Always on top",null,(s,e)=>Dispatcher.Invoke(ToggleTop));menu.Items.Add("Pause / resume timer",null,(s,e)=>Dispatcher.Invoke(ToggleTimer));menu.Items.Add(new Forms.ToolStripSeparator());menu.Items.Add("Exit",null,(s,e)=>Dispatcher.Invoke(()=>{exiting=true;Close();}));tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>Dispatcher.Invoke(Restore);
  pulse.Tick+=(s,e)=>Pulse();pulse.Start();frames.Tick+=Rendering;frames.Start();
  SourceInitialized+=(s,e)=>DwmTheme();Loaded+=(s,e)=>{Focus();if(Environment.GetCommandLineArgs().Contains("--verify"))RunVerification();};
  PreviewKeyDown+=Keys;SizeChanged+=(s,e)=>{Adapt();};
  StateChanged+=(s,e)=>{if(WindowState==WindowState.Minimized){}};
  Closing+=(s,e)=>{if(pref.CloseToTray&&!exiting){e.Cancel=true;Hide();return;}if(!compact&&WindowState==WindowState.Normal){pref.Width=Width;pref.Height=Height;}pref.Save();pulse.Stop();frames.Stop();tray.Dispose();};
  ContextMenu=BuildContext();
 }
 void BuildShell(){
  Content=root;root.SetResourceReference(Panel.BackgroundProperty,"Bg");root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  var hg=new Grid{Margin=new Thickness(30,15,25,15)};hg.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});hg.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});hg.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  var brand=new StackPanel{Orientation=Orientation.Horizontal};brand.Children.Add(new DialThumb(0){Width=25,Height=25,Margin=new Thickness(0,0,11,0)});brand.Children.Add(Label("querais",21));hg.Children.Add(brand);Grid.SetColumn(nav,1);hg.Children.Add(nav);
  foreach(string tab in new[]{"Clock","Timer","Stopwatch","Alarms"}){string t=tab;var b=Btn(t,()=>ShowMode(t));b.Margin=new Thickness(2,0,2,0);tabs[t]=b;nav.Children.Add(b);}
  var tools=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};tools.Children.Add(IconButton("\uE718",ToggleTop,"Always on top · Ctrl+P"));tools.Children.Add(IconButton("\uE740",ToggleCompact,"Compact mode · Ctrl+M"));tools.Children.Add(IconButton("\uE708",()=>{pref.Dark=!pref.Dark;Theme();},"Toggle light / dark · T"));Grid.SetColumn(tools,2);hg.Children.Add(tools);
  header.Child=hg;header.BorderThickness=new Thickness(0,0,0,1);header.SetResourceReference(Border.BorderBrushProperty,"Line");Row(root,header,0);
  var info=new Grid{Margin=new Thickness(40,25,40,0)};info.ColumnDefinitions.Add(new ColumnDefinition());info.ColumnDefinitions.Add(new ColumnDefinition());
  var text=new StackPanel();name.FontSize=27;name.FontWeight=FontWeights.Light;text.Children.Add(name);description.Margin=new Thickness(0,6,0,0);description.FontSize=12;description.SetResourceReference(TextBlock.ForegroundProperty,"Muted");text.Children.Add(description);info.Children.Add(text);
  var right=new StackPanel{HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};date.FontSize=12;date.HorizontalAlignment=HorizontalAlignment.Right;date.SetResourceReference(TextBlock.ForegroundProperty,"Muted");right.Children.Add(date);zone.FontSize=12;zone.HorizontalAlignment=HorizontalAlignment.Right;zone.Margin=new Thickness(0,6,0,0);zone.SetResourceReference(TextBlock.ForegroundProperty,"Muted");right.Children.Add(zone);Grid.SetColumn(right,1);info.Children.Add(right);collectionHead.Child=info;Row(root,collectionHead,1);
  stage.Margin=new Thickness(28,4,28,0);stage.Children.Add(face);stage.Children.Add(sculpture);stage.Children.Add(taskArea);stage.Children.Add(focusExit);focusExit.Click+=(s,e)=>{if(compact)ToggleCompact();else ToggleFocus();};focusExit.MouseEnter+=(s,e)=>focusExit.Opacity=1;focusExit.MouseLeave+=(s,e)=>focusExit.Opacity=.40;Row(root,stage,2);
  notification.Orientation=Orientation.Horizontal;notification.HorizontalAlignment=HorizontalAlignment.Center;notification.VerticalAlignment=VerticalAlignment.Top;notification.Visibility=Visibility.Collapsed;stage.Children.Add(notification);
  stage.MouseWheel+=(s,e)=>{pref.Zoom=Math.Clamp(pref.Zoom+(e.Delta>0?.05:-.05),.65,1.3);pref.Save();face.InvalidateVisual();};
  stage.MouseLeftButtonDown+=(s,e)=>{if(e.ClickCount==2&&mode=="Clock")ToggleFocus();};
  var basePanel=new StackPanel{Margin=new Thickness(26,0,26,14)};
  variants.Margin=new Thickness(0,8,0,20);basePanel.Children.Add(variants);
  var railBox=new Border{CornerRadius=new CornerRadius(11),Padding=new Thickness(10,8,10,8),HorizontalAlignment=HorizontalAlignment.Center};railBox.SetResourceReference(Border.BackgroundProperty,"Panel");railBox.Child=rail;basePanel.Children.Add(railBox);
  for(int i=0;i<3;i++){int type=i;var b=Btn("",()=>{pref.Type=type;pref.Save();RefreshCollection();},new[]{"Analog · Ctrl+1","Sculpture · Ctrl+2","Digital · Ctrl+3"}[i]);var sp=new StackPanel{Orientation=Orientation.Horizontal};sp.Children.Add(new DialThumb(i){Width=30,Height=30,Margin=new Thickness(0,0,9,0)});sp.Children.Add(Label(new[]{"Analog","Sculpture","Digital"}[i],12));b.Content=sp;b.Tag=i;b.Padding=new Thickness(12,7,12,7);b.Margin=new Thickness(2,0,2,0);rail.Children.Add(b);}
  rail.Children.Add(Divider());
  for(int i=0;i<3;i++){int f=i;var swatch=Btn("",()=>{pref.Finish=f;pref.Save();sculpture.ApplyFinish();RefreshCollection();},new[]{"Silver · Porcelain dial","Champagne · Graphite dial","Petrol · Deep blue dial"}[i]);swatch.Tag="finish"+i;swatch.Padding=new Thickness(6);swatch.Margin=new Thickness(2,0,2,0);var circle=new Ellipse{Width=23,Height=23,StrokeThickness=1,Stroke=R("Line"),Fill=new LinearGradientBrush((Color)ColorConverter.ConvertFromString(new[]{"#E8E9E4","#D9C696","#A0B4BB"}[i]),(Color)ColorConverter.ConvertFromString(new[]{"#7F8991","#7E6949","#253E49"}[i]),45)};swatch.Content=circle;rail.Children.Add(swatch);}
  rail.Children.Add(Divider());rail.Children.Add(IconButton("\uE713",OpenSettings,"Settings · Ctrl+,"));rail.Children.Add(IconButton("\uE740",ToggleFocus,"Focus mode · F · Escape to return"));
  var foot=new Grid{Margin=new Thickness(14,18,14,0)};foot.ColumnDefinitions.Add(new ColumnDefinition());foot.ColumnDefinitions.Add(new ColumnDefinition());status.FontSize=11;status.SetResourceReference(TextBlock.ForegroundProperty,"Muted");foot.Children.Add(status);var hint=Label("Drag  Orbit    ·    Shift-drag  Light",11,"Muted");hint.HorizontalAlignment=HorizontalAlignment.Right;Grid.SetColumn(hint,1);foot.Children.Add(hint);basePanel.Children.Add(foot);bottom.Child=basePanel;Row(root,bottom,3);
 }
 UIElement Divider()=>new Border{Width=1,Height=24,Background=R("Line"),Margin=new Thickness(12,0,12,0)};
 void Adapt(){
  bool narrow=ActualWidth<830;
  ((Grid)header.Child).Margin=narrow?new Thickness(12,10,12,10):new Thickness(30,15,25,15);
  var hg=(Grid)header.Child;hg.Children[0].Visibility=narrow?Visibility.Collapsed:Visibility.Visible;
  hg.ColumnDefinitions[0].Width=narrow?new GridLength(0):new GridLength(1,GridUnitType.Star);
  hg.ColumnDefinitions[2].Width=narrow?GridLength.Auto:new GridLength(1,GridUnitType.Star);
  for(int i=0;i<3;i++){var b=(Button)rail.Children[i];var sp=(StackPanel)b.Content;sp.Children[1].Visibility=narrow?Visibility.Collapsed:Visibility.Visible;((FrameworkElement)sp.Children[0]).Margin=narrow?new Thickness():new Thickness(0,0,9,0);b.Padding=narrow?new Thickness(7):new Thickness(12,7,12,7);}
  lapList.Visibility=ActualWidth<830?Visibility.Collapsed:Visibility.Visible;
  if(ActualWidth<660){((Grid)collectionHead.Child).Margin=new Thickness(24,18,24,0);zone.Visibility=Visibility.Collapsed;}else{((Grid)collectionHead.Child).Margin=new Thickness(40,25,40,0);zone.Visibility=Visibility.Visible;}
 }
 void RefreshCollection(){
  string[] titles={pref.AnalogFace==0?"Studio analog":"Precision analog","Atelier",pref.Digital3D?new[]{"Slate","Relay","Signal"}[pref.DigitalFace]:new[]{"Typography","Split flap","Luminous"}[pref.DigitalFace]};
  string[] desc={pref.AnalogFace==0?"Essential forms. A quieter passage of time.":"An instrument for the everyday.","A precision object, made to be explored.",new[]{"A study in proportion and negative space.","Mechanical rhythm. Quietly reimagined.","Light, reduced to its essential form."}[pref.DigitalFace]};
  if(mode=="Clock"){name.Text=titles[pref.Type];description.Text=desc[pref.Type];}
  if(sculpture.Is3D)sculpture.SelectModel();bool objectView=mode=="Clock"&&sculpture.Is3D;face.Visibility=objectView?Visibility.Collapsed:Visibility.Visible;sculpture.Visibility=objectView?Visibility.Visible:Visibility.Collapsed;
  variants.Children.Clear();if(mode=="Clock"){
   if(pref.Type==0){Variant("Studio",pref.AnalogFace==0,()=>{pref.AnalogFace=0;RefreshCollection();});Variant("Precision",pref.AnalogFace==1,()=>{pref.AnalogFace=1;RefreshCollection();});}
   else if(pref.Type==2){
    for(int i=0;i<3;i++){int f=i;Variant(new[]{"Typography","Split flap","Luminous"}[i],pref.DigitalFace==i,()=>{pref.DigitalFace=f;RefreshCollection();});}
    Variant(pref.Digital3D?"3D object":"Flat face",pref.Digital3D,()=>{pref.Digital3D=!pref.Digital3D;RefreshCollection();});
   }
   if(sculpture.Is3D){
    Variant("Inspect",pref.Exploded,()=>{pref.Exploded=!pref.Exploded;if(pref.Exploded)sculpture.SetView("Exploded");RefreshCollection();});
    if(pref.Type==1||pref.DigitalFace!=1)Variant("Glass",pref.Glass,()=>{pref.Glass=!pref.Glass;RefreshCollection();});
    Variant("Turntable",pref.Turntable,()=>{pref.Turntable=!pref.Turntable;RefreshCollection();});
    Variant("View",false,OpenView);Variant("Light",false,OpenLight);
    if(pref.Type==2&&pref.DigitalFace==1)Variant("Replay flip",false,sculpture.ReplayFlip);
   }
  }

  foreach(var el in rail.Children){if(el is not Button b)continue;bool selected=b.Tag is int i&&i==pref.Type||b.Tag is string s&&s=="finish"+pref.Finish;b.Background=selected?R("Hover"):Brushes.Transparent;b.BorderBrush=selected?R("Line"):Brushes.Transparent;}
  face.Effect=mode=="Clock"&&pref.Type==2&&!pref.Digital3D&&pref.DigitalFace==2?new DropShadowEffect{Color=(Color)ColorConverter.ConvertFromString(pref.Finish==2?"#92ADB0":"#D8795C"),BlurRadius=17,ShadowDepth=0,Opacity=.35}:null;sculpture.ApplyFinish();face.InvalidateVisual();pref.Save();
 }
 void Variant(string title,bool selected,Action click){var b=Btn(title,click);b.Foreground=selected?R("Fg"):R("Muted");b.Background=selected?R("Panel"):Brushes.Transparent;b.Padding=new Thickness(13,6,13,6);b.Margin=new Thickness(2,0,2,0);variants.Children.Add(b);}
 void ShowMode(string value){
  mode=value;face.Mode=mode;face.Visibility=Visibility.Visible;taskArea.Children.Clear();taskControls.Children.Clear();lapList.Children.Clear();taskArea.Visibility=mode=="Clock"?Visibility.Collapsed:Visibility.Visible;
  foreach(var entry in tabs){entry.Value.Background=entry.Key==mode?R("Panel"):Brushes.Transparent;entry.Value.Foreground=entry.Key==mode?R("Fg"):R("Muted");}
  rail.Visibility=mode=="Clock"?Visibility.Visible:Visibility.Collapsed;variants.Visibility=mode=="Clock"?Visibility.Visible:Visibility.Collapsed;
  if(mode=="Clock"){RefreshCollection();return;}
  name.Text=mode;description.Text=mode switch{"Timer"=>"Make room for one thing.","Stopwatch"=>"Every moment accounted for.",_=>"A gentle nudge, right on time."};sculpture.Visibility=Visibility.Collapsed;
  taskControls.VerticalAlignment=VerticalAlignment.Bottom;taskControls.Margin=new Thickness(0,0,0,20);taskArea.Children.Add(taskControls);
  if(mode=="Timer"){
   timerInput=new TextBox{Text=TimeText.Duration(TimeSpan.FromSeconds(pref.TimerSeconds),true),Width=110,HorizontalContentAlignment=HorizontalAlignment.Center,ToolTip="Duration HH:mm:ss · Scroll to adjust minutes"};System.Windows.Automation.AutomationProperties.SetName(timerInput,"Timer duration HH:mm:ss");
   timerInputDirty=false;timerInput.TextChanged+=(s,e)=>timerInputDirty=true;
   timerInput.LostKeyboardFocus+=(s,e)=>ReadTimer();timerInput.KeyDown+=(s,e)=>{if(e.Key==Key.Enter){ReadTimer();Keyboard.ClearFocus();}};timerInput.PreviewMouseWheel+=(s,e)=>{if(timerDeadline!=null)return;pref.TimerSeconds=Math.Clamp(pref.TimerSeconds+(e.Delta>0?60:-60),1,359999);timerRemaining=TimeSpan.FromSeconds(pref.TimerSeconds);timerInput.Text=TimeText.Duration(timerRemaining,true);pref.Save();e.Handled=true;};taskControls.Children.Add(timerInput);
   foreach(int min in new[]{5,15,25}){int m=min;taskControls.Children.Add(Btn(m+" min",()=>{timerDeadline=null;timerStarted=false;pref.TimerSeconds=m*60;timerRemaining=TimeSpan.FromMinutes(m);timerInput.Text=TimeText.Duration(timerRemaining,true);pref.Save();UpdateTaskButtons();}));}
   timerStart=Btn(timerDeadline!=null?"Pause":"Start",ToggleTimer,"Start or pause timer · Space");timerStart.Background=R("Accent");timerStart.Foreground=Brushes.Black;timerStart.Margin=new Thickness(14,0,4,0);taskControls.Children.Add(timerStart);taskControls.Children.Add(IconButton("\uE777",ResetTimer,"Reset timer · R"));
  }
  if(mode=="Stopwatch"){
   stopStart=Btn(stopwatch.IsRunning?"Pause":"Start",()=>{if(stopwatch.IsRunning)stopwatch.Stop();else stopwatch.Start();UpdateTaskButtons();},"Start or pause stopwatch · Space");stopStart.Background=R("Accent");stopStart.Foreground=Brushes.Black;stopStart.MinWidth=90;taskControls.Children.Add(stopStart);
   lapButton=Btn("Lap",AddLap,"Record lap · L");lapButton.IsEnabled=stopwatch.IsRunning;taskControls.Children.Add(lapButton);taskControls.Children.Add(IconButton("\uE777",()=>{stopwatch.Reset();laps.Clear();DrawLaps();UpdateTaskButtons();},"Reset stopwatch · R"));
   lapList.HorizontalAlignment=HorizontalAlignment.Right;lapList.VerticalAlignment=VerticalAlignment.Center;lapList.Width=140;lapList.Margin=new Thickness(0,0,15,20);taskArea.Children.Add(lapList);DrawLaps();
  }
  if(mode=="Alarms"){
   face.Visibility=Visibility.Collapsed;alarmList.HorizontalAlignment=HorizontalAlignment.Center;alarmList.VerticalAlignment=VerticalAlignment.Center;alarmList.Width=390;var scroll=new ScrollViewer{Content=alarmList,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,MaxHeight=340,Margin=new Thickness(0,0,0,70)};taskArea.Children.Add(scroll);taskControls.Children.Add(Btn("+  Add alarm",OpenAlarm));DrawAlarms();
  }
  UpdateTaskButtons();
 }
 bool ReadTimer(){if(timerInput==null||timerDeadline!=null||!timerInputDirty)return true;if(TimeText.TryDuration(timerInput.Text,out var duration)&&duration.TotalSeconds>=1&&duration.TotalHours<100){timerRemaining=duration;pref.TimerSeconds=(int)duration.TotalSeconds;pref.Save();timerStarted=false;timerInputDirty=false;timerInput.BorderBrush=R("Line");return true;}else{timerInput.BorderBrush=R("Accent");timerInput.ToolTip="Enter HH:mm:ss, from 00:00:01 to 99:59:59";return false;}}
 void ToggleTimer(){if(timerDeadline!=null){timerRemaining=timerDeadline.Value-DateTimeOffset.UtcNow;timerDeadline=null;}else{if(mode=="Timer"&&!ReadTimer())return;if(timerRemaining<=TimeSpan.Zero)timerRemaining=TimeSpan.FromSeconds(pref.TimerSeconds);timerDeadline=DateTimeOffset.UtcNow+timerRemaining;timerStarted=true;}UpdateTaskButtons();}
 void ResetTimer(){timerDeadline=null;timerStarted=false;timerRemaining=TimeSpan.FromSeconds(pref.TimerSeconds);if(timerInput!=null){timerInput.Text=TimeText.Duration(timerRemaining,true);timerInputDirty=false;}UpdateTaskButtons();}
 void UpdateTaskButtons(){if(timerStart!=null)timerStart.Content=timerDeadline!=null?"Pause":timerStarted?"Resume":"Start";if(timerInput!=null)timerInput.IsEnabled=timerDeadline==null;if(stopStart!=null)stopStart.Content=stopwatch.IsRunning?"Pause":"Start";if(lapButton!=null)lapButton.IsEnabled=stopwatch.IsRunning;}
 void AddLap(){if(!stopwatch.IsRunning)return;laps.Insert(0,stopwatch.Elapsed);DrawLaps();}
 void DrawLaps(){lapList.Children.Clear();for(int i=0;i<Math.Min(laps.Count,5);i++){var t=Label("Lap "+(laps.Count-i)+"    "+TimeText.Duration(laps[i])+"."+(laps[i].Milliseconds/10).ToString("00"),12,"Muted");t.Margin=new Thickness(0,5,0,5);lapList.Children.Add(t);}}
 void DrawAlarms(){alarmList.Children.Clear();if(pref.Alarms.Count==0){var empty=Label("A calmer start.",27);empty.HorizontalAlignment=HorizontalAlignment.Center;alarmList.Children.Add(empty);var t=Label("Add an alarm and leave Querais running.",13,"Muted");t.Margin=new Thickness(0,12,0,0);t.HorizontalAlignment=HorizontalAlignment.Center;alarmList.Children.Add(t);return;}
  foreach(var a in pref.Alarms){var row=new Grid{Margin=new Thickness(0,10,0,10)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});var text=new StackPanel();text.Children.Add(Label(a.Time,38));text.Children.Add(Label(a.Label+" · "+(a.Daily?"Every day":"Next occurrence"),12,"Muted"));row.Children.Add(text);var enabled=new CheckBox{IsChecked=a.Enabled,VerticalAlignment=VerticalAlignment.Center,ToolTip="Enable alarm"};enabled.Checked+=(s,e)=>{a.Enabled=true;pref.Save();};enabled.Unchecked+=(s,e)=>{a.Enabled=false;pref.Save();};Grid.SetColumn(enabled,1);row.Children.Add(enabled);var del=IconButton("\uE74D",()=>{pref.Alarms.Remove(a);pref.Save();DrawAlarms();},"Delete "+a.Label+" alarm");Grid.SetColumn(del,2);row.Children.Add(del);alarmList.Children.Add(row);}
 }
 void OpenAlarm(){
  var content=new StackPanel{Width=270};content.Children.Add(Label("New alarm",20));content.Children.Add(Label("Time · 24-hour",12,"Muted"));var time=new TextBox{Text=DateTime.Now.AddMinutes(1).ToString("HH:mm"),Margin=new Thickness(0,7,0,14)};content.Children.Add(time);content.Children.Add(Label("Label",12,"Muted"));var title=new TextBox{Text="A moment for yourself",Margin=new Thickness(0,7,0,8)};content.Children.Add(title);var daily=new CheckBox{Content="Repeat every day",IsChecked=true};content.Children.Add(daily);var error=Label("",12,"Accent");content.Children.Add(error);content.Children.Add(Btn("Add alarm",()=>{if(!DateTime.TryParseExact(time.Text,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out _)){error.Text="Use HH:mm, for example 07:30.";return;}pref.Alarms.Add(new AlarmEntry{Time=time.Text,Label=title.Text.Trim().Length==0?"Alarm":title.Text.Trim(),Daily=daily.IsChecked==true});pref.Save();popup!.IsOpen=false;DrawAlarms();}));OpenPopup(content,taskControls);time.Focus();time.SelectAll();
 }
 void OpenView(){
  var content=new StackPanel{Width=230};content.Children.Add(Label("Find your angle",19));
  foreach(string view in new[]{"Front","Three-quarter","Rear"}){string v=view;var b=Btn(v,()=>{pref.Turntable=false;sculpture.SetView(v);pref.Save();popup!.IsOpen=false;RefreshCollection();});b.Margin=new Thickness(0,6,0,0);content.Children.Add(b);}
  var tip=Label(@"Drag to orbit · Home to reset
Arrow keys make fine adjustments.
Click the crown to change sweep;
drag it to change the scale.",11,"Muted");tip.LineHeight=20;tip.Margin=new Thickness(0,14,0,0);content.Children.Add(tip);OpenPopup(content,variants);
 }
 void OpenLight(){
  var content=new StackPanel{Width=275};content.Children.Add(Label("Studio lighting",19));
  var presets=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,16,0,12)};
  for(int i=0;i<3;i++){int preset=i;var b=Btn(new[]{"Daylight","Warm","Dramatic"}[i],()=>{pref.StudioLight=preset;foreach(var item in presets.Children)if(item is Button button)button.Background=button.Tag is int tag&&tag==preset?R("Hover"):Brushes.Transparent;sculpture.ApplyLighting();pref.Save();});b.Tag=i;b.Background=pref.StudioLight==i?R("Hover"):Brushes.Transparent;b.Padding=new Thickness(10,8,10,8);presets.Children.Add(b);}content.Children.Add(presets);
  void SliderRow(string text,double min,double max,double value,Action<double> changed){content.Children.Add(Label(text,12,"Muted"));var slider=new Slider{Minimum=min,Maximum=max,Value=value,Margin=new Thickness(0,10,0,16),ToolTip=text};slider.ValueChanged+=(s,e)=>{changed(e.NewValue);sculpture.ApplyLighting();pref.Save();};content.Children.Add(slider);}
  SliderRow("Intensity",.3,1.6,pref.LightPower,x=>pref.LightPower=x);SliderRow("Direction",-180,180,pref.LightAngle,x=>pref.LightAngle=x);
  content.Children.Add(Label("Shift-drag the object to sculpt the light.",11,"Muted"));OpenPopup(content,variants);
 }
 void OpenSettings(){
  var content=new StackPanel{Width=285};var heading=new DockPanel();heading.Children.Add(Label("A little more personal",19));content.Children.Add(heading);var sub=Label("CLOCK & DISPLAY",10,"Muted");sub.Margin=new Thickness(0,20,0,3);content.Children.Add(sub);
  void Check(string title,bool value,Action<bool> setter){var cb=new CheckBox{Content=title,IsChecked=value};cb.Checked+=(s,e)=>{setter(true);pref.Save();};cb.Unchecked+=(s,e)=>{setter(false);pref.Save();};content.Children.Add(cb);}
  Check("Smooth seconds",pref.Smooth,x=>pref.Smooth=x);Check("Show seconds",pref.ShowSeconds,x=>pref.ShowSeconds=x);Check("24-hour digital time",pref.Hour24,x=>pref.Hour24=x);Check("Reduced motion",pref.ReducedMotion,x=>pref.ReducedMotion=x);Check("Keep running in tray when closed",pref.CloseToTray,x=>pref.CloseToTray=x);
  var zoomLabel=Label("Clock scale",12,"Muted");zoomLabel.Margin=new Thickness(0,14,0,4);content.Children.Add(zoomLabel);var zr=new StackPanel{Orientation=Orientation.Horizontal};var slider=new Slider{Minimum=.65,Maximum=1.3,Value=pref.Zoom,Width=200,TickFrequency=.05,IsSnapToTickEnabled=true,ToolTip="Clock scale"};var val=new TextBox{Text=Math.Round(pref.Zoom*100).ToString(CultureInfo.InvariantCulture),Width=57,Margin=new Thickness(10,0,0,0)};slider.ValueChanged+=(s,e)=>{pref.Zoom=e.NewValue;val.Text=Math.Round(pref.Zoom*100).ToString(CultureInfo.InvariantCulture);pref.Save();};val.LostKeyboardFocus+=(s,e)=>{if(double.TryParse(val.Text,out double percent)){slider.Value=Math.Clamp(percent/100,.65,1.3);}else val.Text=Math.Round(pref.Zoom*100).ToString(CultureInfo.InvariantCulture);};zr.Children.Add(slider);zr.Children.Add(val);content.Children.Add(zr);
  var foot=Label("E  Inspect (when object focused)\nCtrl+1–3  Faces    ·    F  Focus\nCtrl+P  Pin    ·    Ctrl+M  Compact\nT  Theme    ·    Esc  Return",11,"Muted");foot.LineHeight=21;foot.Margin=new Thickness(0,18,0,0);content.Children.Add(foot);OpenPopup(content,bottom);
 }
 void OpenPopup(FrameworkElement content,UIElement target){if(popup!=null)popup.IsOpen=false;var border=new Border{Child=content,Padding=new Thickness(23),CornerRadius=new CornerRadius(12),BorderThickness=new Thickness(1),BorderBrush=R("Line"),Background=R("Panel"),Effect=new DropShadowEffect{BlurRadius=26,ShadowDepth=6,Opacity=.25}};popup=new Popup{Child=border,PlacementTarget=target,Placement=PlacementMode.Top,AllowsTransparency=true,StaysOpen=false,PopupAnimation=SystemParameters.ClientAreaAnimation&&!pref.ReducedMotion?PopupAnimation.Fade:PopupAnimation.None};popup.IsOpen=true;}
 void ToggleTop(){pref.Topmost=!pref.Topmost;Topmost=pref.Topmost;pref.Save();status.Text=pref.Topmost?"Always on top":"System time";}
 void ToggleFocus(){focus=!focus;focusExit.Visibility=focus?Visibility.Visible:Visibility.Collapsed;header.Visibility=focus?Visibility.Collapsed:Visibility.Visible;collectionHead.Visibility=focus?Visibility.Collapsed:Visibility.Visible;bottom.Visibility=focus?Visibility.Collapsed:Visibility.Visible;stage.Margin=focus?new Thickness(16):new Thickness(28,4,28,0);if(focus)ShowFocusHint();}
 void ShowFocusHint(){status.Text="Focus mode · press F or Escape to return";}
 void ToggleCompact(){if(!compact){normalWidth=Width;normalHeight=Height;compact=true;if(!focus)ToggleFocus();Width=520;Height=530;}else{compact=false;Width=normalWidth;Height=normalHeight;if(focus)ToggleFocus();}}
 void Restore(){Show();if(WindowState==WindowState.Minimized)WindowState=WindowState.Normal;Activate();}
 ContextMenu BuildContext(){var cm=new ContextMenu();void Add(string label,Action a){var item=new MenuItem{Header=label};item.Click+=(s,e)=>a();cm.Items.Add(item);}Add("Focus mode\tF",ToggleFocus);Add("Always on top\tCtrl+P",ToggleTop);Add("Compact mode\tCtrl+M",ToggleCompact);cm.Items.Add(new Separator());Add("Settings\tCtrl+,",OpenSettings);Add("Hide to tray",Hide);Add("Exit",()=>{exiting=true;Close();});return cm;}
 void Theme(){
  var values=pref.Dark?new[]{"#191C20","#F2F0E9","#9BA2AA","#24282D","#343A41","#3A4047","#D8795C"}:new[]{"#F2F0E9","#252A30","#646A70","#E5E2DB","#D8D5CE","#C8C6BF","#B8553A"};string[] keys={"Bg","Fg","Muted","Panel","Hover","Line","Accent"};for(int i=0;i<keys.Length;i++)Application.Current.Resources[keys[i]]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(values[i]));DwmTheme();pref.Save();foreach(var pair in tabs){pair.Value.Background=pair.Key==mode?R("Panel"):Brushes.Transparent;pair.Value.Foreground=pair.Key==mode?R("Fg"):R("Muted");}RefreshCollection();if(mode!="Clock")ShowMode(mode);
 }
 void DwmTheme(){var hwnd=new WindowInteropHelper(this).Handle;if(hwnd==IntPtr.Zero)return;int dark=pref.Dark?1:0;DwmSetWindowAttribute(hwnd,20,ref dark,4);int caption=pref.Dark?0x00201C19:0x00E9F0F2;DwmSetWindowAttribute(hwnd,35,ref caption,4);int backdrop=2;DwmSetWindowAttribute(hwnd,38,ref backdrop,4);}
 void Keys(object sender,KeyEventArgs e){
  bool ctrl=(Keyboard.Modifiers&ModifierKeys.Control)!=0;bool editing=Keyboard.FocusedElement is TextBox;
  if(e.Key==Key.Escape){if(popup?.IsOpen==true)popup.IsOpen=false;else if(compact)ToggleCompact();else if(focus)ToggleFocus();e.Handled=true;return;}
  if(editing)return;
  if(ctrl&&e.Key>=Key.D1&&e.Key<=Key.D3){pref.Type=e.Key-Key.D1;ShowMode("Clock");e.Handled=true;return;}
  if(ctrl&&e.Key==Key.P)ToggleTop();else if(ctrl&&e.Key==Key.M)ToggleCompact();else if(ctrl&&e.Key==Key.OemComma)OpenSettings();else if(e.Key==Key.F)ToggleFocus();else if(e.Key==Key.T){pref.Dark=!pref.Dark;Theme();}else if(e.Key==Key.Space){if(mode=="Clock"&&ringingUntil==null)return;if(mode=="Timer")ToggleTimer();if(mode=="Stopwatch"){if(stopwatch.IsRunning)stopwatch.Stop();else stopwatch.Start();UpdateTaskButtons();}if(ringingUntil!=null)Dismiss();}else if(e.Key==Key.L&&mode=="Stopwatch")AddLap();else if(e.Key==Key.R){if(mode=="Timer")ResetTimer();if(mode=="Stopwatch"){stopwatch.Reset();laps.Clear();DrawLaps();UpdateTaskButtons();}}else return;e.Handled=true;
 }
 void Pulse(){
  pulse.Interval=TimeSpan.FromMilliseconds(!IsVisible||WindowState==WindowState.Minimized?250:50);
  var now=DateTime.Now;
  if(timerDeadline is {} deadline){timerRemaining=deadline-DateTimeOffset.UtcNow;if(timerRemaining<=TimeSpan.Zero){timerRemaining=TimeSpan.Zero;timerDeadline=null;timerStarted=false;UpdateTaskButtons();Ring("Your timer is complete.");}}
  if((now-lastAlarmCheck).TotalMilliseconds>500){var previous=lastAlarmCheck;lastAlarmCheck=now;foreach(var a in pref.Alarms){if(!a.Enabled||a.LastFired==now.ToString("yyyy-MM-dd"))continue;if(TimeSpan.TryParse(a.Time,out var at)&&now.TimeOfDay>=at&&(now.TimeOfDay-at<TimeSpan.FromMinutes(1)||(previous.Date==now.Date&&previous.TimeOfDay<at))){a.LastFired=now.ToString("yyyy-MM-dd");if(!a.Daily)a.Enabled=false;pref.Save();Ring(a.Label);if(mode=="Alarms")DrawAlarms();}}}
  if(ringingUntil is {} until){if(now>until)Dismiss();else if(now.Millisecond<75)System.Media.SystemSounds.Exclamation.Play();}
  if(!IsVisible||WindowState==WindowState.Minimized){frames.Stop();return;}
  if(mode=="Clock"&&sculpture.Is3D){if(!frames.IsEnabled)frames.Start();}else frames.Stop();
  date.Text=now.ToString("dddd, d MMMM",CultureInfo.InvariantCulture);zone.Text="Local time  /  "+(TimeZoneInfo.Local.IsDaylightSavingTime(now)?TimeZoneInfo.Local.DaylightName:TimeZoneInfo.Local.StandardName);
  status.Text=ringingUntil!=null?ringingText:timerDeadline!=null&&mode!="Timer"?"Timer running · "+timerRemaining.ToString(@"mm\:ss"):pref.Topmost?"Always on top  ·  System time":"System time  ·  UTC"+now.ToString("zzz");
  face.Time=now;face.Duration=mode=="Stopwatch"?stopwatch.Elapsed:timerRemaining;face.BottomLabel=timerDeadline!=null?"Stay with it.":timerStarted?"Paused":"Ready when you are.";
  if(mode!="Clock"||pref.Type==0&&pref.Smooth&&!pref.ReducedMotion||faceStamp!=now.Second||pref.Type==2&&pref.DigitalFace==1&&now.Second==0){faceStamp=now.Second;face.InvalidateVisual();}
 }
 void Rendering(object? sender,EventArgs e){if(!IsVisible||WindowState==WindowState.Minimized||mode!="Clock"||!sculpture.Is3D)return;long elapsed=renderWatch.ElapsedMilliseconds;if(elapsed-lastFrame<(pref.ReducedMotion||!pref.Smooth?100:33))return;lastFrame=elapsed;sculpture.Update(DateTime.Now);}
 void Ring(string text){ringingText=text;ringingUntil=DateTime.Now.AddSeconds(30);notification.Children.Clear();var box=new Border{Background=R("Panel"),CornerRadius=new CornerRadius(8),Padding=new Thickness(18,9,10,9)};var row=new StackPanel{Orientation=Orientation.Horizontal};row.Children.Add(Label(text,14));var stop=Btn("Dismiss",Dismiss,"Dismiss alert · Space");stop.Margin=new Thickness(14,0,0,0);row.Children.Add(stop);box.Child=row;notification.Children.Add(box);notification.Visibility=Visibility.Visible;System.Media.SystemSounds.Exclamation.Play();tray.BalloonTipTitle="Querais";tray.BalloonTipText=text;tray.ShowBalloonTip(10000);}
 void Dismiss(){ringingUntil=null;notification.Visibility=Visibility.Collapsed;}
 // Built-in deterministic capture/behavior harness, invoked only with --verify.
 async void RunVerification(){await Verification.Run(this);}
 internal Preferences Settings=>pref;
 internal bool VerifyRinging=>ringingUntil!=null;
 internal void VerifyDismiss()=>Dismiss();
 internal long VerifyFrameCount=>sculpture.FrameCount;
 internal SculptureView ObjectView=>sculpture;
 internal void VerifyClick(string tooltip){
  Button? Find(DependencyObject node){for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++){var child=VisualTreeHelper.GetChild(node,i);if(child is Button b&&b.ToolTip as string==tooltip)return b;var found=Find(child);if(found!=null)return found;}return null;}
  var button=Find(this)??throw new InvalidOperationException("Button not found: "+tooltip);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
 }
 internal void VerifyStopwatchPause()=>stopwatch.Stop();
 internal TimeSpan VerifyStopwatchElapsed=>stopwatch.Elapsed;

 internal void VerifyMode(string value)=>ShowMode(value);
 internal void VerifyCollection()=>RefreshCollection();
 internal void VerifyTheme()=>Theme();
 internal void VerifyFocus()=>ToggleFocus();
 internal void VerifyTimer()=>ToggleTimer();
 internal void VerifyResetTimer()=>ResetTimer();
 internal TimeSpan VerifyRemaining=>timerRemaining;
 internal void VerifyTimerInput(string value){timerInput!.Text=value;ReadTimer();}
 internal bool VerifyTimerRunning=>timerDeadline!=null;
 internal bool VerifyTimerInputEnabled=>timerInput?.IsEnabled==true;
 internal void VerifyStopwatchStart()=>stopwatch.Start();
 internal void VerifyLap()=>AddLap();
 internal int VerifyLapCount=>laps.Count;
 internal void VerifySettings()=>OpenSettings();
 internal FrameworkElement? VerifyPopupContent=>popup?.Child as FrameworkElement;
 internal void VerifyAlarmEditor()=>OpenAlarm();
 internal void VerifyClosePopup(){if(popup!=null)popup.IsOpen=false;}
}
public sealed class DialThumb : FrameworkElement {
 readonly int type;public DialThumb(int type){this.type=type;}
 protected override void OnRender(DrawingContext dc){var c=new Point(ActualWidth/2,ActualHeight/2);var b=new SolidColorBrush(Color.FromRgb(188,194,195));if(type<2){dc.DrawEllipse(type==1?new SolidColorBrush(Color.FromRgb(92,102,108)):Brushes.Transparent,new Pen(b,1),c,ActualWidth*.4,ActualHeight*.4);dc.DrawLine(new Pen(b,1.2),c,new Point(c.X,c.Y-ActualHeight*.23));dc.DrawLine(new Pen(b,1.2),c,new Point(c.X+ActualWidth*.2,c.Y+ActualHeight*.12));}else{var ft=new FormattedText("12",CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Bahnschrift"),ActualWidth*.55,b,VisualTreeHelper.GetDpi(this).PixelsPerDip);dc.DrawText(ft,new Point(c.X-ft.Width/2,c.Y-ft.Height/2));}}
}
