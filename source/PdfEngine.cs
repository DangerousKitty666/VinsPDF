using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace VinsPDF {
// All calls, including disposal, belong to the single rendering thread.
internal sealed partial class PdfEngine : IDisposable {
    IntPtr document;
    readonly Dictionary<int,IntPtr> pages = new Dictionary<int,IntPtr>();
    readonly LinkedList<int> recent = new LinkedList<int>();
    public SizeF[] Sizes;
    public int Count { get { return Sizes == null ? 0 : Sizes.Length; } }
    internal static readonly object NativeGate=new object();static int users;bool disposed;
    public PdfEngine() {lock(NativeGate){if(users++==0)Native.FPDF_InitLibrary();}}
    public void Open(string path) {
        Close(); openedFile=path;groupsPrepared=false;
        byte[] utf8 = Encoding.UTF8.GetBytes(path + "\0");
        document = Native.FPDF_LoadDocument(utf8, null);
        if (document == IntPtr.Zero) {
            uint code = Native.FPDF_GetLastError();
            throw new IOException(code == 4 ? "This PDF needs a password. Password-protected files are not supported in this prototype." : "Could not open this PDF (error " + code + "). The file may be damaged or unavailable.");
        }
        int count = Native.FPDF_GetPageCount(document);
        if (count < 1) throw new IOException("This PDF has no pages.");
        Sizes = new SizeF[count];
        for (int i=0;i<count;i++) {
            double w,h;
            if (Native.FPDF_GetPageSizeByIndex(document,i,out w,out h)==0 || w<=0 || h<=0) { w=595; h=842; }
            Sizes[i]=new SizeF((float)w,(float)h);
        }
    }
    IntPtr Page(int index) {
        if (index<0 || index>=Count) throw new ArgumentOutOfRangeException("index");
        IntPtr page;
        if (pages.TryGetValue(index,out page)) { recent.Remove(index); recent.AddLast(index); return page; }
        page=Native.FPDF_LoadPage(document,index);
        if(page==IntPtr.Zero) throw new IOException("Could not read page " + (index+1));
        pages.Add(index,page); recent.AddLast(index);
        var node=recent.First;
        while(pages.Count>pinned.Count+2 && node!=null) { var next=node.Next; int old=node.Value; if(!pinned.Contains(old) && old!=index) { recent.Remove(node); Native.FPDF_ClosePage(pages[old]); pages.Remove(old); } node=next; }
        return page;
    }
    public Bitmap Render(int index,int rotation,int width,int height,int x,int y,int pageWidth,int pageHeight,Func<bool> cancelled=null) {
        if(width<1 || height<1 || (long)width*height>24000000) throw new ArgumentException("Render area is too large.");
        if(cancelled!=null&&cancelled())throw new OperationCanceledException();
        var cacheArea=new Rectangle(-x,-y,width,height);
        if(CacheEnabled){var cached=RenderCache.Get(this,index,rotation,pageWidth,pageHeight,cacheArea);if(cached!=null){CacheHits++;return cached;}}
        IntPtr page=Page(index);
        Bitmap image=new Bitmap(width,height,PixelFormat.Format32bppArgb);
        BitmapData data=null; IntPtr bitmap=IntPtr.Zero;
        try {
            data=image.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            bitmap=Native.FPDFBitmap_CreateEx(width,height,4,data.Scan0,data.Stride);
            if(bitmap==IntPtr.Zero) throw new OutOfMemoryException("Could not allocate a page image.");
            Native.FPDFBitmap_FillRect(bitmap,0,0,width,height,0xffffffff);
            Native.PauseCallback callback=delegate(IntPtr ignored) { return cancelled!=null && cancelled()?1:0; };
            var pause=new Native.Pause { version=1,callback=Marshal.GetFunctionPointerForDelegate(callback),user=IntPtr.Zero };
            try {
                int result=Native.FPDF_RenderPageBitmap_Start(bitmap,page,x,y,pageWidth,pageHeight,rotation,0x201,ref pause);
                while(result==1) {
                    if(cancelled!=null && cancelled())throw new OperationCanceledException();
                    result=Native.FPDF_RenderPage_Continue(page,ref pause);
                }
                if(result==3)throw new IOException("PDF rendering failed.");
            } finally { Native.FPDF_RenderPage_Close(page); GC.KeepAlive(callback); }
        } catch { if(bitmap!=IntPtr.Zero) Native.FPDFBitmap_Destroy(bitmap); if(data!=null) image.UnlockBits(data); image.Dispose(); throw; }
        Native.FPDFBitmap_Destroy(bitmap); image.UnlockBits(data); if(CacheEnabled)RenderCache.Add(this,index,rotation,pageWidth,pageHeight,cacheArea,image); return image;
    }
    public Bitmap Preview(int index,int rotation,int longest,Func<bool> cancelled=null) {
        SizeF s=Sizes[index]; if(rotation%2!=0) s=new SizeF(s.Height,s.Width);
        double scale=longest/Math.Max(s.Width,s.Height);
        int w=Math.Max(1,(int)Math.Round(s.Width*scale)), h=Math.Max(1,(int)Math.Round(s.Height*scale));
        return Render(index,rotation,w,h,0,0,w,h,cancelled);
    }
    public void Close() {
        RenderCache.Clear(this); ResetEditing();
        foreach(IntPtr p in pages.Values) Native.FPDF_ClosePage(p);
        pages.Clear(); recent.Clear(); Sizes=null;
        if(document!=IntPtr.Zero) Native.FPDF_CloseDocument(document);
        document=IntPtr.Zero;
    }
    public void Dispose() {lock(NativeGate){if(disposed)return;disposed=true;Close();CleanupScratch();if(--users==0)Native.FPDF_DestroyLibrary();}}
}
internal static partial class Native {
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int PauseCallback(IntPtr pause);
    [StructLayout(LayoutKind.Sequential)] internal struct Pause { internal int version; internal IntPtr callback; internal IntPtr user; }
    [DllImport("pdfium.dll")] internal static extern int FPDF_RenderPageBitmap_Start(IntPtr bitmap,IntPtr page,int x,int y,int width,int height,int rotate,int flags,ref Pause pause);
    [DllImport("pdfium.dll")] internal static extern int FPDF_RenderPage_Continue(IntPtr page,ref Pause pause);
    [DllImport("pdfium.dll")] internal static extern void FPDF_RenderPage_Close(IntPtr page);
    [DllImport("pdfium.dll")] internal static extern void FPDF_InitLibrary();
    [DllImport("pdfium.dll")] internal static extern void FPDF_DestroyLibrary();
    [DllImport("pdfium.dll")] internal static extern IntPtr FPDF_LoadDocument(byte[] path,byte[] password);
    [DllImport("pdfium.dll")] internal static extern uint FPDF_GetLastError();
    [DllImport("pdfium.dll")] internal static extern void FPDF_CloseDocument(IntPtr doc);
    [DllImport("pdfium.dll")] internal static extern int FPDF_GetPageCount(IntPtr doc);
    [DllImport("pdfium.dll")] internal static extern int FPDF_GetPageSizeByIndex(IntPtr doc,int index,out double width,out double height);
    [DllImport("pdfium.dll")] internal static extern IntPtr FPDF_LoadPage(IntPtr doc,int index);
    [DllImport("pdfium.dll")] internal static extern void FPDF_ClosePage(IntPtr page);
    [DllImport("pdfium.dll")] internal static extern IntPtr FPDFBitmap_CreateEx(int width,int height,int format,IntPtr buffer,int stride);
    [DllImport("pdfium.dll")] internal static extern void FPDFBitmap_FillRect(IntPtr bitmap,int left,int top,int width,int height,uint color);
    [DllImport("pdfium.dll")] internal static extern void FPDFBitmap_Destroy(IntPtr bitmap);
    [DllImport("pdfium.dll")] internal static extern void FPDF_RenderPageBitmap(IntPtr bitmap,IntPtr page,int x,int y,int width,int height,int rotate,int flags);
}
}
