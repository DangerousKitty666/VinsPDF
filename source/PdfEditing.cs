using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace VinsPDF {
internal sealed class EditObject {
    public int Id,Kind;public string Layer;public bool Locked;
    public RectangleF Bounds;
    public string Text;
    public string GroupKey,LineText;
    public string SourceText;
    public int TextStart=-1,TextEnd=-1;
    public float FontSize,DisplayFontSize;
    public int[] Members;
    public float Angle,BaselineY,BaselineX;
    public uint Color=0xff000000;
    public int[] Ids { get { return Members??new int[]{Id}; } }
}
internal sealed class EditState {
    public List<EditObject> Objects=new List<EditObject>();
    public bool CanUndo,CanRedo;
}
internal sealed partial class PdfEngine {
    readonly HashSet<int> pinned=new HashSet<int>();
    sealed class Change { public int Page; public Action Undo,Redo; }
    readonly Stack<Change> undo=new Stack<Change>(),redo=new Stack<Change>();
    IntPtr editFont;
    void ResetEditing() { hiddenLayers.Clear();lockedLayers.Clear();layerHiddenObjects.Clear(); foreach(var font in extraFonts.Values)Native.FPDFFont_Close(font);extraFonts.Clear();undo.Clear(); redo.Clear(); pinned.Clear(); if(editFont!=IntPtr.Zero) { Native.FPDFFont_Close(editFont); editFont=IntPtr.Zero; } }
    void Check(bool okay,string message) { if(!okay)throw new IOException(message); }
    void Generate(int index) { Check(Native.FPDFPage_GenerateContent(Page(index))!=0,"Unable to generate edited page content."); }
    void Commit(int index,Action back,Action forward) {
        // Render edited native objects directly. Generate content only at save;
        // repeated regeneration can reorder content across original PDF streams.
        RenderCache.Clear(this); pinned.Add(index); forward();
        undo.Push(new Change { Page=index,Undo=back,Redo=forward }); redo.Clear();
    }
    public int UndoEdit(bool isRedo) {
        var source=isRedo?redo:undo; var dest=isRedo?undo:redo;
        if(source.Count==0)return -1;
        RestoreLayerVisibility();RenderCache.Clear(this); Change c=source.Peek(); if(isRedo)c.Redo(); else c.Undo(); source.Pop(); dest.Push(c); return c.Page;
    }
    public EditState GetObjects(int index) {
        var result=new EditState { CanUndo=undo.Count>0,CanRedo=redo.Count>0 };
        IntPtr page=Page(index),textPage=Native.FPDFText_LoadPage(page);
        var textObjects=new Dictionary<IntPtr,EditObject>();
        try {
            int count=Native.FPDFPage_CountObjects(page);
            for(int i=0;i<count;i++) {
                IntPtr obj=Native.FPDFPage_GetObject(page,i); int kind=Native.FPDFPageObj_GetType(obj),active;
                if((kind!=1 && kind!=3) || Native.FPDFPageObj_GetIsActive(obj,out active)==0 || active==0)continue;
                float l,b,r,t; if(Native.FPDFPageObj_GetBounds(obj,out l,out b,out r,out t)==0)continue;
                PointF a=ToDisplay(page,l,b),z=ToDisplay(page,r,t);
                var item=new EditObject { Id=i,Kind=kind,Bounds=RectangleF.FromLTRB(Math.Min(a.X,z.X),Math.Min(a.Y,z.Y),Math.Max(a.X,z.X),Math.Max(a.Y,z.Y)),Text="" };
                if(kind==1) {
                    textObjects[obj]=item;
                    for(uint markIndex=0;markIndex<Native.FPDFPageObj_CountMarks(obj);markIndex++){IntPtr mark=Native.FPDFPageObj_GetMark(obj,markIndex);uint length;Native.FPDFPageObjMark_GetName(mark,IntPtr.Zero,0,out length);if(length>2 && length<2048){IntPtr buffer=Marshal.AllocHGlobal((int)length);try{if(Native.FPDFPageObjMark_GetName(mark,buffer,length,out length)!=0){string name=Marshal.PtrToStringUni(buffer);if(name!=null&&name.StartsWith("VinsPDF.Line.")){item.GroupKey=name;uint required;Native.FPDFPageObjMark_GetParamStringValue(mark,"FragmentText",IntPtr.Zero,0,out required);if(required>2 && required<2000000){IntPtr textBuffer=Marshal.AllocHGlobal((int)required);try{if(Native.FPDFPageObjMark_GetParamStringValue(mark,"FragmentText",textBuffer,required,out required)!=0){try{item.LineText=Encoding.UTF8.GetString(Convert.FromBase64String(Marshal.PtrToStringUni(textBuffer)));}catch(FormatException){}}}finally{Marshal.FreeHGlobal(textBuffer);}}}}}finally{Marshal.FreeHGlobal(buffer);}}}
                    Native.FPDFTextObj_GetFontSize(obj,out item.FontSize);
                    Native.Matrix matrix; if(Native.FPDFPageObj_GetMatrix(obj,out matrix)!=0) { item.DisplayFontSize=(float)Math.Sqrt(matrix.C*matrix.C+matrix.D*matrix.D)*item.FontSize; PointF p1=ToDisplay(page,matrix.E,matrix.F),p2=ToDisplay(page,matrix.E+matrix.A,matrix.F+matrix.B); item.BaselineY=p1.Y;item.BaselineX=p1.X; item.Angle=(float)(Math.Atan2((p2.Y-p1.Y)*Sizes[index].Height,(p2.X-p1.X)*Sizes[index].Width)*180/Math.PI); }
                    uint cr,cg,cb,ca; if(Native.FPDFPageObj_GetFillColor(obj,out cr,out cg,out cb,out ca)!=0)item.Color=(ca<<24)|(cr<<16)|(cg<<8)|cb;
                    uint size=Native.FPDFTextObj_GetText(obj,textPage,IntPtr.Zero,0);
                    if(size>2 && size<2000000) { IntPtr buffer=Marshal.AllocHGlobal((int)size); try { Native.FPDFTextObj_GetText(obj,textPage,buffer,size); item.Text=(Marshal.PtrToStringUni(buffer)??"").Replace('\u00a0',' '); } finally { Marshal.FreeHGlobal(buffer); } }
                }
                if(kind==1){if(item.LineText!=null)item.Text=item.LineText;else if(item.Text.Length==0 && item.Bounds.Height<.00001f)item.Text=" ";}
                item.Layer=ObjectLayer(obj);item.Locked=lockedLayers.Contains(LayerKey(index,item.Layer));result.Objects.Add(item);
            }
            var source=new StringBuilder();int chars=Native.FPDFText_CountChars(textPage);
            if(chars<2000000)for(int charIndex=0;charIndex<chars;charIndex++){uint code=Native.FPDFText_GetUnicode(textPage,charIndex);if(code==0||code>0x10ffff||(code>=0xd800&&code<=0xdfff))continue;int start=source.Length;source.Append(code==160?" ":char.ConvertFromUtf32((int)code));EditObject item;if(textObjects.TryGetValue(Native.FPDFText_GetTextObject(textPage,charIndex),out item)){if(item.TextStart<0)item.TextStart=start;item.TextEnd=source.Length;}}
            string pageText=source.ToString();foreach(var item in textObjects.Values)item.SourceText=pageText;
        } finally { if(textPage!=IntPtr.Zero)Native.FPDFText_ClosePage(textPage); }
        return result;
    }
    PointF ToDisplay(IntPtr page,double x,double y) { int dx,dy; Check(Native.FPDF_PageToDevice(page,0,0,1000000,1000000,0,x,y,out dx,out dy)!=0,"Invalid page coordinates."); return new PointF(dx/1000000f,dy/1000000f); }
    PointF ToPdf(IntPtr page,float x,float y) { double px,py; Check(Native.FPDF_DeviceToPage(page,0,0,1000000,1000000,0,(int)Math.Round(x*1000000),(int)Math.Round(y*1000000),out px,out py)!=0,"Invalid page coordinates."); return new PointF((float)px,(float)py); }
    RectangleF PdfRect(IntPtr page,RectangleF box) { PointF a=ToPdf(page,box.Left,box.Top),b=ToPdf(page,box.Right,box.Bottom); return RectangleF.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y)); }
    IntPtr ObjectAt(int index,int id) { IntPtr obj=Native.FPDFPage_GetObject(Page(index),id); if(obj==IntPtr.Zero)throw new IOException("This object is no longer available. Select it again."); return obj; }
    public void TransformObject(int index,int id,RectangleF bounds) {
        IntPtr obj=ObjectAt(index,id),page=Page(index); float l,b,r,t;
        Check(Native.FPDFPageObj_GetBounds(obj,out l,out b,out r,out t)!=0,"Could not read object size.");
        RectangleF target=PdfRect(page,bounds);
        if(r-l<.001 || t-b<.001 || target.Width<.01 || target.Height<.01)throw new IOException("The object is too small to resize.");
        double sx=target.Width/(r-l),sy=target.Height/(t-b);
        double tx=target.Left-l*sx,ty=target.Top-b*sy;
        Native.Matrix old; Check(Native.FPDFPageObj_GetMatrix(obj,out old)!=0,"Could not read transform.");
        var changed=new Native.Matrix { A=(float)(sx*old.A),B=(float)(sy*old.B),C=(float)(sx*old.C),D=(float)(sy*old.D),E=(float)(sx*old.E+tx),F=(float)(sy*old.F+ty) };
        Commit(index,delegate { SetObjectMatrix(obj,old); },delegate { SetObjectMatrix(obj,changed); });
    }
    void SetObjectMatrix(IntPtr obj,Native.Matrix matrix) {
        Check(Native.FPDFPageObj_SetMatrix(obj,ref matrix)!=0,"Could not set object transform.");
        // PDFium can clear an image's matrix-dirty flag when restoring its parsed
        // matrix. Force the stream dirty even when undo returns to that matrix.
        Native.FPDFPageObj_Transform(obj,1,0,0,1,0,0);
    }
    IntPtr FontForEdit() {
        if(editFont==IntPtr.Zero) { string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf"); byte[] data=File.ReadAllBytes(path); editFont=Native.FPDFText_LoadFont(document,data,(uint)data.Length,2,1); Check(editFont!=IntPtr.Zero,"Unable to load Arial for editing."); }
        return editFont;
    }
    public int PutText(int index,int oldId,string text,float size,PointF position) {
        if(string.IsNullOrWhiteSpace(text))throw new IOException("Enter some text.");
        if(text.IndexOf('\n')>=0 || text.IndexOf('\r')>=0)throw new IOException("Use one text line per object in this version.");
        if(size<1 || size>500)throw new IOException("Font size must be between 1 and 500 pt.");
        IntPtr page=Page(index),old=oldId>=0?ObjectAt(index,oldId):IntPtr.Zero;
        IntPtr obj=Native.FPDFPageObj_CreateTextObj(document,FontForEdit(),size); Check(obj!=IntPtr.Zero,"Could not create text.");
        bool inserted=false;
        try {
            Check(Native.FPDFText_SetText(obj,text)!=0,"The text could not be encoded.");
            if(old!=IntPtr.Zero) {
                Native.Matrix matrix; Check(Native.FPDFPageObj_GetMatrix(old,out matrix)!=0,"Could not read text transform."); Native.FPDFPageObj_SetMatrix(obj,ref matrix);
                uint r,g,b,a; if(Native.FPDFPageObj_GetFillColor(old,out r,out g,out b,out a)!=0)Native.FPDFPageObj_SetFillColor(obj,r,g,b,a);
            } else { PointF pt=ToPdf(page,position.X,position.Y),px=ToPdf(page,position.X+.01f,position.Y),py=ToPdf(page,position.X,position.Y-.01f); double lx=Math.Sqrt(Math.Pow(px.X-pt.X,2)+Math.Pow(px.Y-pt.Y,2)),ly=Math.Sqrt(Math.Pow(py.X-pt.X,2)+Math.Pow(py.Y-pt.Y,2)); Native.FPDFPageObj_Transform(obj,(px.X-pt.X)/lx,(px.Y-pt.Y)/lx,(py.X-pt.X)/ly,(py.Y-pt.Y)/ly,pt.X,pt.Y); Native.FPDFPageObj_SetFillColor(obj,0,0,0,255); }
            Native.FPDFPage_InsertObject(page,obj); inserted=true; Native.FPDFPageObj_SetIsActive(obj,0);
            Commit(index,delegate { Native.FPDFPageObj_SetIsActive(obj,0); if(old!=IntPtr.Zero)Native.FPDFPageObj_SetIsActive(old,1); },delegate { Native.FPDFPageObj_SetIsActive(obj,1); if(old!=IntPtr.Zero)Native.FPDFPageObj_SetIsActive(old,0); });
            return Native.FPDFPage_CountObjects(page)-1;
        } catch { if(!inserted)Native.FPDFPageObj_Destroy(obj); throw; }
    }
    public int PutImage(int index,string path,RectangleF bounds) {
        IntPtr page=Page(index),obj=Native.FPDFPageObj_NewImageObj(document); Check(obj!=IntPtr.Zero,"Could not create image.");
        bool inserted=false;
        try {
            using(var original=new Bitmap(path)) {
                if((long)original.Width*original.Height>50000000)throw new IOException("Use an image smaller than 50 megapixels.");
                using(var bitmap=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb)) {
                    using(Graphics g=Graphics.FromImage(bitmap))g.DrawImageUnscaled(original,0,0);
                    var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
                    IntPtr native=IntPtr.Zero;
                    try { native=Native.FPDFBitmap_CreateEx(bitmap.Width,bitmap.Height,4,data.Scan0,data.Stride); Check(native!=IntPtr.Zero,"Could not decode image."); Check(Native.FPDFImageObj_SetBitmap(IntPtr.Zero,0,obj,native)!=0,"Could not import image."); }
                    finally { if(native!=IntPtr.Zero)Native.FPDFBitmap_Destroy(native); bitmap.UnlockBits(data); }
                }
            }
            PointF origin=ToPdf(page,bounds.Left,bounds.Bottom),right=ToPdf(page,bounds.Right,bounds.Bottom),top=ToPdf(page,bounds.Left,bounds.Top);
            Native.FPDFPageObj_Transform(obj,right.X-origin.X,right.Y-origin.Y,top.X-origin.X,top.Y-origin.Y,origin.X,origin.Y);
            Native.FPDFPage_InsertObject(page,obj); inserted=true; Native.FPDFPageObj_SetIsActive(obj,0);
            Commit(index,delegate { Native.FPDFPageObj_SetIsActive(obj,0); },delegate { Native.FPDFPageObj_SetIsActive(obj,1); });
            return Native.FPDFPage_CountObjects(page)-1;
        } catch { if(!inserted)Native.FPDFPageObj_Destroy(obj); throw; }
    }
    public void DeleteObject(int index,int id) { IntPtr obj=ObjectAt(index,id); Commit(index,delegate { Native.FPDFPageObj_SetIsActive(obj,1); },delegate { Native.FPDFPageObj_SetIsActive(obj,0); }); }
    public void SaveCopy(string target) {
        // Serialize before touching the destination. File.Replace is atomic on local NTFS.
        target=Path.GetFullPath(target); string temp=Path.Combine(Path.GetDirectoryName(target),"."+Path.GetFileName(target)+"."+Guid.NewGuid().ToString("N")+".tmp");
        bool serialized=false;
        try {
            using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                RestoreLayerVisibility();foreach(int pageIndex in pinned)Generate(pageIndex);
                Exception failure=null;
                Native.WriteBlock callback=delegate(IntPtr self,IntPtr data,uint count) { try { byte[] bytes=new byte[count]; Marshal.Copy(data,bytes,0,(int)count); stream.Write(bytes,0,bytes.Length); return 1; } catch(Exception ex) { failure=ex; return 0; } };
                var writer=new Native.FileWrite { Version=1,Callback=Marshal.GetFunctionPointerForDelegate(callback) };
                int result=Native.FPDF_SaveAsCopy(document,ref writer,2); GC.KeepAlive(callback);
                if(failure!=null)throw failure; Check(result!=0,"The PDF could not be saved."); stream.Flush(true); serialized=true;
            }
            Close();
            try { if(File.Exists(target))File.Replace(temp,target,null); else File.Move(temp,target); }
            catch { Open(temp); throw new IOException("Could not replace the destination. Your edited document remains open; use Save As to choose another location. Recovery copy: "+temp); }
            Open(target);
        } catch { if(!serialized && File.Exists(temp))File.Delete(temp); throw; }
    }
}
internal static partial class Native {
    [StructLayout(LayoutKind.Sequential)] internal struct Matrix { public float A,B,C,D,E,F; }
    [StructLayout(LayoutKind.Sequential)] internal struct FileWrite { public int Version; public IntPtr Callback; }
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int WriteBlock(IntPtr self,IntPtr data,uint count);
    [DllImport("pdfium.dll")] internal static extern int FPDF_SaveAsCopy(IntPtr doc,ref FileWrite writer,uint flags);
    [DllImport("pdfium.dll")] internal static extern int FPDFPage_CountObjects(IntPtr page);
    [DllImport("pdfium.dll")] internal static extern IntPtr FPDFPage_GetObject(IntPtr page,int index);
    [DllImport("pdfium.dll")] internal static extern int FPDFPageObj_GetType(IntPtr obj);
    [DllImport("pdfium.dll")] internal static extern int FPDFPageObj_GetBounds(IntPtr obj,out float left,out float bottom,out float right,out float top);
    [DllImport("pdfium.dll")] internal static extern int FPDFPageObj_GetIsActive(IntPtr obj,out int active);
    [DllImport("pdfium.dll")] internal static extern int FPDFPageObj_SetIsActive(IntPtr obj,int active);
    [DllImport("pdfium.dll")] internal static extern int FPDFPageObj_GetMatrix(IntPtr obj,out Matrix matrix);
    [DllImport("pdfium.dll")] internal static extern int FPDFPageObj_SetMatrix(IntPtr obj,ref Matrix matrix);
    [DllImport("pdfium.dll")] internal static extern void FPDFPageObj_Transform(IntPtr obj,double a,double b,double c,double d,double e,double f);
    [DllImport("pdfium.dll")] internal static extern int FPDF_PageToDevice(IntPtr page,int x,int y,int w,int h,int rotation,double px,double py,out int dx,out int dy);
    [DllImport("pdfium.dll")] internal static extern int FPDF_DeviceToPage(IntPtr page,int x,int y,int w,int h,int rotation,int dx,int dy,out double px,out double py);
    [DllImport("pdfium.dll")] internal static extern IntPtr FPDFText_LoadPage(IntPtr page);
    [DllImport("pdfium.dll")] internal static extern void FPDFText_ClosePage(IntPtr page);
    [DllImport("pdfium.dll")] internal static extern uint FPDFTextObj_GetText(IntPtr obj,IntPtr textPage,IntPtr buffer,uint length);
    [DllImport("pdfium.dll")] internal static extern int FPDFTextObj_GetFontSize(IntPtr obj,out float size);
    [DllImport("pdfium.dll")] internal static extern IntPtr FPDFText_LoadFont(IntPtr doc,byte[] bytes,uint size,int type,int cid);
    [DllImport("pdfium.dll")] internal static extern void FPDFFont_Close(IntPtr font);
    [DllImport("pdfium.dll")] internal static extern IntPtr FPDFPageObj_CreateTextObj(IntPtr doc,IntPtr font,float size);
    [DllImport("pdfium.dll",CharSet=CharSet.Unicode)] internal static extern int FPDFText_SetText(IntPtr obj,string text);
    [DllImport("pdfium.dll")] internal static extern int FPDFPageObj_GetFillColor(IntPtr obj,out uint r,out uint g,out uint b,out uint a);
    [DllImport("pdfium.dll")] internal static extern int FPDFPageObj_SetFillColor(IntPtr obj,uint r,uint g,uint b,uint a);
    [DllImport("pdfium.dll")] internal static extern IntPtr FPDFPageObj_NewImageObj(IntPtr doc);
    [DllImport("pdfium.dll")] internal static extern int FPDFImageObj_SetBitmap(IntPtr pages,int count,IntPtr obj,IntPtr bitmap);
    [DllImport("pdfium.dll")] internal static extern void FPDFPage_InsertObject(IntPtr page,IntPtr obj);
    [DllImport("pdfium.dll")] internal static extern void FPDFPageObj_Destroy(IntPtr obj);
    [DllImport("pdfium.dll")] internal static extern int FPDFPage_GenerateContent(IntPtr page);
}
}
