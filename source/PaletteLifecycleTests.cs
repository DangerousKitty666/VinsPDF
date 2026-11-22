using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace VinsPDF {
internal static class PaletteLifecycleTests {
 public static bool Active;
 [System.Runtime.InteropServices.DllImport("kernel32.dll")]static extern uint SetErrorMode(uint mode);
 public static int Run(string[] args){string directory=args[1],scenario=args[3];Directory.CreateDirectory(directory);string result=Path.Combine(directory,"palette-"+scenario+".txt");int code=1,stage=0,ticks=0;bool running=false;Active=true;SetErrorMode(2);Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=delegate(object sender,System.Threading.ThreadExceptionEventArgs e){File.WriteAllText(result,"FAIL "+scenario+" UI exception\n"+e.Exception);Environment.Exit(1);};AppDomain.CurrentDomain.UnhandledException+=delegate(object sender,UnhandledExceptionEventArgs e){File.WriteAllText(result,"FAIL "+scenario+" unhandled exception\n"+e.ExceptionObject);Environment.Exit(1);};var session=new WindowSession(args[2]);var shell=Application.OpenForms.OfType<TabWindow>().First();var document=shell.Documents.First();using(var timer=new Timer{Interval=75}){timer.Tick+=delegate{if(running)return;running=true;try{if(++ticks>600)throw new Exception("Lifecycle timeout at "+stage);if(!document.LifecycleReady)return;bool done=document.LifecycleStep(scenario,ref stage,shell,directory);if(done){timer.Stop();File.WriteAllText(result,"PASS "+scenario+": editor lifetime and unsaved text preserved as expected.");code=0;document.LifecycleDiscard();session.CloseAll();Application.ExitThread();}}catch(Exception ex){timer.Stop();File.WriteAllText(result,"FAIL "+scenario+" stage "+stage+Environment.NewLine+ex);Application.ExitThread();}finally{running=false;}};timer.Start();try{Application.Run(session);}catch(Exception ex){File.WriteAllText(result,"FAIL "+scenario+" outside tick"+Environment.NewLine+ex);code=1;}}return code;}
 internal static Timer Answer(DialogResult answer){var timer=new Timer{Interval=30};timer.Tick+=delegate{foreach(Form form in Application.OpenForms)if(form is SaveChangesDialog){timer.Stop();form.DialogResult=answer;break;}};timer.Start();return timer;}
}
internal sealed partial class MainForm {
 internal bool LifecycleReady{get{return !editBusy&&sizes!=null&&lastRenderedVersion==viewVersion;}}
 internal void LifecycleDiscard(){if(IsDisposed)return;CancelInline();dirty=false;}
 internal bool LifecycleStep(string scenario,ref int stage,TabWindow original,string directory){
  if(stage==0){ToggleEditing();stage++;return false;}
  if(stage==1){if(canvas.Objects.Count==0)return false;BeginInline(null);inlineText.Text="Palette lifetime test text";if(scenario=="inline-close"){palette.Close();Version029Tests.Assert(inlineText!=null&&inlineText.Text=="Palette lifetime test text"&&!palette.IsDisposed,"Closing tools discarded pending text");}CommitInline();stage++;return false;}
  if(stage==2){Version029Tests.Assert(dirty&&canvas.Editing&&palette.Visible,"Editor not ready");
   if(scenario=="close"||scenario=="save-close"||scenario=="cancel-close"){
    DialogResult answer=scenario=="cancel-close"?DialogResult.Cancel:scenario=="save-close"?DialogResult.Yes:DialogResult.No;
    if(answer==DialogResult.Yes){currentPath=Path.Combine(directory,"close-saved.pdf");savedOnce=true;}
    using(var prompt=PaletteLifecycleTests.Answer(answer))original.Close();
    if(answer!=DialogResult.Cancel){Version029Tests.Assert(original.IsDisposed,"Window remained open");if(answer==DialogResult.Yes)VerifyLifecycleCopy(Path.Combine(directory,"close-saved.pdf"));return true;}
    Version029Tests.Assert(!original.IsDisposed&&dirty&&palette!=null&&!palette.IsDisposed&&palette.Visible,"Cancel close lost editor or edits");stage++;return false;
   }
   if(scenario=="tab-close"){using(var prompt=PaletteLifecycleTests.Answer(DialogResult.No))CloseTab();Version029Tests.Assert(IsDisposed&&original.DocumentCount==1,"Tab did not close to Home");return true;}
   if(scenario=="detach"){original.Detach();Version029Tests.Assert(TopLevelControl!=original,"Tab did not detach");original.Close();}
   if(scenario=="recreate"){RefreshLayers();palette.Dispose();}
   var host=(TabWindow)TopLevelControl;host.Open(null);host.Activate(this);stage++;return false;
  }
  if(stage==3){Version029Tests.Assert(palette!=null&&!palette.IsDisposed&&palette.Visible&&palette.Owner==TopLevelControl,"Palette belongs to closed window or was not restored");
   for(int i=0;i<12;i++){ToggleEditing();ToggleEditing();}
   stage++;return false;
  }
  if(stage==4){string path=Path.Combine(directory,scenario+"-saved.pdf");Version029Tests.Assert(SaveBeforeClose(path),"Could not save after palette lifecycle");VerifyLifecycleCopy(path);return true;}
  return false;
 }
 static void VerifyLifecycleCopy(string file){lock(PdfEngine.NativeGate)using(var engine=new PdfEngine()){engine.Open(file);Version029Tests.Assert(engine.SearchPage(0,"Palette lifetime test text",false).Count>0,"Edited text did not survive saving");}}
}
}
