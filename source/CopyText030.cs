using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed partial class PageCanvas {
    internal string SelectedText{get{return markStart<0||markEnd<0?"":string.Concat(MarkGlyphs.Where(g=>g.Index>=Math.Min(markStart,markEnd)&&g.Index<=Math.Max(markStart,markEnd)).OrderBy(g=>g.Index).Select(g=>g.Text));}}
    internal void CopyText(){if(SelectedText.Length>0)try{Clipboard.SetText(SelectedText);}catch(System.Runtime.InteropServices.ExternalException){}}
    internal void SelectAllText(){if(Editing||MarkGlyphs.Count==0)return;markStart=MarkGlyphs.Min(g=>g.Index);markEnd=MarkGlyphs.Max(g=>g.Index);Invalidate();}
    internal void ClearTextSelection(){markStart=markEnd=-1;marking=false;Invalidate();}
    void InstallCopyMenu(){var menu=new ContextMenuStrip{Renderer=new DarkRenderer(),BackColor=Color.Black,ForeColor=Color.Gainsboro};var copy=menu.Items.Add("Copy",null,delegate{CopyText();});menu.Items.Add("Select all text",null,delegate{SelectAllText();});menu.Opening+=delegate(object sender,System.ComponentModel.CancelEventArgs e){e.Cancel=Editing||!HasPage;copy.Enabled=SelectedText.Length>0;};ContextMenuStrip=menu;Disposed+=delegate{menu.Dispose();};}
}
internal sealed partial class MainForm {
    void LoadViewerGlyphs(PageCanvas target,int index){target.ClearTextSelection();target.MarkGlyphs.Clear();if(sizes==null||target.Editing||index<0||index>=sizes.Length)return;int token=generation,turn=rotations[index];worker.Idle(delegate(PdfEngine e){if(token!=generation||target.IsDisposed||target.Editing||(target==canvas?pageIndex!=index:!twoPages||pageIndex+1!=index))return;try{var glyphs=e.Glyphs(index);Post(delegate{if(token!=generation||target.IsDisposed||target.Editing||(target==canvas?pageIndex!=index:!twoPages||pageIndex+1!=index)||turn!=rotations[index])return;target.MarkGlyphs=glyphs.Select(g=>new TextGlyph{Index=g.Index,Text=g.Text,Bounds=g.Bounds.IsEmpty?RectangleF.Empty:ViewBox(g.Bounds,turn)}).ToList();target.Invalidate();},null);}catch(Exception ex){string message=ex.Message;Post(delegate{if(token==generation)status.Text="Text selection unavailable: "+message;},null);}});}
}
}
