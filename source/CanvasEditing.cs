using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed partial class PageCanvas {
 public int ToolMode; public void SetTool(int mode){ToolMode=mode;SelectTool=mode!=2;Cursor=mode==0?Cursors.Cross:mode==1?Cursors.SizeAll:Cursors.Hand;Invalidate();}
 public bool InlineActive;
 public bool Editing,SelectTool=true,HighlightObjects=true,GroupLines=true,GridVisible,SnapGrid;
 public float GridMm=10; public int GridAlpha=65;
 public List<EditObject> Objects=new List<EditObject>(),Selection=new List<EditObject>(); public EditObject Selected;
 public event Action<EditObject> SelectionChanged; public event Action<EditObject,RectangleF> ObjectMoved; public event Action<EditObject> TextEditRequested;
 public event Action<float> ObjectRotated;bool rotating;float rotateStart,rotateDelta;
 bool objectDrag,marquee; int resizeHandle=-1; Point objectStart,marqueeEnd; RectangleF startBounds,dragBounds; EditObject moving;
 List<EditObject> rawObjects=new List<EditObject>();
 public void SelectMembers(int[] ids){Selection=TextParagraphs.Group(TextLines.Group(rawObjects.Where(o=>ids.Contains(o.Id)).ToList(),PageSize,GroupLines),PageSize,GroupLines);Selected=Selection.FirstOrDefault();NotifySelection();}
 public void SetObjects(EditState state,int selectedId) {rawObjects=state.Objects;Objects=TextParagraphs.Group(TextLines.Group(state.Objects,PageSize,GroupLines),PageSize,GroupLines);Selected=Objects.Find(o=>o.Ids.Contains(selectedId));Selection.Clear();if(Selected!=null)Selection.Add(Selected);objectDrag=marquee=false;NotifySelection();}
 public void ClearObjects() {Objects.Clear();Selection.Clear();Selected=null;objectDrag=marquee=false;NotifySelection();}
 void NotifySelection() {Cursor=Editing?(ToolMode==0?Cursors.Cross:ToolMode==1?Cursors.SizeAll:Cursors.Hand):Cursors.Hand;Invalidate();if(SelectionChanged!=null)SelectionChanged(Selected);}
 public EditObject CombinedSelection() {
  var chosen=Selection.Count>0?Selection:(Selected==null?new List<EditObject>():new List<EditObject>{Selected});if(chosen.Count==0)return null;if(chosen.Count==1)return chosen[0];
  RectangleF bounds=chosen[0].Bounds;foreach(var o in chosen.Skip(1))bounds=RectangleF.Union(bounds,o.Bounds);
  return new EditObject {Runs=TextRuns030.Join(chosen.OrderBy(o=>o.Bounds.Top).ThenBy(o=>o.Bounds.Left),string.Join("\n",chosen.OrderBy(o=>o.Bounds.Top).ThenBy(o=>o.Bounds.Left).Select(o=>o.Text))),Id=chosen[0].Id,Style=chosen[0].Style,Color=chosen[0].Color,Members=chosen.SelectMany(o=>o.Ids).Distinct().ToArray(),Bounds=bounds,Kind=chosen.All(o=>o.Kind==1)?1:0,BaselineX=chosen.OrderBy(o=>o.Bounds.Top).First().BaselineX,Angle=chosen.OrderBy(o=>o.Bounds.Top).First().Angle,BaselineY=chosen.OrderBy(o=>o.Bounds.Top).First().BaselineY,FontSize=chosen[0].FontSize,DisplayFontSize=chosen[0].DisplayFontSize,Text=string.Join("\n",chosen.OrderBy(o=>o.Bounds.Top).ThenBy(o=>o.Bounds.Left).Select(o=>o.Text).ToArray())};
 }
 public PointF Normalized(Point pt) {Rectangle p=PageRectangle;return new PointF((pt.X-p.X)/(float)p.Width,(pt.Y-p.Y)/(float)p.Height);}
 Rectangle ScreenRect(RectangleF b) {Rectangle p=PageRectangle;return Rectangle.Round(new RectangleF(p.X+b.X*p.Width,p.Y+b.Y*p.Height,b.Width*p.Width,b.Height*p.Height));}
 internal Rectangle SelectionRect(RectangleF b){Rectangle r=ScreenRect(b);if(r.Height<24)r.Inflate(0,(24-r.Height+1)/2);if(r.Width<24)r.Inflate((24-r.Width+1)/2,0);return r;}
 Point[] HandlePoints(RectangleF b) {Rectangle r=SelectionRect(b);int mx=(r.Left+r.Right)/2,my=(r.Top+r.Bottom)/2;return new[]{new Point(r.Left,r.Top),new Point(mx,r.Top),new Point(r.Right,r.Top),new Point(r.Right,my),new Point(r.Right,r.Bottom),new Point(mx,r.Bottom),new Point(r.Left,r.Bottom),new Point(r.Left,my)};}
 int HitHandle(RectangleF b,Point pt) {var points=HandlePoints(b);for(int i=0;i<8;i++)if(new Rectangle(points[i].X-6,points[i].Y-6,12,12).Contains(pt))return i;return -1;}
 bool EditMouseDown(MouseEventArgs e) {
  if(InlineActive)return true;
  if(!Editing||!SelectTool||e.Button!=MouseButtons.Left||(ModifierKeys&Keys.Alt)!=0)return false;
  moving=CombinedSelection();bool special=moving!=null&&HitSelectionControl(moving.Bounds,e.Location);if(ToolMode==0&&!special){if(e.Clicks==2){var hit=Objects.Where(o=>o.Kind==1&&!o.Locked&&ScreenRect(o.Bounds).Contains(e.Location)).OrderBy(o=>o.Bounds.Width*o.Bounds.Height).FirstOrDefault();if(hit!=null&&TextEditRequested!=null)TextEditRequested(hit);return true;}if((ModifierKeys&Keys.Shift)==0)Selection.Clear();Selected=null;marquee=true;objectStart=marqueeEnd=e.Location;Capture=true;NotifySelection();return true;}
  moving=CombinedSelection();if(moving==null)return true;
  if(moving!=null){Rectangle selected=SelectionRect(moving.Bounds);Point handle=new Point((selected.Left+selected.Right)/2,selected.Top-25);if(new Rectangle(handle.X-9,handle.Y-9,18,18).Contains(e.Location)){rotating=true;Capture=true;rotateStart=AngleTo(e.Location,moving.Bounds);rotateDelta=0;return true;}}
  resizeHandle=moving==null?-1:HitHandle(moving.Bounds,e.Location);
  Rectangle selectedRect=moving==null?Rectangle.Empty:ScreenRect(moving.Bounds);bool centerMove=moving!=null && new Rectangle((selectedRect.Left+selectedRect.Right)/2-6,(selectedRect.Top+selectedRect.Bottom)/2-6,12,12).Contains(e.Location);
  if(centerMove)resizeHandle=-1;
  if(resizeHandle<0 && !centerMove && !selectedRect.Contains(e.Location))return true;
   if(e.Clicks==2&&moving!=null&&moving.Kind==1){if(TextEditRequested!=null)TextEditRequested(moving);return true;}
  if(moving!=null){objectDrag=true;Capture=true;objectStart=e.Location;startBounds=moving.Bounds;dragBounds=startBounds;}Invalidate();return true;
 }
 bool RotationHit(RectangleF b,Point p){Rectangle r=SelectionRect(b);return new Rectangle((r.Left+r.Right)/2-9,r.Top-34,18,18).Contains(p);}
 bool HitSelectionControl(RectangleF b,Point p){Rectangle r=ScreenRect(b);return RotationHit(b,p)||HitHandle(b,p)>=0||new Rectangle((r.Left+r.Right)/2-7,(r.Top+r.Bottom)/2-7,14,14).Contains(p);}
 float SnapX(float v){float s=GridMm*72/25.4f/PageSize.Width;return SnapGrid?(float)Math.Round(v/s)*s:v;}
 float SnapY(float v){float s=GridMm*72/25.4f/PageSize.Height;return SnapGrid?(float)Math.Round(v/s)*s:v;}
 float AngleTo(Point point,RectangleF box){Rectangle r=ScreenRect(box);return(float)(Math.Atan2(point.Y-(r.Top+r.Bottom)/2.0,point.X-(r.Left+r.Right)/2.0)*180/Math.PI);}
 bool EditMouseMove(MouseEventArgs e) {
  if(rotating){rotateDelta=AngleTo(e.Location,moving.Bounds)-rotateStart;if((ModifierKeys&Keys.Shift)!=0)rotateDelta=(float)Math.Round(rotateDelta/15)*15;Invalidate();return true;}
  if(marquee){marqueeEnd=e.Location;Invalidate();return true;}
  if(!objectDrag){if(Editing&&ToolMode!=2){var s=CombinedSelection();int h=s==null?-1:HitHandle(s.Bounds,e.Location);Rectangle cr=s==null?Rectangle.Empty:ScreenRect(s.Bounds);bool centre=s!=null&&new Rectangle((cr.Left+cr.Right)/2-7,(cr.Top+cr.Bottom)/2-7,14,14).Contains(e.Location);bool turn=s!=null&&RotationHit(s.Bounds,e.Location);Cursor=centre||turn?Cursors.Hand:h<0?(ToolMode==0?Cursors.Cross:Cursors.SizeAll):(h==1||h==5)?Cursors.SizeNS:(h==3||h==7)?Cursors.SizeWE:(h==0||h==4)?Cursors.SizeNWSE:Cursors.SizeNESW;}return false;}
  Rectangle p=PageRectangle;float dx=(e.X-objectStart.X)/(float)p.Width,dy=(e.Y-objectStart.Y)/(float)p.Height;
  if(resizeHandle>=0){float l=startBounds.Left,t=startBounds.Top,r=startBounds.Right,b=startBounds.Bottom;
   if(resizeHandle==0||resizeHandle==6||resizeHandle==7)l=SnapX(l+dx);if(resizeHandle==2||resizeHandle==3||resizeHandle==4)r=SnapX(r+dx);
   if(resizeHandle==0||resizeHandle==1||resizeHandle==2)t=SnapY(t+dy);if(resizeHandle==4||resizeHandle==5||resizeHandle==6)b=SnapY(b+dy);
   if(r-l<.001f||b-t<.001f)return true;
   if(resizeHandle%2==0&&(ModifierKeys&Keys.Shift)==0){float f=Math.Max((r-l)/startBounds.Width,(b-t)/startBounds.Height),w=startBounds.Width*f,h=startBounds.Height*f;if(resizeHandle==0||resizeHandle==6)l=r-w;else r=l+w;if(resizeHandle==0||resizeHandle==2)t=b-h;else b=t+h;}
   dragBounds=RectangleF.FromLTRB(l,t,r,b);
  }else dragBounds=new RectangleF(SnapX(startBounds.X+dx),SnapY(startBounds.Y+dy),startBounds.Width,startBounds.Height);
  Invalidate();return true;
 }
 bool EditMouseUp(MouseEventArgs e) {
  if(rotating){rotating=false;Capture=false;if(ObjectRotated!=null&&Math.Abs(rotateDelta)>.1)ObjectRotated(rotateDelta);Invalidate();return true;}
  if(marquee){marquee=false;Capture=false;PointF a=Normalized(objectStart),b=Normalized(e.Location);var r=RectangleF.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));foreach(var o in Objects)if(!o.Locked&&(r.IntersectsWith(o.Bounds)||(Math.Abs(e.X-objectStart.X)+Math.Abs(e.Y-objectStart.Y)<3&&ScreenRect(o.Bounds).Contains(e.Location)))&&!Selection.Contains(o))Selection.Add(o);Selected=Selection.FirstOrDefault();NotifySelection();return true;}
  if(!objectDrag)return false;objectDrag=false;Capture=false;if(Math.Abs(e.X-objectStart.X)+Math.Abs(e.Y-objectStart.Y)>2&&ObjectMoved!=null)ObjectMoved(moving,dragBounds);NotifySelection();return true;
 }
 void PaintSelection(Graphics g) {
  Rectangle page=PageRectangle;var state=g.Save();g.SetClip(page);
  if(GridVisible){float step=GridMm*72/25.4f*(float)Scale;if(step>=4)using(var pen=new Pen(Color.FromArgb(GridAlpha,30,130,185))){for(float x=page.Left;x<page.Right;x+=step)g.DrawLine(pen,x,page.Top,x,page.Bottom);for(float y=page.Top;y<page.Bottom;y+=step)g.DrawLine(pen,page.Left,y,page.Right,y);}}
  if(Editing&&HighlightObjects)using(var pen=new Pen(Color.FromArgb(85,20,125,200)))foreach(var o in Objects){Rectangle r=ScreenRect(o.Bounds);if(r.IntersectsWith(ClientRectangle)&&r.Width>0&&r.Height>0)g.DrawRectangle(pen,r);}
  g.Restore(state);if(!Editing)return;
  if(marquee){Rectangle r=Rectangle.FromLTRB(Math.Min(objectStart.X,marqueeEnd.X),Math.Min(objectStart.Y,marqueeEnd.Y),Math.Max(objectStart.X,marqueeEnd.X),Math.Max(objectStart.Y,marqueeEnd.Y));using(var brush=new SolidBrush(Color.FromArgb(40,30,160,255)))g.FillRectangle(brush,r);using(var pen=new Pen(Color.DeepSkyBlue)){pen.DashStyle=DashStyle.Dash;g.DrawRectangle(pen,r);}}
  var selection=CombinedSelection();if(selection==null)return;RectangleF box=objectDrag?dragBounds:selection.Bounds;Rectangle bounds=SelectionRect(box);
  using(var pen=new Pen(Color.White,2))g.DrawRectangle(pen,bounds);using(var pen=new Pen(Color.Black,1)){pen.DashStyle=DashStyle.Dash;g.DrawRectangle(pen,bounds);}
  g.SmoothingMode=SmoothingMode.AntiAlias;foreach(Point pt in HandlePoints(box)){g.FillEllipse(Brushes.White,pt.X-4,pt.Y-4,8,8);g.DrawEllipse(Pens.Black,pt.X-4,pt.Y-4,8,8);}
  Point rotation=new Point((bounds.Left+bounds.Right)/2,bounds.Top-25);g.DrawLine(Pens.DodgerBlue,rotation.X,rotation.Y+8,rotation.X,bounds.Top);g.FillEllipse(Brushes.White,rotation.X-8,rotation.Y-8,16,16);using(var rp=new Pen(Color.DodgerBlue,2))g.DrawArc(rp,rotation.X-5,rotation.Y-5,10,10,30,290);g.FillPolygon(Brushes.DodgerBlue,new[]{new Point(rotation.X+7,rotation.Y-3),new Point(rotation.X+1,rotation.Y-3),new Point(rotation.X+6,rotation.Y+2)});if(rotating){var saved=g.Save();g.TranslateTransform((bounds.Left+bounds.Right)/2f,(bounds.Top+bounds.Bottom)/2f);g.RotateTransform(rotateDelta);using(var outline=new Pen(Color.LimeGreen,2)){outline.DashStyle=DashStyle.Dash;g.DrawRectangle(outline,-bounds.Width/2f,-bounds.Height/2f,bounds.Width,bounds.Height);}g.Restore(saved);}if(rotating)TextRenderer.DrawText(g,rotateDelta.ToString("0")+"°",Font,new Point(rotation.X+12,rotation.Y-8),Color.DeepSkyBlue);
  Point c=new Point((bounds.Left+bounds.Right)/2,(bounds.Top+bounds.Bottom)/2);g.FillEllipse(Brushes.White,c.X-4,c.Y-4,8,8);g.DrawEllipse(Pens.Black,c.X-4,c.Y-4,8,8);g.DrawLine(Pens.Black,c.X-6,c.Y,c.X+6,c.Y);g.DrawLine(Pens.Black,c.X,c.Y-6,c.X,c.Y+6);
 }
 internal void TestGesture(bool resize,int dx,int dy){if(Selected==null)throw new Exception("No selection");Rectangle r=SelectionRect(Selected.Bounds);Point p=resize?new Point(r.Right,r.Bottom):new Point((r.Left+r.Right)/2,(r.Top+r.Bottom)/2);TestDrag(p,new Point(p.X+dx,p.Y+dy));}
 internal void TestRotationHandle(float degrees){var selected=CombinedSelection();Rectangle r=SelectionRect(selected.Bounds);Point c=new Point((r.Left+r.Right)/2,(r.Top+r.Bottom)/2);Point a=new Point(c.X,r.Top-25);double radius=c.Y-a.Y,angle=degrees*Math.PI/180;TestDrag(a,new Point(c.X+(int)(radius*Math.Sin(angle)),c.Y-(int)(radius*Math.Cos(angle))));}
 internal string GestureTrace,RotationPreviewPath;
 internal void TestHover(Point p){OnMouseMove(new MouseEventArgs(MouseButtons.None,0,p.X,p.Y,0));}
 internal void TestDrag(Point a,Point b){GestureTrace="enabled="+Enabled+" editing="+Editing+" select="+SelectTool;OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,a.X,a.Y,0));GestureTrace+=" down="+objectDrag+" handle="+resizeHandle;OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,b.X,b.Y,0));GestureTrace+=" bounds="+dragBounds;if(rotating&&RotationPreviewPath!=null)using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(RotationPreviewPath);}OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,b.X,b.Y,0));}
}
}
