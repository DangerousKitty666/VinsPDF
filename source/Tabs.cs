using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed class WindowSession : ApplicationContext {
 public static WindowSession Current;readonly List<TabWindow> windows=new List<TabWindow>();
 public WindowSession(string path){Current=this;NewWindow(null,path);}
 public TabWindow NewWindow(MainForm document,string path=null){var window=new TabWindow();windows.Add(window);window.FormClosed+=delegate{windows.Remove(window);if(windows.Count==0)ExitThread();};if(document!=null)window.Attach(document);else window.Open(path);window.Show();return window;}
 public bool PrepareUpdate(){return windows.SelectMany(w=>w.Documents).All(d=>d.ReadyForUpdate());}
 public void CloseAll(){foreach(var window in windows.ToArray())window.Close();}
}
internal sealed class TabWindow : DarkWindow {
 readonly FlowLayoutPanel tabs=new FlowLayoutPanel{Dock=DockStyle.Top,Height=30,WrapContents=false,AutoScroll=true,BackColor=Color.FromArgb(12,12,12)};
 internal int DocumentCount{get{return buttons.Count;}}
 readonly Panel body=new Panel{Dock=DockStyle.Fill};readonly Dictionary<MainForm,Button> buttons=new Dictionary<MainForm,Button>();MainForm active;bool shutting;
 public IEnumerable<MainForm> Documents{get{return buttons.Keys;}}
 public TabWindow(){Text="VinsPDF";KeyPreview=true;Width=1320;Height=900;MinimumSize=new Size(860,500);StartPosition=FormStartPosition.CenterScreen;Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);Controls.Add(body);Controls.Add(tabs);InstallTitle();var home=Button("Home",70);home.Click+=delegate{var empty=Documents.FirstOrDefault(d=>d.DocumentPath==null);if(empty!=null)Activate(empty);else Open(null);};tabs.Controls.Add(home);var tools=Button("Tools",70);var toolsMenu=new ContextMenuStrip{Renderer=new DarkRenderer(),BackColor=Color.Black,ForeColor=Color.Gainsboro};foreach(var pair in new[]{new[]{"Combine files…","combine"},new[]{"Organise pages…","organise"},new[]{"Extract pages…","extract"},new[]{"Export PDF…","export"},new[]{"Print…","print"}}){string action=pair[1];toolsMenu.Items.Add(pair[0],null,delegate{if(active!=null)active.RunTool(action);});}tools.Click+=delegate{toolsMenu.Show(tools,new Point(0,tools.Height));};tabs.Controls.Add(tools);var about=Button("About",70);var aboutMenu=new ContextMenuStrip{Renderer=new DarkRenderer(),BackColor=Color.Black,ForeColor=Color.Gainsboro};aboutMenu.Items.Add("VinsPDF 0.28",null,delegate{DarkMessage.Show(this,"VinsPDF 0.28 · Lightweight PDF viewer and editor","About VinsPDF");});aboutMenu.Items.Add("Check for updates",null,delegate{Updater.Check(this);});aboutMenu.Items.Add("PDF association / default app…",null,delegate{Associations.Setup(this);});about.Click+=delegate{aboutMenu.Show(about,new Point(0,about.Height));};tabs.Controls.Add(about);var plus=Button("+",35);plus.Click+=delegate{Choose();};tabs.Controls.Add(plus);FormClosing+=delegate(object s,FormClosingEventArgs e){if(shutting)return;foreach(var d in buttons.Keys){if(!d.CanCloseDocument()){Activate(d);e.Cancel=true;return;}}shutting=true;foreach(var d in buttons.Keys.ToArray())d.CloseApproved();};}
 protected override bool ProcessCmdKey(ref Message msg,Keys key){if(key==(Keys.Control|Keys.P)&&active!=null){active.RunTool("print");return true;}if(key==(Keys.Control|Keys.O)){Choose();return true;}return base.ProcessCmdKey(ref msg,key);}
 Button Button(string text,int width){var b=new Button{Text=text,Width=width,Height=27,FlatStyle=FlatStyle.Flat,ForeColor=Color.Gainsboro,BackColor=Color.FromArgb(18,18,18),Margin=new Padding(1),TabStop=false};b.FlatAppearance.BorderSize=0;return b;}
 void Choose(){using(var d=new OpenFileDialog{Filter="PDF files|*.pdf",Multiselect=true})if(d.ShowDialog(this)==DialogResult.OK)foreach(string path in d.FileNames)Open(path);}
 public void Open(string path){if(path!=null){var existing=Documents.FirstOrDefault(d=>string.Equals(d.DocumentPath,path,StringComparison.OrdinalIgnoreCase));if(existing!=null){Activate(existing);return;}}Attach(new MainForm(path));}
 public void Attach(MainForm document){document.TopLevel=false;document.EmbeddedTitle(false);document.Dock=DockStyle.Fill;document.OpenInTab=Open;document.CloseTab=delegate{CloseDocument(document);};body.Controls.Add(document);var b=Button("",190);buttons[document]=b;tabs.Controls.Add(b);b.Click+=delegate{Activate(document);};b.MouseUp+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Middle)CloseDocument(document);};Point press=Point.Empty;bool dragging=false;b.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){press=b.PointToScreen(e.Location);dragging=false;}};b.MouseMove+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){Point now=b.PointToScreen(e.Location);if(Math.Abs(now.X-press.X)+Math.Abs(now.Y-press.Y)>12)dragging=true;}};b.MouseUp+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left&&dragging&&!tabs.RectangleToScreen(tabs.ClientRectangle).Contains(b.PointToScreen(e.Location))){Activate(document);Detach();}dragging=false;};var menu=new ContextMenuStrip{Renderer=new DarkRenderer(),BackColor=Color.Black,ForeColor=Color.Gainsboro};menu.Items.Add("Move to new window",null,delegate{Activate(document);Detach();});menu.Items.Add("Close tab",null,delegate{CloseDocument(document);});b.ContextMenuStrip=menu;document.TextChanged+=delegate{if(!b.IsDisposed){b.Visible=document.DocumentPath!=null;b.Text=(document.DocumentPath==null?"New document":Path.GetFileName(document.DocumentPath))+(document.Unsaved?" *":"");if(active==document)Text=document.Text;}};b.Visible=document.DocumentPath!=null;b.Text=document.DocumentPath==null?"Home":Path.GetFileName(document.DocumentPath);Activate(document);}
 internal void Activate(MainForm document){foreach(var d in buttons.Keys){d.Visible=d==document;buttons[d].BackColor=d==document?Color.FromArgb(40,65,85):Color.FromArgb(18,18,18);}active=document;document.Show();document.BringToFront();Text=document.Text;}
 void CloseDocument(MainForm document){if(!document.CanCloseDocument())return;Remove(document);document.CloseApproved();if(buttons.Count==0)Open(null);else Activate(buttons.Keys.Last());}
 void Remove(MainForm document){Button b=buttons[document];buttons.Remove(document);tabs.Controls.Remove(b);b.Dispose();body.Controls.Remove(document);}
 internal void Detach(){if(active==null)return;MainForm document=active;Remove(document);WindowSession.Current.NewWindow(document);if(buttons.Count==0)Open(null);else Activate(buttons.Keys.Last());}
}
internal sealed partial class MainForm {
 public string DocumentPath{get{return currentPath;}}public bool Unsaved{get{return dirty||inlineText!=null;}}
 bool approvedClose;
 public void CloseApproved(){approvedClose=true;CancelInline();dirty=false;Close();}
 public bool ReadyForUpdate(){if(editBusy||dirty||inlineText!=null){DarkMessage.Show(TopLevelControl as Form??this,"Save or cancel edits in every document before installing an update.","Update pending");return false;}return true;}
}
}
