using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed class OrganiseGrid : Control {
 public readonly List<PageChoice> Pages=new List<PageChoice>();
 readonly Dictionary<int,Bitmap> images=new Dictionary<int,Bitmap>();
 public int Selected;int offset,pressed=-1,drop=-1;bool dragging,scrolling;Point origin;int scrollStart,scrollOffset;
 public OrganiseGrid(int count){for(int i=0;i<count;i++)Pages.Add(new PageChoice{Index=i});DoubleBuffered=true;BackColor=Color.FromArgb(14,17,20);Dock=DockStyle.Fill;TabStop=true;Resize+=delegate{Clamp();Invalidate();};}
 int Columns{get{return Math.Max(1,(Width-16)/150);}}int Extent{get{return ((Pages.Count+Columns-1)/Columns)*180+12;}}
 public int[] VisiblePages{get{return Enumerable.Range(0,Pages.Count).Where(i=>Card(i).Bottom>0&&Card(i).Top<Height).Select(i=>Pages[i].Index).ToArray();}}
 public bool HasImage(int index){return images.ContainsKey(index);}public int ReadyCount{get{return images.Count;}}
 public void SetImage(int index,Bitmap image){if(IsDisposed){if(image!=null)image.Dispose();return;}images[index]=image;Invalidate();}
 public Rectangle Card(int i){return new Rectangle(8+i%Columns*150,8+i/Columns*180-offset,138,166);}
 int Hit(Point p){if(p.X>=Width-12)return -1;for(int i=0;i<Pages.Count;i++)if(Card(i).Contains(p))return i;return -1;}
 void Clamp(){offset=Math.Max(0,Math.Min(offset,Math.Max(0,Extent-Height)));}
 Rectangle Thumb{get{int h=Math.Min(Height,Math.Max(28,Height*Height/Math.Max(1,Extent)));return new Rectangle(Width-8,(Height-h)*offset/Math.Max(1,Extent-Height),6,h);}}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);for(int i=0;i<Pages.Count;i++){var r=Card(i);if(r.Bottom<0||r.Top>Height)continue;using(var b=new SolidBrush(i==Selected?Color.FromArgb(37,61,79):Color.FromArgb(24,29,34)))g.FillRectangle(b,r);using(var p=new Pen(i==Selected?Color.FromArgb(101,168,213):Color.FromArgb(55,62,68)))g.DrawRectangle(p,r);var slot=new Rectangle(r.X+7,r.Y+7,r.Width-14,130);Bitmap image;if(images.TryGetValue(Pages[i].Index,out image)&&image!=null){bool odd=(Pages[i].Rotation%2)!=0;float ratio=Math.Min(slot.Width/(float)(odd?image.Height:image.Width),slot.Height/(float)(odd?image.Width:image.Height));var state=g.Save();g.TranslateTransform(slot.X+slot.Width/2f,slot.Y+slot.Height/2f);g.RotateTransform(Pages[i].Rotation*90);g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.DrawImage(image,-image.Width*ratio/2,-image.Height*ratio/2,image.Width*ratio,image.Height*ratio);g.Restore(state);}else TextRenderer.DrawText(g,images.ContainsKey(Pages[i].Index)?"Preview unavailable":"Loading…",Font,slot,Color.Silver,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);TextRenderer.DrawText(g,"Page "+(Pages[i].Index+1),Font,new Rectangle(r.X,r.Bottom-25,r.Width,22),Color.Gainsboro,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
 if(dragging&&drop>=0){Rectangle r=Card(Math.Min(drop,Pages.Count-1));int x=drop==Pages.Count?r.Right+5:r.Left-4;using(var p=new Pen(Color.FromArgb(109,190,241),3))g.DrawLine(p,x,r.Top,x,r.Bottom);}if(Extent>Height){using(var b=new SolidBrush(Color.FromArgb(64,75,84)))g.FillRectangle(b,Thumb);}}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;Focus();if(e.X>=Width-12&&Extent>Height){if(!Thumb.Contains(e.Location)){offset+=e.Y<Thumb.Top?-Height:Height;Clamp();Invalidate();}scrolling=true;scrollStart=e.Y;scrollOffset=offset;Capture=true;return;}pressed=Hit(e.Location);if(pressed<0)return;Selected=pressed;origin=e.Location;Capture=true;Invalidate();}
 protected override void OnMouseMove(MouseEventArgs e){if(scrolling){offset=scrollOffset+(e.Y-scrollStart)*Math.Max(1,Extent-Height)/Math.Max(1,Height-Thumb.Height);Clamp();Invalidate();return;}if(pressed<0)return;if(Math.Abs(e.X-origin.X)+Math.Abs(e.Y-origin.Y)>8)dragging=true;if(!dragging)return;Cursor=Cursors.SizeAll;if(e.Y<25)offset-=20;else if(e.Y>Height-25)offset+=20;Clamp();int hit=Hit(e.Location);drop=hit<0?(e.Y>Card(Pages.Count-1).Bottom?Pages.Count:pressed):(e.X>Card(hit).Left+Card(hit).Width/2?hit+1:hit);Invalidate();}
 protected override void OnMouseUp(MouseEventArgs e){if(dragging&&drop>=0)Reorder(pressed,drop);pressed=-1;drop=-1;dragging=false;scrolling=false;Capture=false;Cursor=Cursors.Default;Invalidate();}
 public void Reorder(int from,int insert){if(from<0||from>=Pages.Count)return;var item=Pages[from];Pages.RemoveAt(from);if(insert>from)insert--;insert=Math.Max(0,Math.Min(Pages.Count,insert));Pages.Insert(insert,item);Selected=insert;Invalidate();}
 public void Rotate(int turn){if(Pages.Count>0){Pages[Selected].Rotation=(Pages[Selected].Rotation+turn+4)%4;Invalidate();}}
 public void Remove(){if(Pages.Count<=1)return;Pages.RemoveAt(Selected);Selected=Math.Min(Selected,Pages.Count-1);Clamp();Invalidate();}
 protected override void OnMouseWheel(MouseEventArgs e){offset-=e.Delta;Clamp();Invalidate();}
 protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Delete)Remove();else if(e.Control&&e.KeyCode==Keys.Left)Reorder(Selected,Selected-1);else if(e.Control&&e.KeyCode==Keys.Right)Reorder(Selected,Selected+2);base.OnKeyDown(e);}
 protected override void Dispose(bool disposing){if(disposing)foreach(var image in images.Values)if(image!=null)image.Dispose();base.Dispose(disposing);}
}
internal sealed class OrganiseDialog : StyledDialog {
 public readonly OrganiseGrid Grid;
 public OrganiseDialog(int count):base("Organise pages",790,620){Grid=new OrganiseGrid(count);var note=new Label{Text="Drag page previews to rearrange · Select a page to rotate or remove\nSave creates a new PDF. Bookmarks and document-level features may not transfer.",Dock=DockStyle.Top,Height=52,Padding=new Padding(12,5,8,0),ForeColor=Color.Silver};var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=42,Padding=new Padding(6,0,0,0)};actions.Controls.Add(ActionButton("↶ 90°",delegate{Grid.Rotate(-1);}));actions.Controls.Add(ActionButton("↷ 90°",delegate{Grid.Rotate(1);}));actions.Controls.Add(ActionButton("Remove page",delegate{Grid.Remove();}));actions.Controls.Add(ActionButton("Save PDF…",delegate{DialogResult=DialogResult.OK;}));var cancel=ActionButton("Cancel",delegate{DialogResult=DialogResult.Cancel;});actions.Controls.Add(cancel);CancelButton=cancel;Controls.Add(Grid);Controls.Add(note);Controls.Add(actions);Ready();MinimumSize=new Size(650,450);Resizable=true;}
}
internal sealed partial class MainForm {
 void OrganisePages(){using(var dialog=new OrganiseDialog(sizes.Length))using(var timer=new Timer{Interval=120}){bool pending=false;timer.Tick+=delegate{if(pending||dialog.IsDisposed)return;int i=dialog.Grid.VisiblePages.FirstOrDefault(n=>!dialog.Grid.HasImage(n));if(dialog.Grid.HasImage(i))return;pending=true;worker.Command(delegate(PdfEngine e){Bitmap image=null;try{image=e.Preview(i,0,250);}catch{}Bitmap ready=image;Post(delegate{pending=false;dialog.Grid.SetImage(i,ready);},ready);});};dialog.Shown+=delegate{timer.Start();};DialogResult result=dialog.ShowDialog(TopLevelControl as Form??this);timer.Stop();if(result!=DialogResult.OK)return;string path=ExportPath("Organise pages");if(path==null)return;var choices=dialog.Grid.Pages.ToArray();ToolWork("Organising",delegate(PdfEngine e){e.ExportPages(path,choices.Select(p=>p.Index).ToArray(),choices.Select(p=>p.Rotation).ToArray());},path);}}
}
}
