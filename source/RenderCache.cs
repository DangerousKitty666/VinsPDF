using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
namespace VinsPDF {
// Accessed only under NativeGate. One budget for all open documents.
internal static class RenderCache {
 sealed class Entry {public PdfEngine Owner;public int Page,Rotation,W,H;public Rectangle Area;public Bitmap Image;}
 static readonly LinkedList<Entry> entries=new LinkedList<Entry>();
 public static long Bytes;public const long Limit=96L*1024*1024;
 public static Bitmap Get(PdfEngine owner,int page,int rotation,int w,int h,Rectangle area){
  for(var n=entries.First;n!=null;n=n.Next){var e=n.Value;if(e.Owner==owner&&e.Page==page&&e.Rotation==rotation&&e.W==w&&e.H==h&&e.Area.Contains(area)){entries.Remove(n);entries.AddFirst(n);return e.Image.Clone(new Rectangle(area.X-e.Area.X,area.Y-e.Area.Y,area.Width,area.Height),PixelFormat.Format32bppArgb);}}return null;
 }
 public static void Add(PdfEngine owner,int page,int rotation,int w,int h,Rectangle area,Bitmap image){long bytes=(long)image.Width*image.Height*4;if(bytes>Limit/2)return;while(Bytes+bytes>Limit&&entries.Last!=null)Remove(entries.Last);entries.AddFirst(new Entry{Owner=owner,Page=page,Rotation=rotation,W=w,H=h,Area=area,Image=(Bitmap)image.Clone()});Bytes+=bytes;}
 static void Remove(LinkedListNode<Entry> n){Bytes-=(long)n.Value.Image.Width*n.Value.Image.Height*4;n.Value.Image.Dispose();entries.Remove(n);}
 public static void Clear(PdfEngine owner){for(var n=entries.First;n!=null;){var next=n.Next;if(n.Value.Owner==owner)Remove(n);n=next;}}
}
internal sealed partial class PdfEngine {public bool CacheEnabled=true;public int CacheHits;}
}
