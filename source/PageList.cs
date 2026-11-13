using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed class PageList:Control {
 public readonly List<object> Items=new List<object>();public int ItemHeight=112;public DrawMode DrawMode;public bool IntegralHeight;public BorderStyle BorderStyle;
 public event DrawItemEventHandler DrawItem;public event EventHandler SelectedIndexChanged;public event Action ViewportChanged;
 int selected=-1,top;bool scrolling;
 public PageList(){DoubleBuffered=true;SetStyle(ControlStyles.Selectable,true);TabStop=true;}
 public int TopIndex{get{return top;}set{int next=Math.Max(0,Math.Min(Math.Max(0,Items.Count-Math.Max(1,Height/ItemHeight)),value));if(next==top)return;top=next;Invalidate();if(ViewportChanged!=null)ViewportChanged();}}
 public int SelectedIndex{get{return selected;}set{if(value==selected)return;selected=value;if(selected<top)TopIndex=selected;else if(selected>=top+Math.Max(1,Height/ItemHeight))TopIndex=selected-Math.Max(1,Height/ItemHeight)+1;Invalidate();if(SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty);}}
 public void BeginUpdate(){}public void EndUpdate(){top=0;Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);for(int index=top;index<Items.Count&&((index-top)*ItemHeight)<Height;index++){if(DrawItem!=null)DrawItem(this,new DrawItemEventArgs(e.Graphics,Font,new Rectangle(0,(index-top)*ItemHeight,Width-9,ItemHeight),index,index==selected?DrawItemState.Selected:DrawItemState.None));}if(Items.Count*ItemHeight>Height){e.Graphics.FillRectangle(Brushes.Black,Width-8,0,8,Height);float h=Math.Max(24,Height*(Height/(float)(Items.Count*ItemHeight)));float y=top/(float)Math.Max(1,Items.Count-Math.Max(1,Height/ItemHeight))*(Height-h);using(var b=new SolidBrush(Color.FromArgb(75,100,120)))e.Graphics.FillRectangle(b,Width-7,y,6,h);}}
 void Seek(int y){TopIndex=(int)Math.Round(Math.Max(0,Math.Min(1,y/(double)Math.Max(1,Height)))*Math.Max(0,Items.Count-Height/ItemHeight));}
 protected override void OnMouseDown(MouseEventArgs e){Focus();if(e.X>=Width-10){scrolling=true;Capture=true;Seek(e.Y);}else{int index=top+e.Y/ItemHeight;if(index<Items.Count)SelectedIndex=index;}}
 protected override void OnMouseMove(MouseEventArgs e){if(scrolling)Seek(e.Y);}
 protected override void OnMouseUp(MouseEventArgs e){scrolling=false;Capture=false;}
 protected override void OnMouseWheel(MouseEventArgs e){TopIndex-=Math.Sign(e.Delta)*3;}
 protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Down)SelectedIndex=Math.Min(Items.Count-1,selected+1);else if(e.KeyCode==Keys.Up)SelectedIndex=Math.Max(0,selected-1);else base.OnKeyDown(e);}
}
}
