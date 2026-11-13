using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace VinsPDF {
internal static class TextLines {
    // Conservative visual-line clustering, in physical page coordinates. No OCR.
    public static List<EditObject> Group(List<EditObject> raw,SizeF page,bool enabled) {
        if(!enabled)return raw;
        var marked=raw.Where(o=>!string.IsNullOrEmpty(o.GroupKey)).GroupBy(o=>o.GroupKey);
        var output=raw.Where(o=>string.IsNullOrEmpty(o.GroupKey)&&(o.Kind!=1 || Math.Abs(o.Angle)>4)).ToList();
        foreach(var group in marked){var items=group.OrderBy(o=>o.Id).ToList();var combined=Combine(items,page);combined.Text=string.Concat(items.Select(o=>o.LineText??o.Text));output.Add(combined);}
        var candidates=raw.Where(o=>string.IsNullOrEmpty(o.GroupKey)&&o.Kind==1 && Math.Abs(o.Angle)<=4).OrderBy(o=>o.Bounds.Top).ThenBy(o=>o.Bounds.Left).ToList();
        var rows=new List<List<EditObject>>();
        foreach(var item in candidates) {
            List<EditObject> best=null;
            foreach(var row in rows) {
                var reference=row[0]; float h=Math.Max(item.Bounds.Height,reference.Bounds.Height);
                if(reference.Color==item.Color && Math.Abs(item.BaselineY-reference.BaselineY)*page.Height<Math.Max(item.DisplayFontSize,reference.DisplayFontSize)*.18f && Math.Min(item.DisplayFontSize,reference.DisplayFontSize)>Math.Max(item.DisplayFontSize,reference.DisplayFontSize)*.75f) { best=row; break; }
            }
            if(best==null) { best=new List<EditObject>();rows.Add(best); } best.Add(item);
        }
        foreach(var row in rows) {
            var sorted=row.OrderBy(o=>o.Bounds.Left).ToList(); var run=new List<EditObject>();
            foreach(var item in sorted) {
                if(run.Count>0) { var last=run[run.Count-1]; float gap=(item.Bounds.Left-last.Bounds.Right)*page.Width,h=Math.Max(last.Bounds.Height,item.Bounds.Height)*page.Height;
                    if(gap>h*1.05f || gap< -h*.4f) { output.Add(Combine(run,page)); run.Clear(); }
                }
                run.Add(item);
            }
            if(run.Count>0)output.Add(Combine(run,page));
        }
        return output;
    }
    static EditObject Combine(List<EditObject> items,SizeF page) {
        if(items.Count==1)return items[0];
        RectangleF bounds=items[0].Bounds; var text=new StringBuilder(); EditObject last=null;
        foreach(var o in items) {
            if(last!=null) { float gap=(o.Bounds.Left-last.Bounds.Right)*page.Width,h=Math.Max(o.Bounds.Height,last.Bounds.Height)*page.Height;
                if(gap>Math.Max(o.DisplayFontSize,last.DisplayFontSize)*.35f && !text.ToString().EndsWith(" ") && !o.Text.StartsWith(" "))text.Append(' ');
            }
            text.Append(o.Text);bounds=RectangleF.Union(bounds,o.Bounds);last=o;
        }
        string joined=text.ToString();
        if(items.All(o=>o.TextStart>=0 && o.TextEnd>=0) && items[0].SourceText!=null){int start=items.Min(o=>o.TextStart),end=items.Max(o=>o.TextEnd);string span=items[0].SourceText.Substring(start,end-start);if(!span.Contains("\n")&&!span.Contains("\r")&&span.Count(c=>char.IsWhiteSpace(c))>=items.Sum(o=>o.Text.Count(c=>char.IsWhiteSpace(c)))&&span.Count(c=>!char.IsWhiteSpace(c))==items.Sum(o=>o.Text.Count(c=>!char.IsWhiteSpace(c))))joined=span.Trim();}
        return new EditObject { Id=items[0].Id,Kind=1,Bounds=bounds,Text=System.Text.RegularExpressions.Regex.Replace(joined,@"\s+"," ").Trim(),FontSize=items[0].FontSize,DisplayFontSize=items[0].DisplayFontSize,Color=items[0].Color,Angle=items[0].Angle,BaselineY=items[0].BaselineY,BaselineX=items[0].BaselineX,Members=items.SelectMany(o=>o.Ids).ToArray() };
    }
}
}
