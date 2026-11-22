using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace VinsPDF {
internal static class EditingTests {
    public static int Probe(string[] args) {
        string dir=args[1];
        try {
            for(int mode=0;mode<3;mode++)using(var e=new PdfEngine()) {
                e.Open(args[2]);
                if(mode==1)e.PutText(0,-1,"Save appearance probe",16,new PointF(.3f,.08f));
                if(mode==2) { var image=e.GetObjects(0).Objects.First(o=>o.Kind==3); var box=image.Bounds; box.Offset(.01f,.01f); e.TransformObject(0,image.Id,box); e.UndoEdit(false); }
                using(var b=e.Preview(0,0,1400))b.Save(Path.Combine(dir,"probe-"+mode+"-before.png"));
                e.SaveCopy(Path.Combine(dir,"probe-"+mode+".pdf"));
                using(var b=e.Preview(0,0,1400))b.Save(Path.Combine(dir,"probe-"+mode+"-after.png"));
            }
            File.WriteAllText(Path.Combine(dir,"probe.txt"),"completed"); return 0;
        } catch(Exception ex) { File.WriteAllText(Path.Combine(dir,"probe.txt"),ex.ToString()); return 1; }
    }
    static void Assert(bool value,string text) { if(!value)throw new Exception(text); }
    public static int Run(string[] args) {
        string dir=args[1],image=args[2]; var log=new List<string>();
        try {
            for(int file=3;file<args.Length;file++) {
                string path=args[file],name=Path.GetFileNameWithoutExtension(path),output=Path.Combine(dir,name+"-edited.pdf");
                using(var engine=new PdfEngine()) {
                    var sw=Stopwatch.StartNew(); engine.Open(path); log.Add(name+" open_ms="+sw.ElapsedMilliseconds); int count=engine.Count;
                    using(var b=engine.Preview(0,0,1400))b.Save(Path.Combine(dir,name+"-before.png"));
                    sw.Restart(); var state=engine.GetObjects(0); log.Add("edit_scan_ms="+sw.ElapsedMilliseconds+" objects="+state.Objects.Count);
                    EditObject existing=state.Objects.First(o=>o.Kind==1 && !string.IsNullOrWhiteSpace(o.Text)); string oldText=existing.Text;
                    int textId=engine.PutText(0,existing.Id,"VinsPDF EDIT Žąė 2026",12,new PointF(.1f,.1f));
                    Assert(engine.GetObjects(0).Objects.Any(o=>o.Id==textId && o.Text.Contains("VinsPDF EDIT")),"Text replacement failed");
                    engine.UndoEdit(false); Assert(engine.GetObjects(0).Objects.Any(o=>o.Id==existing.Id && o.Text==oldText),"Text undo failed");
                    engine.UndoEdit(true); Assert(!engine.GetObjects(0).Objects.Any(o=>o.Id==existing.Id),"Text redo failed");
                    EditObject text=engine.GetObjects(0).Objects.First(o=>o.Id==textId);
                    var newBounds=new RectangleF(.10f,.10f,Math.Max(.12f,text.Bounds.Width*1.3f),Math.Max(.02f,text.Bounds.Height*1.3f));
                    engine.TransformObject(0,textId,newBounds);
                    var moved=engine.GetObjects(0).Objects.First(o=>o.Id==textId); Assert(Math.Abs(moved.Bounds.Left-newBounds.Left)<.003,"Text move failed");
                    int added=engine.PutText(0,-1,"VinsPDF saved text",18,new PointF(.2f,.85f));
                    int imported=engine.PutImage(0,image,new RectangleF(.60f,.12f,.16f,.23f));
                    engine.TransformObject(0,imported,new RectangleF(.60f,.15f,.20f,.28f));
                    engine.DeleteObject(0,added); Assert(!engine.GetObjects(0).Objects.Any(o=>o.Id==added),"Delete failed"); engine.UndoEdit(false);
                    var originalImage=state.Objects.FirstOrDefault(o=>o.Kind==3 && o.Bounds.Width>.001 && o.Bounds.Height>.001);
                    if(originalImage!=null) { var rect=originalImage.Bounds; rect.Offset(.01f,.01f); engine.TransformObject(0,originalImage.Id,rect); engine.UndoEdit(false); }
                    // A second page edit checks that earlier edited native pages remain valid.
                    engine.PutText(1,-1,"Second page edit",14,new PointF(.15f,.15f));
                    using(var b=engine.Preview(0,0,1400))b.Save(Path.Combine(dir,name+"-edited.png"));
                    bool failed=false; try { engine.SaveCopy(Path.Combine(dir,"missing-directory","cannot-save.pdf")); } catch(IOException) { failed=true; }
                    Assert(failed && engine.GetObjects(0).Objects.Any(o=>o.Text.Contains("VinsPDF saved text")),"Failed save lost edits");
                    sw.Restart(); engine.SaveCopy(output); log.Add("save_ms="+sw.ElapsedMilliseconds);
                    Assert(engine.Count==count,"Page count changed");
                    var reopened=engine.GetObjects(0);
                    Assert(reopened.Objects.Any(o=>o.Text.Contains("VinsPDF EDIT Žąė 2026")),"Unicode replacement not preserved after save");
                    Assert(reopened.Objects.Any(o=>o.Text.Contains("VinsPDF saved text")),"Added text not preserved");
                    Assert(reopened.Objects.Count(o=>o.Kind==3)==state.Objects.Count(o=>o.Kind==3)+1,"Image not preserved");
                    Assert(engine.GetObjects(1).Objects.Any(o=>o.Text.Contains("Second page edit")),"Second-page edit not saved");
                    using(var b=engine.Preview(0,0,1400))b.Save(Path.Combine(dir,name+"-reopened.png"));
                    engine.PutText(0,-1,"Overwrite test",14,new PointF(.2f,.8f)); engine.SaveCopy(output);
                    Assert(engine.GetObjects(0).Objects.Any(o=>o.Text.Contains("Overwrite test")),"Overwrite-save failed");
                    log.Add("PASS replace/add Unicode text; move/resize text and images; import; delete; undo/redo; multi-page edit; save failure; save/reopen; overwrite");
                }
            }
            File.WriteAllLines(Path.Combine(dir,"editing-results.txt"),log); return 0;
        } catch(Exception ex) { log.Add("FAIL "+ex); File.WriteAllLines(Path.Combine(dir,"editing-results.txt"),log); return 1; }
    }
}
}
