using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Querais;
public static class Verification {
 public static async Task Run(MainWindow w){
  var location=new DirectoryInfo(AppContext.BaseDirectory);while(location!=null&&!File.Exists(Path.Combine(location.FullName,"src","Querais","Querais.csproj")))location=location.Parent;
  string dest=location!=null?Path.Combine(location.FullName,"artifacts","qa"):Path.Combine(Preferences.Folder,"verification");Directory.CreateDirectory(dest);
  var results=new List<object>();var p=w.Settings;var original=JsonSerializer.Serialize(p);
  async Task Capture(string file){await Task.Delay(350);w.UpdateLayout();var visual=(FrameworkElement)w.Content;var bmp=new RenderTargetBitmap((int)visual.ActualWidth,(int)visual.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(visual);var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(bmp));using var f=File.Create(Path.Combine(dest,file+".png"));enc.Save(f);results.Add(new{capture=file,width=visual.ActualWidth,height=visual.ActualHeight});}
  async Task CapturePopup(string file){await Task.Delay(250);var visual=w.VerifyPopupContent!;visual.UpdateLayout();var bmp=new RenderTargetBitmap((int)visual.ActualWidth,(int)visual.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(visual);var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(bmp));using var f=File.Create(Path.Combine(dest,file+".png"));enc.Save(f);}
  try{
   w.Width=1180;w.Height=890;p.Zoom=1;p.Exploded=false;p.Turntable=false;p.Digital3D=true;p.Glass=true;p.LightPower=1;p.LightAngle=0;p.StudioLight=0;p.ReducedMotion=false;p.Hour24=false;p.ShowSeconds=true;p.Smooth=true;
   w.VerifyClick("Analog · Ctrl+1");results.Add(new{test="visible Analog selector",passed=p.Type==0});
   w.VerifyClick("Digital · Ctrl+3");results.Add(new{test="visible Digital selector",passed=p.Type==2});
   w.VerifyClick("Sculpture · Ctrl+2");results.Add(new{test="visible Sculpture selector",passed=p.Type==1});
   bool top=w.Topmost;w.VerifyClick("Always on top · Ctrl+P");results.Add(new{test="native always-on-top control",passed=w.Topmost!=top});w.VerifyClick("Always on top · Ctrl+P");
   w.VerifyClick("Compact mode · Ctrl+M");results.Add(new{test="compact control",passed=w.Width==520&&w.Height==530});w.VerifyClick("Compact mode · Ctrl+M");
   p.Dark=true;p.Type=1;p.Finish=0;w.VerifyTheme();w.VerifyMode("Clock");await Capture("01-sculpture-dark");
   p.Dark=false;w.VerifyTheme();await Capture("02-sculpture-light");
   p.Dark=true;p.Type=0;p.AnalogFace=1;w.VerifyTheme();w.VerifyCollection();await Capture("03-analog-precision");
   p.AnalogFace=0;p.Finish=2;w.VerifyCollection();await Capture("04-analog-studio");
   p.Type=2;p.Finish=0;p.DigitalFace=0;w.VerifyCollection();await Capture("05-typography");
   p.DigitalFace=1;w.VerifyCollection();await Capture("06-split-flap");
   p.DigitalFace=2;p.Finish=2;w.VerifyCollection();await Capture("07-segment");
   w.Width=640;w.Height=650;p.Type=1;w.VerifyCollection();await Capture("08-small-window");
   w.VerifyFocus();w.Width=520;w.Height=530;await Capture("09-compact-focus");w.VerifyFocus();
   w.Width=1180;w.Height=890;w.VerifyMode("Timer");await Capture("10-timer");
   w.VerifyTimerInput("27:05:10");results.Add(new{test="direct timer input beyond 24 hours",passed=w.VerifyRemaining==new TimeSpan(27,5,10)});
   w.VerifyTimerInput("00:99:00");w.VerifyTimer();results.Add(new{test="invalid numeric input does not start timer",passed=!w.VerifyTimerRunning});

   p.TimerSeconds=20;w.VerifyResetTimer();w.VerifyTimer();await Task.Delay(100);w.VerifyTimer();w.VerifyTimerInput("invalid");w.VerifyTimer();results.Add(new{test="invalid edited duration cannot resume paused timer",passed=!w.VerifyTimerRunning});
   w.VerifyTimerInput("00:00:15");w.VerifyTimer();w.VerifyMode("Clock");w.VerifyMode("Timer");results.Add(new{test="running timer field disabled after changing views",passed=!w.VerifyTimerInputEnabled});
   p.TimerSeconds=1;w.VerifyResetTimer();w.VerifyMode("Clock");w.VerifyTimer();await Task.Delay(1400);results.Add(new{test="timer completion across view change",passed=!w.VerifyTimerRunning&&w.VerifyRemaining==TimeSpan.Zero});
   w.VerifyDismiss();w.VerifyMode("Stopwatch");w.VerifyClick("Start or pause stopwatch · Space");await Task.Delay(130);w.VerifyLap();results.Add(new{test="stopwatch records running lap",passed=w.VerifyLapCount==1});await Capture("11-stopwatch");
   w.VerifyClick("Start or pause stopwatch · Space");var stopped=w.VerifyStopwatchElapsed;await Task.Delay(120);results.Add(new{test="stopwatch pause holds elapsed time",passed=w.VerifyStopwatchElapsed==stopped});
   w.VerifyMode("Alarms");await Capture("12-alarms");w.VerifyAlarmEditor();await CapturePopup("14-alarm-editor");w.VerifyClosePopup();
   var testAlarm=new AlarmEntry{Time=DateTime.Now.ToString("HH:mm"),Label="Verification alarm",Daily=false};p.Alarms.Add(testAlarm);await Task.Delay(800);results.Add(new{test="alarm fires once and disables one-shot",passed=!testAlarm.Enabled&&testAlarm.LastFired==DateTime.Now.ToString("yyyy-MM-dd")});p.Alarms.Remove(testAlarm);w.VerifyDismiss();
   w.VerifyMode("Clock");p.Type=1;w.VerifyCollection();await Task.Delay(150);w.WindowState=WindowState.Minimized;await Task.Delay(150);long frames=w.VerifyFrameCount;await Task.Delay(200);results.Add(new{test="3D rendering pauses while minimized",passed=w.VerifyFrameCount==frames});w.WindowState=WindowState.Normal;
   w.Hide();await Task.Delay(100);frames=w.VerifyFrameCount;await Task.Delay(200);results.Add(new{test="3D rendering pauses while hidden",passed=w.VerifyFrameCount==frames});w.Show();
   w.VerifyMode("Clock");p.Type=1;p.Finish=0;p.Zoom=1;p.Exploded=false;p.Turntable=false;p.Glass=true;p.Digital3D=true;w.VerifyCollection();w.ObjectView.ResetOrbit();
   w.VerifyClick("Inspect");await Task.Delay(600);results.Add(new{test="exploded assembly opens from visible control",passed=w.ObjectView.Explosion>.95});await Capture("16-atelier-exploded");w.VerifyClick("Inspect");
   w.ObjectView.SetView("Rear");await Task.Delay(1000);results.Add(new{test="rear view rotates to caseback",angle=w.ObjectView.ViewYaw,passed=Math.Abs(Math.Abs(Math.IEEERemainder(w.ObjectView.ViewYaw,360))-180)<5});await Capture("17-atelier-caseback");
   w.VerifyClick("Glass");w.ObjectView.Update(DateTime.Now);results.Add(new{test="glass control hides actual crystal mesh",passed=!w.ObjectView.GlassVisible});w.VerifyClick("Glass");
   w.ObjectView.ResetOrbit();await Task.Delay(250);double yaw=w.ObjectView.ViewYaw;w.ObjectView.OrbitBy(50,-25);await Task.Delay(450);results.Add(new{test="orbit changes live mesh orientation",passed=Math.Abs(w.ObjectView.ViewYaw-yaw)>8});
   p.LightPower=1;p.LightAngle=0;p.StudioLight=0;double light=p.LightAngle;w.ObjectView.MoveLight(30,-10);results.Add(new{test="interactive lighting direction and intensity",passed=p.LightAngle!=light&&p.LightPower>1});p.LightAngle=0;p.LightPower=1;w.ObjectView.ApplyLighting();
   bool sweep=p.Smooth;w.ObjectView.ToggleSweep();results.Add(new{test="crown changes smooth or ticking seconds",passed=p.Smooth!=sweep});w.ObjectView.ToggleSweep();
   w.ObjectView.ResetOrbit();await Task.Delay(650);yaw=w.ObjectView.ViewYaw;w.VerifyClick("Turntable");await Task.Delay(700);results.Add(new{test="turntable animates model",before=yaw,after=w.ObjectView.ViewYaw,passed=Math.Abs(w.ObjectView.ViewYaw-yaw)>1});w.VerifyClick("Turntable");
   results.Add(new{test="orbit angle stays continuous across multiple revolutions",passed=Math.Abs(SculptureView.ShortestDelta(25,745))<.001&&Math.Abs(SculptureView.ShortestDelta(-179,179)-2)<.001&&Math.Abs(SculptureView.ShortestDelta(179,-179)+2)<.001});
   p.Type=2;p.DigitalFace=1;p.ReducedMotion=false;w.VerifyCollection();w.ObjectView.ReplayFlip();w.ObjectView.Update(DateTime.Now);results.Add(new{test="split-flap replay activates real moving plates",passed=w.ObjectView.ActiveFlipPlates>0});await Task.Delay(250);await Capture("18-relay-flipping");
   p.ReducedMotion=true;w.ObjectView.ReplayFlip();w.ObjectView.Update(DateTime.Now);results.Add(new{test="reduced motion suppresses mechanical transition",passed=w.ObjectView.ActiveFlipPlates==0});p.ReducedMotion=false;
   p.DigitalFace=2;p.ShowSeconds=false;w.VerifyCollection();w.ObjectView.Update(DateTime.Now);results.Add(new{test="luminous native segment meshes follow digit masks",passed=w.ObjectView.VisibleSegments>=8&&w.ObjectView.VisibleSegments<=28});
   p.Digital3D=false;w.VerifyCollection();await Capture("19-flat-fallback");results.Add(new{test="flat face remains available",passed=!w.ObjectView.Is3D});p.Digital3D=true;p.ShowSeconds=true;
   w.Width=640;w.Height=650;p.Type=2;p.DigitalFace=1;w.VerifyCollection();w.ObjectView.ResetOrbit();await Capture("20-small-digital-object");
   w.Width=1180;w.Height=890;p.Type=1;p.Finish=0;p.Exploded=false;p.Turntable=false;w.VerifyCollection();w.ObjectView.ResetOrbit();
   w.VerifyMode("Clock");p.Type=1;p.Finish=0;w.VerifyCollection();w.VerifySettings();await Capture("13-settings-underlying-window");await CapturePopup("15-settings-popover");w.VerifyClosePopup();
   p.Save();var read=Preferences.Load();results.Add(new{test="preferences round trip",passed=read.Type==p.Type&&read.Finish==p.Finish&&read.Dark==p.Dark});
  }catch(Exception e){results.Add(new{error=e.ToString()});}
  finally{
   // Restore user choices after the verification pass.
   File.WriteAllText(Preferences.FilePath,original);var restored=JsonSerializer.Deserialize<Preferences>(original)!;
   foreach(var property in typeof(Preferences).GetProperties(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance))if(property.CanRead&&property.CanWrite)property.SetValue(p,property.GetValue(restored));
   w.VerifyStopwatchPause();w.VerifyDismiss();w.Width=Math.Clamp(restored.Width,520,1600);w.Height=Math.Clamp(restored.Height,530,1200);w.VerifyTheme();w.VerifyMode("Clock");
   File.WriteAllText(Path.Combine(dest,"results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
  }
 }
}
