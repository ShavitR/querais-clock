using System;

using System.Globalization;

using System.Windows;

using System.Windows.Media;

namespace Querais;

public sealed class ClockSurface : FrameworkElement {

 public Preferences Pref {get;set;}=new();

 public DateTime Time {get;set;}=DateTime.Now;

 public string Mode {get;set;}="Clock";

 public TimeSpan Duration {get;set;}

 public string BottomLabel {get;set;}="";

 static readonly Typeface Numerals=new("Bahnschrift");

 Brush B(string hex)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

 Brush Fore=>B(Pref.Dark?"#F2F0E9":"#24282D");

 Brush Muted=>B(Pref.Dark?"#9BA2AA":"#646A70");

 Brush Accent=>B(new[]{"#D8795C","#B39968","#92ADB0"}[Math.Clamp(Pref.Finish,0,2)]);

 void Text(DrawingContext d,string str,double size,Brush brush,Point p,Typeface? face=null,TextAlignment align=TextAlignment.Center){var f=new FormattedText(str,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,face??Numerals,size,brush,VisualTreeHelper.GetDpi(this).PixelsPerDip);f.TextAlignment=align;d.DrawText(f,p);}

 protected override void OnRender(DrawingContext d){

  double w=ActualWidth,h=ActualHeight;if(w<2||h<2)return;

  if(Mode!="Clock"){DrawDuration(d,w,h);return;}

  if(Pref.Type==0)DrawAnalog(d,w,h);else DrawDigital(d,w,h);

 }

 void DrawAnalog(DrawingContext d,double w,double h){

  double size=Math.Min(w,h)*.92*Pref.Zoom;double r=size/2;var center=new Point(w/2,h/2);

  d.PushTransform(new TranslateTransform(center.X,center.Y));d.PushTransform(new ScaleTransform(r/200,r/200));

  bool ivory=Pref.Finish==0;bool detailed=Pref.AnalogFace==1;

  Brush dial=ivory?B("#ECEAE2"):Pref.Finish==1?B("#282927"):B("#182C32");Brush ink=ivory?B("#30373C"):B("#E8E7DF");

  for(int j=13;j>0;j--)d.DrawEllipse(B($"#{(int)(2+j*.7):X2}000000"),null,new Point(0,8+j*.6),196+j*.55,196+j*.55);

  var metal=new LinearGradientBrush();metal.StartPoint=new Point(0,0);metal.EndPoint=new Point(1,1);metal.GradientStops.Add(new GradientStop(Color.FromRgb(210,214,214),0));metal.GradientStops.Add(new GradientStop(Color.FromRgb(79,86,91),.28));metal.GradientStops.Add(new GradientStop(Color.FromRgb(193,198,199),.65));metal.GradientStops.Add(new GradientStop(Color.FromRgb(72,79,84),1));

  d.DrawEllipse(metal,null,new Point(),199,199);d.DrawEllipse(B("#161C20"),null,new Point(),193.8,193.8);d.DrawEllipse(dial,new Pen(B("#E0E0D8"),.65),new Point(),191.5,191.5);

  for(int i=0;i<60;i++){bool major=i%5==0;double a=i*Math.PI/30;double inner=major?167:detailed?177:181;d.DrawLine(new Pen(ink,major?2.4:.65),new Point(Math.Sin(a)*inner,-Math.Cos(a)*inner),new Point(Math.Sin(a)*183,-Math.Cos(a)*183));}

  if(detailed){d.DrawEllipse(null,new Pen(ivory?B("#BBC0BA"):B("#476068"),.6),new Point(),162,162);for(int i=1;i<=12;i++){double a=i*Math.PI/6;Text(d,i.ToString(),17,ink,new Point(Math.Sin(a)*145,-Math.Cos(a)*145-11));}}

  else foreach(int i in new[]{12,3,6,9}){double a=i*Math.PI/6;Text(d,i.ToString(),22,ink,new Point(Math.Sin(a)*148,-Math.Cos(a)*148-13));}

  Text(d,"Q U E R A I S",10,ink,new Point(0,-69));Text(d,detailed?"P R E C I S I O N":"S T U D I O",6,ink,new Point(0,-50));

  Text(d,Time.ToString("ddd  dd",CultureInfo.InvariantCulture).ToUpper(),8,ink,new Point(0,83));

  double sec=Time.Second+((Pref.Smooth&&!Pref.ReducedMotion&&SystemParameters.ClientAreaAnimation)?Time.Millisecond/1000.0:0);

  Hand(d,(Time.Hour%12+Time.Minute/60.0)*30,98,5.5,ink,17);Hand(d,(Time.Minute+sec/60)*6,145,3.5,ink,20);

  if(Pref.ShowSeconds)Hand(d,sec*6,163,1.15,Accent,35);

  d.DrawEllipse(ink,null,new Point(),6,6);d.DrawEllipse(Accent,null,new Point(),2.6,2.6);

  var glare=new LinearGradientBrush(Color.FromArgb(35,255,255,255),Color.FromArgb(0,255,255,255),new Point(0,0),new Point(.6,1));d.DrawEllipse(glare,null,new Point(),190,190);

  d.Pop();d.Pop();

 }

 void Hand(DrawingContext d,double angle,double len,double width,Brush brush,double tail){d.PushTransform(new RotateTransform(angle));var geo=new StreamGeometry();using(var g=geo.Open()){g.BeginFigure(new Point(-width*.5,tail),true,true);g.LineTo(new Point(width*.5,tail),true,false);g.LineTo(new Point(width*.32,-len),true,false);g.LineTo(new Point(0,-len-3),true,false);g.LineTo(new Point(-width*.32,-len),true,false);}d.DrawGeometry(brush,null,geo);d.Pop();}

 void DrawDigital(DrawingContext d,double w,double h){

  string hour=Time.ToString(Pref.Hour24?"HH":"hh",CultureInfo.InvariantCulture);if(!Pref.Hour24)hour=hour.TrimStart('0');string minute=Time.ToString("mm");

  if(Pref.DigitalFace==1){DrawFlap(d,w,h,hour.PadLeft(2,'0')+minute);return;}

  if(Pref.DigitalFace==2){DrawSegments(d,w,h,hour.PadLeft(2,'0')+minute);return;}

  double fs=Math.Min(w/4.0,h*.46)*Pref.Zoom;

  Text(d,hour+":"+minute,fs,Fore,new Point(w/2,h/2-fs*.66),new Typeface(new FontFamily("Georgia"),FontStyles.Normal,FontWeights.Normal,FontStretches.Normal));

  Text(d,Pref.ShowSeconds?Time.ToString("ss")+"  /  "+(Pref.Hour24?"24H":Time.ToString("tt",CultureInfo.InvariantCulture)):Time.ToString("tt",CultureInfo.InvariantCulture),14,Accent,new Point(w/2,h/2+fs*.52));

 }

 string lastDigits="",previousDigits=""; DateTime changeAt=DateTime.MinValue;

 void DrawFlap(DrawingContext d,double w,double h,string digits){

  if(lastDigits.Length==0){lastDigits=previousDigits=digits;}else if(digits!=lastDigits){previousDigits=lastDigits;lastDigits=digits;changeAt=DateTime.UtcNow;}

  double unit=Math.Min(w/4.9,h*.40)*Pref.Zoom;double total=unit*4.35;double left=(w-total)/2,top=h/2-unit*.75;

  for(int i=0;i<4;i++){double x=left+i*unit*1.08+(i>=2?unit*.09:0);Rect rect=new(x,top,unit,unit*1.40);

   d.DrawRoundedRectangle(B(Pref.Dark?"#101215":"#D5D2CA"),null,new Rect(x+1,top+5,unit,unit*1.4),7,7);

   var bg=new LinearGradientBrush((Color)ColorConverter.ConvertFromString(Pref.Dark?"#393E43":"#F8F6F0"),(Color)ColorConverter.ConvertFromString(Pref.Dark?"#24282D":"#E5E1D8"),90);d.DrawRoundedRectangle(bg,new Pen(B(Pref.Dark?"#454B50":"#B8B6AF"),.7),rect,7,7);

   double progress=(DateTime.UtcNow-changeAt).TotalMilliseconds/280;

   void Glyph(string str){Text(d,str,unit*1.19,Fore,new Point(x+unit/2,top-unit*.025));}

   var upper=new Rect(x,top,unit,unit*.70);var lower=new Rect(x,top+unit*.70,unit,unit*.70);

   bool flipping=progress<1&&previousDigits.Length==4&&previousDigits[i]!=digits[i]&&!Pref.ReducedMotion&&SystemParameters.ClientAreaAnimation;

   if(flipping){

    d.PushClip(new RectangleGeometry(upper));Glyph(digits[i].ToString());d.Pop();

    d.PushClip(new RectangleGeometry(lower));Glyph(previousDigits[i].ToString());d.Pop();

    d.PushClip(new RectangleGeometry(progress<.5?upper:lower));

    d.PushTransform(new ScaleTransform(1,Math.Max(.005,Math.Abs(Math.Cos(progress*Math.PI))),x+unit/2,top+unit*.70));

    d.DrawRectangle(bg,null,rect);Glyph(progress<.5?previousDigits[i].ToString():digits[i].ToString());d.Pop();d.Pop();

   }else Glyph(digits[i].ToString());

   d.DrawLine(new Pen(B(Pref.Dark?"#080A0C":"#ACA9A2"),2),new Point(x,top+unit*.70),new Point(x+unit,top+unit*.70));

   foreach(double xx in new[]{x+3,x+unit-7})d.DrawRoundedRectangle(B(Pref.Dark?"#7C8287":"#9C9D98"),null,new Rect(xx,top+unit*.70-5,4,10),1,1);

  }

  Text(d,"H O U R S                         M I N U T E S"+(Pref.ShowSeconds?"    /    "+Time.ToString("ss"):""),10,Muted,new Point(w/2,top+unit*1.4+24));

 }

 static readonly int[] segments={0x3f,0x06,0x5b,0x4f,0x66,0x6d,0x7d,0x07,0x7f,0x6f};

 void DrawSegments(DrawingContext d,double w,double h,string digits){

  double s=Math.Min(w/5.9,h/2.95)*Math.Min(Pref.Zoom,1.12);double left=(w-s*4.7)/2,top=h/2-s;Brush on=Accent;Brush off=B(Pref.Dark?"#303437":"#D2CFC5");

  for(int i=0;i<4;i++){double x=left+i*s*1.1+(i>=2?s*.3:0);int bits=segments[digits[i]-'0'];

   for(int j=0;j<7;j++){bool horizontal=j==0||j==3||j==6;double px=x+(j==1||j==2?.78:0)*s;double py=top+(j==0?0:j==1?0:j==2?.98:j==3?1.96:j==4?.98:j==5?0:.98)*s;

    var geo=new StreamGeometry();using(var g=geo.Open()){if(horizontal){g.BeginFigure(new Point(px+s*.1,py),true,true);g.LineTo(new Point(px+s*.70,py),true,false);g.LineTo(new Point(px+s*.78,py+s*.07),true,false);g.LineTo(new Point(px+s*.70,py+s*.14),true,false);g.LineTo(new Point(px+s*.1,py+s*.14),true,false);g.LineTo(new Point(px+s*.02,py+s*.07),true,false);}else{g.BeginFigure(new Point(px,py+s*.16),true,true);g.LineTo(new Point(px+s*.07,py+s*.08),true,false);g.LineTo(new Point(px+s*.14,py+s*.16),true,false);g.LineTo(new Point(px+s*.14,py+s*.87),true,false);g.LineTo(new Point(px+s*.07,py+s*.95),true,false);g.LineTo(new Point(px,py+s*.87),true,false);}}

    d.DrawGeometry((bits&(1<<j))!=0?on:off,null,geo);

   }

  }

  double cx=left+s*2.25;foreach(double y in new[]{top+s*.7,top+s*1.4})d.DrawEllipse(on,null,new Point(cx,y),s*.04,s*.04);

  Text(d,"L U M I N O U S   /   "+(Pref.Hour24?"24 HOUR":Time.ToString("tt",CultureInfo.InvariantCulture))+(Pref.ShowSeconds?"   /   "+Time.ToString("ss"):""),11,Muted,new Point(w/2,top+s*2.4));

 }

 void DrawDuration(DrawingContext d,double w,double h){

  string value=TimeText.Duration(Duration);double fs=Math.Min(w/(value.Length>5?6.5:5.0),h*.42);
  Text(d,value,fs,Fore,new Point(w/2,h/2-fs*.77));

  string sub=Mode=="Stopwatch"?"."+Duration.Milliseconds.ToString("000"):BottomLabel;

  Text(d,sub,Mode=="Stopwatch"?30:13,Accent,new Point(w/2,h/2+fs*.45));

 }

}
