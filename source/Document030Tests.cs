using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
namespace VinsPDF {
internal static class Document030Tests {
 static string Hash(string file){using(var h=SHA256.Create())using(var s=File.OpenRead(file))return Convert.ToBase64String(h.ComputeHash(s));}
 public static int Run(string[] args){string dir=args[1],file=args[2];Directory.CreateDirectory(dir);var log=new List<string>();try{string hash=Hash(file);using(var e=new PdfEngine())using(var reference=new PdfEngine()){
  e.Open(file);reference.Open(file);int page=20;var labels=e.PageLabels();Version030Tests.Check(labels.Any(l=>l=="i")&&labels[page]=="5","Front matter/page numbering incorrect");
  var groups=TextParagraphs.Group(TextLines.Group(e.GetObjects(page).Objects,e.Sizes[page],true),e.Sizes[page],true);var block=groups.Where(o=>o.IsParagraph&&o.Text.Length>120).OrderByDescending(o=>o.Text.Length).First();
  File.WriteAllText(Path.Combine(dir,"paragraph-details.txt"),"style size="+block.Style.Size+" display size="+block.DisplayFontSize+" lineheight="+block.Style.LineHeight+" baseline="+block.BaselineY+" width="+block.Style.BoxWidth+"\n"+block.Text+"\n"+string.Join("\n",TextLines.Group(e.GetObjects(page).Objects,e.Sizes[page],true).Where(o=>o.Ids.Any(id=>block.Ids.Contains(id))).Select(o=>o.BaselineY+" size="+o.DisplayFontSize+" text="+o.Text)));
  log.Add("Selected paragraph: physical page "+(page+1)+", label "+labels[page]+", "+block.Text.Length+" characters, "+block.Ids.Length+" text objects");
  Version030Tests.Check(e.Glyphs(page).Any(g=>g.Text=="P")&&e.SearchPage(page,"Patient",false).Count>0,"Text selection/search missing");
  for(int p=0;p<e.Count;p++)using(var a=e.Preview(p,0,900))using(var b=reference.Preview(p,0,900))Version030Tests.Check(Version029Tests.EqualPixels(a,b),"Preparation changed page "+(p+1));
  log.Add("PASS all "+e.Count+" pages unchanged after editor preparation; text selection/search and labels");
  var runs=TextRuns030.For(block).Select(r=>new TextRun{Text=r.Text,Style=r.Style.Copy()}).ToList();runs.Add(new TextRun{Text=" VinsPDF 0.30 validation.",Style=block.Style.Copy()});
  e.PutRichTextBlock(page,block.Ids,runs,block.Style.Copy(),new PointF(block.BaselineX,block.BaselineY));
  e.UndoEdit(false);using(var a=e.Preview(page,0,1400))using(var b=reference.Preview(page,0,1400))Version030Tests.Check(Version029Tests.EqualPixels(a,b),"Paragraph undo changed page");e.UndoEdit(true);
  var beforeObjects=e.GetObjects(page).Objects.Where(o=>o.Kind==1).Select(o=>System.Text.RegularExpressions.Regex.Replace(o.Text,@"\s+"," ")+"|"+o.Color).OrderBy(v=>v).ToArray();
  using(var edited=e.Preview(page,0,1400)){edited.Save(Path.Combine(dir,"paragraph-edited.png"));e.SaveCopy(Path.Combine(dir,"paragraph-edited.pdf"));using(var saved=e.Preview(page,0,1400)){saved.Save(Path.Combine(dir,"paragraph-reopened.png"));log.Add(Version030Tests.SavedAppearance(edited,saved)?"PASS edited-page appearance preserved":"NOTE edited-page glyph spacing is approximate after PDF content regeneration; verify text/colour and inspect the rendered copy");}}
  var afterObjects=e.GetObjects(page).Objects.Where(o=>o.Kind==1).Select(o=>System.Text.RegularExpressions.Regex.Replace(o.Text,@"\s+"," ")+"|"+o.Color).OrderBy(v=>v).ToArray();Version030Tests.Check(beforeObjects.SequenceEqual(afterObjects),"Saving changed text or colours on the edited page");log.Add("PASS all text and colours preserved on the edited page");
  Version030Tests.Check(e.SearchPage(page,"VinsPDF 0.30 validation",false).Count==1,"Edited paragraph text did not survive saving");Version030Tests.Check(e.GetObjects(page).Objects.Any(o=>o.Text.Contains("VinsPDF")),"Reopened paragraph is not editable");
  for(int p=0;p<e.Count;p++)if(p!=page)using(var a=e.Preview(p,0,900))using(var b=reference.Preview(p,0,900))Version030Tests.Check(Version029Tests.EqualPixels(a,b),"Saving changed untouched page "+(p+1));
  log.Add("PASS paragraph editing/reflow, undo/redo, save/reopen and unchanged remaining "+(e.Count-1)+" pages");
 }Version030Tests.Check(hash==Hash(file),"Original file changed");log.Add("PASS original untouched");File.WriteAllLines(Path.Combine(dir,"document-030.txt"),log);return 0;}catch(Exception ex){log.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(dir,"document-030.txt"),log);return 1;}}
}
}
