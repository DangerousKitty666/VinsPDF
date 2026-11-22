using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace VinsPDF {
internal class StyledDialog : DarkWindow {
 public StyledDialog(string title,int width,int height){Text=title;Size=new Size(width,height);Font=new Font("Segoe UI",9);Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);StartPosition=FormStartPosition.CenterParent;ShowInTaskbar=false;Resizable=false;}
 public static Button ActionButton(string text,Action action){var b=new Button{Text=text,Height=30,Width=108,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(28,33,38),ForeColor=Color.Gainsboro,Margin=new Padding(4)};b.FlatAppearance.BorderColor=Color.FromArgb(65,74,81);b.Click+=delegate{action();};return b;}
 protected void Ready(){InstallTitle();HideMinMax();}
}
internal sealed class SaveChangesDialog : StyledDialog {
 public SaveChangesDialog(string name,bool original=false):base("Save changes?",440,170){var content=new Panel{Dock=DockStyle.Fill,Padding=new Padding(14)};content.Controls.Add(new Label{Text="Save changes to "+name+"?",Dock=DockStyle.Top,Height=52,ForeColor=Color.Gainsboro});var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=40,FlowDirection=FlowDirection.RightToLeft};var cancel=ActionButton("Cancel",delegate{DialogResult=DialogResult.Cancel;});var discard=ActionButton(original?"No":"Don't save",delegate{DialogResult=DialogResult.No;});var save=ActionButton(original?"Yes":"Save",delegate{DialogResult=DialogResult.Yes;});actions.Controls.AddRange(new Control[]{cancel,discard,save});content.Controls.Add(actions);Controls.Add(content);AcceptButton=save;CancelButton=cancel;Ready();}
}
internal sealed class SaveProgressDialog : StyledDialog {
 public bool Done;
 public SaveProgressDialog():base("Saving PDF",300,110){Controls.Add(new Label{Text="Saving your changes…",TextAlign=ContentAlignment.MiddleCenter,Dock=DockStyle.Fill,ForeColor=Color.Gainsboro});Ready();FormClosing+=delegate(object s,FormClosingEventArgs e){if(!Done)e.Cancel=true;};}
}
internal sealed partial class MainForm {
 bool SaveBeforeClose(string target){string value=inlineText==null?null:inlineText.Text;if(value!=null&&string.IsNullOrWhiteSpace(value)){status.Text="Enter text, or cancel the empty text box before saving.";return false;}var rich=inlineText==null?null:CaptureInlineRuns();EditObject obj=inlineObject;PointF position=inlinePosition;TextStyle style=textStyle.Copy();style.Wrap=inlineResized||style.Wrap;int index=pageIndex;bool committed=false,success=false;string error=null;
 editBusy=true;viewVersion++;debounce.Stop();UpdateEditorTools();
 using(var progress=new SaveProgressDialog()){progress.Shown+=delegate{worker.Command(delegate(PdfEngine engine){try{if(value!=null){engine.PutRichTextBlock(index,obj==null?new int[0]:obj.Ids,rich,style,position,obj==null?0:obj.Angle);committed=true;}engine.SaveCopy(target);success=true;}catch(Exception ex){error=ex.Message;}Post(delegate{progress.Done=true;progress.Close();},null);});};progress.ShowDialog(TopLevelControl as Form??this);}
 editBusy=false;if(committed)CancelInline();if(success){currentPath=target;dirty=false;structurePending=false;ClearUndoViews();savedOnce=true;uiHiddenLayers.Clear();uiLockedLayers.Clear();editorState=new EditState();canvas.ClearObjects();ClearImages();UpdateDocumentTitle();}else{ClearUndoViews();dirty=dirty||committed;UpdateDocumentTitle();}UpdateEditorTools();LoadEditObjects(-1);RequestRender();if(error!=null)DarkMessage.Show(TopLevelControl as Form??this,error,"Save failed");return success;
 }
}
}
