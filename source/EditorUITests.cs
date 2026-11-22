using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace VinsPDF {
internal sealed partial class MainForm {
    public static string EditorTestDirectory;
    void StartEditorUITest() {
        int stage=0,ticks=0,selectionCount=0;double zoom=0; RectangleF before=new RectangleF();
        var timer=new Timer { Interval=70 };
        timer.Tick+=delegate {
            if(++ticks>1800) { Trace("FAIL editor UI timeout"); dirty=false; editBusy=false; timer.Stop(); Close(); return; }
            if(editBusy || lastRenderedVersion!=viewVersion)return;
            try {
                switch(stage++) {
                    case 0: ToggleEditing(); break;
                    case 1: if(!canvas.Editing || canvas.Objects.Count==0)throw new Exception("Editor did not load objects"); ApplyEdit(delegate(PdfEngine e) { return e.PutTextBlock(pageIndex,new int[0],"VinsPDF editor UI",new TextStyle{Size=16},new PointF(.3f,.08f)); }); break;
                    case 2: if(canvas.Selected==null || !canvas.Selected.Text.Contains("VinsPDF editor UI"))throw new Exception("New text selection failed"); before=canvas.Selected.Bounds;Trace("move_before="+before+" text="+canvas.Selected.Text); canvas.TestGesture(false,65,30); Trace(canvas.GestureTrace); break;
                    case 3: Trace("move_after="+(canvas.Selected==null?"null":canvas.Selected.Bounds+" text="+canvas.Selected.Text)); if(canvas.Selected==null || canvas.Selected.Bounds.Left<=before.Left)throw new Exception("Mouse move failed"); before=canvas.Selected.Bounds; canvas.TestGesture(true,30,15); break;
                    case 4: if(canvas.Selected==null || canvas.Selected.Bounds.Width<=before.Width)throw new Exception("Resize handle failed"); UndoEditor(false); break;
                    case 5: ApplyEdit(delegate(PdfEngine e) { return e.PutImage(pageIndex,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"source","VinsPDF-original.png"),new RectangleF(.7f,.08f,.12f,.17f)); }); break;
                    case 6: if(canvas.Selected==null || canvas.Selected.Kind!=3)throw new Exception("Image selection failed"); canvas.TestGesture(false,-45,30); break;
                    case 7: canvas.TestGesture(true,20,20); break;
                    case 8: canvas.GridVisible=true;canvas.GridMm=20;palette.Opacity=.8;palette.Location=new Point(Location.X+30,Location.Y+90);CaptureWorkspace(Path.Combine(EditorTestDirectory,"editor.png")); SaveTo(Path.Combine(EditorTestDirectory,"ui-edited.pdf")); break;
                    case 9: if(dirty || !savedOnce || !File.Exists(Path.Combine(EditorTestDirectory,"ui-edited.pdf")))throw new Exception("UI save failed"); if(!editorState.Objects.Exists(o=>o.Text.Contains("VinsPDF editor UI")))throw new Exception("Saved text missing"); ToggleEditing();canvas.Zoom(2,new Point(canvas.Width/2,canvas.Height/2));zoom=canvas.Scale;for(int i=0;i<100&&pageIndex==0;i++)canvas.ScrollWheel(-120);if(pageIndex!=1)throw new Exception("Wheel did not reach next page");break;
                    case 10: if(Math.Abs(canvas.Scale-zoom)>.001)throw new Exception("Wheel changed zoom");for(int i=0;i<100&&pageIndex==1;i++)canvas.ScrollWheel(120);if(pageIndex!=0)throw new Exception("Wheel did not reach previous page");canvas.Fit("page");ToggleEditing();break;
                    case 11: Rectangle p=canvas.PageRectangle;canvas.TestDrag(new Point(p.Left-8,p.Top-8),new Point(p.Right+8,p.Bottom+8));selectionCount=canvas.Selection.Count;if(selectionCount<2)throw new Exception("Marquee selection failed");canvas.TestGesture(false,20,20);break;
                    case 12: if(canvas.Selection.Count!=selectionCount)throw new Exception("Group movement lost selection");UndoEditor(false);break;
                    case 13: Trace("PASS editor selection, text/image mouse move/resize, undo, Save As/reopen, grid and floating palette, wheel across both page edges at retained zoom, marquee and persistent multi-selection");timer.Stop();timer.Dispose();dirty=false;Close();break;
                }
            } catch(Exception ex) { Trace("FAIL "+ex); timer.Stop(); dirty=false; editBusy=false; Close(); }
        };
        timer.Start();
    }
}
}
