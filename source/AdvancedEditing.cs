using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace VinsPDF {
internal sealed class TextStyle {
    public string Family="Arial";
    public float Size=14,LineHeight=1.2f,ParagraphGap=6,CharacterSpacing=0;
    public bool Bold,Italic;
    public Color? Ink; public int Alignment; public float BoxWidth;
    public TextStyle Copy() { return (TextStyle)MemberwiseClone(); }
}
internal sealed partial class PdfEngine {
    readonly Dictionary<string,IntPtr> extraFonts=new Dictionary<string,IntPtr>();
    static readonly Dictionary<string,string[]> fontFiles=new Dictionary<string,string[]> {
        {"Arial",new[]{"arial.ttf","arialbd.ttf","ariali.ttf","arialbi.ttf"}},
        {"Calibri",new[]{"calibri.ttf","calibrib.ttf","calibrii.ttf","calibriz.ttf"}},
        {"Segoe UI",new[]{"segoeui.ttf","segoeuib.ttf","segoeuii.ttf","segoeuiz.ttf"}},
        {"Times New Roman",new[]{"times.ttf","timesbd.ttf","timesi.ttf","timesbi.ttf"}},
        {"Consolas",new[]{"consola.ttf","consolab.ttf","consolai.ttf","consolaz.ttf"}},
        {"Verdana",new[]{"verdana.ttf","verdanab.ttf","verdanai.ttf","verdanaz.ttf"}}
    };
    public static string[] AvailableFamilies { get { string root=Environment.GetFolderPath(Environment.SpecialFolder.Fonts); return fontFiles.Where(k=>File.Exists(Path.Combine(root,k.Value[0]))).Select(k=>k.Key).ToArray(); } }
    IntPtr FontForStyle(TextStyle style) {
        string[] files;if(!fontFiles.TryGetValue(style.Family,out files))throw new IOException("Choose an available font.");
        string file=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),files[(style.Bold?1:0)+(style.Italic?2:0)]);
        IntPtr font;if(!extraFonts.TryGetValue(file,out font)) { byte[] data=File.ReadAllBytes(file);font=Native.FPDFText_LoadFont(document,data,(uint)data.Length,2,1);Check(font!=IntPtr.Zero,"Could not load font.");extraFonts[file]=font; } return font;
    }
    void Batch(int page,Action action) {
        int count=undo.Count; Change[] oldRedo=redo.ToArray();
        try { action(); }
        catch { while(undo.Count>count)undo.Pop().Undo();redo.Clear();for(int i=oldRedo.Length-1;i>=0;i--)redo.Push(oldRedo[i]);throw; }
        var changes=new List<Change>();while(undo.Count>count)changes.Add(undo.Pop());
        if(changes.Count==0)return;
        undo.Push(new Change { Page=page,Undo=delegate { foreach(var c in changes)c.Undo(); },Redo=delegate { for(int i=changes.Count-1;i>=0;i--)changes[i].Redo(); } });redo.Clear();
    }
    public void TransformObjects(int index,int[] ids,RectangleF oldBounds,RectangleF newBounds) {
        if(oldBounds.Width<=0 || oldBounds.Height<=0)throw new IOException("Selection has no measurable size.");
        IntPtr page=Page(index);RectangleF from=PdfRect(page,oldBounds),to=PdfRect(page,newBounds);
        double sx=to.Width/from.Width,sy=to.Height/from.Height,tx=to.Left-from.Left*sx,ty=to.Top-from.Top*sy;
        Batch(index,delegate {foreach(int id in ids.Distinct()){
            IntPtr obj=ObjectAt(index,id);Native.Matrix old;Check(Native.FPDFPageObj_GetMatrix(obj,out old)!=0,"Could not read transform.");
            var changed=new Native.Matrix{A=(float)(sx*old.A),B=(float)(sy*old.B),C=(float)(sx*old.C),D=(float)(sy*old.D),E=(float)(sx*old.E+tx),F=(float)(sy*old.F+ty)};
            Commit(index,delegate{SetObjectMatrix(obj,old);},delegate{SetObjectMatrix(obj,changed);});
        }});
    }
    public void DeleteObjects(int index,int[] ids) { Batch(index,delegate {foreach(int id in ids.Distinct())DeleteObject(index,id);}); }
    public int PutTextBlock(int index,int[] oldIds,string text,TextStyle style,PointF position,float angle=0) {
        if(string.IsNullOrWhiteSpace(text))throw new IOException("Enter text.");
        if(style.Size<1 || style.Size>500 || style.LineHeight<.5f || style.LineHeight>5 || style.CharacterSpacing< -2 || style.CharacterSpacing>30)throw new IOException("Invalid text settings.");
        IntPtr page=Page(index),font=FontForStyle(style); var added=new List<IntPtr>();var previous=new List<IntPtr>(); int first=-1;
        foreach(int id in oldIds.Distinct())previous.Add(ObjectAt(index,id));
        uint r=0,g=0,b=0,a=255;if(previous.Count>0)Native.FPDFPageObj_GetFillColor(previous[0],out r,out g,out b,out a);if(style.Ink.HasValue){Color ink=style.Ink.Value;r=ink.R;g=ink.G;b=ink.B;a=ink.A;}
        try {
            using(var scratch=new Bitmap(1,1))using(var graphics=Graphics.FromImage(scratch))using(var measureFont=new Font(style.Family,style.Size,(style.Bold?FontStyle.Bold:FontStyle.Regular)|(style.Italic?FontStyle.Italic:FontStyle.Regular),GraphicsUnit.Pixel)) {
                var measureFormat=(StringFormat)StringFormat.GenericTypographic.Clone();measureFormat.FormatFlags|=StringFormatFlags.MeasureTrailingSpaces;
                float down=0;
                foreach(string line in text.Replace("\r\n","\n").Replace('\r','\n').Split('\n')) {
                    if(string.IsNullOrWhiteSpace(line)) { down+=style.Size*style.LineHeight+style.ParagraphGap;continue; }
                    var fragments=new List<string>();
                    string lineKey="VinsPDF.Line."+Guid.NewGuid().ToString("N");
                    if(Math.Abs(style.CharacterSpacing)<.001f)fragments.Add(line);
                    else {var en=StringInfo.GetTextElementEnumerator(line);while(en.MoveNext())fragments.Add(en.GetTextElement());}
                    float measured=graphics.MeasureString(line,measureFont,int.MaxValue,measureFormat).Width+Math.Max(0,fragments.Count-1)*style.CharacterSpacing;float advance=style.Alignment==0?0:(Math.Max(measured,style.BoxWidth)-measured)*(style.Alignment==1?.5f:1f);
                    foreach(string fragment in fragments) {
                        IntPtr obj=Native.FPDFPageObj_CreateTextObj(document,font,style.Size);Check(obj!=IntPtr.Zero,"Could not create text.");
                        bool inserted=false;
                        try {
                            Check(Native.FPDFText_SetText(obj,fragment)!=0,"Text could not be encoded.");
                            IntPtr mark=Native.FPDFPageObj_AddMark(obj,lineKey);
                            Check(mark!=IntPtr.Zero && Native.FPDFPageObjMark_SetStringParam(document,obj,mark,"FragmentText",Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fragment)))!=0,"Could not retain line information.");
                            double radians=angle*Math.PI/180,cos=Math.Cos(radians),sin=Math.Sin(radians);
                            PointF pos=new PointF(position.X+(float)(advance*cos-down*sin)/Sizes[index].Width,position.Y+(float)(advance*sin+down*cos)/Sizes[index].Height);
                            PointF origin=ToPdf(page,pos.X,pos.Y),px=ToPdf(page,pos.X+(float)cos/Sizes[index].Width,pos.Y+(float)sin/Sizes[index].Height),py=ToPdf(page,pos.X+(float)sin/Sizes[index].Width,pos.Y-(float)cos/Sizes[index].Height);
                            double lx=Math.Sqrt(Math.Pow(px.X-origin.X,2)+Math.Pow(px.Y-origin.Y,2)),ly=Math.Sqrt(Math.Pow(py.X-origin.X,2)+Math.Pow(py.Y-origin.Y,2));
                            Native.FPDFPageObj_Transform(obj,(px.X-origin.X)/lx,(px.Y-origin.Y)/lx,(py.X-origin.X)/ly,(py.Y-origin.Y)/ly,origin.X,origin.Y);Native.FPDFPageObj_SetFillColor(obj,r,g,b,a);
                            if(previous.Count>0)SetObjectLayer(obj,ObjectLayer(previous[0]));Native.FPDFPage_InsertObject(page,obj);inserted=true;Native.FPDFPageObj_SetIsActive(obj,0);added.Add(obj);if(first<0)first=Native.FPDFPage_CountObjects(page)-1;
                        } finally {if(!inserted)Native.FPDFPageObj_Destroy(obj);}
                        advance+=graphics.MeasureString(fragment,measureFont,int.MaxValue,measureFormat).Width+style.CharacterSpacing;
                    }
                    down+=style.Size*style.LineHeight;
                }
            }
            Commit(index,delegate {foreach(var obj in added)Native.FPDFPageObj_SetIsActive(obj,0);foreach(var obj in previous)Native.FPDFPageObj_SetIsActive(obj,1);},delegate {foreach(var obj in added)Native.FPDFPageObj_SetIsActive(obj,1);foreach(var obj in previous)Native.FPDFPageObj_SetIsActive(obj,0);});
            return first;
        } catch {foreach(var obj in added)Native.FPDFPageObj_SetIsActive(obj,0);throw;}
    }
    public void AddHeaderFooter(int current,bool all,string header,string footer,TextStyle style,float marginMm,int alignment) {
        int start=all?0:current,end=all?Count:current+1;
        Batch(current,delegate {
            for(int index=start;index<end;index++) {
                string h=header.Replace("{page}",(index+1).ToString()).Replace("{pages}",Count.ToString());string f=footer.Replace("{page}",(index+1).ToString()).Replace("{pages}",Count.ToString());
                float margin=marginMm*72/25.4f;
                using(var bitmap=new Bitmap(1,1))using(var graphics=Graphics.FromImage(bitmap))using(var font=new Font(style.Family,style.Size,(style.Bold?FontStyle.Bold:FontStyle.Regular)|(style.Italic?FontStyle.Italic:FontStyle.Regular),GraphicsUnit.Pixel)) {
                    for(int part=0;part<2;part++) { string text=part==0?h:f;
                        if(string.IsNullOrWhiteSpace(text))continue;
                        float width=graphics.MeasureString(text,font,int.MaxValue,StringFormat.GenericTypographic).Width;
                        float x=alignment==0?margin:alignment==1?(Sizes[index].Width-width)/2:Sizes[index].Width-margin-width;
                        PutTextBlock(index,new int[0],text,style,new PointF(x/Sizes[index].Width,(part==0?margin+style.Size:Sizes[index].Height-margin)/Sizes[index].Height));
                    }
                }
            }
        });
    }
}
}
