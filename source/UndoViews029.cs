using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
namespace VinsPDF {
internal sealed class UndoView : IDisposable {
 public Bitmap Image;public Rectangle Detail,PageRect;public int Page;public EditState State;public int[] Selection;public long Bytes{get{return Image==null?0:(long)Image.Width*Image.Height*4;}}
 public void Dispose(){if(Image!=null){Image.Dispose();Image=null;}}
}
internal sealed partial class PageCanvas {
 internal UndoView CaptureView(int index,EditState state){return new UndoView{Image=detail==null?null:(Bitmap)detail.Clone(),Detail=detailRect,PageRect=renderedPage,Page=index,State=state,Selection=CombinedSelection()==null?new int[0]:CombinedSelection().Ids};}
 internal bool RestoreView(UndoView snapshot){if(snapshot==null||snapshot.Image==null||snapshot.PageRect!=PageRectangle)return false;SetPreview(null);SetDetail((Bitmap)snapshot.Image.Clone(),snapshot.Detail,snapshot.PageRect);return true;}
}
internal sealed partial class MainForm {
 readonly List<UndoView> undoViews=new List<UndoView>(),redoViews=new List<UndoView>();
 void ClearUndoViews(){foreach(var v in undoViews.Concat(redoViews))v.Dispose();undoViews.Clear();redoViews.Clear();}
 void PushUndoView(UndoView view){foreach(var v in redoViews)v.Dispose();redoViews.Clear();undoViews.Add(view);LimitUndoViews();}
 void LimitUndoViews(){while(undoViews.Concat(redoViews).Sum(v=>v.Bytes)>32L*1024*1024||undoViews.Count+redoViews.Count>8){var list=undoViews.Count>0?undoViews:redoViews;if(list.Count==0)break;list[0].Dispose();list.RemoveAt(0);}}
 void InvalidateTextIndex(){searchEpoch++;searchHits.Clear();activeHit=-1;UpdateSearchOverlay();if(searchBar.Visible&&!string.IsNullOrEmpty(searchInput.Text))searchCount.Text="Press Enter to search again";LoadMarkGlyphs();}
 void FastUndo(bool isRedo){if(sizes==null||editBusy||!FinishInline()||(isRedo?!editorState.CanRedo:!editorState.CanUndo))return;var source=isRedo?redoViews:undoViews;var destination=isRedo?undoViews:redoViews;UndoView target=source.Count==0?null:source[source.Count-1];UndoView current=canvas.CaptureView(pageIndex,editorState);bool quick=target!=null&&target.Page==pageIndex&&canvas.RestoreView(target);editBusy=true;viewVersion++;worker.ClearIdle();debounce.Stop();UpdateEditorTools();bool editing=canvas.Editing;int token=generation;status.Text=isRedo?"Redo…":"Undo…";worker.Command(delegate(PdfEngine engine){try{int changed=engine.UndoEdit(isRedo);bool cached=target!=null&&target.Page==changed;EditState state=cached?target.State:editing?engine.GetObjects(changed):engine.HistoryState();var flags=engine.HistoryState();Post(delegate{if(token!=generation){current.Dispose();return;}if(target!=null)source.RemoveAt(source.Count-1);destination.Add(current);LimitUndoViews();state.CanUndo=flags.CanUndo;state.CanRedo=flags.CanRedo;editorState=state;dirty=true;editBusy=false;uiHiddenLayers.Clear();ClearImages();if(changed!=pageIndex)Navigate(changed);else{canvas.SetObjects(state,-1);if(target!=null)canvas.SelectMembers(target.Selection);if(quick){lastRenderedVersion=viewVersion;status.Text=isRedo?"Redone":"Undone";QueueThumbnails(generation);}else{canvas.ClearDetail();canvas.SetPreview(null);RequestRender();}}if(target!=null)target.Dispose();RefreshLayers();InvalidateTextIndex();UpdateEditorTools();UpdateDocumentTitle();},null);}catch(Exception ex){string message=ex.Message;Post(delegate{canvas.RestoreView(current);current.Dispose();editBusy=false;UpdateEditorTools();RequestRender();DarkMessage.Show(this,message,"Undo/redo failed");},null);}});}
}
}
