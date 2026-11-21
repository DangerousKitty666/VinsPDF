using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VinsPDF {
internal static class Program {
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [STAThread] static int Main(string[] args) {
        if(args.Length>0 && args[0]=="--v027-engine-test")return Version027Tests.Run(args);
        if(args.Length>0 && args[0]=="--v026-engine-test")return Version026Tests.Run(args);
        if(args.Length>0 && args[0]=="--v025-test")return Version025Tests.Run(args);
        if(args.Length>0 && args[0]=="--benchmark") return Benchmark(args);
        if(args.Length>0 && args[0]=="--edit-test") return EditingTests.Run(args);
        if(args.Length>0 && args[0]=="--save-probe") return EditingTests.Probe(args);
        if(args.Length>0 && args[0]=="--register-pdf"){try{Associations.Register();return 0;}catch(Exception ex){System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"association-error.txt"),ex.ToString());return 1;}}
        Associations.SetCurrentProcessExplicitAppUserModelID("VinsPDF.Desktop");
        SetProcessDPIAware(); ToolStripManager.Renderer=new DarkRenderer(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        string path=null;
        for(int i=0;i<args.Length;i++) { if(args[i]=="--v027-ui-test" && i+1<args.Length)MainForm.V027Directory=args[++i];else if(args[i]=="--v026-ui-test" && i+1<args.Length)MainForm.V026Directory=args[++i];else if(args[i]=="--trace" && i+1<args.Length) MainForm.TracePath=args[++i]; else if(args[i]=="--snapshot" && i+1<args.Length) MainForm.SnapshotPath=args[++i]; else if(args[i]=="--self-test" && i+1<args.Length) MainForm.SelfTestDirectory=args[++i]; else if(args[i]=="--editor-ui-test" && i+1<args.Length) MainForm.EditorTestDirectory=args[++i]; else if(args[i]=="--test")MainForm.TestRun=true; else path=args[i]; }
        if(MainForm.TestRun||MainForm.SelfTestDirectory!=null||MainForm.EditorTestDirectory!=null||MainForm.SnapshotPath!=null)Application.Run(new MainForm(path));else{Application.Run(new WindowSession(path));} return 0;
    }
    static int Benchmark(string[] args) {
        if(args.Length<3)return 2;
        var lines=new List<string>(); lines.Add("file,operation,page,milliseconds,width,height,working_set_mb");
        try {
            using(var engine=new PdfEngine()) {
                for(int f=2;f<args.Length;f++) {
                    string path=args[f],name=Path.GetFileName(path);
                    var sw=Stopwatch.StartNew(); engine.Open(path); Add(lines,name,"open",0,sw.Elapsed.TotalMilliseconds,0,0);
                    for(int p=0;p<engine.Count;p++) {
                        sw.Restart(); using(Bitmap b=engine.Preview(p,0,1400)) {
                            Add(lines,name,"page_first_render",p+1,sw.Elapsed.TotalMilliseconds,b.Width,b.Height);
                            if(p==0)b.Save(Path.Combine(Path.GetDirectoryName(args[1]),Path.GetFileNameWithoutExtension(path)+"-preview.png"));
                        }
                    }
                    for(int j=0;j<5;j++) {
                        sw.Restart(); using(Bitmap b=engine.Preview(0,0,1400))Add(lines,name,"page_repeat_render",1,sw.Elapsed.TotalMilliseconds,b.Width,b.Height);
                    }
                    for(int r=1;r<=3;r++) { sw.Restart(); using(Bitmap b=engine.Preview(0,r,1400)) { Add(lines,name,"rotate_"+(r*90),1,sw.Elapsed.TotalMilliseconds,b.Width,b.Height); if(r==1)b.Save(Path.Combine(Path.GetDirectoryName(args[1]),Path.GetFileNameWithoutExtension(path)+"-rotated.png")); } }
                    sw.Restart(); using(Bitmap b=engine.Render(0,0,1400,900,-1800,-1200,6000,4242))Add(lines,name,"zoom_viewport",1,sw.Elapsed.TotalMilliseconds,b.Width,b.Height);
                    engine.Close();
                }
            }
            File.WriteAllLines(args[1],lines); return 0;
        } catch(Exception ex) { File.WriteAllText(args[1]+".error.txt",ex.ToString()); return 1; }
    }
    static void Add(List<string> lines,string file,string op,int page,double ms,int w,int h) {
        lines.Add(string.Join(",",new string[]{"\""+file.Replace("\"","\"\"")+"\"",op,page.ToString(),ms.ToString("0.00",CultureInfo.InvariantCulture),w.ToString(),h.ToString(),(Process.GetCurrentProcess().WorkingSet64/1048576.0).ToString("0.0",CultureInfo.InvariantCulture)}));
    }
}
}
