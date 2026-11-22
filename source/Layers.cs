using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
namespace VinsPDF {
internal sealed partial class PdfEngine {
 readonly HashSet<string> hiddenLayers=new HashSet<string>(),lockedLayers=new HashSet<string>();
 readonly Dictionary<IntPtr,bool> layerHiddenObjects=new Dictionary<IntPtr,bool>();
 string LayerKey(int page,string name){return page+":"+name;}
 string ObjectLayer(IntPtr obj){for(uint i=0;i<Native.FPDFPageObj_CountMarks(obj);i++){IntPtr m=Native.FPDFPageObj_GetMark(obj,i);uint n;Native.FPDFPageObjMark_GetName(m,IntPtr.Zero,0,out n);if(n<2||n>4096)continue;IntPtr buffer=Marshal.AllocHGlobal((int)n);try{Native.FPDFPageObjMark_GetName(m,buffer,n,out n);if(Marshal.PtrToStringUni(buffer)!="VinsPDF.Layer")continue;uint count;Native.FPDFPageObjMark_GetParamStringValue(m,"Name",IntPtr.Zero,0,out count);if(count<2||count>4096)continue;IntPtr b=Marshal.AllocHGlobal((int)count);try{Native.FPDFPageObjMark_GetParamStringValue(m,"Name",b,count,out count);return Encoding.UTF8.GetString(Convert.FromBase64String(Marshal.PtrToStringUni(b)));}finally{Marshal.FreeHGlobal(b);}}catch(FormatException){}finally{Marshal.FreeHGlobal(buffer);}}return "Ungrouped";}
 void SetObjectLayer(IntPtr obj,string name){for(int i=Native.FPDFPageObj_CountMarks(obj)-1;i>=0;i--){IntPtr m=Native.FPDFPageObj_GetMark(obj,(uint)i);uint n;Native.FPDFPageObjMark_GetName(m,IntPtr.Zero,0,out n);if(n<2||n>4096)continue;IntPtr b=Marshal.AllocHGlobal((int)n);try{Native.FPDFPageObjMark_GetName(m,b,n,out n);if(Marshal.PtrToStringUni(b)=="VinsPDF.Layer")Native.FPDFPageObj_RemoveMark(obj,m);}finally{Marshal.FreeHGlobal(b);}}if(name!="Ungrouped"){IntPtr mark=Native.FPDFPageObj_AddMark(obj,"VinsPDF.Layer");Check(Native.FPDFPageObjMark_SetStringParam(document,obj,mark,"Name",Convert.ToBase64String(Encoding.UTF8.GetBytes(name)))!=0,"Cannot store layer name.");}}
 public string[] Layers(int index){IntPtr p=Page(index);var names=new HashSet<string>();for(int i=0;i<Native.FPDFPage_CountObjects(p);i++){IntPtr o=Native.FPDFPage_GetObject(p,i);int kind=Native.FPDFPageObj_GetType(o),active;Native.FPDFPageObj_GetIsActive(o,out active);if((kind==1||kind==3)&&(active!=0||layerHiddenObjects.ContainsKey(o)))names.Add(ObjectLayer(o));}return names.OrderBy(n=>n).ToArray();}
 public void AssignLayer(int page,int[] ids,string name){name=(name??"").Trim();Check(name.Length>0&&name.Length<=80,"Use a layer name between 1 and 80 characters.");Batch(page,delegate{foreach(int id in ids){IntPtr o=ObjectAt(page,id);string old=ObjectLayer(o),next=name;Commit(page,delegate{SetObjectLayer(o,old);},delegate{SetObjectLayer(o,next);});}});}
 public void RenameLayer(int page,string old,string name){var ids=new List<int>();IntPtr p=Page(page);for(int i=0;i<Native.FPDFPage_CountObjects(p);i++)if(ObjectLayer(Native.FPDFPage_GetObject(p,i))==old)ids.Add(i);ShowLayer(page,old,true);AssignLayer(page,ids.ToArray(),name);}
 public void ShowLayer(int page,string name,bool show){pinned.Add(page);string key=LayerKey(page,name);if(show)hiddenLayers.Remove(key);else hiddenLayers.Add(key);IntPtr p=Page(page);for(int i=0;i<Native.FPDFPage_CountObjects(p);i++){IntPtr o=Native.FPDFPage_GetObject(p,i);if(ObjectLayer(o)!=name)continue;int active;Native.FPDFPageObj_GetIsActive(o,out active);if(show){if(layerHiddenObjects.Remove(o))Native.FPDFPageObj_SetIsActive(o,1);}else if(active!=0){layerHiddenObjects[o]=true;Native.FPDFPageObj_SetIsActive(o,0);}}RenderCache.Clear(this);}
 public void LockLayer(int page,string name,bool locked){if(locked)lockedLayers.Add(LayerKey(page,name));else lockedLayers.Remove(LayerKey(page,name));}
 void RestoreLayerVisibility(){foreach(var o in layerHiddenObjects.Keys)Native.FPDFPageObj_SetIsActive(o,1);layerHiddenObjects.Clear();hiddenLayers.Clear();}
}
internal static partial class Native {[DllImport("pdfium.dll")]internal static extern int FPDFPageObj_RemoveMark(IntPtr obj,IntPtr mark);}
internal sealed partial class MainForm {
 readonly HashSet<string> uiHiddenLayers=new HashSet<string>(),uiLockedLayers=new HashSet<string>();
 void RefreshLayers(){if(closing||!HasPalette||!canvas.Editing||sizes==null)return;EditorPalette target=palette;int token=generation,index=pageIndex;worker.Command(delegate(PdfEngine e){var names=e.Layers(index);Post(delegate{if(token!=generation||index!=pageIndex||!CurrentPalette(target))return;string selected=palette.LayerList.SelectedItem as string;palette.SetLayers(names,selected,!uiHiddenLayers.Contains(index+":"+selected),uiLockedLayers.Contains(index+":"+selected));},null);});}
 void LayerCommand(string command){string name=palette.LayerList.SelectedItem as string,key=pageIndex+":"+name;var selected=canvas.CombinedSelection();if(command=="layerselect"){palette.LayerName.Text=name??"Layer 1";palette.SetLayerFlags(!uiHiddenLayers.Contains(key),uiLockedLayers.Contains(key));return;}
 if(command=="layerassign"||command=="layerremove"){if(selected==null)return;string target=command=="layerremove"?"Ungrouped":palette.LayerName.Text;ApplyEdit(delegate(PdfEngine e){e.AssignLayer(pageIndex,selected.Ids,target);return selected.Id;});return;}
 if(name==null)return;if(command=="layerrename"){string value=palette.LayerName.Text;ApplyEdit(delegate(PdfEngine e){e.RenameLayer(pageIndex,name,value);return -1;});return;}
 bool visible=palette.LayerVisible.Checked,locked=palette.LayerLocked.Checked;if(visible)uiHiddenLayers.Remove(key);else uiHiddenLayers.Add(key);if(locked)uiLockedLayers.Add(key);else uiLockedLayers.Remove(key);int index=pageIndex,token=generation;worker.Command(delegate(PdfEngine e){e.ShowLayer(index,name,visible);e.LockLayer(index,name,locked);Post(delegate{if(token!=generation)return;ClearImages();InvalidateEditedPage(index);LoadEditObjects(-1);RequestRender();},null);});
 }
}
}
