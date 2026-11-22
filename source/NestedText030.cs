using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace VinsPDF {
// Expand ordinary Form XObjects into equivalent vector/text content, on demand
// when editing begins. q/Q, matrices, bounding-box clips and optional content are
// retained. Isolated/transparency groups and soft-mask content stay untouched.
// The original file is never modified. Resource names are unique per invocation,
// so editing one use of a shared form cannot change its other uses.
internal sealed class NestedText030 {
    readonly string source;
    readonly Dictionary<string,object> objects;
    readonly Dictionary<string,string> streams=new Dictionary<string,string>();
    readonly JavaScriptSerializer json=new JavaScriptSerializer { MaxJsonLength=int.MaxValue,RecursionLimit=256 };
    int serial,nextId; public int Expanded;
    static readonly Encoding Bytes=Encoding.GetEncoding(28591);
    static readonly string[] resourceTypes={"/Font","/XObject","/ExtGState","/ColorSpace","/Pattern","/Shading","/Properties"};
    NestedText030(string path,Dictionary<string,object> all,int max){source=path;objects=all;nextId=max+1;}
    static object[] Array(object o){return o==null?null:((IEnumerable)o).Cast<object>().ToArray();}
    static Dictionary<string,object> Dict(object o){return o as Dictionary<string,object>;}
    object Resolve(object o){string s=o as string;object entry;if(s!=null&&objects.TryGetValue("obj:"+s,out entry)){var d=Dict(entry);object v;if(d.TryGetValue("value",out v))return v;return Dict(d["stream"])["dict"];}return o;}
    Dictionary<string,object> Res(object o){return Dict(Resolve(o))??new Dictionary<string,object>();}
    object Get(Dictionary<string,object> d,string k){object v;return d.TryGetValue(k,out v)?v:null;}
    object Inherit(Dictionary<string,object> d,string key){for(int i=0;i<64;i++){object value=Get(d,key);if(value!=null)return value;var p=Get(d,"/Parent");if(p==null)break;d=Res(p);}return null;}
    string Stream(string reference){string text;if(!streams.TryGetValue(reference,out text)){text=Bytes.GetString(Capture(source,"--show-object="+reference.Replace(" ","," ).Replace(",R",""),"--filtered-stream-data"));streams[reference]=text;}return text;}
    static string Number(object value){return Convert.ToDouble(value,CultureInfo.InvariantCulture).ToString("0.################",CultureInfo.InvariantCulture);}
    // PDFium's content writer keys simple fonts by their name after removing the
    // subset prefix. Distinct embedded subsets named Arial must remain distinct.
    int DistinguishFontSubsets(Dictionary<string,object> updates){int changed=0;
        var fonts=objects.Where(pair=>{var d=Dict(Get(Dict(pair.Value),"value"));return d!=null&&(Get(d,"/Subtype") as string=="/TrueType"||Get(d,"/Subtype") as string=="/Type1")&&Get(d,"/BaseFont") is string;}).ToList();
        foreach(var group in fonts.GroupBy(pair=>Regex.Replace((string)Get(Dict(Get(Dict(pair.Value),"value")),"/BaseFont"),@"^/[A-Z]{6}\+","/"))){
            if(group.Count()<2)continue;
            foreach(var pair in group){var font=new Dictionary<string,object>(Dict(Get(Dict(pair.Value),"value")));var descriptor=Res(Get(font,"/FontDescriptor"));if(Get(descriptor,"/FontFile")==null&&Get(descriptor,"/FontFile2")==null&&Get(descriptor,"/FontFile3")==null)continue;
                string name=(string)font["/BaseFont"]+"_VinsPDF"+pair.Key.Split(':')[1].Split(' ')[0];font["/BaseFont"]=name;
                var fd=new Dictionary<string,object>(descriptor);fd["/FontName"]=name;string id=nextId+++" 0 R";font["/FontDescriptor"]=id;
                changed++;updates[pair.Key]=new Dictionary<string,object>{{"value",font}};updates["obj:"+id]=new Dictionary<string,object>{{"value",fd}};
            }
        }return changed;
    }
    Dictionary<string,object> Merge(Dictionary<string,object> local,Dictionary<string,object> merged,string prefix){var maps=new Dictionary<string,object>();foreach(string type in resourceTypes){var names=new Dictionary<string,object>();foreach(var pair in Res(Get(local,type))){string name=prefix.Length==0?pair.Key:"/VP"+prefix+"_"+names.Count;names[pair.Key]=name;((Dictionary<string,object>)merged[type])[name]=pair.Value;}maps[type]=names;}return maps;}
    static string EncodeName(string value){var s=new StringBuilder();foreach(char c in value){if(char.IsLetterOrDigit(c)&&c<128)s.Append(c);else s.Append('#').Append(((int)c).ToString("X2"));}return s.ToString();}
    static string DecodeName(string value){return Regex.Replace(value,@"#([0-9a-fA-F]{2})",m=>((char)Convert.ToInt32(m.Groups[1].Value,16)).ToString());}
    static bool White(char c){return c==0||c==' '||c=='\t'||c=='\r'||c=='\n'||c=='\f';}
    static bool Delimiter(char c){return White(c)||"()<>[]{}/%".IndexOf(c)>=0;}
    // Operands remain byte-for-byte strings, including escaped literal strings.
    static List<string> Tokens(string s){var result=new List<string>();for(int i=0;i<s.Length;){if(White(s[i])){i++;continue;}if(s[i]=='%'){while(i<s.Length&&s[i]!='\r'&&s[i]!='\n')i++;continue;}int start=i;char c=s[i++];if(c=='('){int depth=1;while(i<s.Length&&depth>0){char v=s[i++];if(v=='\\'){if(i<s.Length)i++;}else if(v=='(')depth++;else if(v==')')depth--;}if(depth!=0)throw new IOException("Unbalanced PDF text string.");}else if(c=='<'&&i<s.Length&&s[i]!='<'){while(i<s.Length&&s[i]!='>')i++;if(i<s.Length)i++;}else if((c=='<'||c=='>')&&i<s.Length&&s[i]==c)i++;else if(c=='/'){while(i<s.Length&&!Delimiter(s[i]))i++;}else if(!Delimiter(c)){while(i<s.Length&&!Delimiter(s[i]))i++;}result.Add(s.Substring(start,i-start));}return result;}
    static bool Operator(string s){return s.Length>0&&s[0]!='/'&&s[0]!='('&&s[0]!='<'&&s[0]!='>'&&s!="["&&s!="]"&&s!="true"&&s!="false"&&s!="null"&&!char.IsDigit(s[0])&&s[0]!='.'&&s[0]!='-'&&s[0]!='+';}
    bool CanExpand(Dictionary<string,object> form,string content){return Get(form,"/Subtype") as string=="/Form"&&Get(form,"/Group")==null&&Get(form,"/Ref")==null&&!Tokens(content).Contains("BI");}
    string Expand(string content,Dictionary<string,object> resources,Dictionary<string,object> merged,Dictionary<string,object> maps,HashSet<string> ancestors,int depth){if(depth>48)throw new IOException("PDF groups are nested too deeply to edit safely.");var output=new StringBuilder();var operands=new List<string>();int array=0,dict=0;
        foreach(string token in Tokens(content)){if(token=="BI")throw new IOException("This page contains inline image data that cannot yet be ungrouped safely.");if(token=="[")array++;if(token=="]")array--;if(token=="<<")dict++;if(token==">>")dict--;if(array>0||dict>0||!Operator(token)){operands.Add(token);continue;}
            if(token=="Do"&&operands.Count==1){string key=DecodeName(operands[0]);object reference=Get(Res(Get(resources,"/XObject")),key);var form=Res(reference);if(Get(form,"/Subtype") as string=="/Form"&&reference is string){string name=(string)reference,body=Stream(name);if(CanExpand(form,body)){if(!ancestors.Add(name))throw new IOException("PDF contains a circular group reference.");var local=Get(form,"/Resources")==null?resources:Res(form["/Resources"]);var childMaps=Merge(local,merged,(++serial).ToString());var bbox=Array(Resolve(Get(form,"/BBox")));var matrix=Get(form,"/Matrix")==null?new object[]{1,0,0,1,0,0}:Array(Resolve(form["/Matrix"]));if(bbox==null||bbox.Length!=4||matrix.Length!=6)throw new IOException("Invalid PDF group geometry.");output.Append("q\n").Append(string.Join(" ",matrix.Select(Number))).Append(" cm\n");output.Append(Number(bbox[0])).Append(' ').Append(Number(bbox[1])).Append(' ').Append(Number(Convert.ToDouble(bbox[2])-Convert.ToDouble(bbox[0]))).Append(' ').Append(Number(Convert.ToDouble(bbox[3])-Convert.ToDouble(bbox[1]))).Append(" re W n\n");object oc=Get(form,"/OC");if(oc!=null){string ocname="/VPoc"+(++serial);((Dictionary<string,object>)merged["/Properties"])[ocname]=oc;output.Append("/OC ").Append(ocname).Append(" BDC\n");}output.Append(Expand(body,local,merged,childMaps,ancestors,depth+1));if(oc!=null)output.Append("EMC\n");output.Append("Q\n");ancestors.Remove(name);Expanded++;operands.Clear();continue;}}}
            string category=token=="Tf"?"/Font":token=="Do"?"/XObject":token=="gs"?"/ExtGState":token=="cs"||token=="CS"?"/ColorSpace":token=="scn"||token=="SCN"?"/Pattern":token=="sh"?"/Shading":token=="BDC"||token=="DP"?"/Properties":null;
            if(category!=null){var names=(Dictionary<string,object>)maps[category];int which=category=="/Font"?0:operands.Count-1;if(which>=0&&which<operands.Count&&operands[which].StartsWith("/")){object replacement;if(names.TryGetValue(DecodeName(operands[which]),out replacement))operands[which]=(string)replacement;}}
            if(operands.Count>0)output.Append(string.Join(" ",operands)).Append(' ');output.Append(token).Append('\n');operands.Clear();
        }if(operands.Count>0)output.Append(string.Join(" ",operands)).Append('\n');return output.ToString();}
    public static int Prepare(string source,string target){var serializer=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=serializer.Deserialize<Dictionary<string,object>>(Encoding.UTF8.GetString(Capture(source,"--json=2","--json-key=pages","--json-key=qpdf","--json-stream-data=none")));var q=Array(root["qpdf"]);var n=new NestedText030(source,Dict(q[1]),Convert.ToInt32(Dict(q[0])["maxobjectid"]));var updates=new Dictionary<string,object>();
        foreach(var pageInfo in Array(root["pages"])){var info=Dict(pageInfo);var page=new Dictionary<string,object>(n.Res(info["object"]));var resources=n.Res(n.Inherit(page,"/Resources"));bool hasForms=n.Res(n.Get(resources,"/XObject")).Values.Any(v=>n.Get(n.Res(v),"/Subtype") as string=="/Form");if(!hasForms)continue;string body=string.Join("\n",Array(info["contents"]).Select(v=>n.Stream((string)v)));if(Tokens(body).Contains("BI"))continue;int before=n.Expanded;var merged=new Dictionary<string,object>();foreach(string type in resourceTypes)merged[type]=new Dictionary<string,object>();var maps=n.Merge(resources,merged,"");string expanded=n.Expand(body,resources,merged,maps,new HashSet<string>(),0);if(n.Expanded==before)continue;string streamId=n.nextId+++" 0 R";page["/Resources"]=merged;page["/Contents"]=streamId;updates["obj:"+(string)info["object"]]=new Dictionary<string,object>{{"value",page}};updates["obj:"+streamId]=new Dictionary<string,object>{{"stream",new Dictionary<string,object>{{"dict",new Dictionary<string,object>()},{"data",Convert.ToBase64String(Bytes.GetBytes(expanded))}}}};
        }int changed=n.Expanded+n.DistinguishFontSubsets(updates);if(changed==0)return 0;string update=target+".json";try{File.WriteAllText(update,serializer.Serialize(new Dictionary<string,object>{{"qpdf",new object[]{new Dictionary<string,object>{{"jsonversion",2}},updates}}}),new UTF8Encoding(false));PdfTools.Run(source,"--update-from-json="+update,"--object-streams=generate",target);}finally{if(File.Exists(update))File.Delete(update);}return changed;}
    internal static byte[] Capture(params string[] args){string exe=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"qpdf","qpdf.exe");var info=new ProcessStartInfo(exe,string.Join(" ",args.Select(PdfTools.Quote))){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};bool held=Monitor.IsEntered(PdfEngine.NativeGate);if(held)Monitor.Exit(PdfEngine.NativeGate);try{using(var p=Process.Start(info))using(var buffer=new MemoryStream()){var error=p.StandardError.ReadToEndAsync();var read=p.StandardOutput.BaseStream.CopyToAsync(buffer);if(!p.WaitForExit(300000)){p.Kill();throw new IOException("Preparing PDF groups took too long.");}read.Wait();if(p.ExitCode!=0&&p.ExitCode!=3)throw new IOException(error.Result);return buffer.ToArray();}}finally{if(held)Monitor.Enter(PdfEngine.NativeGate);}}
    static string Quote(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"")+"\"";}
}
internal sealed partial class PdfEngine {
    string openedFile;bool groupsPrepared;
    public void EnsureEditableGroups(){if(groupsPrepared)return;if(string.IsNullOrEmpty(openedFile)){groupsPrepared=true;return;}if(undo.Count!=0)throw new IOException("Save this document before preparing nested text.");string prepared=Scratch();try{int count=NestedText030.Prepare(openedFile,prepared);if(count>0){string original=openedFile;try{Open(prepared);}catch{Open(original);throw;}}else ReleaseScratch(prepared);groupsPrepared=true;}catch{ReleaseScratch(prepared);throw;}}
}
}

