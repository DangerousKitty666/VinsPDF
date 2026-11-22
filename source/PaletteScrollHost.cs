using System;
using System.Drawing;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed class PaletteScrollHost : Panel {
 public readonly FlowLayoutPanel Content=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=false,Padding=new Padding(5),Width=222};
 public event Action<int> ExtentChanged;int offset,extent;bool arranging,dragging;int startY,startOffset;
 public PaletteScrollHost(){Dock=DockStyle.Fill;DoubleBuffered=true;BackColor=Color.Black;Controls.Add(Content);Content.Layout+=delegate{Arrange();};Resize+=delegate{Arrange();};Content.MouseWheel+=delegate(object s,MouseEventArgs e){ScrollBy(-e.Delta/3);};}
 public void Arrange(){if(arranging)return;arranging=true;int h=Content.Padding.Vertical;foreach(Control c in Content.Controls)if(c.Visible)h+=c.Height+c.Margin.Vertical;extent=h;Content.Height=h;Content.Width=Width-10;offset=Math.Max(0,Math.Min(offset,extent-Height));Content.Location=new Point(0,-offset);arranging=false;Invalidate();if(ExtentChanged!=null)ExtentChanged(h);}
 public void ScrollBy(int delta){offset=Math.Max(0,Math.Min(Math.Max(0,extent-Height),offset+delta));Content.Top=-offset;Invalidate();}
 public void Reveal(Control c){int top=c.Top;if(top<offset)ScrollBy(top-offset);else if(top+c.Height>offset+Height)ScrollBy(top+c.Height-offset-Height);}
 Rectangle Thumb{get{int h=Math.Max(24,Height*Height/Math.Max(1,extent));return new Rectangle(Width-8,extent<=Height?0:(Height-h)*offset/Math.Max(1,extent-Height),6,h);}}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(extent>Height){using(var b=new SolidBrush(Color.FromArgb(22,25,28)))e.Graphics.FillRectangle(b,Width-10,0,10,Height);using(var b=new SolidBrush(dragging?Color.FromArgb(120,155,180):Color.FromArgb(75,82,88)))e.Graphics.FillRectangle(b,Thumb);}}
 protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left&&e.X>=Width-10&&extent>Height){if(!Thumb.Contains(e.Location))ScrollBy(e.Y<Thumb.Top?-Height:Height);dragging=true;Capture=true;startY=e.Y;startOffset=offset;}}
 protected override void OnMouseMove(MouseEventArgs e){if(dragging){int value=startOffset+(e.Y-startY)*Math.Max(1,extent-Height)/Math.Max(1,Height-Thumb.Height);ScrollBy(value-offset);}}
 protected override void OnMouseUp(MouseEventArgs e){dragging=false;Capture=false;Invalidate();}
 protected override void OnMouseWheel(MouseEventArgs e){ScrollBy(-e.Delta/3);}
}
}
