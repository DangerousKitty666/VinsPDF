using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VinsPDF {
internal sealed class DarkColors : ProfessionalColorTable {
    public override Color ToolStripGradientBegin { get { return Color.FromArgb(12,12,12); } }
    public override Color ToolStripGradientMiddle { get { return ToolStripGradientBegin; } }
    public override Color ToolStripGradientEnd { get { return ToolStripGradientBegin; } }
    public override Color MenuStripGradientBegin { get { return ToolStripGradientBegin; } }
    public override Color MenuStripGradientEnd { get { return ToolStripGradientBegin; } }
    public override Color ToolStripDropDownBackground { get { return ToolStripGradientBegin; } }
    public override Color ImageMarginGradientBegin { get { return ToolStripGradientBegin; } }
    public override Color ImageMarginGradientMiddle { get { return ToolStripGradientBegin; } }
    public override Color ImageMarginGradientEnd { get { return ToolStripGradientBegin; } }
    public override Color MenuItemSelected { get { return Color.FromArgb(65,89,112); } }
    public override Color MenuItemSelectedGradientBegin { get { return MenuItemSelected; } }
    public override Color MenuItemSelectedGradientEnd { get { return MenuItemSelected; } }
    public override Color MenuItemPressedGradientBegin { get { return MenuItemSelected; } }
    public override Color MenuItemPressedGradientEnd { get { return MenuItemSelected; } }
    public override Color ButtonSelectedGradientBegin { get { return MenuItemSelected; } }
    public override Color ButtonSelectedGradientEnd { get { return MenuItemSelected; } }
    public override Color ToolStripBorder { get { return Color.FromArgb(61,65,72); } }
}
internal sealed partial class MainForm : DarkWindow {
    readonly RenderWorker worker=new RenderWorker();
    readonly PageCanvas canvas=new PageCanvas();
    readonly PageNavigator navigator=new PageNavigator();
    readonly PageList pages=new PageList();
    volatile int thumbnailStart,thumbnailEnd,imageEpoch;
    readonly Panel sidebar=new Panel();
    readonly Panel editorDock=new Panel{Dock=DockStyle.Right,Width=236,Visible=false,BackColor=Color.Black};
    readonly Panel documentArea=new Panel{Dock=DockStyle.Fill};
    readonly ToolStrip bar=new ToolStrip();
    readonly ToolStripTextBox pageBox=new ToolStripTextBox();
    readonly ToolStripLabel pageTotal=new ToolStripLabel("/ —");
    readonly ToolStripLabel zoomLabel=new ToolStripLabel("—");
    readonly ToolStripDropDownButton scope=new ToolStripDropDownButton("Current page");
    bool rotateAll;
    readonly ToolStripStatusLabel status=new ToolStripStatusLabel("Ready");
    readonly ToolStripStatusLabel dimensions=new ToolStripStatusLabel();
    readonly Label documentLabel=new Label();
    readonly Timer debounce=new Timer();
    readonly Dictionary<int,Bitmap> thumbs=new Dictionary<int,Bitmap>();
    readonly Dictionary<string,Bitmap> previews=new Dictionary<string,Bitmap>();
    readonly Queue<string> previewOrder=new Queue<string>();
    readonly List<ToolStripItem> documentTools=new List<ToolStripItem>();
    readonly Stopwatch openClock=new Stopwatch();
    volatile int generation;
    volatile int viewVersion;
    volatile bool thumbnailsEnabled;
    volatile bool documentVisible=true;
    int pageIndex;
    int[] rotations;
    SizeF[] sizes;
    string currentPath;
    bool selecting,closing,firstPaint;
    public static Stopwatch Startup=Stopwatch.StartNew();
    public static string TracePath;
    public static string SnapshotPath;
    public static bool TestRun;
    Timer testTimer;
    public static string SelfTestDirectory;
    int lastRenderedVersion=-1;
    public MainForm(string initialPath) {
        Text="VinsPDF"; Width=1320; Height=900; MinimumSize=new Size(860,500);
        Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition=FormStartPosition.CenterScreen; BackColor=Color.Black; ForeColor=Color.FromArgb(224,228,235);
        Font=new Font("Segoe UI",9); KeyPreview=true; AllowDrop=true;
        var renderer=new DarkRenderer(); renderer.RoundedEdges=false;
        var menu=new MenuStrip { BackColor=BackColor,ForeColor=ForeColor,Renderer=renderer,Padding=new Padding(8,2,0,2) };
        var file=new ToolStripMenuItem("&File");
        file.DropDownItems.Add(Item("&Open PDF…",delegate { ChooseFile(); },Keys.Control|Keys.O));
        file.DropDownItems.Add(Item("&Close document",delegate { if(CloseTab!=null)CloseTab();else if(ConfirmDiscard())CloseDocument(); },Keys.Control|Keys.W));
        file.DropDownItems.Add(Item("&Save",delegate { SaveEdited(false); },Keys.Control|Keys.S));
        file.DropDownItems.Add(Item("Save &as…",delegate { SaveEdited(true); },Keys.Control|Keys.Shift|Keys.S));
        file.DropDownItems.Add(Item("PDF association / default app…",delegate{Associations.Setup(this);},Keys.None));
        file.DropDownItems.Add(new ToolStripSeparator()); file.DropDownItems.Add(Item("E&xit",delegate { (TopLevelControl as Form??this).Close(); },Keys.Alt|Keys.F4));
        var view=new ToolStripMenuItem("&View");
        view.DropDownItems.Add(Item("Fit &page",delegate { canvas.Fit("page"); },Keys.Control|Keys.D0));
        view.DropDownItems.Add(Item("Fit &width",delegate { canvas.Fit("width"); },Keys.Control|Keys.D2));
        view.DropDownItems.Add(Item("&Actual size",delegate { canvas.ActualSize(); },Keys.Control|Keys.D1));
        view.DropDownItems.Add(Item("&Page thumbnails",ToggleThumbnails,Keys.F9));
        var help=new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(Item("Controls & about",delegate { DarkMessage.Show(this,"VinsPDF 0.29.1 · Viewer + editor\n\nCtrl+O Open · Ctrl+S Save · Ctrl+Shift+S Save As\nCtrl+E Edit mode · Ctrl+Z Undo · Ctrl+Y Redo\nCtrl+0 Fit page · Ctrl+2 Fit width · F9 Thumbnails · F11 Full screen · Ctrl+F Search\nCtrl+wheel Zoom · Middle-drag or Alt+drag Pan\n\nEdit: select text or an image. Drag its centre handle to move.\nDrag the corner to resize; Shift allows free proportions.\nDouble-click text to edit. Delete removes the selection.\nText blocks are recognised geometrically. Nested form content is not editable.\nToolbar rotation changes the view. Selection rotation is saved.\nInline text: Ctrl+Enter applies; Esc cancels.","VinsPDF",MessageBoxButtons.OK,MessageBoxIcon.Information); },Keys.None));
        menu.Items.AddRange(new ToolStripItem[]{file,view,help}); MainMenuStrip=menu;
        bar.Renderer=renderer; bar.BackColor=BackColor; bar.ForeColor=ForeColor; bar.GripStyle=ToolStripGripStyle.Hidden; bar.Padding=new Padding(3,2,3,2); bar.AutoSize=false; bar.Height=30;
        menu.Items.Clear();
        var appMenu=new ToolStripDropDownButton("☰"); appMenu.DropDownItems.AddRange(new ToolStripItem[]{file,view,help}); bar.Items.Add(appMenu);

        bar.Items.Add(new ToolStripSeparator());
        AddButton("◀","Previous page (Page Up)",delegate { Go(-1); },true);
        pageBox.AutoSize=false; pageBox.Width=30;pageBox.TextBox.TextAlign=HorizontalAlignment.Center;pageBox.TextBox.Font=new Font("Segoe UI",8);pageBox.Margin=new Padding(1,2,1,2); pageBox.BackColor=Color.FromArgb(27,30,35); pageBox.ForeColor=ForeColor; pageBox.BorderStyle=BorderStyle.FixedSingle;
        pageBox.KeyDown+=delegate(object s,KeyEventArgs e) { if(e.KeyCode==Keys.Enter) { JumpToLabel(); e.SuppressKeyPress=true; canvas.Focus(); } };
        bar.Items.Add(pageBox); documentTools.Add(pageBox); bar.Items.Add(pageTotal);
        AddButton("▶","Next page (Page Down)",delegate { Go(1); },true);
        bar.Items.Add(new ToolStripSeparator());
        AddButton("−","Zoom out",delegate { canvas.Zoom(1/1.2,new Point(canvas.Width/2,canvas.Height/2)); },true);
        zoomLabel.AutoSize=false; zoomLabel.Width=43; bar.Items.Add(zoomLabel);
        AddButton("+","Zoom in",delegate { canvas.Zoom(1.2,new Point(canvas.Width/2,canvas.Height/2)); },true);
        AddButton("Fit","Fit the whole page (Ctrl+0)",delegate { canvas.Fit("page"); },true);
        AddButton("Width","Fit page width (Ctrl+2)",delegate { canvas.Fit("width"); },true);
        bar.Items.Add(new ToolStripSeparator());
        AddRotationButton(false,"Rotate anticlockwise 90°",delegate { Rotate(-1); },true);
        AddRotationButton(true,"Rotate clockwise 90°",delegate { Rotate(1); },true);
        scope.DropDownItems.Add(Item("Current page",delegate { rotateAll=false; scope.Text="Current page"; },Keys.None));
        scope.DropDownItems.Add(Item("Whole document",delegate { rotateAll=true; scope.Text="Whole document"; },Keys.None)); scope.AutoSize=false; scope.Width=110;
        scope.BackColor=Color.FromArgb(12,12,12); scope.ForeColor=ForeColor; bar.Items.Add(scope); documentTools.Add(scope);
        InstallEditorToolbar();InstallSearchToolbar();
        var strip=new StatusStrip { Renderer=renderer,BackColor=BackColor,ForeColor=Color.FromArgb(176,185,197),SizingGrip=true };
        status.Spring=true; status.TextAlign=ContentAlignment.MiddleLeft; strip.Items.Add(status); strip.Items.Add(dimensions);
        documentLabel.Dock=DockStyle.Top; documentLabel.Height=35; documentLabel.Padding=new Padding(14,8,12,0); documentLabel.BackColor=Color.Black; documentLabel.ForeColor=ForeColor; documentLabel.Text="No document open"; documentLabel.AutoEllipsis=true;
        sidebar.Dock=DockStyle.Left; sidebar.Width=116; sidebar.Visible=true; thumbnailsEnabled=true; sidebar.Padding=new Padding(2); sidebar.BackColor=Color.Black;
        var heading=new Label { Text="PAGES",Dock=DockStyle.Top,Height=30,ForeColor=Color.FromArgb(146,156,172),Padding=new Padding(7,7,0,0) };
        pages.Dock=DockStyle.Fill; pages.BackColor=sidebar.BackColor; pages.ForeColor=ForeColor; pages.BorderStyle=BorderStyle.None; pages.DrawMode=DrawMode.OwnerDrawFixed; pages.ItemHeight=112; pages.IntegralHeight=false;
        pages.DrawItem+=DrawPage;pages.ViewportChanged+=delegate{worker.ClearIdle();if(sizes!=null)QueueThumbnails(generation);};
        pages.SelectedIndexChanged+=delegate { if(!selecting && pages.SelectedIndex>=0) Navigate(pages.SelectedIndex); };
        var headingBar=new Panel{Dock=DockStyle.Top,Height=30};heading.Dock=DockStyle.Fill;expandPages=new Button{Text="»",Dock=DockStyle.Right,Width=30,FlatStyle=FlatStyle.Flat,ForeColor=Color.Silver,BackColor=Color.Black};expandPages.FlatAppearance.BorderSize=0;expandPages.Click+=delegate{ShowOverview();};headingBar.Controls.Add(heading);headingBar.Controls.Add(expandPages);sidebar.Controls.Add(pages);sidebar.Controls.Add(headingBar);
        var workspace=new Panel { Dock=DockStyle.Fill }; documentArea.Controls.Add(canvas);workspace.Controls.Add(documentArea); workspace.Controls.Add(sidebar);workspace.Controls.Add(editorDock);navigator.Jump+=delegate(int index){if(index!=pageIndex)Navigate(index);};navigator.Wheel+=canvas.ScrollWheel;
        Controls.Add(workspace); Controls.Add(searchBar); Controls.Add(bar); Controls.Add(strip); InstallTitle();
        canvas.ViewChanged+=delegate { LayoutInline(); if(sizes==null)return; viewVersion++; zoomLabel.Text=canvas.ZoomPercent+"%"; debounce.Stop(); debounce.Start(); };
        canvas.PageStep+=ScrollPage; canvas.OpenRequested+=ChooseFile;
        debounce.Interval=130; debounce.Tick+=delegate { debounce.Stop(); RequestRender(); };
        DragEnter+=delegate(object s,DragEventArgs e) { if(e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect=DragDropEffects.Copy; };
        DragDrop+=delegate(object s,DragEventArgs e) { var paths=e.Data.GetData(DataFormats.FileDrop) as string[]; if(paths!=null)foreach(string path in paths){if(OpenInTab!=null)OpenInTab(path);else OpenFile(path);} };
        Shown+=delegate { TryDarkTitle(); Trace("window_shown_ms="+Startup.ElapsedMilliseconds); if(initialPath!=null) OpenFile(initialPath); else if(SnapshotPath!=null) BeginInvoke((Action)SaveSnapshot); };
        FormClosed+=delegate {closing=true;generation++;DisposePalette();ClearUndoViews();worker.Stop();debounce.Dispose();ClearImages();};
        Disposed+=delegate{closing=true;DisposePalette();};
        FormClosing+=delegate(object sender,FormClosingEventArgs e) { if(!approvedClose&&!ConfirmDiscard())e.Cancel=true; };
        VisibleChanged+=delegate{if(closing||IsDisposed||Disposing)return;documentVisible=Visible;worker.ClearIdle();if(Visible&&sizes!=null){RequestRender();RenderSecond();QueueThumbnails(generation);}SyncPaletteVisibility();};InstallLayout();InstallWheelRouter();EnableDocument(false);
    }
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attr,ref int value,int size);
    void TryDarkTitle() { try { int one=1; DwmSetWindowAttribute(Handle,20,ref one,4); } catch {} }
    static void Trace(string line) { if(TracePath!=null) try { File.AppendAllText(TracePath,line+Environment.NewLine); } catch {} }
    void SaveSnapshot() {
        CaptureWorkspace(SnapshotPath);
        if(TestRun)Close();
    }
    void StartSelfTest() {
        int stage=0,waitTicks=0; long last=Startup.ElapsedMilliseconds,maxGap=0; int beats=0;
        var timer=new Timer { Interval=40 };
        timer.Tick+=delegate {
            long now=Startup.ElapsedMilliseconds; maxGap=Math.Max(maxGap,now-last); last=now; beats++;
            if(lastRenderedVersion!=viewVersion) { if(++waitTicks>1000) { Trace("FAIL render timeout"); timer.Stop(); Close(); } return; }
            waitTicks=0;
            try {
                switch(stage++) {
                    case 0: if(sizes.Length<2)throw new Exception("Expected multipage PDF"); Navigate(1); break;
                    case 1: if(pageIndex!=1)throw new Exception("Navigation failed"); Rotate(1); break;
                    case 2: if(rotations[1]!=1 || rotations[0]!=0 || canvas.PageSize.Height<canvas.PageSize.Width)throw new Exception("Current-page rotation failed"); SnapshotTo("rotation.png"); rotateAll=true; Rotate(-1); break;
                    case 3: if(rotations[1]!=0 || rotations[0]!=3)throw new Exception("Document rotation failed"); rotateAll=false; Navigate(0); break;
                    case 4: Rotate(1); break;
                    case 5: canvas.Zoom(3,new Point(canvas.Width/2,canvas.Height/2)); break;
                    case 6: SnapshotTo("zoom.png"); canvas.PanBy(130,-100); break;
                    case 7: canvas.Fit("width"); break;
                    case 8: Size=new Size(1100,720); break;
                    case 9: canvas.Fit("page"); Navigate(1); Navigate(2); Navigate(sizes.Length-1); break;
                    case 10: if(pageIndex!=sizes.Length-1)throw new Exception("Latest navigation lost"); SnapshotTo("last-page.png"); Trace("PASS navigation, current/all rotation, zoom, pan, fit, resize, rapid navigation"); Trace("heartbeat_count="+beats); Trace("maximum_ui_heartbeat_gap_ms="+maxGap); CloseDocument(); if(canvas.HasPage || sizes!=null)throw new Exception("Close failed"); Trace("PASS close document"); timer.Stop(); timer.Dispose(); Close(); break;
                }
            } catch(Exception ex) { Trace("FAIL "+ex); timer.Stop(); Close(); }
        };
        timer.Start();
    }
    void SnapshotTo(string name) { using(var b=new Bitmap(Width,Height)) { DrawToBitmap(b,new Rectangle(0,0,Width,Height)); b.Save(Path.Combine(SelfTestDirectory,name)); } }
    ToolStripMenuItem Item(string text,Action action,Keys shortcut) { var item=new ToolStripMenuItem(text); item.ForeColor=ForeColor; item.Click+=delegate { action(); }; item.ShortcutKeys=shortcut; return item; }
    void AddButton(string text,string tip,Action action,bool needsDocument) { var b=new ToolStripButton(text) { ToolTipText=tip,ForeColor=ForeColor,Padding=new Padding(2,0,2,0) }; b.Click+=delegate { action(); }; bar.Items.Add(b); if(needsDocument)documentTools.Add(b); }
    void EnableDocument(bool enabled) { foreach(var t in documentTools)t.Enabled=enabled; pages.Enabled=enabled; UpdateEditorTools(); }
    void Post(Action action,Bitmap disposable) {
        if(closing || IsDisposed || Disposing) { if(disposable!=null)disposable.Dispose(); return; }
        try { BeginInvoke((Action)delegate { if(closing||IsDisposed||Disposing) { if(disposable!=null)disposable.Dispose(); } else action(); }); }
        catch(InvalidOperationException) { if(disposable!=null)disposable.Dispose(); }
    }
    public Action<string> OpenInTab;public Action CloseTab;
    void ChooseFile() { using(var d=new OpenFileDialog { Filter="PDF documents (*.pdf)|*.pdf",Title="Open PDF",CheckFileExists=true }) if(d.ShowDialog(this)==DialogResult.OK){if(OpenInTab!=null)OpenInTab(d.FileName);else OpenFile(d.FileName);} }
    void ClearImages() { imageEpoch++; foreach(var b in thumbs.Values)b.Dispose(); thumbs.Clear(); foreach(var b in previews.Values)b.Dispose(); previews.Clear(); previewOrder.Clear(); }
    void CloseDocument() {
        if(overview!=null&&!overview.IsDisposed)overview.Close();uiHiddenLayers.Clear();uiLockedLayers.Clear();dirty=false;structurePending=false;ClearUndoViews(); savedOnce=false; editBusy=false; editorState=new EditState(); canvas.Editing=false; canvas.ClearObjects(); if(editToggle!=null)editToggle.Checked=false;
        generation++; viewVersion++; debounce.Stop(); worker.ClearPending(); worker.Command(delegate(PdfEngine e) { e.Close(); });
        navigator.SetPage(0,0);searchEpoch++;searchHits.Clear();activeHit=-1;canvas.SearchBoxes=canvas.ActiveSearchBoxes=new RectangleF[0];canvas.MarkGlyphs.Clear();canvas.MarkMode=false;if(markButton!=null)markButton.Checked=false;pageLabels=null;sizes=null; rotations=null; currentPath=null; ClearImages(); pages.Items.Clear(); canvas.ClearPage(); canvas.EmptyTitle="A clear view of your PDFs.";
        documentLabel.Text="No document open"; Text="VinsPDF"; pageBox.Text=""; pageTotal.Text="/ —"; zoomLabel.Text="—"; dimensions.Text=""; status.Text="Ready"; EnableDocument(false);
    }
    void OpenFile(string path) {
        if(!ConfirmDiscard())return;
        if(!File.Exists(path)) { DarkMessage.Show(this,"That file is no longer available.","Open PDF"); return; }
        CloseDocument(); int token=generation; currentPath=path; openClock.Restart(); firstPaint=true;
        canvas.EmptyTitle="Opening your PDF…"; canvas.Invalidate(); documentLabel.Text=Path.GetFileName(path); status.Text="Opening document…";
        worker.Command(delegate(PdfEngine e) {
            try {
                e.Open(path); SizeF[] result=(SizeF[])e.Sizes.Clone();string[] labels=e.PageLabels(); long ms=openClock.ElapsedMilliseconds;
                Post(delegate {
                    if(token!=generation)return;
                    sizes=result;pageLabels=labels; rotations=new int[sizes.Length]; pageIndex=0;
                    pages.BeginUpdate(); for(int i=0;i<sizes.Length;i++)pages.Items.Add("Page "+PageLabel(i)); pages.EndUpdate();
                    pageTotal.Text="/ "+sizes.Length; Text=Path.GetFileName(path)+" — VinsPDF";
                    documentLabel.Text=Path.GetFileName(path)+"    /    "+sizes.Length+" pages    /    "+(new FileInfo(path).Length/1000000.0).ToString("0.0")+" MB";
                    EnableDocument(true); status.Text="Opened in "+ms+" ms · Drawing first page…"; Trace("open_ms="+ms);
                    canvas.FitMode="page"; Navigate(0);
                },null);
            } catch(Exception ex) { string message=ex.Message; Post(delegate { if(token!=generation)return; CloseDocument(); status.Text=message; DarkMessage.Show(this,message,"Cannot open PDF",MessageBoxButtons.OK,MessageBoxIcon.Warning); },null); }
        });
    }
    void Navigate(int index) {
        if(sizes==null || editBusy || !FinishInline())return; canvas.ClearObjects(); index=Math.Max(0,Math.Min(sizes.Length-1,index));
        pageIndex=index;navigator.SetPage(index,sizes.Length); pageBox.Text=PageLabel(index);pageBox.ToolTipText="Page "+PageLabel(index)+" ("+(index+1)+" of "+sizes.Length+") · #number jumps by position"; selecting=true; pages.SelectedIndex=index; selecting=false;
        if(canvas.Editing)rotations[index]=0;
        SizeF s=sizes[index]; int r=rotations[index]; if(r%2!=0)s=new SizeF(s.Height,s.Width);
        string key=index+":"+r; Bitmap image; Bitmap preview=null;
        if(previews.TryGetValue(key,out image))preview=(Bitmap)image.Clone();
        else if(thumbs.TryGetValue(index,out image)) { preview=(Bitmap)image.Clone(); if(r!=0)preview.RotateFlip((RotateFlipType)r); }
        canvas.SetPage(s,preview); dimensions.Text=(sizes[index].Width*25.4/72).ToString("0")+" × "+(sizes[index].Height*25.4/72).ToString("0")+" mm  |  View "+(r*90)+"°";
        if(canvas.Editing) { rotations[index]=0; LoadEditObjects(-1); }
        UpdateSearchOverlay();LoadMarkGlyphs();debounce.Stop(); RequestRender();RenderSecond();
    }
    void ScrollPage(int delta){if(sizes==null||editBusy||canvas.Editing)return;int next=pageIndex+delta;if(next<0||next>=sizes.Length)return;Navigate(next);canvas.AlignScrollEdge(delta>0);}
    void Go(int delta) { Navigate(pageIndex+delta); }
    void Rotate(int delta) {
        if(sizes==null || editBusy)return;
        if(canvas.Editing) { status.Text="Switch to View mode to rotate the view."; return; }
        if(rotateAll)for(int i=0;i<rotations.Length;i++)rotations[i]=(rotations[i]+delta+4)%4;
        else rotations[pageIndex]=(rotations[pageIndex]+delta+4)%4;
        canvas.FitMode="page"; Navigate(pageIndex); pages.Invalidate();
    }
    void RequestRender() {
        if(sizes==null || !canvas.HasPage || !documentVisible)return;
        int token=generation,version=viewVersion,index=pageIndex,rotation=rotations[index];
        Rectangle canvasViewport=canvas.ClientRectangle;Rectangle page=canvas.PageRectangle,viewport=canvasViewport; viewport.Inflate(128,128); Rectangle area=Rectangle.Intersect(page,viewport);
        if(area.Width<1 || area.Height<1)return;
        status.Text="Drawing page "+(index+1)+"…";
        worker.Render(delegate(PdfEngine e) {
            if(token!=generation || version!=viewVersion)return;
            var clock=Stopwatch.StartNew();
            try {
                Rectangle shown=Rectangle.Intersect(page,canvasViewport);Bitmap bitmap=RenderCache.Get(e,index,rotation,page.Width,page.Height,new Rectangle(shown.X-page.X,shown.Y-page.Y,shown.Width,shown.Height));Rectangle painted=area;if(bitmap!=null){painted=shown;}else bitmap=e.Render(index,rotation,area.Width,area.Height,page.X-area.X,page.Y-area.Y,page.Width,page.Height,delegate { return token!=generation || version!=viewVersion || !documentVisible; });
                long elapsed=clock.ElapsedMilliseconds;
                Post(delegate {
                    if(token!=generation || version!=viewVersion) { bitmap.Dispose(); return; }
                    canvas.SetDetail(bitmap,painted,page);
                    lastRenderedVersion=version;
                    if(painted==page) {
                        string key=index+":"+rotation;
                        if(!previews.ContainsKey(key)) { previews[key]=(Bitmap)bitmap.Clone(); previewOrder.Enqueue(key); while(previewOrder.Count>3) { string old=previewOrder.Dequeue(); previews[old].Dispose(); previews.Remove(old); } }
                        canvas.SetPreview((Bitmap)bitmap.Clone());
                        if(!thumbs.ContainsKey(index) && rotation==0) { thumbs[index]=new Bitmap(bitmap,new Size(150,Math.Max(1,(int)(150.0*bitmap.Height/bitmap.Width)))); pages.Invalidate(); }
                    }
                    if(documentVisible&&thumbnailsEnabled){worker.ClearIdle();QueueNextPage(token,index);QueueThumbnails(token);}
                    status.Text="Page "+(index+1)+" of "+sizes.Length+"  ·  "+elapsed+" ms  ·  "+(canvas.Editing?"Select / Pan · Double-click text to edit":"Ctrl+wheel to zoom / drag to pan");
                    if(firstPaint) { firstPaint=false; canvas.Update(); Trace("first_page_ms="+openClock.ElapsedMilliseconds); Trace("startup_to_page_ms="+Startup.ElapsedMilliseconds); QueueThumbnails(token); if(V029Directory!=null&&!V029Started){V029Started=true;Start029Test();}else if(V028Directory!=null&&!V028Started){V028Started=true;Start028Test();}else if(V027Directory!=null&&!V027Started){V027Started=true;Start027Test();}else if(V026Directory!=null&&!V026Started){V026Started=true;Start026Test();}else if(EditorTestDirectory!=null)StartEditorUITest(); else if(SelfTestDirectory!=null)StartSelfTest(); else if(SnapshotPath!=null) { testTimer=new Timer(); testTimer.Interval=3500; testTimer.Tick+=delegate { testTimer.Stop(); testTimer.Dispose(); SaveSnapshot(); }; testTimer.Start(); } }
                },bitmap);
            } catch(OperationCanceledException) {} catch(Exception ex) { string message=ex.Message; Post(delegate { if(token==generation)status.Text="Could not render page: "+message; },null); }
        });
    }
    void ToggleThumbnails() { sidebar.Visible=!sidebar.Visible; thumbnailsEnabled=sidebar.Visible; worker.ClearIdle(); if(sizes!=null && thumbnailsEnabled)QueueThumbnails(generation); }
    void QueueThumbnails(int token) {
        int epoch=imageEpoch;
        if(!thumbnailsEnabled || !documentVisible)return;
        thumbnailStart=Math.Max(0,pages.TopIndex-1);thumbnailEnd=Math.Min(sizes.Length,pages.TopIndex+Math.Max(1,pages.Height/pages.ItemHeight)+2);
        for(int i=thumbnailStart;i<thumbnailEnd;i++) { int index=i;
            if(thumbs.ContainsKey(index))continue;
            Action<PdfEngine> job=null;
            job=delegate(PdfEngine e) {
                if(epoch!=imageEpoch || token!=generation || !thumbnailsEnabled || !documentVisible || index<thumbnailStart || index>=thumbnailEnd)return;
                try {
                    Bitmap b=e.Preview(index,0,150,delegate { return token!=generation || !thumbnailsEnabled || !documentVisible || index<thumbnailStart || index>=thumbnailEnd || worker.HasUrgent; });
                    Post(delegate { if(epoch!=imageEpoch || token!=generation || thumbs.ContainsKey(index)) { b.Dispose(); return; } thumbs[index]=b; pages.Invalidate(); },b);
                } catch(OperationCanceledException) { if(epoch==imageEpoch && token==generation && thumbnailsEnabled && documentVisible && index>=thumbnailStart && index<thumbnailEnd)worker.Idle(job); } catch { /* An unreadable thumbnail must not prevent opening other pages. */ }
            };
            worker.Idle(job);
        }
    }
    void DrawPage(object sender,DrawItemEventArgs e) {
        if(e.Index<0)return; bool selected=(e.State&DrawItemState.Selected)!=0;
        using(var brush=new SolidBrush(selected?Color.FromArgb(49,66,84):sidebar.BackColor))e.Graphics.FillRectangle(brush,e.Bounds);
        var bounds=new Rectangle(e.Bounds.X+8,e.Bounds.Y+6,e.Bounds.Width-16,78);
        Bitmap b;
        if(thumbs.TryGetValue(e.Index,out b)) {
            Bitmap rotated=null;
            if(rotations!=null && rotations[e.Index]!=0) { rotated=(Bitmap)b.Clone(); rotated.RotateFlip((RotateFlipType)rotations[e.Index]); b=rotated; }
            float scale=Math.Min((float)bounds.Width/b.Width,(float)bounds.Height/b.Height);
            var target=new Rectangle(bounds.X+(bounds.Width-(int)(b.Width*scale))/2,bounds.Y+(bounds.Height-(int)(b.Height*scale))/2,(int)(b.Width*scale),(int)(b.Height*scale));
            e.Graphics.DrawImage(b,target);
            if(rotated!=null)rotated.Dispose();
        } else { using(var brush=new SolidBrush(Color.FromArgb(58,61,67)))e.Graphics.FillRectangle(brush,bounds); TextRenderer.DrawText(e.Graphics,(e.Index+1).ToString(),Font,bounds,Color.FromArgb(149,158,172),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter); }
        TextRenderer.DrawText(e.Graphics,"Page "+PageLabel(e.Index)+(rotations!=null && rotations[e.Index]!=0?"  ·  "+rotations[e.Index]*90+"°":""),Font,new Rectangle(e.Bounds.X,e.Bounds.Y+86,e.Bounds.Width,21),selected?Color.FromArgb(164,208,243):ForeColor,TextFormatFlags.HorizontalCenter);
        if(selected)using(var pen=new Pen(Color.FromArgb(107,167,217),2))e.Graphics.DrawRectangle(pen,e.Bounds.X+2,e.Bounds.Y+2,e.Bounds.Width-5,e.Bounds.Height-5);
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys key) {
        if(key==Keys.F11){var shell=TopLevelControl as TabWindow;if(shell!=null)shell.ToggleFullScreen();return true;}if(key==(Keys.Control|Keys.F)){ShowSearch();return true;}if(key==Keys.F3||key==(Keys.Shift|Keys.F3)){NextSearch(key==Keys.F3?1:-1);return true;}if(InlineKey(key))return true; if(inlineText!=null&&inlineText.Focused)return base.ProcessCmdKey(ref msg,key);
        if(EditorShortcut(key))return true;
        if(!pageBox.Focused) {
            if(key==Keys.PageDown) { Go(1); return true; }
            if(key==Keys.PageUp) { Go(-1); return true; }
            if(key==(Keys.Control|Keys.Home)) { Navigate(0); return true; }
            if(key==(Keys.Control|Keys.End) && sizes!=null) { Navigate(sizes.Length-1); return true; }
        }
        return base.ProcessCmdKey(ref msg,key);
    }
}
}
