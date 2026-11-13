using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace VinsPDF {
internal sealed partial class MainForm {
    ToolStripButton editToggle,addText,addImage,editText,undoButton,redoButton,saveButton;
    bool dirty,editBusy;
    EditorPalette palette;
    TextStyle textStyle=new TextStyle();
    EditState editorState=new EditState();
    void InstallEditorToolbar() {
        bar.Items.Add(new ToolStripSeparator());
        saveButton=EditorButton("Save","Save PDF (Ctrl+S)",delegate { SaveEdited(false); });
        editToggle=EditorButton("Edit","Toggle editing (Ctrl+E)",ToggleEditing);
        addText=EditorButton("T+","Add text",delegate { EditText(null); });
        addImage=EditorButton("▧+","Import image",ImportImage);
        editText=EditorButton("✎","Edit selected text",delegate { EditText(canvas.Selected); });
        undoButton=EditorButton("↶","Undo (Ctrl+Z)",delegate { UndoEditor(false); });
        redoButton=EditorButton("↷","Redo (Ctrl+Y)",delegate { UndoEditor(true); });
        foreach(var button in new[]{addText,addImage,editText,undoButton,redoButton})bar.Items.Remove(button);
        canvas.SelectionChanged+=delegate { UpdateEditorTools(); };
        canvas.TextEditRequested+=EditText;canvas.ObjectRotated+=RotateSelected;
        canvas.ObjectMoved+=delegate(EditObject obj,RectangleF bounds) { if(obj!=null)ApplyEdit(delegate(PdfEngine engine) { engine.TransformObjects(pageIndex,obj.Ids,obj.Bounds,bounds); return obj.Id; },obj.Ids); };
        UpdateEditorTools();
    }
    ToolStripButton EditorButton(string text,string tip,Action action) { var b=new ToolStripButton(text) { ToolTipText=tip,ForeColor=ForeColor,Padding=new Padding(2,0,2,0) }; b.Click+=delegate { action(); }; bar.Items.Add(b); return b; }
    void UpdateEditorTools() {
        if(editToggle==null)return;
        bool available=sizes!=null && !editBusy;
        foreach(var tool in documentTools)tool.Enabled=available; pages.Enabled=available;
        editToggle.Enabled=available; editToggle.Checked=canvas.Editing; saveButton.Enabled=available;
        addText.Visible=addImage.Visible=editText.Visible=undoButton.Visible=redoButton.Visible=false;
        if(palette!=null){palette.SetBusy(editBusy);if(!canvas.Editing){palette.Hide();editorDock.Visible=false;}}
        addText.Enabled=addImage.Enabled=available;
        editText.Enabled=available && canvas.Selected!=null && canvas.Selected.Kind==1;
        undoButton.Enabled=available && editorState.CanUndo; redoButton.Enabled=available && editorState.CanRedo;
        canvas.Enabled=!editBusy;
    }
    void ToggleEditing() {
        if(sizes==null || editBusy || !FinishInline())return;
        if(!canvas.Editing && twoPages)SetTwoPages(false);
        canvas.Editing=!canvas.Editing; canvas.ClearObjects();
        if(canvas.Editing) { rotations[pageIndex]=0; Navigate(pageIndex); status.Text="Edit · Click text/image · Drag to move · Corner to resize · Double-click text"; }
        else { canvas.Cursor=Cursors.Hand; status.Text="View mode"; }
        if(canvas.Editing)ShowPalette(); else if(palette!=null){palette.Hide();editorDock.Visible=false;}
        UpdateEditorTools(); canvas.Focus();
    }
    void LoadEditObjects(int selectedId) {
        if(sizes==null || !canvas.Editing)return;
        int token=generation,index=pageIndex;
        worker.Command(delegate(PdfEngine engine) {
            try { EditState state=engine.GetObjects(index); Post(delegate { if(token!=generation || index!=pageIndex || !canvas.Editing)return; editorState=state; canvas.SetObjects(state,selectedId); UpdateEditorTools(); },null); }
            catch(Exception ex) { string message=ex.Message; Post(delegate { if(token==generation)status.Text="Could not prepare editing: "+message; },null); }
        });
    }
    void ApplyEdit(Func<PdfEngine,int> change,int[] keepSelection=null) {
        if(sizes==null || editBusy)return;
        editBusy=true; viewVersion++; debounce.Stop(); UpdateEditorTools(); int index=pageIndex,token=generation;
        status.Text="Applying edit…";
        worker.Command(delegate(PdfEngine engine) {
            bool applied=false;
            try {
                int selected=change(engine); applied=true; EditState state=engine.GetObjects(index);
                Post(delegate { if(token!=generation)return; dirty=true; editBusy=false; editorState=state; ClearImages(); InvalidateEditedPage(index); canvas.SetObjects(state,selected); if(keepSelection!=null)canvas.SelectMembers(keepSelection); UpdateEditorTools(); UpdateDocumentTitle(); RequestRender(); },null);
            } catch(Exception ex) { string message=ex.Message; Post(delegate { if(token!=generation)return; dirty=dirty||applied; editBusy=false; UpdateEditorTools(); UpdateDocumentTitle(); LoadEditObjects(-1); DarkMessage.Show(this,message,"Edit failed",MessageBoxButtons.OK,MessageBoxIcon.Warning); RequestRender(); },null); }
        });
    }
    void InvalidateEditedPage(int index) {
        foreach(var b in previews.Values)b.Dispose(); previews.Clear(); previewOrder.Clear();
        Bitmap thumb; if(thumbs.TryGetValue(index,out thumb)) { thumb.Dispose(); thumbs.Remove(index); }
        pages.Invalidate(); canvas.ClearDetail(); canvas.SetPreview(null); viewVersion++;
    }
    void EditText(EditObject obj) {BeginInline(obj);}
    void ApplyText(EditObject obj,string value,TextStyle style) {
        PointF position=obj==null?canvas.Normalized(new Point(canvas.Width/2,canvas.Height/2)):new PointF(obj.BaselineX,obj.BaselineY);
        if(obj==null){position.X=Math.Max(.05f,Math.Min(.85f,position.X));position.Y=Math.Max(.05f,Math.Min(.9f,position.Y));}
        int[] ids=obj==null?new int[0]:obj.Ids;TextStyle copy=style.Copy();ApplyEdit(delegate(PdfEngine e){return e.PutTextBlock(pageIndex,ids,value,copy,position,obj==null?0:obj.Angle);});
    }
    void ShowPalette() {
        if(palette==null || palette.IsDisposed){palette=new EditorPalette();palette.Command+=PaletteCommand;palette.Highlight.CheckedChanged+=delegate{canvas.HighlightObjects=palette.Highlight.Checked;canvas.Invalidate();};palette.Lines.CheckedChanged+=delegate{if(!FinishInline())return;canvas.GroupLines=palette.Lines.Checked;canvas.SetObjects(editorState,-1);};editorDock.Controls.Add(palette);palette.SetStyle(textStyle);VisibleChanged+=delegate{if(palette!=null&&palette.TopLevel){if(Visible&&canvas.Editing)palette.Show(TopLevelControl as Form??this);else palette.Hide();}};}
        editorDock.Visible=!palette.TopLevel;palette.Mode(canvas.SelectTool);if(palette.TopLevel)palette.Show(TopLevelControl as Form??this);else palette.Show();
    }
    void TogglePaletteFloat(){
        if(palette==null)return;palette.Hide();
        if(!palette.TopLevel){editorDock.Controls.Remove(palette);editorDock.Visible=false;palette.TopLevel=true;palette.Dock=DockStyle.None;palette.EmbeddedTitle(true);palette.Size=new Size(236,Math.Min(760,Math.Max(480,Height-70)));palette.StartPosition=FormStartPosition.Manual;palette.Location=PointToScreen(new Point(Math.Max(0,Width-250),40));palette.ShowInTaskbar=false;palette.Opacity=palette.FloatingOpacity;}
        else{palette.Owner=null;palette.TopLevel=false;palette.EmbeddedTitle(false);palette.Opacity=1;palette.Dock=DockStyle.Fill;editorDock.Controls.Add(palette);}
        ShowPalette();
    }
    void PaletteCommand(string command) {
        if(editBusy)return;
        switch(command){
            case "close":ToggleEditing();break;case "float":TogglePaletteFloat();break;
            case "select":canvas.SelectTool=true;palette.Mode(true);break;
            case "hand":if(!FinishInline())return;canvas.SelectTool=false;palette.Mode(false);break;
            case "addtext":EditText(null);break;case "image":if(FinishInline())ImportImage();break;
            case "edittext":EditText(canvas.CombinedSelection());break;
            case "undo":if(FinishInline())UndoEditor(false);break;case "redo":if(FinishInline())UndoEditor(true);break;
            case "delete":if(FinishInline())DeleteSelection();break;
            case "gridapply":palette.ApplyGrid(canvas);break;
            case "stylepreview":textStyle=palette.Style;UpdateInlineFont();break;
            case "fontapply":case "spacingapply":textStyle=palette.Style;if(inlineText!=null)CommitInline();else{var selected=canvas.CombinedSelection();if(selected!=null&&selected.Kind==1)ApplyText(selected,selected.Text,textStyle);}break;
            case "canceltext":CancelInline();break;
            case "rotateapply":if(FinishInline())RotateSelected(palette.Rotation);break;
            case "headerapply":if(!FinishInline())return;bool all=palette.AllPages;string h=palette.Header,f=palette.Footer;float margin=palette.MarginMm;int alignment=palette.Alignment;TextStyle style=palette.Style;if(string.IsNullOrWhiteSpace(h)&&string.IsNullOrWhiteSpace(f))return;ApplyEdit(delegate(PdfEngine e){e.AddHeaderFooter(pageIndex,all,h,f,style,margin,alignment);return -1;});break;
        }
    }
    void DeleteSelection(){var selected=canvas.CombinedSelection();if(selected!=null)ApplyEdit(delegate(PdfEngine e){e.DeleteObjects(pageIndex,selected.Ids);return -1;});}
    void ImportImage() {
        if(sizes==null || !canvas.Editing || editBusy)return;
        using(var dialog=new OpenFileDialog { Filter="Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff",Title="Import image" }) {
            if(dialog.ShowDialog(this)!=DialogResult.OK)return;
            string path=dialog.FileName;
            try {
                float ratio; using(var image=Image.FromFile(path))ratio=(float)image.Height/image.Width;
                SizeF size=canvas.PageSize; float w=.25f,h=w*size.Width*ratio/size.Height;
                if(h>.5f) { w*=.5f/h; h=.5f; }
                PointF center=canvas.Normalized(new Point(canvas.Width/2,canvas.Height/2));
                var bounds=new RectangleF(Math.Max(0,Math.Min(1-w,center.X-w/2)),Math.Max(0,Math.Min(1-h,center.Y-h/2)),w,h);
                ApplyEdit(delegate(PdfEngine engine) { return engine.PutImage(pageIndex,path,bounds); });
            } catch(Exception ex) { DarkMessage.Show(this,ex.Message,"Cannot import image"); }
        }
    }
    void UndoEditor(bool isRedo) {
        if(sizes==null || editBusy || !FinishInline() || (isRedo?!editorState.CanRedo:!editorState.CanUndo))return;
        editBusy=true; viewVersion++; UpdateEditorTools(); int token=generation;
        worker.Command(delegate(PdfEngine engine) {
            try { int changed=engine.UndoEdit(isRedo); Post(delegate { if(token!=generation)return; editBusy=false; dirty=true; if(changed>=0) { ClearImages(); InvalidateEditedPage(changed); if(changed!=pageIndex)Navigate(changed);else RequestRender(); } LoadEditObjects(-1); UpdateEditorTools(); UpdateDocumentTitle(); },null); }
            catch(Exception ex) { string message=ex.Message; Post(delegate { editBusy=false; UpdateEditorTools(); DarkMessage.Show(this,message,"Undo/redo failed"); },null); }
        });
    }
    void UpdateDocumentTitle() { Text=(currentPath==null?"":Path.GetFileName(currentPath)+(dirty?" *":"")+" — ")+"VinsPDF"; }
    public bool CanCloseDocument(){return ConfirmDiscard();}
    bool ConfirmDiscard() {
        if(!FinishInline())return false;
        if(editBusy) { status.Text="Please wait for the current edit or save to finish."; return false; }
        if(!dirty)return true;
        return DarkMessage.Show(this,"Discard unsaved changes?\nChoose No to return and save your PDF.","VinsPDF",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)==DialogResult.Yes;
    }
    void SaveEdited(bool saveAs) {
        if(sizes==null || editBusy || !FinishInline())return;
        string target=currentPath;
        if(saveAs || !savedOnce) {
            using(var dialog=new SaveFileDialog { Filter="PDF document|*.pdf",Title="Save edited PDF",FileName=Path.GetFileNameWithoutExtension(currentPath)+"-edited.pdf",InitialDirectory=Path.GetDirectoryName(currentPath),OverwritePrompt=true,AddExtension=true,DefaultExt="pdf" }) {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return; target=dialog.FileName;
            }
        }
        SaveTo(target);
    }
    bool savedOnce;
    void SaveTo(string path) {
        editBusy=true; viewVersion++; debounce.Stop(); UpdateEditorTools(); status.Text="Saving PDF…"; int token=generation;
        worker.Command(delegate(PdfEngine engine) {
            try { engine.SaveCopy(path); Post(delegate {
                if(token!=generation)return; currentPath=path; dirty=false; editBusy=false; savedOnce=true; editorState=new EditState();
                ClearImages(); canvas.ClearObjects(); UpdateDocumentTitle(); UpdateEditorTools(); LoadEditObjects(-1); RequestRender(); status.Text="Saved · "+Path.GetFileName(path);
            },null); }
            catch(Exception ex) { string message=ex.Message; Post(delegate { if(token!=generation)return; editBusy=false; canvas.ClearObjects(); editorState=new EditState(); UpdateEditorTools(); LoadEditObjects(-1); RequestRender(); DarkMessage.Show(this,message,"Save failed",MessageBoxButtons.OK,MessageBoxIcon.Warning); },null); }
        });
    }
    bool EditorShortcut(Keys key) {
        if(key==(Keys.Control|Keys.O)) { ChooseFile(); return true; }
        if(key==(Keys.Control|Keys.W)) { if(CloseTab!=null)CloseTab();else if(ConfirmDiscard())CloseDocument(); return true; }
        if(key==(Keys.Control|Keys.S)) { SaveEdited(false); return true; }
        if(key==(Keys.Control|Keys.Shift|Keys.S)) { SaveEdited(true); return true; }
        if(key==(Keys.Control|Keys.E)) { ToggleEditing(); return true; }
        if(key==(Keys.Control|Keys.Z)) { UndoEditor(false); return true; }
        if(key==(Keys.Control|Keys.Y)) { UndoEditor(true); return true; }
        if(key==(Keys.Control|Keys.D0)) { canvas.Fit("page"); return true; }
        if(key==(Keys.Control|Keys.D1)) { canvas.ActualSize(); return true; }
        if(key==(Keys.Control|Keys.D2)) { canvas.Fit("width"); return true; }
        if(key==Keys.F9) { ToggleThumbnails(); return true; }
        if(key==Keys.Delete && canvas.Editing && inlineText==null && !pageBox.Focused) {DeleteSelection();return true;}
        if(key==Keys.Escape && canvas.Editing) { canvas.Selected=null;canvas.Selection.Clear(); canvas.Invalidate(); UpdateEditorTools(); return true; }
        return false;
    }
}
}
