using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed class DocumentTab : Button {
 public event Action CloseRequested;bool closingPress;
 Rectangle CloseBox{get{return new Rectangle(Width-23,0,23,Height);}}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(7,0,Width-34,Height),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);using(var p=new Pen(Color.Silver,1.2f)){int x=Width-12,y=Height/2;e.Graphics.DrawLine(p,x-3,y-3,x+3,y+3);e.Graphics.DrawLine(p,x+3,y-3,x-3,y+3);}}
 protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left&&CloseBox.Contains(e.Location)){closingPress=true;Capture=true;return;}base.OnMouseDown(e);}
 protected override void OnMouseUp(MouseEventArgs e){if(closingPress){closingPress=false;Capture=false;if(CloseBox.Contains(e.Location)&&CloseRequested!=null)CloseRequested();return;}base.OnMouseUp(e);}
}
internal sealed partial class TabWindow {
 bool fullScreen;Rectangle restoredBounds;FormWindowState restoredState;
 internal void ToggleFullScreen(){if(!fullScreen){restoredBounds=ActualWindowBounds;restoredState=WindowState;WindowState=FormWindowState.Normal;SetTitleVisible(false);Resizable=false;RefreshWindowStyle();Padding=Padding.Empty;Bounds=Screen.FromControl(this).Bounds;fullScreen=true;}else{fullScreen=false;SetTitleVisible(true);Resizable=true;RefreshWindowStyle();Padding=new Padding(1);WindowState=restoredState;if(restoredState==FormWindowState.Normal)Bounds=restoredBounds;}}
}
internal sealed partial class PageCanvas {
 static readonly Image homeLogo=LoadHomeLogo();
 static Image LoadHomeLogo(){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("VinsPDF.HomeLogo"))using(var i=Image.FromStream(s))return new Bitmap(i);}
 internal Rectangle HomeLogoRectangle{get{int size=Math.Max(100,Math.Min(360,Math.Min(Width,Height)*2/3));return new Rectangle((Width-size)/2,(Height-size)/2,size,size);}}
 void PaintHome(Graphics g){Rectangle r=HomeLogoRectangle;using(var a=new ImageAttributes()){var m=new ColorMatrix();m.Matrix33=.18f;a.SetColorMatrix(m);g.DrawImage(homeLogo,r,0,0,homeLogo.Width,homeLogo.Height,GraphicsUnit.Pixel,a);}if(EmptyTitle.StartsWith("Opening"))TextRenderer.DrawText(g,"Opening PDF…",Font,new Rectangle(0,r.Bottom+8,Width,24),Color.Silver,TextFormatFlags.HorizontalCenter);}
}
internal sealed partial class EditorPalette {
 Button ColourButton(Control parent,int x,int y,int w){var b=Mini(parent,"",x,y,w,delegate{Send("colour");});tips.SetToolTip(b,"Text colour");b.Paint+=delegate(object sender,PaintEventArgs e){using(var brush=new SolidBrush(textColor??Color.Black))e.Graphics.FillRectangle(brush,9,7,17,14);e.Graphics.DrawRectangle(Pens.Silver,9,7,17,14);TextRenderer.DrawText(e.Graphics,"▾",Font,new Rectangle(28,3,14,22),Color.Silver,TextFormatFlags.VerticalCenter);};return b;}
}
internal sealed partial class MainForm {
 Button expandPages;
 void PositionOverview(PageOverview popup){Rectangle screen=Screen.FromControl(this).WorkingArea;Point start=expandPages.PointToScreen(new Point(expandPages.Width+8,0));popup.Size=new Size(Math.Min(456,screen.Width-20),Math.Min(popup.Height,screen.Height-40));popup.Location=new Point(Math.Max(screen.Left,Math.Min(start.X,screen.Right-popup.Width)),Math.Max(screen.Top,Math.Min(start.Y,screen.Bottom-popup.Height)));}
}
internal sealed class AboutVinsPDF : StyledDialog {
 readonly Stream animationStream;readonly Image animation;
 public AboutVinsPDF():base("About VinsPDF",620,620){var panel=new Panel{Dock=DockStyle.Fill,Padding=new Padding(16),BackColor=Color.Black};var words=new Panel{Dock=DockStyle.Top,Height=272};var red=Color.FromArgb(160,0,0);var font=new Font("Segoe UI",24,FontStyle.Regular,GraphicsUnit.Pixel);var digits=new Font("Segoe UI",52,FontStyle.Regular,GraphicsUnit.Pixel);words.Paint+=delegate(object s,PaintEventArgs e){string[] lines={"This app has been made","with help of chat gpt while","listening to maid core."};for(int i=0;i<3;i++)TextRenderer.DrawText(e.Graphics,lines[i],font,new Rectangle(0,i*31,words.Width,31),red,TextFormatFlags.HorizontalCenter);TextRenderer.DrawText(e.Graphics,"Fuck consumerism",font,new Rectangle(0,111,words.Width,31),red,TextFormatFlags.HorizontalCenter);TextRenderer.DrawText(e.Graphics,"by Dangerous Kitty",font,new Rectangle(0,160,words.Width,31),red,TextFormatFlags.HorizontalCenter);for(int n=0;n<3;n++)TextRenderer.DrawText(e.Graphics,"6",digits,new Rectangle(words.Width/2-185+n*159,194,52,68),red,TextFormatFlags.HorizontalCenter);};animationStream=Assembly.GetExecutingAssembly().GetManifestResourceStream("VinsPDF.AboutGif");animation=Image.FromStream(animationStream);var picture=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,Image=animation,BackColor=Color.Black};panel.Controls.Add(picture);panel.Controls.Add(words);Controls.Add(panel);Ready();Disposed+=delegate{picture.Image=null;animation.Dispose();animationStream.Dispose();font.Dispose();digits.Dispose();};}
}
internal sealed class ExtractDialog : StyledDialog {
 internal readonly TextBox Range=new TextBox{BackColor=Color.FromArgb(29,34,39),ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};
 public ExtractDialog(int current,int count):base("Extract pages",450,285){var content=new Panel{Dock=DockStyle.Fill};content.Controls.Add(new Label{Text="Enter page numbers/ranges   e.g. 1,3,5-8.\nA new PDF is created; open document unchanged.",Left=18,Top=18,Width=410,Height=50,ForeColor=Color.Silver});Range.SetBounds(36,91,378,27);Range.Text=(current+1).ToString();content.Controls.Add(Range);var cancel=ActionButton("Cancel",delegate{DialogResult=DialogResult.Cancel;});cancel.SetBounds(24,196,100,31);var extract=ActionButton("Extract…",delegate{try{PdfEngine.PageRange(Range.Text,count);DialogResult=DialogResult.OK;}catch(Exception ex){DarkMessage.Show(this,ex.Message,"Page range");}});extract.SetBounds(132,196,100,31);content.Controls.AddRange(new Control[]{cancel,extract});Controls.Add(content);AcceptButton=extract;CancelButton=cancel;Ready();Shown+=delegate{Range.Focus();Range.SelectAll();};}
}
}
