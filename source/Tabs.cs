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
 public TabWindow(){Text="VinsPDF";Width=1320;Height=900;MinimumSize=new Size(860,500);StartPosition=FormStartPosition.CenterScreen;Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);Controls.Add(body);Controls.Add(tabs);InstallTitle();var plus=Button("+",35);plus.Click+=delegate{Choose();};tabs.Controls.Add(plus);var detach=Button("Detach tab",90);detach.Click+=delegate{Detach();};tabs.Controls.Add(detach);var update=Button("Check for updates",130);update.Click+=delegate{Updater.Check(this);};tabs.Controls.Add(update);FormClosing+=delegate(object s,FormClosingEventArgs e){if(shutting)return;foreach(var d in buttons.Keys){if(!d.CanCloseDocument()){Activate(d);e.Cancel=true;return;}}shutting=true;foreach(var d in buttons.Keys.ToArray())d.CloseApproved();};}
 Button Button(string text,int width){var b=new Button{Text=text,Width=width,Height=27,FlatStyle=FlatStyle.Flat,ForeColor=Color.Gainsboro,BackColor=Color.FromArgb(18,18,18),Margin=new Padding(1),TabStop=false};b.FlatAppearance.BorderSize=0;return b;}
 void Choose(){using(var d=new OpenFileDialog{Filter="PDF files|*.pdf",Multiselect=true})if(d.ShowDialog(this)==DialogResult.OK)foreach(string path in d.FileNames)Open(path);}
 public void Open(string path){if(path!=null){var existing=Documents.FirstOrDefault(d=>string.Equals(d.DocumentPath,path,StringComparison.OrdinalIgnoreCase));if(existing!=null){Activate(existing);return;}}Attach(new MainForm(path));}
 public void Attach(MainForm document){document.TopLevel=false;document.EmbeddedTitle(false);document.Dock=DockStyle.Fill;document.OpenInTab=Open;document.CloseTab=delegate{CloseDocument(document);};body.Controls.Add(document);var b=Button("",190);buttons[document]=b;tabs.Controls.Add(b);b.Click+=delegate{Activate(document);};b.MouseUp+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Middle)CloseDocument(document);};var menu=new ContextMenuStrip{BackColor=Color.Black,ForeColor=Color.Gainsboro};menu.Items.Add("Move to new window",null,delegate{Activate(document);Detach();});menu.Items.Add("Close tab",null,delegate{CloseDocument(document);});b.ContextMenuStrip=menu;document.TextChanged+=delegate{if(!b.IsDisposed){b.Text=(document.DocumentPath==null?"New document":Path.GetFileName(document.DocumentPath))+(document.Unsaved?" *":"");if(active==document)Text=document.Text;}};b.Text=document.DocumentPath==null?"New document":Path.GetFileName(document.DocumentPath);Activate(document);}
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
