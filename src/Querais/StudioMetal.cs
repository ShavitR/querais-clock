using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Querais;

/// <summary>A local studio reflection lookup for WPF's fixed-function renderer.
/// Normal coordinates are updated as the object turns; this is an approximation
/// of an environment reflection, not a PBR renderer or a baked object picture.</summary>
internal static class StudioMetal {
 static readonly Dictionary<string,Brush> cache=new();
 public static bool Reflects(string role)=>role is "steel" or "polished" or "brass" or "shell" or "jewel";
 public static Brush Reflection(string color,string role){
  string id=color+role;if(cache.TryGetValue(id,out var result))return result;
  var tint=(Color)ColorConverter.ConvertFromString(color);const int n=384;var bytes=new byte[n*n*4];
  double rough=role=="polished"?.065:role=="steel"?.22:role=="brass"?.18:.36;
  for(int y=0;y<n;y++)for(int x=0;x<n;x++){
   double nx=2.0*x/(n-1)-1,ny=1-2.0*y/(n-1),nz=Math.Sqrt(Math.Max(0,1-nx*nx-ny*ny));
   double rx=2*nx*nz,ry=2*ny*nz,rz=2*nz*nz-1;
   double Lobe(double cx,double cy,double cz,double wx,double wy)=>Math.Exp(-Math.Pow((rx-cx)/wx,4)-Math.Pow((ry-cy)/wy,4))*Math.Clamp((rz+cz)*1.2,0,1);
   double broad=Lobe(-.45,.62,.60,.33+rough,.19+rough),strip=Lobe(.78,.06,.38,.055+rough*.35,.77),edge=Math.Pow(1-nz,3);
   double surface=.24+.17*(ry+1)*.5+.83*broad+.70*strip+.28*edge;
   // Subpixel tooling grain belongs to the metal. It never becomes a dirt layer.
   double grain=role=="steel"?Math.Sin(y*2.3+x*.013)*.009:0;
   if(role=="shell")surface=.46+surface*.34;
   byte Channel(byte c)=> (byte)Math.Clamp(c*(surface+grain),0,255);
   int at=(y*n+x)*4;bytes[at]=Channel(tint.B);bytes[at+1]=Channel(tint.G);bytes[at+2]=Channel(tint.R);bytes[at+3]=255;
  }
  var bitmap=BitmapSource.Create(n,n,96,96,PixelFormats.Bgra32,null,bytes,n*4);bitmap.Freeze();
  var brush=new ImageBrush(bitmap){Stretch=Stretch.Fill,TileMode=TileMode.None};brush.Freeze();cache[id]=brush;return brush;
 }
 public static Material Dial(string color){
  Color c=(Color)ColorConverter.ConvertFromString(color);Color Shade(double f)=>Color.FromRgb((byte)Math.Min(255,c.R*f),(byte)Math.Min(255,c.G*f),(byte)Math.Min(255,c.B*f));
  var dg=new DrawingGroup();using(var d=dg.Open()){
   var brush=new RadialGradientBrush(Shade(1.04),Shade(.83)){GradientOrigin=new Point(.35,.28)};d.DrawRectangle(brush,null,new Rect(0,0,1024,1024));
   var pen=new Pen(new SolidColorBrush(Color.FromArgb(13,18,30,37)),.65);
   for(int r=80;r<500;r+=3)d.DrawEllipse(null,pen,new Point(512,512),r,r);
  }
  dg.Freeze();var tex=new DrawingBrush(dg);tex.Freeze();var mat=new DiffuseMaterial(tex);mat.Freeze();return mat;
 }
}
