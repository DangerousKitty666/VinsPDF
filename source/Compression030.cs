using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
namespace VinsPDF {
internal sealed partial class PdfEngine {
    static HashSet<string> MaskedImages(string file){var result=new HashSet<string>();var j=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256}.DeserializeObject(Encoding.UTF8.GetString(NestedText030.Capture(file,"--json=2","--json-key=qpdf","--json-stream-data=none"))) as Dictionary<string,object>;var q=(object[])j["qpdf"];foreach(var entry in (Dictionary<string,object>)q[1]){var o=entry.Value as Dictionary<string,object>;if(o==null||!o.ContainsKey("stream"))continue;var d=(Dictionary<string,object>)((Dictionary<string,object>)o["stream"])["dict"];if(d.ContainsKey("/Width")&&d.ContainsKey("/Height")&&(d.ContainsKey("/Mask")||d.ContainsKey("/SMask")||d.ContainsKey("/ImageMask")))result.Add(d["/Width"]+"x"+d["/Height"]);}return result;}
    int CompressPageImages(int index,int dpi,int quality,HashSet<string> masked){IntPtr page=Page(index);int changed=0;for(int n=0;n<Native.FPDFPage_CountObjects(page);n++){IntPtr obj=Native.FPDFPage_GetObject(page,n);if(Native.FPDFPageObj_GetType(obj)!=3)continue;IntPtr bitmap=Native.FPDFImageObj_GetBitmap(obj);if(bitmap==IntPtr.Zero)continue;try{int width=Native.FPDFBitmap_GetWidth(bitmap),height=Native.FPDFBitmap_GetHeight(bitmap),format=Native.FPDFBitmap_GetFormat(bitmap),stride=Native.FPDFBitmap_GetStride(bitmap);if(width<64||height<64||(long)width*height>50000000||masked.Contains(width+"x"+height)||format<2||format>4)continue;IntPtr buffer=Native.FPDFBitmap_GetBuffer(bitmap);if(format==4){bool transparent=false;byte[] row=new byte[stride];for(int y=0;y<height&&!transparent;y++){Marshal.Copy(IntPtr.Add(buffer,y*stride),row,0,stride);for(int x=0;x<width;x++)if(row[x*4+3]!=255){transparent=true;break;}}if(transparent)continue;}
            Native.Matrix matrix;Check(Native.FPDFPageObj_GetMatrix(obj,out matrix)!=0,"Could not read image position.");double xSize=Math.Sqrt(matrix.A*matrix.A+matrix.B*matrix.B),ySize=Math.Sqrt(matrix.C*matrix.C+matrix.D*matrix.D);int w=Math.Max(1,Math.Min(width,(int)Math.Ceiling(xSize*dpi/72))),h=Math.Max(1,Math.Min(height,(int)Math.Ceiling(ySize*dpi/72)));double ratio=Math.Min(w/(double)width,h/(double)height);w=Math.Max(1,(int)Math.Round(width*ratio));h=Math.Max(1,(int)Math.Round(height*ratio));using(var original=new Bitmap(width,height,stride,format==2?PixelFormat.Format24bppRgb:format==4?PixelFormat.Format32bppArgb:PixelFormat.Format32bppRgb,buffer))using(var resized=new Bitmap(w,h,PixelFormat.Format24bppRgb))using(var stream=new MemoryStream()){using(var g=Graphics.FromImage(resized)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(original,new Rectangle(0,0,w,h),new Rectangle(0,0,width,height),GraphicsUnit.Pixel);}SaveJpeg(resized,stream,quality);byte[] bytes=stream.ToArray();Native.GetBlock read=delegate(IntPtr p,uint offset,IntPtr target,uint length){if((ulong)offset+length>(ulong)bytes.Length)return 0;Marshal.Copy(bytes,(int)offset,target,(int)length);return 1;};var access=new Native.FileAccess{Length=(uint)bytes.Length,Read=Marshal.GetFunctionPointerForDelegate(read)};try{Check(Native.FPDFImageObj_LoadJpegFileInline(IntPtr.Zero,0,obj,ref access)!=0,"Could not compress an image.");SetObjectMatrix(obj,matrix);changed++;}finally{GC.KeepAlive(read);}}
        }finally{Native.FPDFBitmap_Destroy(bitmap);}}if(changed>0)pinned.Add(index);return changed;}
    public void ExportStrongReduced(string target,int[] order,int dpi,int quality){string source=Scratch(),selected=Scratch(),images=Scratch(),candidate=Scratch();try{ExportCopy(source);PdfTools.Run(source,"--pages",".",string.Join(",",order.Select(i=>(i+1).ToString())),"--",selected);long before=new FileInfo(selected).Length;int changed=0;var masks=MaskedImages(selected);using(var copy=new PdfEngine()){copy.Open(selected);copy.EnsureEditableGroups();for(int p=0;p<copy.Count;p++)changed+=copy.CompressPageImages(p,dpi,quality,masks);copy.ExportCopy(images);}PdfTools.Run(images,"--object-streams=generate","--compress-streams=y","--recompress-flate","--compression-level=9",candidate);using(var check=new PdfEngine()){check.Open(candidate);Check(check.Count==order.Length,"Compressed PDF page count did not match.");}long after=new FileInfo(candidate).Length;PdfTools.InstallFile(after<before?candidate:selected,target);ExportReport="Saved · "+Math.Max(0,(before-after)*100.0/before).ToString("0.0")+"% smaller · text/vectors retained · "+changed+" images compressed";}finally{ReleaseScratch(source);ReleaseScratch(selected);ReleaseScratch(images);ReleaseScratch(candidate);}}
}
internal static partial class Native {
    [DllImport("pdfium.dll")]internal static extern IntPtr FPDFImageObj_GetBitmap(IntPtr image);
    [DllImport("pdfium.dll")]internal static extern int FPDFBitmap_GetWidth(IntPtr bitmap);
    [DllImport("pdfium.dll")]internal static extern int FPDFBitmap_GetHeight(IntPtr bitmap);
    [DllImport("pdfium.dll")]internal static extern int FPDFBitmap_GetStride(IntPtr bitmap);
    [DllImport("pdfium.dll")]internal static extern int FPDFBitmap_GetFormat(IntPtr bitmap);
    [DllImport("pdfium.dll")]internal static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);
}
}

