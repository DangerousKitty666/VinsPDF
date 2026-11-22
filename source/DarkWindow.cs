using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace VinsPDF {
internal class DarkWindow : Form {
    readonly Panel title=new Panel();
    readonly Label caption=new Label();
    protected bool Resizable=true;
    // Retain the normal Windows resize/snap styles beneath the dark custom frame.
    protected override CreateParams CreateParams {get{var p=base.CreateParams;if(TopLevel&&Resizable)p.Style|=0x00040000|0x00080000|0x00020000|0x00010000|0x00C00000;return p;}}
    protected void RefreshWindowStyle(){UpdateStyles();}
    [StructLayout(LayoutKind.Sequential)]struct WindowRect{public int Left,Top,Right,Bottom;}
    [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr window,out WindowRect rect);
    protected Rectangle ActualWindowBounds{get{WindowRect r;return IsHandleCreated&&GetWindowRect(Handle,out r)?Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom):Bounds;}}

    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr w,IntPtr l);
    public DarkWindow() {
        FormBorderStyle=FormBorderStyle.None; BackColor=Color.Black; ForeColor=Color.Gainsboro; Padding=new Padding(1);
        title.Height=26; title.Dock=DockStyle.Top; title.BackColor=Color.Black;
        caption.Dock=DockStyle.Fill; caption.ForeColor=Color.Silver; caption.TextAlign=ContentAlignment.MiddleLeft; caption.Padding=new Padding(6,0,0,0);
        var icon=new PictureBox { Dock=DockStyle.Left,Width=25,SizeMode=PictureBoxSizeMode.Zoom,Padding=new Padding(3) };
        Load+=delegate { if(Icon!=null){if(icon.Image!=null)icon.Image.Dispose();icon.Image=Icon.ToBitmap();}MaximizedBounds=Screen.FromControl(this).WorkingArea; };Disposed+=delegate{if(icon.Image!=null)icon.Image.Dispose();};
        title.Controls.Add(caption); title.Controls.Add(icon);
        TitleButton("—",delegate { WindowState=FormWindowState.Minimized; }); TitleButton("□",delegate { if(Resizable)WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized; }); TitleButton("×",delegate { Close(); });
        caption.MouseDown+=delegate(object s,MouseEventArgs e) { if(e.Button==MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero); } };
        caption.DoubleClick+=delegate { if(Resizable)WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized; };
        TextChanged+=delegate { caption.Text=Text; };
    }
    void TitleButton(string text,Action action) {
        var b=new Button { Text=text,Dock=DockStyle.Right,Width=34,FlatStyle=FlatStyle.Flat,BackColor=Color.Black,ForeColor=Color.Silver,TabStop=false };
        b.FlatAppearance.BorderSize=0; b.FlatAppearance.MouseOverBackColor=Color.FromArgb(40,40,40); b.Click+=delegate { action(); }; title.Controls.Add(b);
    }
    public void EmbeddedTitle(bool show){title.Visible=show;Resizable=show;}
    protected void SetTitleVisible(bool show){title.Visible=show;}
    protected void InstallTitle() { Controls.Add(title); }
    protected void HideMinMax() {foreach(Control c in title.Controls)if(c is Button && c.Text!="×")c.Visible=false;}
    protected override void WndProc(ref Message m) {
        if(m.Msg==0x83&&TopLevel&&Resizable){m.Result=IntPtr.Zero;return;}
        base.WndProc(ref m);
        if(m.Msg==0x84 && Resizable && WindowState==FormWindowState.Normal) {
            Point pt=PointToClient(new Point((short)((long)m.LParam&65535),(short)(((long)m.LParam>>16)&65535)));
            bool l=pt.X<5,r=pt.X>=Width-5,t=pt.Y<5,b=pt.Y>=Height-5;
            if(l||r||t||b)m.Result=new IntPtr(t?(l?13:r?14:12):b?(l?16:r?17:15):l?10:11);
        }
    }
}
internal sealed class TextEditorDialog : DarkWindow {
    readonly TextBox input=new TextBox(); readonly NumericUpDown size=new NumericUpDown();
    public string Value { get { return input.Text; } }
    public float PointSize { get { return (float)size.Value; } }
    public TextEditorDialog(string text,float pointSize,bool replacing) {
        Text=replacing?"Edit text — VinsPDF":"Add text — VinsPDF"; Width=560; Height=210; Resizable=false; StartPosition=FormStartPosition.CenterParent; Font=new Font("Segoe UI",9);
        var content=new Panel { Dock=DockStyle.Fill,Padding=new Padding(14) };
        input.SetBounds(14,18,525,28); input.BackColor=Color.FromArgb(22,22,22); input.ForeColor=Color.White; input.BorderStyle=BorderStyle.FixedSingle; input.Text=text;
        var label=new Label { Text="Arial · Size (pt)",Left=14,Top=58,Width=100,ForeColor=ForeColor };
        size.SetBounds(116,55,75,25); size.Minimum=1; size.Maximum=500; size.DecimalPlaces=1; size.Value=(decimal)Math.Max(1,Math.Min(500,pointSize)); size.BackColor=input.BackColor; size.ForeColor=Color.White;
        var note=new Label { Text=replacing?"Replaces this text fragment using Arial; position and colour are preserved.":"Add one line, then drag it into place. Resize using the corner handle.",Left=14,Top=92,Width=525,Height=30,ForeColor=Color.Silver };
        var okay=new Button { Text="Apply",Left=365,Top=133,Width=80,DialogResult=DialogResult.OK,FlatStyle=FlatStyle.Flat,ForeColor=Color.White };
        var cancel=new Button { Text="Cancel",Left=455,Top=133,Width=80,DialogResult=DialogResult.Cancel,FlatStyle=FlatStyle.Flat,ForeColor=Color.White };
        okay.Click+=delegate { if(string.IsNullOrWhiteSpace(input.Text))DialogResult=DialogResult.None; };
        content.Controls.AddRange(new Control[]{input,label,size,note,okay,cancel}); Controls.Add(content); InstallTitle(); AcceptButton=okay; CancelButton=cancel;
        Shown+=delegate { input.Focus(); input.SelectAll(); };
    }
}
}
