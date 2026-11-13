using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed partial class MainForm {
 void AddRotationButton(bool clockwise,string tip,Action action,bool needsDocument){
  var image=new Bitmap(22,22);using(var g=Graphics.FromImage(image)){g.SmoothingMode=SmoothingMode.AntiAlias;if(!clockwise){g.TranslateTransform(22,0);g.ScaleTransform(-1,1);}using(var p=new Pen(Color.Gainsboro,1.8f))g.DrawArc(p,4,4,14,14,40,290);g.FillPolygon(Brushes.Gainsboro,new[]{new PointF(19,8),new PointF(13,8),new PointF(18,13)});}
  var button=new ToolStripButton("90°",image){ToolTipText=tip,DisplayStyle=ToolStripItemDisplayStyle.ImageAndText,ForeColor=ForeColor,ImageScaling=ToolStripItemImageScaling.None,Padding=new Padding(1,0,1,0)};button.Click+=delegate{action();};bar.Items.Add(button);if(needsDocument)documentTools.Add(button);
 }
}
}
