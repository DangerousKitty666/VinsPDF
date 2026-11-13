using System;
using System.Collections.Generic;
using System.Threading;

namespace VinsPDF {
// A latest-request mailbox prevents fast wheel/resize events building a render backlog.
// PDFium is not thread-safe; this worker owns every native object and call.
internal sealed class RenderWorker {
    readonly object gate=new object();
    readonly Queue<Action<PdfEngine>> commands=new Queue<Action<PdfEngine>>();
    readonly Queue<Action<PdfEngine>> idle=new Queue<Action<PdfEngine>>();
    Action<PdfEngine> render,secondary;
    bool stopping;
    public bool HasUrgent { get { lock(gate)return stopping || render!=null || commands.Count>0; } }
    public RenderWorker() { var t=new Thread(Run); t.IsBackground=true; t.Name="PDF rendering"; t.Start(); }
    public void Command(Action<PdfEngine> action) { lock(gate) { commands.Enqueue(action); Monitor.Pulse(gate); } }
    public void Render(Action<PdfEngine> action) { lock(gate) { render=action; Monitor.Pulse(gate); } }
    public void Secondary(Action<PdfEngine> action){lock(gate){secondary=action;Monitor.Pulse(gate);}}
    public void Idle(Action<PdfEngine> action) { lock(gate) { idle.Enqueue(action); Monitor.Pulse(gate); } }
    public void ClearPending() { lock(gate) { render=null;secondary=null; idle.Clear(); commands.Clear(); } }
    public void ClearIdle() { lock(gate)idle.Clear(); }
    public void Stop() { lock(gate) { stopping=true; Monitor.Pulse(gate); } }
    void Run() {
        using(var engine=new PdfEngine()) {
            while(true) {
                Action<PdfEngine> action;
                lock(gate) {
                    while(!stopping && commands.Count==0 && render==null && secondary==null && idle.Count==0) Monitor.Wait(gate);
                    if(stopping) return;
                    if(commands.Count>0) action=commands.Dequeue();
                    else if(render!=null) { action=render; render=null; }
                    else if(secondary!=null){action=secondary;secondary=null;}else action=idle.Dequeue();
                }
                lock(PdfEngine.NativeGate)action(engine);
            }
        }
    }
}
}
