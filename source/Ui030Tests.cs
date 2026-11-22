using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed partial class MainForm {
 Rectangle test030Camera;int[] test030Ids;
 internal bool Ui030Step(ref int stage,TabWindow shell,string directory){
  if(stage==0){
   if(canvas.MarkGlyphs.Count==0)return false;
   canvas.SelectAllText();Version030Tests.Check(canvas.SelectedText.Contains("Nested text 0.3")&&canvas.SelectedText.Contains("Regular text"),"Viewer text selection lost words");
   canvas.ClearTextSelection();var items=bar.Items.Cast<ToolStripItem>().ToList();int width=items.FindIndex(i=>i.Text=="Width");Version030Tests.Check(width>=0&&items[width+1].Text=="Layout","Layout is not next to Width");
   ToggleEditing();stage++;return false;
  }
  if(stage==1){
   if(canvas.Objects.Count==0)return false;
   var mixed=canvas.Objects.First(o=>o.Text=="Regular text bold text");test030Ids=mixed.Ids;
   BeginInline(mixed);Version030Tests.Check(inlineFrame.Parent==canvas&&!inlineText.WordWrap,"Single-line editing wraps or leaves the page");
   var runs=CaptureInlineRuns();Version030Tests.Check(runs.Any(r=>r.Style.Bold&&r.Text.Contains("bold"))&&runs.Any(r=>!r.Style.Bold&&r.Text.Contains("Regular")),"Entering editor discarded mixed font weights");
   inlineText.Select(0,7);textStyle.Ink=Color.DarkGreen;UpdateInlineFont();runs=CaptureInlineRuns();
   Version030Tests.Check(runs.Any(r=>r.Text=="Regular"&&r.Style.Ink.Value.ToArgb()==Color.DarkGreen.ToArgb())&&runs.Any(r=>r.Style.Bold&&r.Style.Ink.Value.ToArgb()!=Color.DarkGreen.ToArgb()),"Selected colour change affected unrelated run");
   inlineText.Select(inlineText.TextLength,0);inlineText.SelectedText=" edited";
   Version028Tests.Capture(inlineFrame,Path.Combine(directory,"inline-rich.png"));Version028Tests.Capture(palette,Path.Combine(directory,"palette-030.png"));
   test030Camera=canvas.PageRectangle;CommitInline();stage++;return false;
  }
  if(stage==2){Version030Tests.Check(dirty&&canvas.Objects.Any(o=>o.Text.Contains("edited")),"Rich text did not apply");UndoEditor(false);stage++;return false;}
  if(stage==3){Version030Tests.Check(test030Camera==canvas.PageRectangle&&canvas.Objects.Any(o=>o.Text=="Regular text bold text"),"Undo changed camera or lost original text");UndoEditor(true);stage++;return false;}
  if(stage==4){
   string file=Path.Combine(directory,"inline-rich-saved.pdf");Version030Tests.Check(SaveBeforeClose(file),"Rich text save failed");
   lock(PdfEngine.NativeGate)using(var e=new PdfEngine()){e.Open(file);var raw=e.GetObjects(0).Objects;Version030Tests.Check(raw.Any(o=>o.Text=="Regular"&&(o.Color&0xffffff)==0x006400)&&raw.Any(o=>o.Text=="bold"&&o.Style.Bold),"Mixed text formatting did not survive reopen");}
   using(var popup=new PageOverview(13)){popup.Show(shell);popup.PerformLayout();Version030Tests.Check(popup.Grid.Columns==3&&popup.Grid.ClientSize.Height>=522,"Page overview does not fit three rows and columns");popup.Close();}
   return true;
  }
  return false;
 }
}
}
