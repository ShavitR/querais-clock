using System;
using System.Globalization;
namespace Querais;
public static class TimeText {
 public static string Duration(TimeSpan value,bool alwaysHours=false){value=value<TimeSpan.Zero?TimeSpan.Zero:value;return alwaysHours||value.TotalHours>=1?$"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}":$"{(int)value.TotalMinutes:00}:{value.Seconds:00}";}
 public static bool TryDuration(string input,out TimeSpan result){
  result=TimeSpan.Zero;var parts=input.Split(':');if(parts.Length<2||parts.Length>3)return false;
  int[] n=new int[parts.Length];for(int i=0;i<n.Length;i++)if(!int.TryParse(parts[i],NumberStyles.None,CultureInfo.InvariantCulture,out n[i])||n[i]<0)return false;
  int h=parts.Length==3?n[0]:0,m=parts.Length==3?n[1]:n[0],s=n[^1];if(h>99||m>59||s>59)return false;
  result=new TimeSpan(h,m,s);return result.TotalSeconds>=1;
 }
}
