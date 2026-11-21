using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
namespace VinsPDF {
internal sealed partial class PdfEngine {
 public static int[] PageRange(string value,int count){var result=new List<int>();foreach(string part in value.Split(',')){string[] range=part.Trim().Split('-');int a,b;if(range.Length>2||!int.TryParse(range[0],out a))throw new IOException("Use page numbers such as 1,3,5-8.");b=a;if(range.Length==2&&!int.TryParse(range[1],out b))throw new IOException("Invalid page range.");if(a<1||b<1||a>count||b>count)throw new IOException("Page number is outside this document.");int step=a<=b?1:-1;for(int i=a;;i+=step){result.Add(i-1);if(i==b)break;}}if(result.Count==0)throw new IOException("Select at least one page.");return result.ToArray();}
 void PrepareExport(){RestoreLayerVisibility();foreach(int i in pinned)Generate(i);RenderCache.Clear(this);}
 static void WriteDocument(IntPtr doc,string target){target=Path.GetFullPath(target);string temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write)){Exception failure=null;Native.WriteBlock callback=delegate(IntPtr self,IntPtr data,uint count){try{byte[] b=new byte[count];Marshal.Copy(data,b,0,b.Length);stream.Write(b,0,b.Length);return 1;}catch(Exception e){failure=e;return 0;}};var writer=new Native.FileWrite{Version=1,Callback=Marshal.GetFunctionPointerForDelegate(callback)};int ok=Native.FPDF_SaveAsCopy(doc,ref writer,2);GC.KeepAlive(callback);if(failure!=null)throw failure;if(ok==0)throw new IOException("Could not export PDF.");stream.Flush(true);}if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);}finally{if(File.Exists(temp))File.Delete(temp);}}
 public void ExportCopy(string target){PrepareExport();WriteDocument(document,target);}
 public void ExportPages(string target,int[] order,int[] turns=null){if(order.Length==0)throw new IOException("Keep at least one page.");PrepareExport();IntPtr dest=Native.FPDF_CreateNewDocument();try{Check(Native.FPDF_ImportPagesByIndex(dest,document,order,(uint)order.Length,0)!=0,"Could not copy pages.");if(turns!=null)for(int i=0;i<order.Length;i++){IntPtr p=Native.FPDF_LoadPage(dest,i);try{Native.FPDFPage_SetRotation(p,(Native.FPDFPage_GetRotation(p)+turns[i]+4)%4);}finally{Native.FPDF_ClosePage(p);}}WriteDocument(dest,target);}finally{Native.FPDF_CloseDocument(dest);}}
 public void Combine(string target,string[] paths){IntPtr dest=Native.FPDF_CreateNewDocument();try{foreach(string path in paths)using(var src=new PdfEngine()){src.Open(path);Check(Native.FPDF_ImportPages(dest,src.document,null,Native.FPDF_GetPageCount(dest))!=0,"Could not import "+Path.GetFileName(path));}WriteDocument(dest,target);}finally{Native.FPDF_CloseDocument(dest);}}
 public void ExportReduced(string target,int dpi){PrepareExport();using(var dest=new PdfEngine()){dest.document=Native.FPDF_CreateNewDocument();dest.Sizes=(SizeF[])Sizes.Clone();for(int i=0;i<Count;i++){int w=Math.Max(1,(int)(Sizes[i].Width*dpi/72)),h=Math.Max(1,(int)(Sizes[i].Height*dpi/72));double factor=Math.Min(1,Math.Sqrt(18000000.0/((double)w*h)));w=(int)(w*factor);h=(int)(h*factor);using(var image=Render(i,0,w,h,0,0,w,h))using(var jpeg=new MemoryStream()){var codec=ImageCodecInfo.GetImageEncoders().First(c=>c.MimeType=="image/jpeg");using(var parameters=new EncoderParameters(1)){parameters.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,80L);image.Save(jpeg,codec,parameters);}byte[] bytes=jpeg.ToArray();Native.GetBlock read=delegate(IntPtr param,uint offset,IntPtr buffer,uint size){if((ulong)offset+size>(ulong)bytes.Length)return 0;Marshal.Copy(bytes,(int)offset,buffer,(int)size);return 1;};var access=new Native.FileAccess{Length=(uint)bytes.Length,Read=Marshal.GetFunctionPointerForDelegate(read)};IntPtr page=Native.FPDFPage_New(dest.document,i,Sizes[i].Width,Sizes[i].Height),obj=Native.FPDFPageObj_NewImageObj(dest.document);try{Check(Native.FPDFImageObj_LoadJpegFileInline(IntPtr.Zero,0,obj,ref access)!=0,"JPEG export failed.");Native.FPDFPageObj_Transform(obj,Sizes[i].Width,0,0,Sizes[i].Height,0,0);Native.FPDFPage_InsertObject(page,obj);obj=IntPtr.Zero;Check(Native.FPDFPage_GenerateContent(page)!=0,"Page export failed.");}finally{if(obj!=IntPtr.Zero)Native.FPDFPageObj_Destroy(obj);Native.FPDF_ClosePage(page);GC.KeepAlive(read);}}}WriteDocument(dest.document,target);}}
 public void PaintPrintPage(int index,PrintPageEventArgs args){SizeF size=Sizes[index];float factor=Math.Min(args.MarginBounds.Width/size.Width,args.MarginBounds.Height/size.Height);RectangleF box=new RectangleF(args.MarginBounds.Left+(args.MarginBounds.Width-size.Width*factor)/2,args.MarginBounds.Top+(args.MarginBounds.Height-size.Height*factor)/2,size.Width*factor,size.Height*factor);int w=Math.Max(1,(int)(box.Width*3)),h=Math.Max(1,(int)(box.Height*3));using(var image=Render(index,0,w,h,0,0,w,h))args.Graphics.DrawImage(image,box);}
 public void Print(PrintDocument job,int first,int last){int index=first;job.PrintPage+=delegate(object sender,PrintPageEventArgs args){PaintPrintPage(index,args);args.HasMorePages=++index<=last;};job.Print();}
}
internal static partial class Native {
 [DllImport("pdfium.dll")]internal static extern IntPtr FPDF_CreateNewDocument();
 [DllImport("pdfium.dll")]internal static extern IntPtr FPDFPage_New(IntPtr doc,int index,double w,double h);
 [DllImport("pdfium.dll")]internal static extern int FPDF_ImportPagesByIndex(IntPtr dest,IntPtr src,int[] indices,uint length,int index);
 [DllImport("pdfium.dll",CharSet=CharSet.Ansi)]internal static extern int FPDF_ImportPages(IntPtr dest,IntPtr src,string range,int index);
 [DllImport("pdfium.dll")]internal static extern int FPDFPage_GetRotation(IntPtr page);
 [DllImport("pdfium.dll")]internal static extern void FPDFPage_SetRotation(IntPtr page,int rotation);
 [StructLayout(LayoutKind.Sequential)]internal struct FileAccess{public uint Length;public IntPtr Read,Param;}
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]internal delegate int GetBlock(IntPtr param,uint offset,IntPtr buffer,uint size);
 [DllImport("pdfium.dll")]internal static extern int FPDFImageObj_LoadJpegFileInline(IntPtr pages,int count,IntPtr image,ref FileAccess access);
}
}
