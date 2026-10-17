using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
namespace Querais;

/// <summary>Native mesh collection. All housings and moving plates originate in Blender.</summary>
public sealed class SculptureView : Grid {
 sealed class Part {
  public string Name="",Role="";
  public GeometryModel3D Model=new();
  public TranslateTransform3D Lift=new();
  public AxisAngleRotation3D? Rotation;
  public Material? Base;
  public MeshGeometry3D Mesh=null!;public Vector3DCollection Normals=null!;
  public double Ratio=1,ReflectionAngle=double.NaN;public bool Reflective;
 }
 sealed class ObjectScene {
  public string Key="";public double Width,Height;
  public Model3DGroup Model=new();public List<Part> Parts=new();
 }
 sealed class AssetScene {public double Width{get;set;}public double Height{get;set;}public AssetPart[] Parts{get;set;}=Array.Empty<AssetPart>();}
 sealed class AssetPart {
  public string Name{get;set;}="";public string Material{get;set;}="";
  public double[] Positions{get;set;}=Array.Empty<double>();public double[] Normals{get;set;}=Array.Empty<double>();public double[] Uv{get;set;}=Array.Empty<double>();
  public int[] Indices{get;set;}=Array.Empty<int>();public double[]? Pivot{get;set;}public double Ratio{get;set;}=1;
 }
 public Preferences Pref {get;}
 readonly Viewport3D viewport=new();readonly ModelVisual3D content=new();
 readonly Dictionary<string,ObjectScene> scenes=new();
 readonly Dictionary<string,Material> digitMaterials=new();
 readonly AxisAngleRotation3D yaw=new(new Vector3D(0,1,0),-13),pitch=new(new Vector3D(1,0,0),13);
 readonly DirectionalLight keyLight=new(),fillLight=new();readonly AmbientLight ambient=new();
 ObjectScene current=null!;Point? drag;Point dragStart,pointer;bool lightDrag,crownDrag;double crownAngle;
 double targetYaw=-13,targetPitch=13,explosion;long previousTicks;double lastDt=.033;
 string digits="",oldDigits="",lastDisplay="";DateTime flipAt=DateTime.MinValue;bool forceFlip;
 DateTime reflectionAt=DateTime.MinValue;
 static readonly int[] Masks={0x3f,0x06,0x5b,0x4f,0x66,0x6d,0x7d,0x07,0x7f,0x6f};
 public long FrameCount {get;private set;}
 public string ModelKey=>current.Key;
 public int PartCount=>current.Parts.Count;
 public int TriangleCount=>current.Parts.Sum(p=>p.Mesh.TriangleIndices.Count/3);
 public int ReflectiveParts=>current.Parts.Count(p=>p.Reflective);
 public int MechanicalParts=>current.Parts.Count(p=>p.Name.StartsWith("Gear_")||p.Name=="Balance");
 public double MechanismAngle=>current.Parts.FirstOrDefault(p=>p.Name=="Balance")?.Rotation?.Angle??0;
 public bool MechanismAtRest=>current.Parts.Where(p=>p.Name.StartsWith("Gear_")||p.Name=="Balance").All(p=>Math.Abs(p.Rotation!.Angle)<.001);
 public double ReflectionUpdateMilliseconds {get;private set;}
 public double ViewYaw=>yaw.Angle;
 public double Explosion=>explosion;
 public bool Is3D=>Pref.Type==1||Pref.Type==2&&Pref.Digital3D;
 public event Action? Changed;
 public SculptureView(Preferences pref){
  Pref=pref;ClipToBounds=true;Background=Brushes.Transparent;Focusable=true;Cursor=Cursors.Hand;
  Children.Add(viewport);viewport.Camera=new OrthographicCamera(new Point3D(0,0,14),new Vector3D(0,0,-14),new Vector3D(0,1,0),6);
  var lights=new Model3DGroup();lights.Children.Add(ambient);lights.Children.Add(keyLight);lights.Children.Add(fillLight);viewport.Children.Add(new ModelVisual3D{Content=lights});viewport.Children.Add(content);
  SelectModel();ApplyLighting();
  ToolTip="Drag to orbit · Shift-drag to move the light · Scroll to scale · Arrow keys to rotate · Home to reset";
  MouseLeftButtonDown+=(s,e)=>{
   var p=e.GetPosition(this);drag=dragStart=p;lightDrag=(Keyboard.Modifiers&ModifierKeys.Shift)!=0;
   var hit=VisualTreeHelper.HitTest(viewport,e.GetPosition(viewport)) as RayMeshGeometry3DHitTestResult;
   crownDrag=hit!=null&&current.Parts.Any(x=>ReferenceEquals(x.Model,hit.ModelHit)&&x.Name=="Crown");
   if(e.ClickCount==2&&!crownDrag){ResetOrbit();drag=null;e.Handled=true;return;}
   Focus();CaptureMouse();Cursor=lightDrag?Cursors.Cross:Cursors.SizeAll;e.Handled=true;
  };
  MouseMove+=(s,e)=>{
   var p=e.GetPosition(this);pointer=new Point((p.X/Math.Max(1,ActualWidth)-.5)*2,(p.Y/Math.Max(1,ActualHeight)-.5)*2);
   if(drag is not {} old)return;double dx=p.X-old.X,dy=p.Y-old.Y;
   if(lightDrag)MoveLight(dx,dy);else if(crownDrag){crownAngle+=dy*3;Pref.Zoom=Math.Clamp(Pref.Zoom-dy*.003,.65,1.3);}else OrbitBy(dx,dy);
   drag=p;
  };
  MouseLeftButtonUp+=(s,e)=>{
   if(drag!=null&&crownDrag&&(e.GetPosition(this)-dragStart).Length<4)ToggleSweep();
   drag=null;crownDrag=false;ReleaseMouseCapture();Cursor=Cursors.Hand;Pref.Save();Changed?.Invoke();e.Handled=true;
  };
  LostMouseCapture+=(s,e)=>{drag=null;Cursor=Cursors.Hand;};
  MouseLeave+=(s,e)=>pointer=new Point();
  PreviewKeyDown+=(s,e)=>{
   switch(e.Key){case Key.Left:OrbitBy(-30,0);break;case Key.Right:OrbitBy(30,0);break;case Key.Up:OrbitBy(0,-25);break;case Key.Down:OrbitBy(0,25);break;case Key.Home:ResetOrbit();break;case Key.E:Pref.Exploded=!Pref.Exploded;Pref.Save();Changed?.Invoke();break;default:return;}e.Handled=true;
  };
 }
 static Color C(string h)=>(Color)ColorConverter.ConvertFromString(h);
 public void SelectModel(){
  string key=Pref.Type==1?"atelier":new[]{"slate","relay","signal"}[Math.Clamp(Pref.DigitalFace,0,2)];
  if(current!=null&&current.Key==key)return;
  if(!scenes.TryGetValue(key,out var scene)){scene=Load(key);scenes[key]=scene;}
  // Keep one neighbouring object warm; detailed geometry should not accumulate
  // for the entire collection as users browse it.
  if(scenes.Count>2){string? stale=scenes.Keys.FirstOrDefault(k=>k!=key&&k!=current?.Key);if(stale!=null)scenes.Remove(stale);}
  current=scene;content.Content=scene.Model;
  var transform=new Transform3DGroup();transform.Children.Add(new RotateTransform3D(yaw));transform.Children.Add(new RotateTransform3D(pitch));scene.Model.Transform=transform;
  digits=oldDigits=lastDisplay="";digitMaterials.Clear();ApplyFinish();
 }
 ObjectScene Load(string key){
  using var file=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"assets","collection",key+".mesh.json.gz"));using var zip=new GZipStream(file,CompressionMode.Decompress);
  // Stream directly into typed arrays. A JsonDocument token tree for hundreds
  // of thousands of numeric vertices retains large pooled buffers unnecessarily.
  var data=JsonSerializer.Deserialize<AssetScene>(zip,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
  var scene=new ObjectScene{Key=key,Width=data.Width,Height=data.Height};
  foreach(var item in data.Parts){
   string name=item.Name,role=item.Material;var points=item.Positions;var normals=item.Normals;var uv=item.Uv;
   var mesh=new MeshGeometry3D{Positions=new Point3DCollection(points.Length/3),Normals=new Vector3DCollection(normals.Length/3),TextureCoordinates=new PointCollection(uv.Length/2),TriangleIndices=new Int32Collection(item.Indices.Length)};
   bool nativeDial=name=="DialText"&&key=="atelier";
   bool reflective=StudioMetal.Reflects(role);
   bool display=name.StartsWith("Display_")||name.StartsWith("Date_")||name.StartsWith("Flap_")||name.StartsWith("Flip_");
   for(int i=0;!nativeDial&&i<points.Length;i+=3){double x=points[i],y=points[i+1],z=points[i+2];mesh.Positions.Add(new Point3D(x,y,z));mesh.Normals.Add(new Vector3D(normals[i],normals[i+1],normals[i+2]));int v=i/3;mesh.TextureCoordinates.Add(display?new Point(uv[v*2],uv[v*2+1]):new Point((x+scene.Width/2)/scene.Width,(y+scene.Height/2)/scene.Height));}
   if(nativeDial){mesh.Positions=new Point3DCollection{new(-1.65,-1.65,.084),new(1.65,-1.65,.084),new(1.65,1.65,.084),new(-1.65,1.65,.084)};mesh.Normals=new Vector3DCollection{new(0,0,1),new(0,0,1),new(0,0,1),new(0,0,1)};mesh.TextureCoordinates=new PointCollection{new(0,1),new(1,1),new(1,0),new(0,0)};mesh.TriangleIndices=new Int32Collection{0,1,2,0,2,3};}
   else foreach(int ix in item.Indices)mesh.TriangleIndices.Add(ix);
   mesh.Positions.Freeze();mesh.Normals.Freeze();mesh.TriangleIndices.Freeze();if(!reflective)mesh.Freeze();
   var part=new Part{Name=name,Role=role,Mesh=mesh,Normals=mesh.Normals,Reflective=reflective,Model=new GeometryModel3D{Geometry=mesh}};var transform=new Transform3DGroup();
   if(name.StartsWith("Hand_")){part.Rotation=new AxisAngleRotation3D(new Vector3D(0,0,1),0);transform.Children.Add(new RotateTransform3D(part.Rotation));}
   else if(name.StartsWith("Flip_")){int i=int.Parse(name.Split('_')[1]);double x=(i-1.5)*1.48+(i>=2?.10:-.10);part.Rotation=new AxisAngleRotation3D(new Vector3D(1,0,0),0);transform.Children.Add(new RotateTransform3D(part.Rotation,new Point3D(x,.09,.287)));}
   else if(name=="Crown"){part.Rotation=new AxisAngleRotation3D(new Vector3D(1,0,0),0);transform.Children.Add(new RotateTransform3D(part.Rotation,new Point3D(2.12,0,-.03)));}
   else if(item.Pivot is {} pivot){
    part.Ratio=item.Ratio;part.Rotation=new AxisAngleRotation3D(new Vector3D(0,0,1),0);transform.Children.Add(new RotateTransform3D(part.Rotation,new Point3D(pivot[0],pivot[1],pivot[2])));
   }
   transform.Children.Add(part.Lift);part.Model.Transform=transform;scene.Parts.Add(part);scene.Model.Children.Add(part.Model);
  }
  return scene;
 }
 public void ApplyFinish(){
  if(current==null)return;
  digitMaterials.Clear();lastDisplay="";
  foreach(var part in current.Parts){
   string role=part.Role;bool darkDial=Pref.Finish!=0;string color=role switch{
    "dial"=>Pref.Finish==0?"#ECE7D8":Pref.Finish==1?"#282B2B":"#173540",
    "ink"=>darkDial?"#E1DDCF":"#1C2B32",
    "lume"=>darkDial?"#F3EEDB":"#C3BDA8",
    "accent"=>Pref.Finish==2?"#98CFD0":"#EC8159",
    "shell"=>Pref.Finish==1?"#3A352C":Pref.Finish==2?"#243A43":"#2B343E",
    "steel" or "polished"=>Pref.Finish==1?"#C0A578":Pref.Finish==2?"#8CA4AE":"#BCC6CD",
    "lettering"=>current.Key=="atelier"?(darkDial?"#AEB8B8":"#516369"):"#A4B1BA",
    "rubber"=>"#141A20","screen"=>"#10171D",_=>"#AAB3B8"};
   if(role=="brass")color="#B89B61";if(role=="jewel")color="#972943";if(role=="pcb")color="#18473F";if(role=="ceramic")color="#293841";
   if(part.Name=="DialText"&&current.Key=="atelier"){part.Base=DialLettering();part.Model.Material=part.Base;part.Model.BackMaterial=null;continue;}
   if(part.Name=="DialDetail"&&current.Key=="atelier"){part.Base=null;part.Model.Material=part.Model.BackMaterial=null;continue;}
   if(role=="glass"){
    var glass=new MaterialGroup();glass.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(2,220,235,245))));glass.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(45,215,233,249)),120));
    var reflection=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(1,1)};foreach(var stop in new (double x,byte a)[]{(0,1),(.24,2),(.42,13),(.52,2),(1,0)})reflection.GradientStops.Add(new GradientStop(Color.FromArgb(stop.a,220,234,249),stop.x));glass.Children.Add(new EmissiveMaterial(reflection));glass.Freeze();part.Base=glass;
   }else if(role=="emission")part.Base=SegmentMaterial(false);
   else if(role=="dial")part.Base=StudioMetal.Dial(color);
   else{
    Brush diffuse=new SolidColorBrush(C(color));
    if(part.Reflective)diffuse=StudioMetal.Reflection(color,role);
    var mat=new MaterialGroup();mat.Children.Add(new DiffuseMaterial(diffuse));if(part.Reflective||role=="accent")mat.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(role=="polished"?(byte)180:(byte)75,235,241,246)),role=="polished"?105:50));mat.Freeze();part.Base=mat;
   }
   part.Model.Material=part.Name.StartsWith("Flip_")?null:part.Base;part.Model.BackMaterial=part.Name.StartsWith("Flip_")||role=="glass"?null:part.Base;
  }
  onSegment=SegmentMaterial(true);offSegment=SegmentMaterial(false);foreach(var p in current.Parts)p.ReflectionAngle=double.NaN;reflectionAt=DateTime.MinValue;ApplyLighting();Update(DateTime.Now);
 }
 Material? onSegment,offSegment;
 Material SegmentMaterial(bool on){
  // WPF emissive radiance is linear: keep the radiance below clipping.
  Color c=C(Pref.Finish==2?"#8DDBDF":Pref.Finish==1?"#D3AE66":"#EA9A57");
  // Emissive-only WPF surfaces do not establish an opaque depth layer. The
  // black diffuse pass keeps later optical-cell geometry behind the emitter.
  Material mat;
  if(on){var lit=new MaterialGroup();lit.Children.Add(new DiffuseMaterial(Brushes.Black));lit.Children.Add(new EmissiveMaterial(new SolidColorBrush(c)));mat=lit;}
  else mat=new DiffuseMaterial(new SolidColorBrush(C("#10212A")));
  mat.Freeze();return mat;
 }
 Material DialLettering(){
  var dg=new DrawingGroup();bool dark=Pref.Finish!=0;string ink=dark?"#CBCAB9":"#132128";
  using(var d=dg.Open()){
   d.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,1650,1650));
   d.DrawEllipse(null,new Pen(new SolidColorBrush(C(dark?"#314A4F":"#AAADA1")),.7),new Point(825,825),640,640);
   d.DrawEllipse(null,new Pen(new SolidColorBrush(C(dark?"#314A4F":"#AAADA1")),1),new Point(825,825),778,778);
   for(int i=1;i<=12;i++){double a=i*Math.PI/6;DrawText(d,i.ToString(),76,ink,new Point(825+700*Math.Sin(a),825-700*Math.Cos(a)-45));}
   DrawText(d,"Q U E R A I S",60,ink,new Point(825,470));DrawText(d,"A T E L I E R",27,ink,new Point(825,563));DrawText(d,"PRECISION  /  LOCAL TIME",23,ink,new Point(825,1165));
  }
  dg.Freeze();var brush=new DrawingBrush(dg){Stretch=Stretch.Fill};brush.Freeze();var mat=new DiffuseMaterial(brush);mat.Freeze();return mat;
 }
 public void ApplyLighting(){
  double strength=Math.Clamp(Pref.LightPower,.3,1.6);double az=(Pref.LightAngle+(Pref.StudioLight==1?40:Pref.StudioLight==2?-35:0))*Math.PI/180;
  Color Scale(string h,double f){var c=C(h);return Color.FromRgb((byte)Math.Min(255,c.R*f),(byte)Math.Min(255,c.G*f),(byte)Math.Min(255,c.B*f));}
  ambient.Color=Scale(Pref.StudioLight==2?"#2D333B":"#66717A",strength);keyLight.Color=Scale(Pref.StudioLight==1?"#D4AF85":"#A8BBCF",strength);keyLight.Direction=new Vector3D(Math.Sin(az)*.9-.4,-.8*Math.Cos(az),-1);fillLight.Color=Scale(Pref.StudioLight==1?"#626E7D":"#695F53",strength*.7);fillLight.Direction=new Vector3D(.8,.4,-.6);
 }
 public static double ShortestDelta(double target,double angle)=>Math.IEEERemainder(target-angle,360);
 public void OrbitBy(double dx,double dy){targetYaw=(targetYaw+dx*.37)%360;targetPitch=Math.Clamp(targetPitch+dy*.32,-78,78);}
 public void MoveLight(double dx,double dy){Pref.LightAngle=(Pref.LightAngle+dx*.7)%360;Pref.LightPower=Math.Clamp(Pref.LightPower-dy*.006,.3,1.6);ApplyLighting();}
 public void ResetOrbit(){targetYaw=-13;targetPitch=13;}
 public void SetView(string view){targetYaw=view=="Rear"?180:view=="Front"?0:view=="Exploded"?-40:-13;targetPitch=view=="Front"||view=="Rear"?0:view=="Exploded"?28:13;}
 public void ToggleSweep(){Pref.Smooth=!Pref.Smooth;crownAngle+=45;Pref.Save();Changed?.Invoke();}
 public void ReplayFlip(){if(current.Key!="relay")return;forceFlip=true;flipAt=DateTime.UtcNow;}
 public void Update(DateTime now){
  if(current==null)return;FrameCount++;long ticks=DateTime.UtcNow.Ticks;lastDt=previousTicks==0?.033:Math.Clamp((ticks-previousTicks)/10000000.0,.001,.1);previousTicks=ticks;
  bool reduced=Pref.ReducedMotion||!SystemParameters.ClientAreaAnimation;double spring=reduced?1:1-Math.Exp(-lastDt*12);
  if(Pref.Turntable&&!reduced&&drag==null)targetYaw=(targetYaw+lastDt*11)%360;
  double py=targetYaw,px=targetPitch;if(drag==null&&!Pref.Turntable&&!reduced){py+=pointer.X*2;px+=pointer.Y*1.5;}
  double dyaw=ShortestDelta(py,yaw.Angle);yaw.Angle=Math.IEEERemainder(yaw.Angle+dyaw*spring,360);pitch.Angle+=(px-pitch.Angle)*spring;explosion+=((Pref.Exploded?1:0)-explosion)*spring;
  double sec=now.Second+(Pref.Smooth&&!reduced?now.Millisecond/1000.0:0);
  foreach(var p in current.Parts){
   if(p.Name=="Hand_Hour")p.Rotation!.Angle=-(now.Hour%12+now.Minute/60.0)*30;
   if(p.Name=="Hand_Minute")p.Rotation!.Angle=-(now.Minute+sec/60)*6;
   if(p.Name=="Hand_Second")p.Rotation!.Angle=-sec*6;
   if(p.Name=="Crown")p.Rotation!.Angle=crownAngle;
   // Decorative exhibition mechanism: time-driven train and oscillator. The face
   // remains authoritative system time, not a simulated mechanical time source.
   if(p.Name.StartsWith("Gear_"))p.Rotation!.Angle=reduced?0:-(now.TimeOfDay.TotalSeconds*6*p.Ratio)%360;
   if(p.Name=="Balance")p.Rotation!.Angle=reduced?0:Math.Sin(now.TimeOfDay.TotalSeconds*Math.PI*5)*24;
   double layer=p.Name switch{"Glass"=>1.32,"Hand_Second"=>1.04,"Spindle"=>1.06,"Hand_Minute"=>.87,"Hand_Hour"=>.68,"Indices" or "DialText"=>.40,"Dial" or "DialDetail" or "Date_Display" or "DateSurround"=>.25,"Bezel" or "BezelScrews"=>.56,"ScreenBack"=>.3,"Brand"=>.35,_=>p.Name.StartsWith("Flap_")||p.Name.StartsWith("Flip_")||p.Name.StartsWith("Seg_")?.63:0};
   if(p.Name=="RearCover"||p.Name=="RearSeal")layer=-1.0;
   if(p.Name=="RearGlass")layer=-1.2;
   if(p.Name=="MovementBridge"||p.Name=="Jewels")layer=-.40;
   if(p.Name.StartsWith("Gear_")||p.Name=="Balance")layer=-.22;
   if(p.Name=="Electronics"||p.Name=="Components"||p.Name=="Circuit")layer=-.35;
   p.Lift.OffsetZ=layer*explosion;p.Lift.OffsetX=p.Name=="Crown"?explosion*.55:0;
   if(p.Name is "Glass" or "RearGlass"){p.Model.Material=Pref.Glass?p.Base:null;p.Model.BackMaterial=null;}
   if(p.Name=="Hand_Second"){p.Model.Material=Pref.ShowSeconds?p.Base:null;p.Model.BackMaterial=Pref.ShowSeconds?p.Base:null;}
  }
  double aspect=Math.Max(.1,ActualWidth/Math.Max(1,ActualHeight));double width=Math.Max(current.Width,current.Height*aspect)*1.17/Pref.Zoom;
  ((OrthographicCamera)viewport.Camera).Width=width*(1+explosion*.14);
  UpdateDisplays(now,reduced);
  UpdateReflections();
 }
 void UpdateReflections(){
  // Throttled independently of hands. Static surfaces are left untouched while
  // the camera and light rest; only small moving gear batches then need work.
  if((DateTime.UtcNow-reflectionAt).TotalMilliseconds<100)return;reflectionAt=DateTime.UtcNow;
  var timer=System.Diagnostics.Stopwatch.StartNew();
  var lighting=new Matrix3D();lighting.Rotate(new Quaternion(new Vector3D(0,1,0),Pref.LightAngle*.35+(Pref.StudioLight==1?12:Pref.StudioLight==2?-12:0)));
  foreach(var p in current.Parts){
   if(!p.Reflective)continue;double angle=yaw.Angle*3+pitch.Angle*7+Pref.LightAngle*.35+Pref.StudioLight*12+(p.Rotation?.Angle??0)*11;
   if(!double.IsNaN(p.ReflectionAngle)&&Math.Abs(angle-p.ReflectionAngle)<.15)continue;p.ReflectionAngle=angle;
   var matrix=p.Model.Transform.Value*current.Model.Transform.Value*lighting;var points=new Point[p.Normals.Count];
   for(int i=0;i<points.Length;i++){var n=matrix.Transform(p.Normals[i]);points[i]=new Point(Math.Clamp(.5+n.X*.495,.005,.995),Math.Clamp(.5-n.Y*.495,.005,.995));}
   var uv=new PointCollection(points);uv.Freeze();p.Mesh.TextureCoordinates=uv;
  }
  ReflectionUpdateMilliseconds=timer.Elapsed.TotalMilliseconds;
 }
 void UpdateDisplays(DateTime now,bool reduced){
  string time=now.ToString(Pref.Hour24?"HHmm":"hhmm",CultureInfo.InvariantCulture);string stamp=now.ToString("yyyyMMddHHmmss")+Pref.ShowSeconds+Pref.Hour24+Pref.Finish;
  if(current.Key=="atelier"){
   if(lastDisplay!=now.ToString("yyyyMMdd")){lastDisplay=now.ToString("yyyyMMdd");var p=current.Parts.Find(x=>x.Name=="Date_Display");if(p!=null){var dg=new DrawingGroup();using(var d=dg.Open()){d.DrawRectangle(new SolidColorBrush(C(Pref.Finish==0?"#EDE8DA":"#18303A")),null,new Rect(0,0,300,150));DrawText(d,now.Day.ToString("00"),112,Pref.Finish==0?"#26343C":"#F0E8D6",new Point(150,7));}p.Model.Material=new DiffuseMaterial(new DrawingBrush(dg));}}
  }else if(current.Key=="slate"){
   if(stamp==lastDisplay)return;lastDisplay=stamp;var p=current.Parts.Find(x=>x.Name=="Display_Main")!;var dg=new DrawingGroup();using(var d=dg.Open()){
    d.DrawRectangle(new SolidColorBrush(C("#10171D")),null,new Rect(0,0,1600,580));DrawText(d,now.ToString(Pref.Hour24?"HH:mm":"h:mm",CultureInfo.InvariantCulture),414,"#F0ECDD",new Point(800,12),"Georgia");DrawText(d,now.ToString("dddd, d MMMM",CultureInfo.InvariantCulture),31,"#9AADB9",new Point(800,481));DrawText(d,(Pref.ShowSeconds?now.ToString("ss")+"  /  ":"")+(Pref.Hour24?"24H":now.ToString("tt",CultureInfo.InvariantCulture)),25,"#E69874",new Point(1455,515));
   }p.Model.Material=DrawingMaterial(dg);
  }else if(current.Key=="relay"){
   if(digits.Length==0){digits=oldDigits=time;}
   else if(digits!=time){oldDigits=digits;digits=time;flipAt=DateTime.UtcNow;}
   if(forceFlip){oldDigits=now.AddMinutes(-1).ToString(Pref.Hour24?"HHmm":"hhmm",CultureInfo.InvariantCulture);digits=time;forceFlip=false;}
   double progress=reduced?1:(DateTime.UtcNow-flipAt).TotalMilliseconds/600;
   foreach(var p in current.Parts){
    bool plate=p.Name.StartsWith("Flap_");bool flip=p.Name.StartsWith("Flip_");if(!plate&&!flip)continue;var bits=p.Name.Split('_');int i=int.Parse(bits[1]);bool changed=oldDigits[i]!=digits[i]&&progress<1;
    if(plate){bool upper=bits[2]=="Upper";p.Model.Material=Digit(digits[i],upper,false);if(!upper&&changed)p.Model.Material=Digit(oldDigits[i],false,false);p.Model.BackMaterial=p.Model.Material;}
    else{p.Rotation!.Angle=Math.Clamp(progress,0,1)*180;p.Model.Material=changed?Digit(oldDigits[i],true,false):null;p.Model.BackMaterial=changed?Digit(digits[i],false,true):null;}
   }
  }else if(current.Key=="signal"){
   if(stamp==lastDisplay)return;lastDisplay=stamp;string numbers=time+now.ToString("ss");
   foreach(var p in current.Parts){if(p.Name.StartsWith("Seg_")){var bits=p.Name.Split('_');int digit=int.Parse(bits[1]),segment=int.Parse(bits[2]);bool visible=digit<4||Pref.ShowSeconds;bool on=(Masks[numbers[digit]-'0']&(1<<segment))!=0;p.Model.Material=visible?(on?onSegment:offSegment):null;p.Model.BackMaterial=p.Model.Material;}if(p.Name=="Colon")p.Model.Material=onSegment;}
  }
 }
 static void DrawText(DrawingContext d,string value,double size,string color,Point top,string font="Bahnschrift"){
  var text=new FormattedText(value,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(font),size,new SolidColorBrush(C(color)),1){TextAlignment=TextAlignment.Center};d.DrawText(text,top);
 }
 static Material DrawingMaterial(DrawingGroup drawing){drawing.Freeze();var brush=new DrawingBrush(drawing){Stretch=Stretch.Fill};brush.Freeze();var mat=new EmissiveMaterial(brush);mat.Freeze();return mat;}
 Material Digit(char digit,bool upper,bool reverse){
  string key=digit+"/"+upper+"/"+reverse;if(digitMaterials.TryGetValue(key,out var cached))return cached;
  var dg=new DrawingGroup();using(var d=dg.Open()){
   var bg=new LinearGradientBrush(C("#303B44"),C("#19232C"),90);d.DrawRectangle(bg,null,new Rect(0,0,320,560));DrawText(d,digit.ToString(),510,"#F2EDDF",new Point(160,-11));
  }
  dg.Freeze();var brush=new DrawingBrush(dg){ViewboxUnits=BrushMappingMode.Absolute,Viewbox=new Rect(0,upper?0:280,320,280),Stretch=Stretch.Fill};if(reverse)brush.RelativeTransform=new ScaleTransform(1,-1,.5,.5);brush.Freeze();var mat=new EmissiveMaterial(brush);mat.Freeze();digitMaterials[key]=mat;return mat;
 }
 public int VisibleSegments=>current.Parts.Count(p=>p.Name.StartsWith("Seg_")&&p.Model.Material==onSegment);
 public bool GlassVisible=>current.Parts.Any(p=>p.Name=="Glass"&&p.Model.Material!=null);
 public int ActiveFlipPlates=>current.Parts.Count(p=>p.Name.StartsWith("Flip_")&&p.Model.Material!=null);
}
