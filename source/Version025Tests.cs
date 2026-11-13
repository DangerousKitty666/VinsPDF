using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;
namespace VinsPDF {
internal static class Version025Tests {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static int Run(string[] args){var log=new List<string>();string dir=args[1];try{
  using(var e=new PdfEngine()){
   e.Open(args[2]);var raw=e.GetObjects(0).Objects;var groups=TextLines.Group(raw,e.Sizes[0],true);log.Add("Grouped: "+string.Join(" | ",groups.Where(o=>o.Kind==1).Select(o=>o.Text).ToArray()));
   var line=groups.First(o=>o.Text=="Hello VinsPDF");Check(line.Ids.Length>=10,"Character objects did not combine into one line");Check(groups.Any(o=>o.Text=="Other column"),"Separate column merged incorrectly");
   var box=line.Bounds;var target=box;target.Offset(.03f,.03f);e.TransformObjects(0,line.Ids,box,target);var movedGroups=TextLines.Group(e.GetObjects(0).Objects,e.Sizes[0],true);log.Add("After move: "+string.Join(" | ",movedGroups.Select(o=>o.Text).ToArray()));Check(Math.Abs(movedGroups.First(o=>o.Text=="Hello VinsPDF").Bounds.Left-target.Left)<.0001,"Grouped move failed");e.UndoEdit(false);var restored=e.GetObjects(0).Objects.First(o=>o.Id==line.Id);var original=raw.First(o=>o.Id==line.Id);Check(Math.Abs(restored.Bounds.Left-original.Bounds.Left)<.0001,"Grouped move undo failed");
   bool failed=false;try{e.TransformObjects(0,new[]{line.Id,999999},box,target);}catch{failed=true;}Check(failed,"Invalid batch unexpectedly succeeded");Check(Math.Abs(e.GetObjects(0).Objects.First(o=>o.Id==line.Id).Bounds.Left-original.Bounds.Left)<.0001,"Batch rollback failed");
   var style=new TextStyle{Family="Consolas",Size=18,Bold=true,LineHeight=1.6f,ParagraphGap=12,CharacterSpacing=1.2f};
   e.PutTextBlock(0,line.Ids,"Updated line Žąė\nSecond edited line\n\nNew paragraph",style,new PointF(.1f,.2f));
   var newGroups=TextLines.Group(e.GetObjects(0).Objects,e.Sizes[0],true);
   Check(newGroups.Any(o=>o.Text=="Updated line Žąė"),"Character spacing lost line text");Check(newGroups.Any(o=>o.Text=="New paragraph"),"Multiline text failed");
   var lines=newGroups.Where(o=>new[]{"Updated line Žąė","Second edited line","New paragraph"}.Contains(o.Text)).OrderBy(o=>o.Bounds.Top).ToArray();Check(lines.Length==3,"Expected three lines");Check(lines[2].Bounds.Top-lines[1].Bounds.Top>lines[1].Bounds.Top-lines[0].Bounds.Top,"Paragraph spacing not applied");
   var spacedTarget=lines[0].Bounds;spacedTarget.Offset(.01f,.01f);e.TransformObjects(0,lines[0].Ids,lines[0].Bounds,spacedTarget);Check(TextLines.Group(e.GetObjects(0).Objects,e.Sizes[0],true).Any(o=>o.Text=="Updated line Žąė"),"Spaced text move lost text");e.UndoEdit(false);
   e.DeleteObjects(0,lines.SelectMany(o=>o.Ids).ToArray());Check(!e.GetObjects(0).Objects.Any(o=>o.Text.Contains("Updated")),"Multi delete failed");e.UndoEdit(false);
   var headerStyle=new TextStyle{Family="Arial",Size=10};e.AddHeaderFooter(0,true,"VinsPDF 0.25","Page {page} of {pages}",headerStyle,10,1);
   Check(e.GetObjects(0).Objects.Any(o=>o.Text=="Page 1 of 2")&&e.GetObjects(1).Objects.Any(o=>o.Text=="Page 2 of 2"),"Header/footer page tokens failed");e.UndoEdit(false);Check(!e.GetObjects(0).Objects.Any(o=>o.Text=="Page 1 of 2")&&!e.GetObjects(1).Objects.Any(o=>o.Text=="Page 2 of 2"),"All-pages header undo failed");e.UndoEdit(true);
   using(var image=e.Preview(0,0,1200))image.Save(Path.Combine(dir,"advanced-before-save.png"));e.SaveCopy(Path.Combine(dir,"advanced-edited.pdf"));
   newGroups=TextLines.Group(e.GetObjects(0).Objects,e.Sizes[0],true);Check(newGroups.Any(o=>o.Text=="Updated line Žąė"),"Line membership/spacing did not survive save");Check(!newGroups.Any(o=>o.Text=="Hello VinsPDF"),"Replaced characters survived");Check(e.GetObjects(1).Objects.Any(o=>o.Text=="Page 2 of 2"),"Saved footer missing");
   using(var image=e.Preview(0,0,1200))image.Save(Path.Combine(dir,"advanced-after-save.png"));log.Add("PASS character-to-line grouping; separate columns; batch movement/undo/rollback; fonts; character/line/paragraph spacing; multi-delete undo; all-pages header/footer tokens and undo/redo; saved group metadata");
  }
  for(int i=3;i<args.Length;i++)using(var e=new PdfEngine()){
   e.Open(args[i]);int count=e.Count;var clock=Stopwatch.StartNew();var raw=e.GetObjects(0).Objects;var groups=TextLines.Group(raw,e.Sizes[0],true);log.Add(Path.GetFileName(args[i])+": "+raw.Count+" raw objects -> "+groups.Count+" selectable objects in "+clock.ElapsedMilliseconds+" ms");
   var line=groups.First(o=>o.Kind==1&&o.Text.Contains("FRAME"));e.PutTextBlock(0,line.Ids,"VinsPDF 0.25 test",new TextStyle{Family="Segoe UI",Size=14},new PointF(.1f,.08f));e.AddHeaderFooter(0,false,"","Page {page} / {pages}",new TextStyle{Size=10},10,2);
   e.SaveCopy(Path.Combine(dir,Path.GetFileNameWithoutExtension(args[i])+"-025.pdf"));Check(e.Count==count,"Page count changed");Check(e.GetObjects(0).Objects.Any(o=>o.Text=="VinsPDF 0.25 test"),"Real-file line edit lost");Check(e.GetObjects(0).Objects.Any(o=>o.Text=="Page 1 / "+count),"Real-file footer lost");log.Add("PASS real drawing line replacement, footer, save/reopen");
  }
  File.WriteAllLines(Path.Combine(dir,"advanced-results.txt"),log);return 0;
 }catch(Exception ex){log.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(dir,"advanced-results.txt"),log);return 1;}}
}
}
