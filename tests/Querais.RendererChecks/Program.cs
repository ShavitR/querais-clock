using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Querais;
internal static class Program {
 [STAThread] static int Main(){
  // No Application.Run, Window, Show, tray icon, input or desktop interaction.
  var results=new List<object>();bool failed=false;
  void Check(string name,bool pass){results.Add(new{test=name,passed=pass});failed|=!pass;}
  var p=new Preferences{Type=1,Finish=0,Digital3D=true,Glass=true,ReducedMotion=false};var v=new SculptureView(p);var t=new DateTime(2026,10,17,10,8,30);
  v.Update(t);double a=v.MechanismAngle;v.Update(t.AddMilliseconds(100));Check("exhibition balance animates without a window",Math.Abs(v.MechanismAngle-a)>10);
  p.ReducedMotion=true;v.Update(t.AddMilliseconds(200));Check("all exhibition gears and balance rest with reduced motion",v.MechanismAtRest);
  v.Update(t.AddSeconds(2));Check("decorative mechanism stays at rest across elapsed time",v.MechanismAtRest);
  foreach(int face in new[]{-1,0,1,2}){p.Type=face<0?1:2;p.DigitalFace=Math.Max(0,face);v.SelectModel();v.ApplyFinish();v.Update(t);Check("final runtime contract / "+v.ModelKey,v.PartCount<120&&v.TriangleCount<350000&&v.ReflectiveParts>0);}
  string text=JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true});Console.WriteLine(text);Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/headless-check-results.json",text+Environment.NewLine);return failed?1:0;
 }
}
