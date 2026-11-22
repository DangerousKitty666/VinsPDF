using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VinsPDF {
internal sealed partial class PageCanvas : Control {
    public event Action ViewChanged;
    public event Action<int> PageStep;
    public event Action OpenRequested;
    Bitmap preview, detail;
    Rectangle detailRect;
    Rectangle renderedPage;
    public SizeF PageSize { get; private set; }
    public int ZoomPercent { get { return (int)Math.Round(Scale/DeviceDpiScale()*100); } }
    public new double Scale=1;
    public string FitMode="page";
    float panX,panY;
    Point dragStart;
    float dragX,dragY;
    bool dragging;
    public bool HasPage;
    public string EmptyTitle="A clear view of your PDFs.";
    public PageCanvas() {
        SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);
        BackColor=Color.Black; Dock=DockStyle.Fill; TabStop=true; Cursor=Cursors.Hand;
        Resize+=delegate { if(HasPage) { if(FitMode!="") Fit(FitMode); else Changed(); } };
    }
    public void SetPage(SizeF size,Bitmap thumb) {
        HasPage=true; PageSize=size; panX=panY=0;
        SetPreview(thumb); ClearDetail();
        if(FitMode!="") Fit(FitMode); else Changed();
    }
    public void ClearPage() { HasPage=false; SetPreview(null); ClearDetail(); Invalidate(); }
    public void SetPreview(Bitmap value) { if(preview!=null) preview.Dispose(); preview=value; Invalidate(); }
    public void ClearDetail() { if(detail!=null) detail.Dispose(); detail=null; }
    public Rectangle PageRectangle {
        get {
            int w=Math.Max(1,(int)Math.Round(PageSize.Width*Scale)),h=Math.Max(1,(int)Math.Round(PageSize.Height*Scale));
            return new Rectangle((ClientSize.Width-w)/2+(int)panX,(ClientSize.Height-h)/2+(int)panY,w,h);
        }
    }
    public void SetDetail(Bitmap value,Rectangle area,Rectangle page) {
        ClearDetail(); detail=value; detailRect=area; renderedPage=page; Invalidate();
    }
    public void Fit(string mode) {
        if(!HasPage) return;
        FitMode=mode;
        Scale=Math.Max(.05,(ClientSize.Width-24)/PageSize.Width);
        if(mode=="page") Scale=Math.Min(Scale,Math.Max(.05,(ClientSize.Height-24)/PageSize.Height));
        panX=panY=0; Changed();
    }
    public void Zoom(double multiplier,Point anchor) {
        if(!HasPage) return;
        Rectangle old=PageRectangle; double previous=Scale;
        Scale=Math.Max(.08,Math.Min(12,Scale*multiplier)); FitMode="";
        double ratio=Scale/previous;
        panX=(float)(anchor.X-(anchor.X-old.X)*ratio-(ClientSize.Width-PageSize.Width*Scale)/2);
        panY=(float)(anchor.Y-(anchor.Y-old.Y)*ratio-(ClientSize.Height-PageSize.Height*Scale)/2);
        Clamp(); Changed();
    }
    public void ActualSize() { if(!HasPage)return; Zoom((DeviceDpiScale()/Scale),new Point(Width/2,Height/2)); }
    public void PanBy(float x,float y) { panX+=x; panY+=y; Clamp(); Changed(); }
    double DeviceDpiScale() { using(Graphics g=CreateGraphics()) return g.DpiX/72.0; }
    void Clamp() {
        float maxX=PageSize.Width*Scale<=Width?0:Math.Max(0,(float)(PageSize.Width*Scale-Width)/2+36);
        float maxY=PageSize.Height*Scale<=Height?0:Math.Max(0,(float)(PageSize.Height*Scale-Height)/2+36);
        panX=Math.Max(-maxX,Math.Min(maxX,panX)); panY=Math.Max(-maxY,Math.Min(maxY,panY));
    }
    void Changed() { Invalidate(); if(ViewChanged!=null) ViewChanged(); }
    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e); Focus();
        if(!HasPage) { if(e.Button==MouseButtons.Left && HomeLogoRectangle.Contains(e.Location) && OpenRequested!=null) OpenRequested(); return; }
        if(MarkDown(e)||EditMouseDown(e))return;
        if(e.Button==MouseButtons.Left || e.Button==MouseButtons.Middle) { dragging=true; Capture=true; dragStart=e.Location; dragX=panX; dragY=panY; Cursor=Cursors.SizeAll; }
    }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if(MarkMove(e)||EditMouseMove(e))return; if(dragging) { panX=dragX+e.X-dragStart.X; panY=dragY+e.Y-dragStart.Y; Clamp(); Changed(); } }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); if(MarkUp(e)||EditMouseUp(e))return; dragging=false; Capture=false; Cursor=Editing?(ToolMode==0?Cursors.Cross:ToolMode==1?Cursors.SizeAll:Cursors.Hand):Cursors.Hand; }
    protected override void OnMouseWheel(MouseEventArgs e) {
        if(!HasPage)return;
        if((ModifierKeys&Keys.Control)!=0) Zoom(Math.Pow(1.18,e.Delta/120.0),e.Location);
        else ScrollWheel(e.Delta);
    }
    public void ScrollWheel(int delta) {if(!HasPage)return;if(PageSize.Height*Scale<=Height){panY=0;if(!Editing&&delta!=0&&PageStep!=null)PageStep(delta<0?1:-1);return;}float old=panY;panY+=delta*.65f;Clamp();if(Math.Abs(panY-old)<.1f || PageSize.Height*Scale<=Height-24){if(!Editing && PageStep!=null)PageStep(delta<0?1:-1);}else Changed();}
    public void AlignScrollEdge(bool top) {float maxY=PageSize.Height*Scale<=Height?0:Math.Max(0,(float)(PageSize.Height*Scale-Height)/2+12);panY=top?maxY:-maxY;Changed();}
    protected override void OnMouseEnter(EventArgs e) {base.OnMouseEnter(e);if(Enabled && Form.ActiveForm==FindForm())Focus();}
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e); Graphics g=e.Graphics;
        if(!HasPage){PaintHome(g);return;}
        Rectangle p=PageRectangle;
        using(var shadow=new SolidBrush(Color.FromArgb(15,16,19))) g.FillRectangle(shadow,p.X+5,p.Y+6,p.Width,p.Height);
        g.FillRectangle(Brushes.White,p);
        g.InterpolationMode=InterpolationMode.HighQualityBilinear;
        if(preview!=null) g.DrawImage(preview,p);
        if(detail!=null && renderedPage.Width>0 && renderedPage.Height>0) {
            Rectangle target=new Rectangle(p.X+(int)((detailRect.X-renderedPage.X)*(double)p.Width/renderedPage.Width),p.Y+(int)((detailRect.Y-renderedPage.Y)*(double)p.Height/renderedPage.Height),Math.Max(1,(int)(detailRect.Width*(double)p.Width/renderedPage.Width)),Math.Max(1,(int)(detailRect.Height*(double)p.Height/renderedPage.Height)));
            g.DrawImage(detail,target);
        }
        using(var pen=new Pen(Color.FromArgb(70,73,79))) g.DrawRectangle(pen,p);
        PaintSelection(g);PaintTextOverlays(g);
    }
    protected override void Dispose(bool disposing) { if(disposing) { SetPreview(null); ClearDetail(); } base.Dispose(disposing); }
}
}
