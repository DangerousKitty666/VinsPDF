using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
namespace VinsPDF {
internal static class Version030Tests {
    internal static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static bool EqualAppearance(Bitmap a,Bitmap b){if(a.Size!=b.Size)return false;int different=0;for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++){var ca=a.GetPixel(x,y);var cb=b.GetPixel(x,y);if(ca!=cb){if(++different>20||Math.Abs(ca.R-cb.R)>64||Math.Abs(ca.G-cb.G)>64||Math.Abs(ca.B-cb.B)>64)return false;}}return true;}
    // Serialising PDF coordinates can change a few antialiased edge pixels.
    internal static bool SavedAppearance(Bitmap a,Bitmap b){if(a.Size!=b.Size)return false;int limit=Math.Max(20,(int)(a.Width*a.Height*.0002)),different=0;for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++)if(a.GetPixel(x,y)!=b.GetPixel(x,y)&&++different>limit)return false;return true;}
    static string Hash(string file){using(var hash=SHA256.Create())using(var stream=File.OpenRead(file))return Convert.ToBase64String(hash.ComputeHash(stream));}
    public static int Run(string[] args){string dir=args[1];Directory.CreateDirectory(dir);var log=new List<string>();try{string before=Hash(args[2]);using(var e=new PdfEngine())using(var reference=new PdfEngine()){
        e.Open(args[2]);reference.Open(args[2]);int black=0;
        string label=reference.SearchPage(0,"Rėmo apžvalga",false).Count>0?"Rėmo apžvalga":"Nested text 0.3";var highlight=reference.SearchPage(0,label,false).Last();e.HighlightText(0,highlight.Start,highlight.Count,Color.Yellow);e.GetObjects(0);e.SaveCopy(Path.Combine(dir,"nested-highlight.pdf"));e.Open(args[2]);var repeat=e.SearchPage(0,label,false).Last();e.HighlightText(0,repeat.Start,repeat.Count,Color.Yellow);e.GetObjects(0);e.UndoEdit(false);using(var a=e.Preview(0,0,1200))using(var b=reference.Preview(0,0,1200))Check(EqualAppearance(a,b),"Nested highlight undo changed page");e.Open(args[2]);log.Add("PASS highlight nested text before editing, save and undo after entering editor");
        for(int p=0;p<e.Count;p++){using(var original=reference.Preview(p,0,1200)){var state=e.GetObjects(p);black+=state.Objects.Count(o=>o.Kind==1&&(o.Color&0xffffff)==0);using(var prepared=e.Preview(p,0,1200)){if(!EqualAppearance(original,prepared)){original.Save(Path.Combine(dir,"original-"+p+".png"));prepared.Save(Path.Combine(dir,"prepared-"+p+".png"));throw new Exception("Preparing nested text changed page "+(p+1));}}}log.Add("PASS unchanged appearance of prepared page "+(p+1));}
        log.Add("Black text objects: "+black);Check(black>0,"No black text recognised");
        var text=e.GetObjects(0).Objects.First(o=>o.Kind==1&&(o.Text.Contains("Rėmo apžvalga")||o.Text=="Nested text 0.3"));
        Check(e.SearchPage(0,text.Text,false).Any(h=>h.Boxes.Any(b=>b.IntersectsWith(text.Bounds))),"Selection does not cover displayed nested text");
        string old=text.Text;int oldCount=e.GetObjects(0).Objects.Count(o=>o.Text==old);int id=e.PutTextBlock(0,text.Ids,"Rėmo apžvalga · VinsPDF 0.3 test",new TextStyle{Family="Arial",Size=text.DisplayFontSize,Ink=Color.Black},new PointF(text.BaselineX,text.BaselineY));
        Check(e.GetObjects(0).Objects.Count(o=>o.Text==old)==oldCount-1,"Old nested text remains active");e.UndoEdit(false);Check(e.GetObjects(0).Objects.Any(o=>o.Text==old),"Nested edit undo failed");e.UndoEdit(true);
        using(var image=e.Preview(0,0,1600))image.Save(Path.Combine(dir,"edited.png"));
        string saved=Path.Combine(dir,"a013-edited.pdf");e.SaveCopy(saved);Check(e.SearchPage(0,"VinsPDF 0.3 test",false).Count==1,"Edited text did not survive save/reopen");Check(e.GetObjects(0).Objects.Any(o=>o.Text.Contains("VinsPDF 0.3 test")),"Saved text cannot be selected");
        using(var image=e.Preview(0,0,1600))image.Save(Path.Combine(dir,"reopened.png"));
        using(var beforeSave=new Bitmap(Path.Combine(dir,"edited.png")))using(var reopened=e.Preview(0,0,1600))Check(SavedAppearance(beforeSave,reopened),"Saving changed the appearance of the edited page");
        for(int p=1;p<e.Count;p++)using(var a=e.Preview(p,0,1200))using(var b=reference.Preview(p,0,1200))Check(EqualAppearance(a,b),"Saving changed untouched page "+(p+1));
        log.Add("PASS select, edit, undo/redo, save/reopen and unchanged remaining pages");
        var mixed=new List<TextRun>{new TextRun{Text="Regular ",Style=new TextStyle{Size=14,Family="Arial",Ink=Color.Black}},new TextRun{Text="bold blue",Style=new TextStyle{Size=14,Family="Arial",Bold=true,Ink=Color.Blue}},new TextRun{Text=" italic",Style=new TextStyle{Size=14,Family="Arial",Italic=true,Ink=Color.Black}}};
        e.PutRichTextBlock(0,new int[0],mixed,new TextStyle{Size=14,Wrap=true,BoxWidth=300},new PointF(.1f,.18f));string rich=Path.Combine(dir,"mixed-format.pdf");e.SaveCopy(rich);var richObjects=e.GetObjects(0).Objects;Check(richObjects.Any(o=>o.Text=="bold"&&o.Style.Bold&&(o.Color&0xffffff)==255),"Bold colour run not retained");Check(richObjects.Any(o=>o.Text=="italic"&&o.Style.Italic),"Italic run not retained");log.Add("PASS mixed font weight, italics and colour save/reopen");
        e.Open(args[2]);string compressed=Path.Combine(dir,"strong-reduced.pdf");e.ExportStrongReduced(compressed,Enumerable.Range(0,e.Count).ToArray(),150,70);log.Add(e.ExportReport+"; original="+new FileInfo(args[2]).Length+"; output="+new FileInfo(compressed).Length);using(var reduced=new PdfEngine()){reduced.Open(compressed);Check(reduced.Count==reference.Count,"Compression page count changed");for(int p=0;p<reduced.Count;p++){var a=string.Concat(reduced.Glyphs(p).Select(g=>g.Text));var b=string.Concat(reference.Glyphs(p).Select(g=>g.Text));Check(System.Text.RegularExpressions.Regex.Replace(a,@"\s+"," ")==System.Text.RegularExpressions.Regex.Replace(b,@"\s+"," "),"Compression changed text on page "+(p+1));}Check(reduced.GetObjects(0).Objects.Any(o=>o.Kind==1),"Compressed text is not editable");}log.Add("PASS strong compression preserves text on every page");
    }Check(Hash(args[2])==before,"Original file was changed");log.Add("PASS source unchanged");File.WriteAllLines(Path.Combine(dir,"engine-030.txt"),log);return 0;}catch(Exception ex){log.Add(ex.ToString());File.WriteAllLines(Path.Combine(dir,"engine-030.txt"),log);return 1;}}
}
}

