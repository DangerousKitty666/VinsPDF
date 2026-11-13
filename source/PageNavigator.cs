using System;
using System.Drawing;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed class PageNavigator : Control {
    int current,count;bool dragging;public event Action<int> Jump;public event Action<int> Wheel;
    public PageNavigator(){Width=44;Dock=DockStyle.Right;BackColor=Color.FromArgb(8,8,8);ForeColor=Color.Silver;DoubleBuffered=true;Cursor=Cursors.Hand;AccessibleName="Page navigator";}
    public void SetPage(int index,int total){current=index;count=total;Invalidate();}
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;TextRenderer.DrawText(g,"▲",Font,new Rectangle(0,6,Width,22),ForeColor,TextFormatFlags.HorizontalCenter);TextRenderer.DrawText(g,count==0?"–":(current+1).ToString(),Font,new Rectangle(0,32,Width,20),Color.White,TextFormatFlags.HorizontalCenter);TextRenderer.DrawText(g,"/ "+count,Font,new Rectangle(0,52,Width,20),ForeColor,TextFormatFlags.HorizontalCenter);TextRenderer.DrawText(g,"▼",Font,new Rectangle(0,Height-28,Width,22),ForeColor,TextFormatFlags.HorizontalCenter);int top=84,h=Math.Max(1,Height-124);using(var p=new Pen(Color.FromArgb(55,55,55),3))g.DrawLine(p,Width/2,top,Width/2,top+h);if(count>0){float y=top+(count==1?0:current/(float)(count-1))*h;using(var b=new SolidBrush(Color.FromArgb(105,180,235)))g.FillRectangle(b,Width/2-6,y-9,12,18);}}
    void Seek(int y){if(count>0&&Jump!=null)Jump(Math.Max(0,Math.Min(count-1,(int)Math.Round((y-84)/(float)Math.Max(1,Height-124)*(count-1)))));}
    protected override void OnMouseDown(MouseEventArgs e){if(e.Y<30){if(Jump!=null)Jump(Math.Max(0,current-1));}else if(e.Y>Height-32){if(Jump!=null)Jump(Math.Min(count-1,current+1));}else{dragging=true;Capture=true;Seek(e.Y);}}
    protected override void OnMouseMove(MouseEventArgs e){if(dragging)Seek(e.Y);}
    protected override void OnMouseUp(MouseEventArgs e){dragging=false;Capture=false;}
    protected override void OnMouseWheel(MouseEventArgs e){if(Wheel!=null)Wheel(e.Delta);}
}
}
