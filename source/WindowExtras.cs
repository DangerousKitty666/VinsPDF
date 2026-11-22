using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed partial class MainForm {
 sealed class WheelRouter : IMessageFilter {
  readonly MainForm form;public WheelRouter(MainForm form){this.form=form;}
  public bool PreFilterMessage(ref Message message){if(message.Msg!=0x20a||form.IsDisposed||!form.Visible||form.editBusy)return false;if(Form.ActiveForm!=form.TopLevelControl&&Form.ActiveForm!=form&&Form.ActiveForm!=form.palette)return false;Point point=Cursor.Position;if(form.HasPalette&&form.palette.Visible&&form.palette.RectangleToScreen(form.palette.ClientRectangle).Contains(point))return false;PageCanvas target=form.canvas;if(form.secondCanvas.Visible&&form.secondCanvas.RectangleToScreen(form.secondCanvas.ClientRectangle).Contains(point))target=form.secondCanvas;else if(!form.canvas.RectangleToScreen(form.canvas.ClientRectangle).Contains(point))return false;int delta=(short)(((long)message.WParam>>16)&65535);if((Control.ModifierKeys&Keys.Control)!=0)target.Zoom(Math.Pow(1.18,delta/120.0),target.PointToClient(point));else target.ScrollWheel(delta);return true;}
 }
 void InstallWheelRouter(){var router=new WheelRouter(this);Application.AddMessageFilter(router);FormClosed+=delegate{Application.RemoveMessageFilter(router);};}
 void CaptureWorkspace(string path){
  using(var image=new Bitmap(Width,Height)){
   DrawToBitmap(image,new Rectangle(0,0,Width,Height));
   if(HasPalette&&palette.Visible)using(var tools=new Bitmap(palette.Width,palette.Height))using(Graphics graphics=Graphics.FromImage(image))using(var attributes=new ImageAttributes()){
    palette.DrawToBitmap(tools,new Rectangle(0,0,tools.Width,tools.Height));var matrix=new ColorMatrix();matrix.Matrix33=(float)palette.Opacity;attributes.SetColorMatrix(matrix);graphics.DrawImage(tools,new Rectangle(palette.Left-Left,palette.Top-Top,tools.Width,tools.Height),0,0,tools.Width,tools.Height,GraphicsUnit.Pixel,attributes);
   }
   image.Save(path);
  }
 }
}
}
