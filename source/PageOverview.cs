using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace VinsPDF {
internal sealed class PageOverview : DarkWindow {
 internal readonly PreviewGrid Grid=new PreviewGrid();public PageOverview(int count){Text="PAGES · click a preview to jump";Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);Size=new Size(800,580);StartPosition=FormStartPosition.CenterParent;Opacity=.96;ShowInTaskbar=false;Grid.Count=count;Controls.Add(Grid);InstallTitle();HideMinMax();Deactivate+=delegate{Close();};}
}
internal sealed class PreviewGrid : ScrollableControl {
 public int Count;internal int ReadyCount{get{return images.Count;}}public event Action<int> SelectPage;public event Action<int,int> RequestPages;readonly Dictionary<int,Bitmap> images=new Dictionary<int,Bitmap>();
 public PreviewGrid(){Dock=DockStyle.Fill;AutoScroll=true;BackColor=Color.FromArgb(12,12,12);DoubleBuffered=true;Resize+=delegate{RefreshViewport();};Scroll+=delegate{RefreshViewport();};}
 int Columns{get{return Math.Max(1,ClientSize.Width/140);}}
 public void RefreshViewport(){AutoScrollMinSize=new Size(0,((Count+Columns-1)/Columns)*174);int first=Math.Max(0,-AutoScrollPosition.Y/174*Columns),last=Math.Min(Count,first+(ClientSize.Height/174+2)*Columns);if(RequestPages!=null)RequestPages(first,last);Invalidate();}
 public void SetImage(int index,Bitmap image){if(IsDisposed){image.Dispose();return;}Bitmap old;if(images.TryGetValue(index,out old))old.Dispose();images[index]=image;while(images.Count>96){int victim=-1;foreach(int k in images.Keys){if(Math.Abs(k-index)>20){victim=k;break;}}if(victim<0)break;images[victim].Dispose();images.Remove(victim);}Invalidate();}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);for(int i=Math.Max(0,-AutoScrollPosition.Y/174*Columns);i<Count;i++){int x=(i%Columns)*140+10,y=(i/Columns)*174+AutoScrollPosition.Y+8;if(y>Height)break;var rect=new Rectangle(x,y,120,138);Bitmap b;if(images.TryGetValue(i,out b)){float scale=Math.Min(120f/b.Width,138f/b.Height);e.Graphics.DrawImage(b,new Rectangle(x+(120-(int)(b.Width*scale))/2,y+(138-(int)(b.Height*scale))/2,(int)(b.Width*scale),(int)(b.Height*scale)));}else e.Graphics.FillRectangle(Brushes.DimGray,rect);TextRenderer.DrawText(e.Graphics,"Page "+(i+1),Font,new Rectangle(x,y+140,120,25),Color.Gainsboro,TextFormatFlags.HorizontalCenter);}}
 protected override void OnMouseClick(MouseEventArgs e){base.OnMouseClick(e);int i=(e.Y-AutoScrollPosition.Y)/174*Columns+e.X/140;if(i>=0&&i<Count&&SelectPage!=null)SelectPage(i);}
 protected override void Dispose(bool disposing){if(disposing)foreach(var b in images.Values)b.Dispose();base.Dispose(disposing);}
}
internal sealed partial class MainForm {
 PageOverview overview;
 void ShowOverview(){if(sizes==null||!FinishInline())return;if(overview!=null&&!overview.IsDisposed){overview.Close();return;}var popup=new PageOverview(sizes.Length);overview=popup;var overviewReady=new HashSet<int>();int token=generation;popup.Grid.SelectPage+=delegate(int index){popup.Close();Navigate(index);};popup.Grid.RequestPages+=delegate(int first,int last){for(int i=first;i<last;i++){int index=i;if(overviewReady.Contains(index))continue;Bitmap existing;if(thumbs.TryGetValue(index,out existing)){popup.Grid.SetImage(index,(Bitmap)existing.Clone());overviewReady.Add(index);continue;}Action<PdfEngine> job=null;job=delegate(PdfEngine engine){if(popup.IsDisposed||token!=generation)return;try{Bitmap b=engine.Preview(index,0,150,delegate{return popup.IsDisposed||token!=generation||worker.HasUrgent;});Post(delegate{if(popup.IsDisposed){b.Dispose();return;}popup.Grid.SetImage(index,b);overviewReady.Add(index);},b);}catch(OperationCanceledException){if(!popup.IsDisposed&&token==generation)worker.Idle(job);}catch{}};worker.Idle(job);}};var refresh=new Timer{Interval=500};refresh.Tick+=delegate{if(popup.IsDisposed)return;worker.ClearIdle();popup.Grid.RefreshViewport();};popup.Shown+=delegate{worker.ClearIdle();popup.Grid.RefreshViewport();refresh.Start();};popup.FormClosed+=delegate{refresh.Stop();refresh.Dispose();if(sizes!=null&&!closing)QueueThumbnails(generation);};popup.Show(TopLevelControl as Form??this);}
}
}
