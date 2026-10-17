using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
namespace Querais;
public sealed class AlarmEntry {
 public string Id {get;set;}=Guid.NewGuid().ToString("N");
 public string Time {get;set;}="07:00";
 public string Label {get;set;}="Morning";
 public bool Enabled {get;set;}=true;
 public bool Daily {get;set;}=true;
 public string LastFired {get;set;}="";
}
public sealed class Preferences {
 public int Type {get;set;}=1; public int AnalogFace {get;set;}=1; public int DigitalFace {get;set;}=0;
 public int Finish {get;set;}=0; public bool Dark {get;set;}=true; public bool Smooth {get;set;}=true;
 public bool Hour24 {get;set;}=false; public bool ShowSeconds {get;set;}=true; public bool ReducedMotion {get;set;}=false;
 public bool Digital3D {get;set;}=true; public bool Exploded {get;set;}=false; public bool Glass {get;set;}=true; public bool Turntable {get;set;}=false; public int StudioLight {get;set;}=0; public double LightPower {get;set;}=1; public double LightAngle {get;set;}=0;
 public bool Topmost {get;set;}=false; public bool CloseToTray {get;set;}=false; public bool Orbit {get;set;}=false;
 public double Zoom {get;set;}=1; public double Width {get;set;}=1180; public double Height {get;set;}=890;
 public int TimerSeconds {get;set;}=1500; public List<AlarmEntry> Alarms {get;set;}=new();
 public static string Folder=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Querais");
 public static string FilePath=>Path.Combine(Folder,"preferences.json");
 public static Preferences Load(){try{string path=File.Exists(FilePath)?FilePath:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Meridian","preferences.json");return JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path))??new();}catch{return new();}}
 public void Save(){try{Directory.CreateDirectory(Folder);var temp=FilePath+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,FilePath,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
}
