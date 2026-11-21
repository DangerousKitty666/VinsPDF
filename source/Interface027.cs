using System;
using System.Drawing;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed class DarkRenderer : ToolStripProfessionalRenderer {
 public DarkRenderer():base(new DarkColors()){RoundedEdges=false;}
 protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){e.TextColor=e.Item.Enabled?Color.Gainsboro:Color.Gray;base.OnRenderItemText(e);}
 protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e){e.ArrowColor=e.Item.Enabled?Color.Gainsboro:Color.Gray;base.OnRenderArrow(e);}
}
internal sealed partial class MainForm {
 void QueueNextPage(int token,int current){if(canvas.Editing||current+1>=sizes.Length)return;int next=current+1,rotation=rotations[next];SizeF size=sizes[next];if(rotation%2!=0)size=new SizeF(size.Height,size.Width);double scale=Math.Min((canvas.Width-24)/size.Width,(canvas.Height-24)/size.Height);int w=Math.Max(1,(int)Math.Round(size.Width*scale)),h=Math.Max(1,(int)Math.Round(size.Height*scale));worker.Idle(delegate(PdfEngine engine){if(token!=generation||pageIndex!=current||!documentVisible||worker.HasUrgent)return;try{using(var image=engine.Render(next,rotation,w,h,0,0,w,h,delegate{return token!=generation||pageIndex!=current||!documentVisible||worker.HasUrgent;})){} }catch(OperationCanceledException){}catch{} });}
}
}
