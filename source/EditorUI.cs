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
        canvas.SelectionChanged+=delegate { if(inlineText==null&&canvas.Selected!=null&&canvas.Selected.Kind==1){var obj=canvas.Selected;if(obj.Style!=null)textStyle=obj.Style.Copy();textStyle.Size=Math.Max(1,obj.DisplayFontSize);textStyle.Ink=Color.FromArgb((int)obj.Color);textStyle.BoxWidth=obj.Bounds.Width*canvas.PageSize.Width;if(HasPalette)palette.SetStyle(textStyle);}UpdateEditorTools(); };
        canvas.TextEditRequested+=EditText;canvas.ObjectRotated+=RotateSelected;
        canvas.ObjectMoved+=delegate(EditObject obj,RectangleF bounds) { if(obj!=null)ApplyEdit(delegate(PdfEngine engine) { engine.TransformObjects(pageIndex,obj.Ids,obj.Bounds,bounds); return obj.Id; },obj.Ids); };
        UpdateEditorTools();
    }
    ToolStripButton EditorButton(string text,string tip,Action action) { var b=new ToolStripButton(text) { ToolTipText=tip,ForeColor=ForeColor,Padding=new Padding(2,0,2,0) }; b.Click+=delegate { action(); }; bar.Items.Add(b); return b; }
    void UpdateEditorTools() {
        if(editToggle==null||closing||IsDisposed||Disposing)return;
        bool available=sizes!=null && !editBusy;
        foreach(var tool in documentTools)tool.Enabled=available; pages.Enabled=available;
        editToggle.Enabled=available; editToggle.Checked=canvas.Editing; saveButton.Enabled=available;
        addText.Visible=addImage.Visible=editText.Visible=undoButton.Visible=redoButton.Visible=false;
        if(HasPalette){palette.SetBusy(editBusy);if(!canvas.Editing){palette.Hide();editorDock.Visible=false;}}
        addText.Enabled=addImage.Enabled=available;
        editText.Enabled=available && canvas.Selected!=null && canvas.Selected.Kind==1;
        undoButton.Enabled=available && editorState.CanUndo; redoButton.Enabled=available && editorState.CanRedo;
        canvas.Enabled=!editBusy;
    }
    void ToggleEditing() {
        if(sizes==null || editBusy || !FinishInline())return;
        if(!canvas.Editing && twoPages)SetTwoPages(false);
        canvas.MarkMode=false;markButton.Checked=false;canvas.Editing=!canvas.Editing; canvas.ClearObjects();
        if(canvas.Editing) { rotations[pageIndex]=0; Navigate(pageIndex); status.Text="Edit · Select a region · Drag selection handles to transform · Double-click text to edit"; }
        else { canvas.Cursor=Cursors.Hand; status.Text="View mode"; }
        if(canvas.Editing)ShowPalette(); else if(HasPalette){palette.Hide();editorDock.Visible=false;}
        UpdateEditorTools(); canvas.Focus();
    }
    void LoadEditObjects(int selectedId) {
        if(sizes==null || !canvas.Editing)return;
        int token=generation,index=pageIndex;
        worker.Command(delegate(PdfEngine engine) {
            try { EditState state=engine.GetObjects(index); Post(delegate { if(token!=generation || index!=pageIndex || !canvas.Editing)return; editorState=state; canvas.SetObjects(state,selectedId);RefreshLayers(); UpdateEditorTools(); },null); }
            catch(Exception ex) { string message=ex.Message; Post(delegate { if(token==generation)status.Text="Could not prepare editing: "+message; },null); }
        });
    }
    void ApplyEdit(Func<PdfEngine,int> change,int[] keepSelection=null) {
        if(sizes==null || editBusy)return;
        editBusy=true; viewVersion++; debounce.Stop(); UpdateEditorTools(); int index=pageIndex,token=generation;
        bool editing=canvas.Editing;UndoView before=canvas.CaptureView(index,editorState);status.Text="Applying edit…";
        worker.Command(delegate(PdfEngine engine) {
            bool applied=false;
            try {
                int selected=change(engine); applied=true; EditState state=editing?engine.GetObjects(index):engine.HistoryState();
                Post(delegate { if(token!=generation){before.Dispose();return;}PushUndoView(before);dirty=true; editBusy=false; editorState=state; ClearImages(); InvalidateEditedPage(index); canvas.SetObjects(state,selected);RefreshLayers(); if(keepSelection!=null)canvas.SelectMembers(keepSelection); UpdateEditorTools(); UpdateDocumentTitle();InvalidateTextIndex(); RequestRender(); },null);
            } catch(Exception ex) { string message=ex.Message; Post(delegate { if(token!=generation){before.Dispose();return;} if(applied)PushUndoView(before);else before.Dispose();dirty=dirty||applied; editBusy=false; UpdateEditorTools(); UpdateDocumentTitle(); LoadEditObjects(-1); DarkMessage.Show(this,message,"Edit failed",MessageBoxButtons.OK,MessageBoxIcon.Warning); RequestRender(); },null); }
        });
    }
    void InvalidateEditedPage(int index) {
        foreach(var b in previews.Values)b.Dispose(); previews.Clear(); previewOrder.Clear();
        Bitmap thumb; if(thumbs.TryGetValue(index,out thumb)) { thumb.Dispose(); thumbs.Remove(index); }
        pages.Invalidate(); canvas.ClearDetail(); canvas.SetPreview(null); viewVersion++;
    }
    void EditText(EditObject obj) {BeginInline(obj);}
    void ApplyText(EditObject obj,string value,TextStyle style) {
        if(obj!=null&&obj.Style!=null&&obj.Style.ListKind>0)value=System.Text.RegularExpressions.Regex.Replace(value,@"(?m)^\s*(?:[•\-]\s+|\d+(?:\.\d+)*\.\s+)","");
        PointF position=obj==null?canvas.Normalized(new Point(canvas.Width/2,canvas.Height/2)):new PointF(obj.BaselineX,obj.BaselineY);
        if(obj==null){position.X=Math.Max(.05f,Math.Min(.85f,position.X));position.Y=Math.Max(.05f,Math.Min(.9f,position.Y));}
        int[] ids=obj==null?new int[0]:obj.Ids;TextStyle copy=style.Copy();if(obj!=null){copy.BoxWidth=obj.IsParagraph&&obj.Style!=null?obj.Style.BoxWidth:obj.Bounds.Width*canvas.PageSize.Width;copy.Wrap=obj.IsParagraph||copy.Wrap;copy.BlockKey=obj.BlockKey??copy.BlockKey;}ApplyEdit(delegate(PdfEngine e){return e.PutTextBlock(pageIndex,ids,value,copy,position,obj==null?0:obj.Angle);});
    }
    void ShowPalette() {
        if(closing||IsDisposed||Disposing)return;
        EnsurePalette();editorDock.Visible=false;
        if(HasPalette)palette.Mode(canvas.ToolMode);SyncPaletteVisibility();RefreshLayers();
    }
    void TogglePaletteFloat(){ShowPalette();}
    void PaletteCommand(string command) {
        if(editBusy||closing||IsDisposed||Disposing||!HasPalette)return;
        switch(command){
            case "close":ToggleEditing();break;case "float":TogglePaletteFloat();break;
            case "select":if(!FinishInline())return;canvas.SetTool(0);palette.Mode(0);break;
            case "move":if(!FinishInline())return;canvas.SetTool(1);palette.Mode(1);break;
            case "hand":if(!FinishInline())return;canvas.SetTool(2);palette.Mode(2);break;
            case "addtext":EditText(null);break;case "image":if(FinishInline())ImportImage();break;
            case "edittext":EditText(canvas.CombinedSelection());break;
            case "undo":if(FinishInline())UndoEditor(false);break;case "redo":if(FinishInline())UndoEditor(true);break;
            case "delete":if(FinishInline())DeleteSelection();break;
            
            case "colour":palette.ChooseColour();break;
            case "layerassign":case "layerremove":case "layerrename":case "layervisibility":case "layerlock":case "layerselect":LayerCommand(command);break;
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
    void UndoEditor(bool isRedo) {FastUndo(isRedo);}
    void UpdateDocumentTitle() { Text=(currentPath==null?"":Path.GetFileName(currentPath)+(dirty?" *":"")+" — ")+"VinsPDF"; }
    public bool CanCloseDocument(){return ConfirmDiscard();}
    bool ConfirmDiscard() {
        if(editBusy) { status.Text="Please wait for the current edit or save to finish."; return false; }
        if(!dirty&&inlineText==null)return true;
        using(var dialog=new SaveChangesDialog(Path.GetFileName(currentPath),structurePending)){var answer=dialog.ShowDialog(TopLevelControl as Form??this);if(answer==DialogResult.No){CancelInline();return true;}if(answer!=DialogResult.Yes)return false;}
        string target=currentPath;
        if(!savedOnce)using(var picker=new SaveFileDialog{Filter="PDF document|*.pdf",Title="Save changes",FileName=Path.GetFileNameWithoutExtension(currentPath)+"-edited.pdf",InitialDirectory=Path.GetDirectoryName(currentPath),OverwritePrompt=true,AddExtension=true,DefaultExt="pdf"}){if(picker.ShowDialog(TopLevelControl as Form??this)!=DialogResult.OK)return false;target=picker.FileName;}
        return SaveBeforeClose(target);
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
                if(token!=generation)return; currentPath=path;uiHiddenLayers.Clear();uiLockedLayers.Clear(); dirty=false;structurePending=false;ClearUndoViews(); editBusy=false; savedOnce=true; editorState=new EditState();
                ClearImages(); canvas.ClearObjects(); UpdateDocumentTitle(); UpdateEditorTools(); LoadEditObjects(-1); RequestRender(); status.Text="Saved · "+Path.GetFileName(path);
            },null); }
            catch(Exception ex) { string message=ex.Message; Post(delegate { if(token!=generation)return; editBusy=false; ClearUndoViews(); canvas.ClearObjects(); editorState=new EditState(); UpdateEditorTools(); LoadEditObjects(-1); RequestRender(); DarkMessage.Show(this,message,"Save failed",MessageBoxButtons.OK,MessageBoxIcon.Warning); },null); }
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
