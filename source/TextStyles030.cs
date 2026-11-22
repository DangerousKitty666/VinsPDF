using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
namespace VinsPDF {
internal sealed class TextRun { public string Text;public TextStyle Style; }
internal sealed partial class PdfEngine {
    TextStyle ImportedStyle(IntPtr obj,float size,uint colour){IntPtr font=Native.FPDFTextObj_GetFont(obj);string family=FontName(font,false),name=FontName(font,true);int angle;Native.FPDFFont_GetItalicAngle(font,out angle);string clean=Regex.Replace(family.Length>0?family:name,@"^[A-Z]{6}\+","");bool bold=Native.FPDFFont_GetWeight(font)>=600||Regex.IsMatch(name,"Bold|Heavy|Black|Demi",RegexOptions.IgnoreCase),italic=angle!=0||(Native.FPDFFont_GetFlags(font)&64)!=0||Regex.IsMatch(name,"Italic|Oblique",RegexOptions.IgnoreCase);clean=Regex.Replace(clean,@"[-, ]?(BoldItalic|Bold|Italic|Regular|Oblique|MT|PSMT)$","",RegexOptions.IgnoreCase);if(clean.StartsWith("Helvetica",StringComparison.OrdinalIgnoreCase))clean="Arial";if(clean=="Times"||clean.StartsWith("TimesNewRoman"))clean="Times New Roman";if(clean=="Courier")clean="Consolas";string match=AvailableFamilies.FirstOrDefault(f=>string.Equals(f.Replace(" ",""),clean.Replace(" ",""),StringComparison.OrdinalIgnoreCase));return new TextStyle{Family=match??"Arial",Size=Math.Max(1,size),Ink=Color.FromArgb((int)colour),Bold=bold,Italic=italic};}
    static string FontName(IntPtr font,bool baseName){UIntPtr n=baseName?Native.FPDFFont_GetBaseFontName(font,IntPtr.Zero,UIntPtr.Zero):Native.FPDFFont_GetFamilyName(font,IntPtr.Zero,UIntPtr.Zero);if(n.ToUInt64()<2||n.ToUInt64()>4096)return "";IntPtr data=Marshal.AllocHGlobal((int)n.ToUInt64());try{if(baseName)Native.FPDFFont_GetBaseFontName(font,data,n);else Native.FPDFFont_GetFamilyName(font,data,n);byte[] bytes=new byte[(int)n.ToUInt64()-1];Marshal.Copy(data,bytes,0,bytes.Length);return Encoding.UTF8.GetString(bytes);}finally{Marshal.FreeHGlobal(data);}}
}
internal static class TextRuns030 {
    public static List<TextRun> For(EditObject obj){return obj.Runs??new List<TextRun>{new TextRun{Text=obj.Text,Style=obj.Style??new TextStyle{Size=obj.DisplayFontSize,Ink=Color.FromArgb((int)obj.Color)}}};}
    // Map source runs onto the grouped text, retaining inserted spaces and paragraph breaks.
    public static List<TextRun> Join(IEnumerable<EditObject> items,string text){var runs=new List<TextRun>();int at=0;TextStyle previous=null;foreach(var item in items)foreach(var run in For(item)){string value=run.Text??"";if(value.Length==0)continue;int start=text.IndexOf(value,at,StringComparison.Ordinal);if(start<0){value=value.Trim();if(value.Length==0)continue;start=text.IndexOf(value,at,StringComparison.Ordinal);}if(start<0)continue;if(start>at)runs.Add(new TextRun{Text=text.Substring(at,start-at),Style=previous??run.Style});runs.Add(new TextRun{Text=value,Style=run.Style});at=start+value.Length;previous=run.Style;}if(at<text.Length)runs.Add(new TextRun{Text=text.Substring(at),Style=previous??new TextStyle()});return runs;}
}
internal static partial class Native {
    [DllImport("pdfium.dll")]internal static extern IntPtr FPDFTextObj_GetFont(IntPtr obj);
    [DllImport("pdfium.dll")]internal static extern UIntPtr FPDFFont_GetFamilyName(IntPtr font,IntPtr buffer,UIntPtr size);
    [DllImport("pdfium.dll")]internal static extern UIntPtr FPDFFont_GetBaseFontName(IntPtr font,IntPtr buffer,UIntPtr size);
    [DllImport("pdfium.dll")]internal static extern int FPDFFont_GetFlags(IntPtr font);
    [DllImport("pdfium.dll")]internal static extern int FPDFFont_GetWeight(IntPtr font);
    [DllImport("pdfium.dll")]internal static extern int FPDFFont_GetItalicAngle(IntPtr font,out int angle);
}
}
