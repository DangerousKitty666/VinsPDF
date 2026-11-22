using System;
using System.Drawing;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed class DarkComboBox : ComboBox {
 public DarkComboBox(){FlatStyle=FlatStyle.Flat;DropDownStyle=ComboBoxStyle.DropDownList;BackColor=Color.FromArgb(29,34,39);ForeColor=Color.Gainsboro;DrawMode=DrawMode.OwnerDrawFixed;}
 protected override void OnDrawItem(DrawItemEventArgs e){using(var b=new SolidBrush((e.State&DrawItemState.Selected)!=0?Color.FromArgb(46,71,91):BackColor))e.Graphics.FillRectangle(b,e.Bounds);if(e.Index>=0)TextRenderer.DrawText(e.Graphics,Items[e.Index].ToString(),Font,e.Bounds,Enabled?ForeColor:Color.Gray,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);}
 protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==15||m.Msg==0x317||m.Msg==0x318){using(var g=m.Msg==15?Graphics.FromHwnd(Handle):Graphics.FromHdc(m.WParam)){var r=new Rectangle(Width-19,1,18,Height-2);using(var b=new SolidBrush(BackColor))g.FillRectangle(b,r);using(var p=new Pen(Enabled?ForeColor:Color.Gray)){g.DrawLines(p,new[]{new Point(Width-14,Height/2-2),new Point(Width-10,Height/2+2),new Point(Width-6,Height/2-2)});}using(var p=new Pen(Color.FromArgb(66,75,83)))g.DrawRectangle(p,0,0,Width-1,Height-1);}}}
}
}
