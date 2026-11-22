using System;
using System.Drawing;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed partial class MainForm {
 bool HasPalette { get { return palette!=null&&!palette.IsDisposed&&!palette.Disposing; } }
 bool CurrentPalette(EditorPalette value){return !closing&&!IsDisposed&&!Disposing&&HasPalette&&ReferenceEquals(palette,value);}

 void EnsurePalette(){
  if(closing||IsDisposed||Disposing||HasPalette)return;
  var created=new EditorPalette();palette=created;
  created.Location=PointToScreen(new Point(Math.Max(0,Width-250),45));
  created.SetStyle(textStyle);created.RestoreCanvasSettings(canvas);created.Mode(canvas.ToolMode);created.SetBusy(editBusy);
  created.Command+=delegate(string command){if(CurrentPalette(created))PaletteCommand(command);};
  created.Highlight.CheckedChanged+=delegate{if(!CurrentPalette(created))return;canvas.HighlightObjects=created.Highlight.Checked;canvas.Invalidate();};
  created.Lines.CheckedChanged+=delegate{if(!CurrentPalette(created)||!FinishInline())return;canvas.GroupLines=created.Lines.Checked;canvas.SetObjects(editorState,-1);};
  created.Disposed+=delegate{if(ReferenceEquals(palette,created))palette=null;};
 }

 // Visibility is subscribed once by the document, never by individual palettes.
 internal void SyncPaletteVisibility(){
  if(closing||IsDisposed||Disposing)return;
  Form owner=TopLevelControl as Form;
  bool attached=owner!=null&&!owner.IsDisposed&&!owner.Disposing&&(owner!=this||TopLevel);
  if(!canvas.Editing||!Visible||!attached||!owner.Visible){if(HasPalette)palette.Hide();return;}
  EnsurePalette();
  if(!HasPalette)return;
  if(palette.Owner!=owner){palette.Hide();palette.Owner=owner;}
  if(!palette.Visible)palette.Show(owner);
 }

 internal void HidePaletteForTransfer(){if(!HasPalette)return;palette.Hide();palette.Owner=null;}
 void DisposePalette(){var old=palette;palette=null;if(old==null||old.IsDisposed)return;old.AllowClose=true;old.Dispose();}
}
internal sealed partial class EditorPalette {
 internal void RestoreCanvasSettings(PageCanvas canvas){Highlight.Checked=canvas.HighlightObjects;Lines.Checked=canvas.GroupLines;gridShow.Checked=canvas.GridVisible;gridSpacing.Value=(decimal)Math.Max(1,Math.Min(100,canvas.GridMm));gridAlpha.Value=Math.Max(5,Math.Min(80,canvas.GridAlpha*100/255));}
}
}
